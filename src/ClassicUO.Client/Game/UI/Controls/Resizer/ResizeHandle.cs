using System;
using ClassicUO.Assets;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Controls.Resizer;

public class ResizeHandleProps
{
    public string Glyph { get; set; } = StyleConstantsDefaults.BOTTOM_RIGHT_HANDLE_TEXT;
    public string Tooltip { get; set; } = null;
    public int FontSize { get; set; } = StyleConstantsDefaults.RESIZE_HANDLE_FONT_SIZE;
    public ResizerPlacement Placement { get; set; } = new();
    public int MinWidth { get; set; } = StyleConstantsDefaults.WINDOW_MIN_WIDTH;
    public int MinHeight { get; set; } = StyleConstantsDefaults.WINDOW_MIN_HEIGHT;

    public int MaxWidth { get; set; } = StyleConstantsDefaults.WINDOW_MAX_WIDTH;
    public int MaxHeight { get; set; } = StyleConstantsDefaults.WINDOW_MAX_HEIGHT;
}

public class ResizeEventArgs : EventArgs
{
    public int NewWidth { get; set; }
    public int NewHeight { get; set; }
}

public sealed class ResizeHandle : Widget
{
    private readonly ResizeHandleProps _props;

    private bool _isResizing;
    private Point _resizeStartMouse;
    private int _resizeStartHeight;
    private int _resizeStartWidth;
    private bool _isOverridingCursorStyle;

    public event EventHandler<ResizeEventArgs> Resized;

    public ResizeHandle(ResizeHandleProps props)
    {
        _props = props ?? new ResizeHandleProps();

        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        ChildrenLayout = new SingleItemLayout<MyraLabel>(this);
        Children.Add(Build());
        UpdateArrange();
    }

    private MyraLabel Build()
    {
        // Note - we assume only one handle can be resizing at a time.
        // If this ever stops being the case, this logic will behave erratically.
        var handle = new MyraLabel(_props.Glyph, _props.FontSize)
        {
            HorizontalAlignment = _props.Placement.Horizontal,
            VerticalAlignment = _props.Placement.Vertical,
            Tooltip = _props.Tooltip,
            Font = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.EMBEDDED_NOTO_SANS_2_SYMBOLS, _props.FontSize),
            TextColor = StyleConstantsDefaults.ModernUiBorderLight,
            Border = new SolidBrush(StyleConstantsDefaults.ModernUiBorderDark),
            AcceptsKeyboardFocus = false
        };

        // This is quite hacky. The event dispatch doesn't bubble down here, so
        // we have to listen in on the parent component's event and manually filter those relevant to the handle itself
        MouseEntered += OnMouseEntered;
        MouseLeft += OnMouseLeft;
        TouchDown += (_, _) => OnHandleTouchdown(handle);
        TouchUp += (_, _) => OnDragStop();

        return handle;
    }

    private void OnMouseEntered(object _, EventArgs __)
    {
        Client.Game.UO.GameCursor.ForceSetCursorVisualStyle(GameCursorVisualType.Dragging);
        _isOverridingCursorStyle = true;
    }

    private void OnMouseLeft(object _, EventArgs __) => StopOverridingCursorStyle();

    private void OnHandleTouchdown(MyraLabel handle)
    {
        if (handle.HitTest(Mouse.Position) == null)
            return;

        _isResizing = true;
        _resizeStartMouse = Mouse.Position;
        _resizeStartHeight = Parent.Height ?? Parent.Bounds.Height;
        _resizeStartWidth = Parent.Width ?? Parent.Bounds.Width;

        Mouse.LeftButtonClickStateChanged += LeftClickChangedHandler;
        Mouse.Moved += OnMouseMoved;
    }

    private void OnMouseMoved(object _, MouseMovedEventArgs e)
    {
        if (!_isResizing || !Mouse.LButtonPressed)
            return;

        Point mouseDelta = Mouse.Position - _resizeStartMouse;
        int newWidth = Math.Clamp(_resizeStartWidth + mouseDelta.X, _props.MinWidth, _props.MaxWidth);
        int newHeight = Math.Clamp(_resizeStartHeight + mouseDelta.Y, _props.MinHeight, _props.MaxHeight);

        if (newWidth == _resizeStartWidth && newHeight == _resizeStartHeight)
            return;

        _resizeStartMouse = Mouse.Position;
        _resizeStartHeight = newHeight;
        _resizeStartWidth = newWidth;

        Resized?.Invoke(this, new ResizeEventArgs { NewWidth = newWidth, NewHeight = newHeight });
    }

    private void LeftClickChangedHandler(object _, MouseLeftButtonClickStateChangedEventArgs e)
    {
        if (!e.Current && _isResizing)
            OnDragStop();
    }

    private void OnDragStop()
    {
        _isResizing = false;
        Mouse.LeftButtonClickStateChanged -= LeftClickChangedHandler;
        Mouse.Moved -= OnMouseMoved;
    }

    private void StopOverridingCursorStyle()
    {
        if (!_isOverridingCursorStyle)
            return;

        Client.Game.UO.GameCursor.ForceSetCursorVisualStyle(null);
        _isOverridingCursorStyle = false;
    }
}
