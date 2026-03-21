using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Controls.ResizableComponents;

public class ResizableWindowProps : MyraCommonProps
{
    public ResizeBehavior Resize { get; set; } = new();
}

public class ResizableWindow(ResizableWindowProps props = null) : Window, IDisposable
{
    public event EventHandler<ResizeEventArgs> Resized;

    public ResizableWindowProps Props { get; } = props ?? new ResizableWindowProps();

    private ResizeEdges? _activeResizeEdge;

    private Point _resizeStartMouse;
    private int _resizeStartLeft;
    private int _resizeStartTop;
    private int _resizeStartWidth;
    private int _resizeStartHeight;

    private bool _isOverridingCursorStyle;

    public bool IsDisposed { get; private set; }

    public override void OnMouseEntered()
    {
        base.OnMouseEntered();
        Mouse.Moved += OnMouseMovedWhileInWindow;
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
        int radius = (int)Props.Resize.ResizeHandleRadiusPx;
        int radiusSq = radius * radius;

        foreach (ResizeEdges edges in GetEnabledResizeEdges())
            if (IsCursorOverResizer(mousePos, edges, radius, radiusSq))
                return edges;

        return null;
    }

    private bool IsCursorOverResizer(Point mousePos, ResizeEdges edges, int radius, int radiusSq)
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
            return IsWithinRadius(mousePos, left, top, radiusSq);

        if (hasRight && hasTop)
            return IsWithinRadius(mousePos, right, top, radiusSq);

        if (hasLeft && hasBottom)
            return IsWithinRadius(mousePos, left, bottom, radiusSq);

        if (hasRight && hasBottom)
            return IsWithinRadius(mousePos, right, bottom, radiusSq);

        // Edge handles: strip along the edge, with thickness based on radius.
        if (hasLeft)
            return IsWithinVerticalEdge(mousePos, left, top, bottom, radius);

        if (hasRight)
            return IsWithinVerticalEdge(mousePos, right, top, bottom, radius);

        if (hasTop)
            return IsWithinHorizontalEdge(mousePos, top, left, right, radius);

        if (hasBottom)
            return IsWithinHorizontalEdge(mousePos, bottom, left, right, radius);

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWithinRadius(Point mousePos, int x, int y, int radiusSq)
    {
        int dx = mousePos.X - x;
        int dy = mousePos.Y - y;
        return dx * dx + dy * dy <= radiusSq;
    }

    private static bool IsWithinVerticalEdge(Point mousePos, int edgeX, int top, int bottom, int radius) =>
        mousePos.X >= edgeX - radius
        && mousePos.X <= edgeX + radius
        && mousePos.Y >= top - radius
        && mousePos.Y <= bottom + radius;

    private static bool IsWithinHorizontalEdge(Point mousePos, int edgeY, int left, int right, int radius) =>
        mousePos.Y >= edgeY - radius
        && mousePos.Y <= edgeY + radius
        && mousePos.X >= left - radius
        && mousePos.X <= right + radius;

    private Point GetHandlePosition(ResizeEdges edges)
    {
        int width = Width ?? Bounds.Width;
        int height = Height ?? Bounds.Height;

        int left = Bounds.X;
        int top = Bounds.Y;
        int right = left + width;
        int bottom = top + height;

        return edges switch
        {
            ResizeEdges.Left | ResizeEdges.Top => new Point(left, top),
            ResizeEdges.Right | ResizeEdges.Top => new Point(right, top),
            ResizeEdges.Left | ResizeEdges.Bottom => new Point(left, bottom),
            ResizeEdges.Right | ResizeEdges.Bottom => new Point(right, bottom),
            ResizeEdges.Left => new Point(left, top + height / 2),
            ResizeEdges.Right => new Point(right, top + height / 2),
            ResizeEdges.Top => new Point(left + width / 2, top),
            ResizeEdges.Bottom => new Point(left + width / 2, bottom),
            _ => new Point(left, top)
        };
    }

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
        StopOverridingCursorStyle();

        GC.SuppressFinalize(this);

        IsDisposed = true;
    }
}
