using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class SpeechTab
{
    internal static OptionItem GetContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return new OptionItem(
            lang.LabelSpeech,
            () => OptionTabCommons.StyledWrapPanel(
                GetDelaySection(),
                OptionsFactory.CreateCheckboxOption(lang.GetSpeech.SaveJournalE, new Accessor<bool>(() => profile.SaveJournalToFile)),
                OptionsFactory.CreateSpacer(),
                GetActivationSection(),
                OptionsFactory.CreateSpacer(),
                OptionsFactory.CreateCheckboxOption(lang.GetSpeech.ChatGradient, new Accessor<bool>(() => profile.HideChatGradient)),
                OptionsFactory.CreateCheckboxOption(lang.GetSpeech.HideGuildChat, new Accessor<bool>(() => profile.IgnoreGuildMessages)),
                OptionsFactory.CreateCheckboxOption(lang.GetSpeech.HideAllianceChat, new Accessor<bool>(() => profile.IgnoreAllianceMessages)),
                OptionsFactory.CreateSpacer(),
                GetColorSection()
            )
        );
    }

    private static CheckBoxGroup GetDelaySection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return new CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => profile.ScaleSpeechDelay), lang.GetSpeech.ScaleSpeechDelay),
            OptionsFactory.CreateSliderOption(lang.GetSpeech.SpeechDelay, 0, 1000, profile.SpeechDelay, f => profile.SpeechDelay = (int)f)
        );
    }

    private static CheckBoxGroup GetActivationSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return new CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => profile.ActivateChatAfterEnter), lang.GetSpeech.ChatEnterActivation),
            OptionsFactory.CreateCheckboxOption(lang.GetSpeech.ChatEnterSpecial, new Accessor<bool>(() => profile.ActivateChatAdditionalButtons)),
            OptionsFactory.CreateCheckboxOption(lang.GetSpeech.ShiftEnterChat, new Accessor<bool>(() => profile.ActivateChatShiftEnterSupport))
        );
    }

    private static VisualContainer GetColorSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return new VisualContainer(
            new VisualContainerProps { LabelText = lang.LabelSpeech },
            OptionsFactory.CreateHuePicker(lang.GetSpeech.SpeechColor, profile.SpeechHue, b => profile.SpeechHue = b),
            OptionsFactory.CreateHuePicker(lang.GetSpeech.YellColor, profile.YellHue, b => profile.YellHue = b),
            OptionsFactory.CreateHuePicker(lang.GetSpeech.PartyColor, profile.PartyMessageHue, b => profile.PartyMessageHue = b),
            OptionsFactory.CreateHuePicker(lang.GetSpeech.AllianceColor, profile.AllyMessageHue, b => profile.AllyMessageHue = b),
            OptionsFactory.CreateHuePicker(lang.GetSpeech.EmoteColor, profile.EmoteHue, b => profile.EmoteHue = b),
            OptionsFactory.CreateHuePicker(lang.GetSpeech.WhisperColor, profile.WhisperHue, b => profile.WhisperHue = b),
            OptionsFactory.CreateHuePicker(lang.GetSpeech.GuildColor, profile.GuildMessageHue, b => profile.GuildMessageHue = b),
            OptionsFactory.CreateHuePicker(lang.GetSpeech.CharColor, profile.ChatMessageHue, b => profile.ChatMessageHue = b)
        );
    }
}
