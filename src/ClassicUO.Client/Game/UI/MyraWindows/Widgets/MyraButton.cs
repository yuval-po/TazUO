#nullable enable
using System;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraButton : Button
{
    private readonly Action? _onClick;

    public string Text { get; }

    public MyraButton(string text, Action? onClick = null, MyraLabel.TextStyle style = MyraLabel.TextStyle.P)
    {
        _onClick = onClick;
        Text = text;
        Margin = new Thickness(2);
        VerticalAlignment = VerticalAlignment.Center;
        DisabledBackground = Background;

        Build(style);
    }

    public override void OnTouchDown()
    {
        base.OnTouchDown();

        if (Enabled)
            _onClick?.Invoke();
    }

    private void Build(MyraLabel.TextStyle style) => Content = new MyraLabel(Text, style);
}
