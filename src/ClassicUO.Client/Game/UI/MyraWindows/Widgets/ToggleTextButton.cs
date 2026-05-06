#nullable enable
using System;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class ToggleTextButton : ToggleButton
{
    private readonly Action<ToggleTextButton>? _onClick;

    public ToggleTextButton(string text, Action<ToggleTextButton>? onClick = null)
    {
        _onClick = onClick;
        Margin = new Thickness(2);
        VerticalAlignment = VerticalAlignment.Center;
        Content = new MyraLabel(text, MyraLabel.TextStyle.P);
    }

    public override void OnTouchDown()
    {
        base.OnTouchDown();
        _onClick?.Invoke(this);
    }
}
