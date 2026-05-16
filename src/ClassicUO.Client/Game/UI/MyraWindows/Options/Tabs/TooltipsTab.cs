using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class TooltipsTab
{
    internal static OptionItem GetContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return new OptionItem(
            lang.LabelTooltips,
            () => new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.UseTooltip), lang.GetToolTips.EnableToolTips),
                GetGroupContent()
            )
        );
    }

    private static Widget[] GetGroupContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.ToolTips tooltipLang = Language.Instance.GetModernOptionsGumpLanguage.GetToolTips;
        ModernOptionsGumpLanguage.TazUO tuoMiscLang = Language.Instance.GetModernOptionsGumpLanguage.GetTazUO;

        return
        [
            OptionsFactory.CreateSliderOption(
                tooltipLang.ToolTipDelay, 0, 1000, profile.TooltipDelayBeforeDisplay,
                f => profile.TooltipDelayBeforeDisplay = (int)f
            ),
            OptionsFactory.CreateSliderOption(
                tooltipLang.ToolTipBG, 0, 100, profile.TooltipBackgroundOpacity,
                f => profile.TooltipBackgroundOpacity = (int)f
            ),
            OptionsFactory.PropBoundHuePicker(tooltipLang.ToolTipFont, new Accessor<ushort>(() => profile.TooltipTextHue)),
            OptionsFactory.PropBoundHuePicker(tuoMiscLang.BackgroundHue, new Accessor<ushort>(() => profile.ToolTipBGHue)),
            OptionsFactory.CreateCheckboxOption(tuoMiscLang.AlignTooltipsToTheLeftSide, new Accessor<bool>(() => profile.LeftAlignToolTips)),
            OptionsFactory.CreateCheckboxOption(tuoMiscLang.AlignMobileTooltipsToCenter, new Accessor<bool>(() => profile.ForceCenterAlignTooltipMobiles)),
            OptionsFactory.CreateCheckboxOption(tuoMiscLang.ForcedTooltips, new Accessor<bool>(() => profile.ForceTooltipsOnOldClients)),
            OptionsFactory.PropBoundInputField(tuoMiscLang.HeaderFormatItemName, new Accessor<string>(() => profile.TooltipHeaderFormat)),
            new VisualContainer(
                new VisualContainerProps { LabelText = tooltipLang.LabelTooltipOverrides, LabelLink = "https://tazuo.org/wiki/tooltip-override/" },
                new MyraButton(tooltipLang.LabelOpenOverridesConfig, () => UIManager.Add(new TooltipConfigGump()))
            )
        ];
    }
}
