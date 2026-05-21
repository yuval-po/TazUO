using System;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class OptionTabCommons
{
    public static WrapPanel StyledWrapPanel(params Widget[] children)
    {
        var panel = new WrapPanel
        {
            Orientation = Orientation.Vertical,
            UniformSizing = false,
            Aligned = false,
            VerticalSpacing = MyraStyle.STANDARD_SPACING * 2
        };

        if (children?.Length > 0)
            panel.AddRange(children);
        return panel;
    }

    public static StackPanel StyledStackPanel(Orientation orientation, params Widget[] children)
    {
        StackPanel panel;
        if (orientation == Orientation.Horizontal)
            panel = new HorizontalStackPanel();
        else
            panel = new VerticalStackPanel();

        panel.Spacing = MyraStyle.STANDARD_SPACING;
        panel.VerticalAlignment = VerticalAlignment.Center;
        children?.ForEach(child => panel.Widgets.Add(child));
        return panel;
    }
}
