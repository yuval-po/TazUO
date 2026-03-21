using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Input;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Controls.ResizableComponents;

public class ResizableWindowProps : MyraCommonProps
{
    public ResizeBehavior Resize { get; set; } = new();
    public bool Minimizable { get; set; } = true;
}

public class ResizableWindow : Window, IDisposable
{
    public event EventHandler<ResizeEventArgs> Resized;

    public ResizableWindowProps Props { get; }

    public ResizableWindow(ResizableWindowProps props = null)
    {
        Props = props ?? new ResizableWindowProps();

        if (Props.Minimizable)
            ConfigureMinMaxButton();
    }


    public override Widget Content
    {
        get => base.Content;
        set
        {
            if (value == null)
            {
                base.Content = null;
                return;
            }

            Widget component = Props.Resize.ScrollerMode == ScrollViewerMode.None
                ? value
                : WrapWithScrollViewer(value);

            _content = component;
            base.Content = component;
            InvalidateMeasure();
        }
    }

    private ResizeEdges? _activeResizeEdge;

    private Point _resizeStartMouse;
    private int _resizeStartLeft;
    private int _resizeStartTop;
    private int _resizeStartWidth;
    private int _resizeStartHeight;

    private int _restoreWidth;
    private int _restoreHeight;

    private bool _isOverridingCursorStyle;

    private Widget _minMaxButton;
    private MyraLabel _minMaxButtonLabel;

    private Widget _content;

    private string MinMaxButtonText => IsMinimized ? "□" : "−";

    private SpriteFontBase MinMaxButtonFont => IsMinimized
        ? TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.NOTO_SANS_2_SYMBOLS, 24)
        : TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.NOTO_SANS_2_SYMBOLS, 32);

    public bool IsDisposed { get; private set; }

    public bool IsMinimized { get; private set; }

    public void Minimize()
    {
        _restoreWidth = Width ?? Bounds.Width;
        _restoreHeight = Height ?? Bounds.Height;
        Width = null;
        Height = null;

        IsMinimized = true;
        UpdateMinMaxButtonLabel();
        base.Content = null; // Using 'Visible' to hide causes some padding to remain. This is a bit of an ugly workaround.
        InvalidateMeasure();
    }

    protected override Point InternalMeasure(Point availableSize)
    {
        int a = 0;
        return base.InternalMeasure(availableSize);
    }

    public void Maximize()
    {
        IsMinimized = false;
        Width = _restoreWidth;
        Height = _restoreHeight;

        _restoreWidth = 0;
        _restoreHeight = 0;

        UpdateMinMaxButtonLabel();
        base.Content = _content;
        InvalidateMeasure();
    }

    private void UpdateMinMaxButtonLabel()
    {
        _minMaxButtonLabel.Text = MinMaxButtonText;
        _minMaxButtonLabel.Font = MinMaxButtonFont;
    }

    private void OnMinMaxButtonClick(object _, EventArgs _1)
    {
        if (IsMinimized)
            Maximize();
        else
            Minimize();
    }

    private void ConfigureMinMaxButton()
    {
        const int buttonSize = 28;

        _minMaxButtonLabel = new MyraLabel(MinMaxButtonText, 24)
        {
            Font = MinMaxButtonFont,
            Wrap = false,
            SingleLine = true,
            TextAlign = TextHorizontalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Width = buttonSize,
            Height = buttonSize
        };

        _minMaxButton = new Myra.Graphics2D.UI.Button
        {
            Width = buttonSize,
            Height = buttonSize,
            Tooltip = Language.Instance.UiCommons.MinMaxWindowButtonTooltip,
            Content = _minMaxButtonLabel,
            VerticalAlignment = VerticalAlignment.Center
        };

        _minMaxButton.TouchDown += OnMinMaxButtonClick;

        TitlePanel.Widgets.Insert(0, _minMaxButton);
        TitlePanel.TouchDoubleClick += OnMinMaxButtonClick;
    }

    public override void OnMouseEntered()
    {
        base.OnMouseEntered();
        Mouse.Moved += OnMouseMovedWhileInWindow;
    }

    private ScrollViewer WrapWithScrollViewer(Widget widget)
    {
        var scroller = new ScrollViewer
        {
            ShowHorizontalScrollBar = (Props.Resize.ScrollerMode & ScrollViewerMode.Horizontal) != 0,
            ShowVerticalScrollBar = (Props.Resize.ScrollerMode & ScrollViewerMode.Vertical) != 0
        };

        // We need a panel here to serve as a sort-of barrier between the scroller and the widget, otherwise we can get superposition.
        var panel = new Panel
        {
            // Note that if the scroll component's height/width changes post-construction, that won't be reflected. Might improve later.
            Padding = new Thickness(scroller.HorizontalScrollbarHeight(), scroller.VerticalScrollbarWidth())
        };
        panel.Widgets.Add(widget);

        scroller.Content = panel;

        return scroller;
    }

    public override void OnMouseLeft()
    {
        base.OnMouseLeft();
        Mouse.Moved -= OnMouseMovedWhileInWindow;

        // Stop overriding cursor style if the mouse has left the window, and we're not currently resizing
        if (_isOverridingCursorStyle && !_activeResizeEdge.HasValue)
            StopOverridingCursorStyle();
    }

    public override void OnTouchDown()
    {
        // To preserve normal Myra window behavior, we have to consider whether the window is in front.
        // If not, the click is 'directed' to bringing-to-front rather than dragging
        Widget[] widgets = (Parent != null ? Parent.GetChildren() : Desktop.Widgets).ToArray();
        if (widgets[^1] != this)
        {
            BringToFront();
            // If we ever want the visual cursor style changed right after focusing, we can add it here.
            // Just a nitpick.
            return;
        }

        // Don't allow resizing if minimized
        if (IsMinimized)
            return;

        _activeResizeEdge = GetResizerUnderCursor();
        if (!_activeResizeEdge.HasValue)
            return;

        _resizeStartMouse = Mouse.Position;
        _resizeStartLeft = Left;
        _resizeStartTop = Top;
        _resizeStartWidth = Width ?? Bounds.Width;
        _resizeStartHeight = Height ?? Bounds.Height;

        Mouse.LeftButtonClickStateChanged += LeftClickChangedHandler;
        Mouse.Moved += OnMouseMovedWhileResizing;
    }

    public override void OnTouchUp()
    {
        base.OnTouchUp();
        OnDragStop(null, EventArgs.Empty);
    }

    public override void Close()
    {
        base.Close();
        Dispose();
    }

    public void OnFocusLost()
    {
        if (_activeResizeEdge.HasValue)
            OnDragStop(null, EventArgs.Empty);

        StopOverridingCursorStyle();
    }

    private ResizeEdges? GetResizerUnderCursor()
    {
        if (!LocalMousePosition.HasValue)
            return null;

        Point mousePos = LocalMousePosition.Value;
        int radius = (int)Props.Resize.CornerTriggerRadiusPx;
        int radiusSq = radius * radius;

        foreach (ResizeEdges edges in GetEnabledResizeEdges())
            if (IsCursorOverResizer(mousePos, edges, radiusSq))
                return edges;

        return null;
    }

    private bool IsCursorOverResizer(Point mousePos, ResizeEdges edges, int cornerTriggerRadiusSquared)
    {
        int width = Width ?? Bounds.Width;
        int height = Height ?? Bounds.Height;

        int left = Bounds.X;
        int top = Bounds.Y;
        int right = left + width;
        int bottom = top + height;

        bool hasLeft = (edges & ResizeEdges.Left) != 0;
        bool hasRight = (edges & ResizeEdges.Right) != 0;
        bool hasTop = (edges & ResizeEdges.Top) != 0;
        bool hasBottom = (edges & ResizeEdges.Bottom) != 0;

        // Corner handles: circular hit areas around the corner point.
        if (hasLeft && hasTop)
            return IsWithinRadius(mousePos, left, top, cornerTriggerRadiusSquared);

        if (hasRight && hasTop)
            return IsWithinRadius(mousePos, right, top, cornerTriggerRadiusSquared);

        if (hasLeft && hasBottom)
            return IsWithinRadius(mousePos, left, bottom, cornerTriggerRadiusSquared);

        if (hasRight && hasBottom)
            return IsWithinRadius(mousePos, right, bottom, cornerTriggerRadiusSquared);

        // Edge handles: strip along the edge, with thickness based on radius.
        if (hasLeft)
            return IsWithinVerticalEdge(mousePos, left, top, bottom, Props.Resize.EdgeTriggerBandWidthPx);

        if (hasRight)
            return IsWithinVerticalEdge(mousePos, right, top, bottom, Props.Resize.EdgeTriggerBandWidthPx);

        if (hasTop)
            return IsWithinHorizontalEdge(mousePos, top, left, right, Props.Resize.EdgeTriggerBandWidthPx);

        if (hasBottom)
            return IsWithinHorizontalEdge(mousePos, bottom, left, right, Props.Resize.EdgeTriggerBandWidthPx);

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWithinRadius(Point mousePos, int x, int y, int radiusSq)
    {
        int dx = mousePos.X - x;
        int dy = mousePos.Y - y;
        return dx * dx + dy * dy <= radiusSq;
    }

    private static bool IsWithinVerticalEdge(Point mousePos, int edgeX, int top, int bottom, uint radius) =>
        mousePos.X >= edgeX - radius
        && mousePos.X <= edgeX + radius
        && mousePos.Y >= top - radius
        && mousePos.Y <= bottom + radius;

    private static bool IsWithinHorizontalEdge(Point mousePos, int edgeY, int left, int right, uint radius) =>
        mousePos.Y >= edgeY - radius
        && mousePos.Y <= edgeY + radius
        && mousePos.X >= left - radius
        && mousePos.X <= right + radius;

    private IEnumerable<ResizeEdges> GetEnabledResizeEdges()
    {
        ResizeEdges placements = Props.Resize.Placements;

        if ((placements & ResizeEdges.Left) != 0 && (placements & ResizeEdges.Top) != 0)
            yield return ResizeEdges.Left | ResizeEdges.Top;

        if ((placements & ResizeEdges.Right) != 0 && (placements & ResizeEdges.Top) != 0)
            yield return ResizeEdges.Right | ResizeEdges.Top;

        if ((placements & ResizeEdges.Left) != 0 && (placements & ResizeEdges.Bottom) != 0)
            yield return ResizeEdges.Left | ResizeEdges.Bottom;

        if ((placements & ResizeEdges.Right) != 0 && (placements & ResizeEdges.Bottom) != 0)
            yield return ResizeEdges.Right | ResizeEdges.Bottom;

        if ((placements & ResizeEdges.Left) != 0)
            yield return ResizeEdges.Left;

        if ((placements & ResizeEdges.Right) != 0)
            yield return ResizeEdges.Right;

        if ((placements & ResizeEdges.Top) != 0)
            yield return ResizeEdges.Top;

        if ((placements & ResizeEdges.Bottom) != 0)
            yield return ResizeEdges.Bottom;
    }

    private void OnHoverEnterOverDrag()
    {
        Client.Game.UO.GameCursor.ForceSetCursorVisualStyle(GameCursorVisualType.Dragging);
        _isOverridingCursorStyle = true;
    }

    private void OnMouseMovedWhileInWindow(object _, MouseMovedEventArgs e)
    {
        // Resize is disabled when the window is minimized, so no need to even check the cursor position.
        if (IsMinimized)
            return;

        bool isOnHandle = GetResizerUnderCursor() != null;

        switch (isOnHandle)
        {
            case true when !_isOverridingCursorStyle:
                OnHoverEnterOverDrag();
                break;
            case false when _isOverridingCursorStyle:
                StopOverridingCursorStyle();
                break;
        }
    }

    private void OnMouseMovedWhileResizing(object _, MouseMovedEventArgs e)
    {
        if (!_activeResizeEdge.HasValue || !Mouse.LButtonPressed)
        {
            Mouse.Moved -= OnMouseMovedWhileResizing;
            return;
        }

        ResizeEdges edges = _activeResizeEdge.Value;
        Rectangle newBounds = CalculateNewBounds(edges);

        if (newBounds.X == Left &&
            newBounds.Y == Top &&
            newBounds.Width == (Width ?? Bounds.Width) &&
            newBounds.Height == (Height ?? Bounds.Height))
            return;

        Left = newBounds.X;
        Top = newBounds.Y;
        Width = newBounds.Width;
        Height = newBounds.Height;

        Resized?.Invoke(this, new ResizeEventArgs { NewWidth = newBounds.Width, NewHeight = newBounds.Height });
    }

    private Rectangle CalculateNewBounds(ResizeEdges resizeEdges)
    {
        Point delta = Mouse.Position - _resizeStartMouse;

        int newX = _resizeStartLeft;
        int newY = _resizeStartTop;
        int newWidth = _resizeStartWidth;
        int newHeight = _resizeStartHeight;

        if ((resizeEdges & ResizeEdges.Left) != 0)
        {
            newX = _resizeStartLeft + delta.X;
            newWidth = _resizeStartWidth - delta.X;
        }
        else if ((resizeEdges & ResizeEdges.Right) != 0)
            newWidth = _resizeStartWidth + delta.X;

        if ((resizeEdges & ResizeEdges.Top) != 0)
        {
            newY = _resizeStartTop + delta.Y;
            newHeight = _resizeStartHeight - delta.Y;
        }
        else if ((resizeEdges & ResizeEdges.Bottom) != 0)
            newHeight = _resizeStartHeight + delta.Y;

        newWidth = Math.Clamp(newWidth, Props.Resize.MinWidth, Props.Resize.MaxWidth);
        newHeight = Math.Clamp(newHeight, Props.Resize.MinHeight, Props.Resize.MaxHeight);

        if ((resizeEdges & ResizeEdges.Left) != 0)
            newX = _resizeStartLeft + (_resizeStartWidth - newWidth);

        if ((resizeEdges & ResizeEdges.Top) != 0)
            newY = _resizeStartTop + (_resizeStartHeight - newHeight);

        return new Rectangle(newX, newY, newWidth, newHeight);
    }

    private void LeftClickChangedHandler(object _, MouseLeftButtonClickStateChangedEventArgs e)
    {
        if (!e.Current && _activeResizeEdge.HasValue)
            OnDragStop(null, EventArgs.Empty);
    }

    private void OnDragStop(object _, EventArgs _1)
    {
        _activeResizeEdge = null;
        Mouse.LeftButtonClickStateChanged -= LeftClickChangedHandler;
    }

    private void StopOverridingCursorStyle()
    {
        if (!_isOverridingCursorStyle)
            return;

        Client.Game.UO.GameCursor.ForceSetCursorVisualStyle(null);
        _isOverridingCursorStyle = false;
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;

        Mouse.Moved -= OnMouseMovedWhileInWindow;
        Mouse.LeftButtonClickStateChanged -= LeftClickChangedHandler;
        Mouse.Moved -= OnMouseMovedWhileResizing;
        TitlePanel?.TouchDoubleClick -= OnMinMaxButtonClick;
        _minMaxButton?.TouchDown -= OnMinMaxButtonClick;
        StopOverridingCursorStyle();

        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
}
