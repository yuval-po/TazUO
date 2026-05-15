using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class OptionTabCommons
{
    public static WrapPanel StyledWrapPanel(params Widget[] children)
    {
        var panel = new WrapPanel
        {
            Orientation = Orientation.Vertical, UniformSizing = false, Aligned = false, VerticalSpacing = MyraStyle.STANDARD_SPACING
        };

        if (children?.Length > 0)
            panel.AddRange(children);
        return panel;
    }
}
