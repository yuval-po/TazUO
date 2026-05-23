using ClassicUO.Common;
using ClassicUO.Common.Enums;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Profile;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class NameplatesTab
{
    internal static OptionItem GetContent()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        return new OptionItem(lang.ButtonNameplates, GetNameplatesMenuTabs);
    }

    private static MyraTabControl GetNameplatesMenuTabs()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        var tabs = new MyraTabControl();
        tabs.AddTab(lang.ButtonGeneral, GetGeneralNameplatesSubTabContent);
        tabs.AddTab(lang.ButtonProfiles, GetProfilesSubTabContent);
        return tabs;
    }

    private static Widget GetProfilesSubTabContent()
    {
        var profileEditor = new ProfileEditor<NameOverheadOption>(
            GetEditorForProfile,
            name =>
            {
                var newProfile = new NameOverheadOption(name);
                World.Instance.NameOverHeadManager.AddOption(newProfile);
                return newProfile;
            },
            profile =>
            {
                World.Instance.NameOverHeadManager.RemoveOption(profile);
            },
            NameOverHeadManager.GetAllOptions()
        );
        return profileEditor;
    }

    private static Widget GetEditorForProfile(NameOverheadOption profile)
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.TazUO tuoLang = lang.GetTazUO;
        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;

        WrapPanel settingsPanel = OptionTabCommons.StyledHorizontalWrapPanel(
            GetItemsBoxesPanel(profile),
            GetCorpseBoxesPanel(profile),
            GetMobilesByTypeBoxesPanel(profile),
            GetMobilesByNotorietyBoxesPanel(profile)
        );
        settingsPanel.HorizontalAlignment = HorizontalAlignment.Left;
        settingsPanel.Aligned = false;
        settingsPanel.UniformSizing = false;

        return OptionTabCommons.StyledVerticalWrapPanel(
            OptionTabCommons.StyledStackPanel(
                Orientation.Horizontal,
                new MyraButton(
                    "Check All",
                    () => profile.NameOverheadOptionFlags = EnumUtils.AllBits<NameOverheadOptions>()
                ),
                new MyraButton(
                    "Uncheck All",
                    () => profile.NameOverheadOptionFlags = NameOverheadOptions.None
                )
            ),
            settingsPanel
        );
    }

    private static Widget GetItemsBoxesPanel(NameOverheadOption profile) =>
        new VisualContainer(
            new VisualContainerProps { LabelText = "Items" },
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Containers",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Containers
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Stackable",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Stackable
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Moveable",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Moveable
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Other items",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Other
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Gold",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Gold
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Locked down",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.LockedDown
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Immovable",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Immoveable
            )
        );

    private static Widget GetCorpseBoxesPanel(NameOverheadOption profile) =>
        new VisualContainer(
            new VisualContainerProps { LabelText = "Corpses" },
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Monster",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Monster
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Humanoid",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.HumanoidCorpses
            )
        );

    private static Widget GetMobilesByTypeBoxesPanel(NameOverheadOption profile) =>
        new VisualContainer(
            new VisualContainerProps { LabelText = "Mobiles by type" },
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Humanoid",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Humanoid
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Your followers",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.OwnFollowers
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Exclude yourself",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.ExcludeSelf
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Monster",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Monster
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Yourself",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Self
            )
        );

    private static Widget GetMobilesByNotorietyBoxesPanel(NameOverheadOption profile) =>
        new VisualContainer(
            new VisualContainerProps { LabelText = "Mobiles by notoriety" },
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Innocent",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Innocent
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Attackable",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Gray
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Enemy",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Enemy
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Invulnerable",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Invulnerable
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Allied",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Ally
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Criminal",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Criminal
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                "Murderer",
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Murderer
            )
        );


    private static WrapPanel GetGeneralNameplatesSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.TazUO tuoLang = lang.GetTazUO;
        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;

        return OptionTabCommons.StyledVerticalWrapPanel(
            OptionsFactory.CreateSpacer(),
            OptionTabCommons.StyledFontSelector(tuoLang.NameplateFont, new Accessor<string>(() => profile.NamePlateFont), s => profile.NamePlateFont = s),
            OptionsFactory.CreateSliderOption(tuoLang.SharedSize, 5, 50, profile.NamePlateFontSize, i => profile.NamePlateFontSize = (int)i),
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateComboBox(
                genLang.DragNameplatesOnly,
                profile.DragSelect_NameplateModifier,
                [genLang.SharedNone, genLang.SharedCtrl, genLang.SharedShift, genLang.SharedAlt],
                i => profile.DragSelect_NameplateModifier = i
            ),
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateCheckboxOption(genLang.IncomingMobiles, new Accessor<bool>(() => profile.ShowNewMobileNameIncoming)),
            OptionsFactory.CreateCheckboxOption(genLang.IncomingCorpses, new Accessor<bool>(() => profile.ShowNewCorpseNameIncoming)),
            OptionsFactory.CreateSpacer(),
            new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.NamePlateHealthBar), tuoLang.NameplatesAlsoActAsHealthBars),
                OptionsFactory.CreateSliderOption(
                    tuoLang.HpOpacity,
                    0,
                    100,
                    profile.NamePlateHealthBarOpacity,
                    i => profile.NamePlateHealthBarOpacity = (byte)i
                ),
                new CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.NamePlateHideAtFullHealth), tuoLang.HideNameplatesIfFullHealth),
                    OptionsFactory.CreateCheckboxOption(tuoLang.OnlyInWarmode, new Accessor<bool>(() => profile.NamePlateHideAtFullHealthInWarmode))
                )
            ),
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateSliderOption(tuoLang.BorderOpacity, 0, 100, profile.NamePlateBorderOpacity, i => profile.NamePlateBorderOpacity = (byte)i),
            OptionsFactory.CreateSliderOption(tuoLang.BackgroundOpacity, 0, 100, profile.NamePlateOpacity, i => profile.NamePlateOpacity = (byte)i),
            OptionsFactory.CreateCheckboxOption(tuoLang.AvoidOverlap, new Accessor<bool>(() => profile.NamePlateAvoidOverlap))
        );
    }
}
