using System;
using ClassicUO.Assets;
using ClassicUO.Common;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class OptionTabCommons
{
    internal static WrapPanel StyledVerticalWrapPanel(params Widget[] children) => StyledWrapPanel(Orientation.Vertical, children);

    internal static WrapPanel StyledHorizontalWrapPanel(params Widget[] children) => StyledWrapPanel(Orientation.Horizontal, children);

    internal static WrapPanel StyledWrapPanel(Orientation orientation, params Widget[] children)
    {
        var panel = new WrapPanel { Orientation = orientation, UniformSizing = false, Aligned = false, VerticalSpacing = MyraStyle.STANDARD_SPACING };

        if (children?.Length > 0)
            panel.AddRange(children);
        return panel;
    }

    internal static StackPanel StyledStackPanel(Orientation orientation, params Widget[] children)
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

    internal static OptionItem StyledFontSelector(
        string label,
        Accessor<string> backingProp,
        Action<string> onAfterUpdate = null
    )
    {
        Action<string> callback;
        if (onAfterUpdate != null)
            callback = newValue =>
            {
                backingProp.Set(newValue);
                onAfterUpdate(newValue);
            };
        else
            callback = backingProp.Set;

        return OptionsFactory.CreateComboBox(label, backingProp.Get(), TrueTypeLoader.Instance.OrderedFontNames.Names, callback);
    }
}
