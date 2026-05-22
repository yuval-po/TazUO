using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class InterfaceTab
{
    internal static OptionItem GetContent()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        return new OptionItem(lang.ButtonGameplay, GetInterfaceMenuTabs);
    }

    private static MyraTabControl GetInterfaceMenuTabs()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Video videoLang = Language.Instance.GetModernOptionsGumpLanguage.GetVideo;
        ModernOptionsGumpLanguage gumpLang = Language.Instance.GetModernOptionsGumpLanguage;

        var tabs = new MyraTabControl();
        tabs.AddTab(lang.ButtonContainers, ContainersTab.GetContent);
        tabs.AddTab(lang.ButtonNameplates, NameplatesTab.GetContent);
        tabs.AddTab(lang.ButtonInfoBar, InfoBarsTab.GetContent);
        tabs.AddTab(lang.ButtonTerrainStatics, GetTerrainAndStaticsSubTabContent);
        return tabs;
    }

    private static WrapPanel GetTerrainAndStaticsSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;

        return OptionTabCommons.StyledVerticalWrapPanel(
            OptionsFactory.CreateCheckboxOption(genLang.DisableTopMenu, new Accessor<bool>(() => profile.TopbarGumpIsDisabled),
                "The top menu is pretty vital in TazUO, we recommend leaving this unchecked."),
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateCheckboxOption(genLang.AltForAnchorsGumps,
                new Accessor<bool>(() => profile.HoldDownKeyAltToCloseAnchored)),
            OptionsFactory.CreateCheckboxOption(genLang.AltToMoveGumps, new Accessor<bool>(() => profile.HoldAltToMoveGumps)),
            OptionsFactory.CreateCheckboxOption(genLang.CloseEntireAnchorWithRClick,
                new Accessor<bool>(() => profile.CloseAllAnchoredGumpsInGroupWithRightClick)),
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateCheckboxOption(genLang.OriginalSkillsGump, new Accessor<bool>(() => profile.StandardSkillsGump)),
            OptionsFactory.CreateCheckboxOption(genLang.OldStatusGump, new Accessor<bool>(() => profile.UseOldStatusGump)),
            OptionsFactory.CreateCheckboxOption(genLang.PartyInviteGump, new Accessor<bool>(() => profile.PartyInviteGump)),
            OptionsFactory.CreateSpacer(),
            new OptionItem(
                genLang.ModernHealthBars,
                () => new CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.CustomBarsToggled), genLang.ModernHealthBars),
                    OptionsFactory.CreateCheckboxOption(genLang.ModernHPBlackBG, new Accessor<bool>(() => profile.CBBlackBGToggled))
                )
            ),
            OptionsFactory.CreateCheckboxOption(genLang.SaveHPBars, new Accessor<bool>(() => profile.SaveHealthbars)),
            OptionsFactory.CreateComboBox(genLang.CloseHPGumpsWhen, profile.CloseHealthBarType, [
                genLang.CloseHPOptDisable, genLang.CloseHPOptOOR,
                genLang.CloseHPOptDead, genLang.CloseHPOptBoth
            ], b => profile.CloseHealthBarType = b),
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateComboBox(genLang.GridLoot, profile.GridLootType, [
                genLang.GridLootOptDisable, genLang.GridLootOptOnly,
                genLang.GridLootOptBoth
            ], i => profile.GridLootType = i, "This is not the same as grid containers."),
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateCheckboxOption(genLang.ShiftContext, new Accessor<bool>(() => profile.HoldShiftForContext)),
            OptionsFactory.CreateCheckboxOption(genLang.ShiftSplit, new Accessor<bool>(() => profile.HoldShiftToSplitStack))
        );
    }

    private static WrapPanel GetGumpsSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;

        return OptionTabCommons.StyledVerticalWrapPanel(
            OptionsFactory.CreateCheckboxOption(genLang.AltForAnchorsGumps, new Accessor<bool>(() => profile.HoldDownKeyAltToCloseAnchored)),
            OptionsFactory.CreateCheckboxOption(genLang.AltToMoveGumps, new Accessor<bool>(() => profile.HoldAltToMoveGumps)),
            OptionsFactory.CreateCheckboxOption(genLang.CloseEntireAnchorWithRClick,
                new Accessor<bool>(() => profile.CloseAllAnchoredGumpsInGroupWithRightClick)),
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateCheckboxOption(genLang.OriginalSkillsGump, new Accessor<bool>(() => profile.StandardSkillsGump)),
            OptionsFactory.CreateCheckboxOption(genLang.OldStatusGump, new Accessor<bool>(() => profile.UseOldStatusGump)),
            OptionsFactory.CreateCheckboxOption(genLang.PartyInviteGump, new Accessor<bool>(() => profile.PartyInviteGump)),
            OptionsFactory.CreateSpacer(),
            new OptionItem(
                genLang.ModernHealthBars,
                () => new CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.CustomBarsToggled), genLang.ModernHealthBars),
                    OptionsFactory.CreateCheckboxOption(genLang.ModernHPBlackBG, new Accessor<bool>(() => profile.CBBlackBGToggled))
                )
            ),
            OptionsFactory.CreateCheckboxOption(genLang.SaveHPBars, new Accessor<bool>(() => profile.SaveHealthbars)),
            OptionsFactory.CreateComboBox(
                genLang.CloseHPGumpsWhen,
                profile.CloseHealthBarType,
                [
                    genLang.CloseHPOptDisable,
                    genLang.CloseHPOptOOR,
                    genLang.CloseHPOptDead,
                    genLang.CloseHPOptBoth
                ],
                b => profile.CloseHealthBarType = b
            )
        );
    }
}
