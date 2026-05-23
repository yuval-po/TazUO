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

    #region Profiles

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

    private static WrapPanel GetEditorForProfile(NameOverheadOption profile)
    {
        ModernOptionsGumpLanguage.NamePlatesOptionsTab npLang = Language.Instance.GetModernOptionsGumpLanguage.GetNamePlates.OptionsTab;

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
                    npLang.CheckAll,
                    () => profile.NameOverheadOptionFlags = EnumUtils.AllBits<NameOverheadOptions>()
                ),
                new MyraButton(
                    npLang.UncheckAll,
                    () => profile.NameOverheadOptionFlags = NameOverheadOptions.None
                )
            ),
            settingsPanel
        );
    }

    private static VisualContainer GetItemsBoxesPanel(NameOverheadOption profile)
    {
        ModernOptionsGumpLanguage.NamePlatesOptionsTab npLang = Language.Instance.GetModernOptionsGumpLanguage.GetNamePlates.OptionsTab;

        return new VisualContainer(
            new VisualContainerProps { LabelText = npLang.Items },
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Containers,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Containers
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Stackable,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Stackable
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Moveable,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Moveable
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.OtherItems,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Other
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Gold,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Gold
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.LockedDown,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.LockedDown
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Immovable,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Immoveable
            )
        );
    }

    private static VisualContainer GetCorpseBoxesPanel(NameOverheadOption profile)
    {
        ModernOptionsGumpLanguage.NamePlatesOptionsTab npLang = Language.Instance.GetModernOptionsGumpLanguage.GetNamePlates.OptionsTab;
        return new VisualContainer(
            new VisualContainerProps { LabelText = npLang.Corpses },
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Monster,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.MonsterCorpses
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Humanoid,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.HumanoidCorpses
            )
        );
    }

    private static VisualContainer GetMobilesByTypeBoxesPanel(NameOverheadOption profile)
    {
        ModernOptionsGumpLanguage.NamePlatesOptionsTab npLang = Language.Instance.GetModernOptionsGumpLanguage.GetNamePlates.OptionsTab;

        return new VisualContainer(
            new VisualContainerProps { LabelText = npLang.MobilesByType },
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Humanoid,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Humanoid
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.YourFollowers,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.OwnFollowers
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.ExcludeYourself,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.ExcludeSelf
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Monster,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Monster
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Yourself,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Self
            )
        );
    }

    private static VisualContainer GetMobilesByNotorietyBoxesPanel(NameOverheadOption profile)
    {
        ModernOptionsGumpLanguage.NamePlatesOptionsTab npLang = Language.Instance.GetModernOptionsGumpLanguage.GetNamePlates.OptionsTab;
        return new VisualContainer(
            new VisualContainerProps { LabelText = npLang.MobilesByNotoriety },
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Innocent,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Innocent
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Attackable,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Gray
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Enemy,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Enemy
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Invulnerable,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Invulnerable
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Allied,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Ally
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Criminal,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Criminal
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Murderer,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Murderer
            )
        );
    }

    #endregion Profiles

    #region General Sub-Tab

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

    #endregion General Sub-Tab
}
