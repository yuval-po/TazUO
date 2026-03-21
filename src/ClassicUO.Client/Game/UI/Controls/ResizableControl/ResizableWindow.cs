using System;
using System.Linq;
using ClassicUO.Game.UI.Controls.Resizer;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Controls.ResizableControl;

public class ResizableWindowProps : MyraCommonProps
{
    public ResizeProperties Resize { get; set; } = new();
}

public class ResizableWindow(ResizableWindowProps props = null) : Window
{
    public event EventHandler<ResizeEventArgs> Resized;

    public ResizableWindowProps Props { get; } = props ?? new ResizableWindowProps();

    private bool _isResizing;
    private Point _resizeStartMouse;
    private bool _isOverridingCursorStyle;

    public override void OnMouseEntered()
    {
        base.OnMouseEntered();
        Mouse.Moved += OnMouseMovedWhileInWindow;
    }

    public override void OnMouseLeft()
    {
        base.OnMouseLeft();
        Mouse.Moved -= OnMouseMovedWhileInWindow;

        // Stop overriding cursor style if the mouse has left the window and we're not currently resizing
        if (_isOverridingCursorStyle && !_isResizing)
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
            // If we ever want the visual cursor style change right after focusing, we can add it here.
            // Just a nitpick.
            return;
        }

        if (!IsCursorOnDragHandle())
            return;

        _isResizing = true;
        _resizeStartMouse = Mouse.Position;

        Mouse.LeftButtonClickStateChanged += LeftClickChangedHandler;
        Mouse.Moved += OnMouseMovedWhileResizing;
    }

    public override void OnTouchUp()
    {
        base.OnTouchUp();
        OnDragStop(null, EventArgs.Empty);
    }

    private bool IsCursorOnDragHandle()
    {
        if (!LocalMousePosition.HasValue)
            return false;

        Point mousePos = LocalMousePosition.Value;
        Point[] handlePositions = GetDragHandlePositions();

        int radius = (int)Props.Resize.ResizeHandleRadiusPx;
        int radiusSq = radius * radius;

        foreach (Point handlePosition in handlePositions)
        {
            int dx = mousePos.X - handlePosition.X;
            int dy = mousePos.Y - handlePosition.Y;

            if (dx * dx + dy * dy <= radiusSq)
                return true;
        }

        return false;
    }

    private Point[] GetDragHandlePositions()
    {
        var positions = new Point[Props.Resize.ResizerProps.Placements.Length];

        for (int i = 0; i < Props.Resize.ResizerProps.Placements.Length; i++)
        {
            Point? handlePos = Props.Resize.ResizerProps.Placements[i] switch
            {
                { Horizontal: HorizontalAlignment.Right, Vertical: VerticalAlignment.Top } =>
                    new Point(Bounds.X + Width ?? Bounds.Width, Bounds.Y),
                { Horizontal: HorizontalAlignment.Right, Vertical: VerticalAlignment.Bottom }
                    => new Point(Bounds.X + Width ?? Bounds.Width, Bounds.Y + Height ?? Bounds.Height),
                { Horizontal: HorizontalAlignment.Left, Vertical: VerticalAlignment.Bottom }
                    => new Point(Bounds.X, Bounds.Y + Height ?? Bounds.Height),
                { Horizontal: HorizontalAlignment.Left, Vertical: VerticalAlignment.Top }
                    => new Point(Bounds.X, Bounds.Y),
                _ => null
            };

            if (handlePos != null)
                positions[i] = handlePos.Value;
        }

        return positions;
    }

    private void OnHoverEnterOverDrag()
    {
        Client.Game.UO.GameCursor.ForceSetCursorVisualStyle(GameCursorVisualType.Dragging);
        _isOverridingCursorStyle = true;
    }

    private void OnMouseMovedWhileInWindow(object _, MouseMovedEventArgs e)
    {
        bool isOnHandle = IsCursorOnDragHandle();

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
        if (!_isResizing || !Mouse.LButtonPressed)
        {
            Mouse.Moved -= OnMouseMovedWhileResizing;
            return;
        }

        int oldWidth = Width ?? Bounds.Width;
        int oldHeight = Height ?? Bounds.Height;

        Point mouseDelta = Mouse.Position - _resizeStartMouse;
        int newWidth = Math.Clamp(oldWidth + mouseDelta.X, Props.MinWidth, Props.MaxWidth);
        int newHeight = Math.Clamp(oldHeight + mouseDelta.Y, Props.MinHeight, Props.MaxHeight);

        if (newWidth == oldWidth && newHeight == oldHeight)
            return;

        _resizeStartMouse = Mouse.Position;

        Width = newWidth;
        Height = newHeight;

        Resized?.Invoke(this, new ResizeEventArgs { NewWidth = newWidth, NewHeight = newHeight });
    }

    private void LeftClickChangedHandler(object _, MouseLeftButtonClickStateChangedEventArgs e)
    {
        if (!e.Current && _isResizing)
            OnDragStop(null, EventArgs.Empty);
    }

    private void OnDragStop(object _, EventArgs __)
    {
        _isResizing = false;
        Mouse.LeftButtonClickStateChanged -= LeftClickChangedHandler;
    }

    private void StopOverridingCursorStyle()
    {
        if (!_isOverridingCursorStyle)
            return;

        Client.Game.UO.GameCursor.ForceSetCursorVisualStyle(null);
        _isOverridingCursorStyle = false;
    }
}
