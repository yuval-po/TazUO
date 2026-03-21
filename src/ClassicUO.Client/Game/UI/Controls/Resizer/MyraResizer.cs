using System;
using ClassicUO.Assets;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Controls.Resizer;

public class MyraResizer : ContentControl
{
    private readonly ResizerProperties _props;
    private readonly Panel _innerPanel = new();

    private readonly ScrollViewer _scroller;

    private bool _isResizing;
    private Point _resizeStartMouse;
    private int? _resizeStartWidth;
    private int? _resizeStartHeight;

    public MyraResizer(ResizerProperties props)
    {
        _props = props ?? new ResizerProperties();
        _scroller = new ScrollViewer
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            MinHeight = _props.MinHeight,
            MinWidth = _props.MinWidth,
            MaxHeight = _props.MaxHeight,
            MaxWidth = _props.MaxWidth
        };
    }

    public override Widget Content
    {
        get;
        set
        {
            field = value;
            OnContentChanged(value);
        }
    }

    public override void InternalRender(RenderContext context)
    {
        RecomputeSize();
        base.InternalRender(context);
    }

    private void OnContentChanged(Widget newContent)
    {
        _innerPanel.Widgets.Clear();
        _innerPanel.Widgets.Add(newContent);

        foreach (ResizerPlacement alignment in _props.Placements)
            _innerPanel.Widgets.Add(CreateHandle(newContent, alignment));

        _scroller.Content = _innerPanel;
        _scroller.UpdateArrange();
    }

    private MyraLabel CreateHandle(Widget owner, ResizerPlacement placement)
    {
        // Note - we assume only one handle can be resizing at a time.
        // If this ever stops being the case, this logic will behave erratically.
        var handle = new MyraLabel(_props.GetHandleText(placement), _props.FontSize)
        {
            HorizontalAlignment = placement.Horizontal,
            VerticalAlignment = placement.Vertical,
            Tooltip = _props.Tooltip,
            Font = TrueTypeLoader.Instance.GetFont("NotoSansSymbols2-Regular", _props.FontSize),
            TextColor = StyleConstantsDefaults.ModernUiBorderLight,
            Border = new SolidBrush(StyleConstantsDefaults.ModernUiBorderDark),
            BorderThickness = new Thickness(2, 2),
            AcceptsKeyboardFocus = false
        };

        // This is quite hacky. The event dispatch doesn't bubble down here, so
        // we have to listen in on the parent component's event and manually filter those relevant to the handle itself
        owner.TouchDown += (_, _) => OnHandleTouchdown(handle);
        owner.Parent?.TouchDown += (_, _) => OnHandleTouchdown(handle);
        handle.TouchDown += (_, _) => OnHandleTouchdown(handle);

        return handle;
    }

    private void OnHandleTouchdown(MyraLabel handle)
    {
        if (handle.HitTest(Mouse.Position) == null)
            return;

        _isResizing = true;
        _resizeStartMouse = Mouse.Position;

        int maxWidth = Math.Max(_innerPanel.MaxWidth ?? -1, _props.MaxWidth);
        int maxHeight = Math.Max(_innerPanel.MaxHeight ?? -1, _props.MaxHeight);

        Point ownerSize = _innerPanel.Measure(new Point(maxWidth, maxHeight));
        _resizeStartWidth = ownerSize.X;
        _resizeStartHeight = ownerSize.Y;
    }

    private Point? RecomputeSize()
    {
        if (!_isResizing)
            return null;

        // Should not happen but just in case. Better fail resize than throw here.
        if (!_resizeStartWidth.HasValue || !_resizeStartHeight.HasValue)
            return null;

        if (!Mouse.LButtonPressed)
        {
            _isResizing = false;
            return null;
        }

        Point delta = Mouse.Position - _resizeStartMouse;
        int newWidth = Math.Clamp(_resizeStartWidth.Value + delta.X, _props.MinWidth, _props.MaxWidth);
        int newHeight = Math.Clamp(_resizeStartHeight.Value + delta.Y, _props.MinHeight, _props.MaxHeight);

        return new Point(newWidth, newHeight);
    }
}
