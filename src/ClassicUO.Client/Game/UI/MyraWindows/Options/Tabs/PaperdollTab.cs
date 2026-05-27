using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public class PaperdollTab
{
    internal static OptionItem GetContent()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        return new OptionItem(lang.ButtonPaperdoll, GetModernPaperdollSection);
    }

    private static VisualContainer GetModernPaperdollSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.TazUO tuoLang = Language.Instance.GetModernOptionsGumpLanguage.GetTazUO;

        return new VisualContainer(
            new VisualContainerProps { LabelText = tuoLang.ModernPaperdoll, LabelLink = "https://tazuo.org/wiki/alternate-paperdoll/" },
            new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.UseModernPaperdoll), tuoLang.EnableModernPaperdoll),
                OptionsFactory.CreateHuePicker(
                    tuoLang.PaperdollHue,
                    profile.ModernPaperDollHue,
                    newHue =>
                    {
                        profile.ModernPaperDollHue = newHue;
                        ModernPaperdoll.UpdateAllOptions();
                    }
                ),
                OptionsFactory.CreateHuePicker(
                    tuoLang.DurabilityBarHue,
                    profile.ModernPaperDollDurabilityHue,
                    newHue =>
                    {
                        profile.ModernPaperDollDurabilityHue = newHue;
                        ModernPaperdoll.UpdateAllOptions();
                    }
                ),
                OptionsFactory.CreateSpacer(),
                OptionsFactory.PropBoundSliderOption(tuoLang.ShowDurabilityBarBelow, new Accessor<int>(() => profile.ModernPaperDoll_DurabilityPercent), 1, 100),
                OptionsFactory.CreateCheckboxOption(
                    tuoLang.PaperdollAnchor,
                    profile.ModernPaperdollAnchorEnabled,
                    newValue =>
                    {
                        profile.ModernPaperdollAnchorEnabled = newValue;
                        ModernPaperdoll.UpdateAllOptions();
                    }
                )
            )
        );
    }
}
