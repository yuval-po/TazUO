using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Gumps.GridHighLight;

internal static class GridHighlightRulebaseColumns
{
    public static RulebaseColumn<GridHighlightData>[] Get() =>
    [
        new()
        {
            Header = TazLang.Get("gridhighlight_enabled"),
            HeaderTooltip = TazLang.Get("gridhighlight_enabled_tooltip"),
            CellContentAlignment = HorizontalAlignment.Center,
            Proportion = new Proportion(ProportionType.Auto),
            CellFactory = data => MyraCheckButton.CreateWithCallback(
                data.Enabled, isChecked =>
                {
                    data.Enabled = isChecked;
                    GridHighlightData.RecheckMatchStatus();
                },
                tooltip: TazLang.Get("gridhighlight_enabled_tooltip")
            )
        },
        new()
        {
            Header = TazLang.Get("gridhighlight_name"),
            Proportion = new Proportion(ProportionType.Fill),
            CellFactory = data =>
            {
                var nameBox = new MyraInputBox { Text = data.Name ?? "", Width = 150 };
                nameBox.TextChangedByUser += (_, _) => data.Name = nameBox.Text ?? "";
                return nameBox;
            }
        },
        new()
        {
            Header = TazLang.Get("gridhighlight_color"),
            Proportion = new Proportion(ProportionType.Auto),
            CellFactory = data =>
            {
                var colorButton = new MyraButton(TazLang.Get("gridhighlight_color")) { Tooltip = TazLang.Get("gridhighlight_color_tooltip") };
                ApplyColorButtonStyle(colorButton, data.HighlightColor);
                colorButton.OnClick = () => RGBColorPickerGump.Open(data.HighlightColor, selectedColor =>
                {
                    data.HighlightColor = selectedColor;
                    data.Hue = (ushort)(selectedColor.R + (selectedColor.G << 8) + (selectedColor.B << 16));
                    ApplyColorButtonStyle(colorButton, selectedColor);
                    GridHighlightData.RecheckMatchStatus();
                });
                return colorButton;
            }
        },
        new()
        {
            Header = TazLang.Get("gridhighlight_properties"),
            Proportion = new Proportion(ProportionType.Auto),
            CellFactory = data => new MyraButton(TazLang.Get("gridhighlight_properties"), () => GridHighlightProperties.Show(World.Instance, data))
        }
    ];

    private static void ApplyColorButtonStyle(MyraButton button, Color color)
    {
        var brush = new SolidBrush(color);
        button.Background = brush;
        button.OverBackground = brush;
        button.PressedBackground = brush;
        button.DisabledBackground = brush;
    }
}
