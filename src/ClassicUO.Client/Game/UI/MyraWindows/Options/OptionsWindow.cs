#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Common;
using ClassicUO.Common.Enums;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Input;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options;

public class OptionsWindow : MyraControl
{
    private const int MAX_HEIGHT = 850;
    private const int MAX_WIDTH = 1200;

    /// <summary>
    ///     Category, (subcategory? widget)
    /// </summary>
    private readonly Dictionary<string, List<OptionItem>> _options = new();

    private readonly MyraGrid _mainArea = new();

    private readonly WrapPanel _optionsPanel = new()
    {
        UniformSizing = false,
        Orientation = Orientation.Vertical,
        HorizontalSpacing = MyraStyle.STANDARD_SPACING,
        VerticalSpacing = MyraStyle.STANDARD_SPACING,
        Padding = new Thickness(3, 0, 0, 10)
    };

    private readonly WrapPanel _searchPanel = new()
    {
        UniformSizing = false,
        Orientation = Orientation.Vertical
    };
    private readonly WrapPanel _optionsStack = new()
    {
        UniformSizing = false,
        Orientation = Orientation.Vertical
    };

    private readonly MyraInputBox _searchField = new()
    {
        TextVerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(
            MyraStyle.STANDARD_SPACING,
            0,
            MyraStyle.STANDARD_SPACING,
            MyraStyle.STANDARD_SPACING
        ),
        Padding = new Thickness(
            MyraStyle.STANDARD_SPACING,
            5,
            MyraStyle.STANDARD_SPACING,
            5
        )
    };

    private string _lastCategory = string.Empty;

    public event EventHandler<string>? SelectedCategoryChanged;

    public OptionsWindow() : base("Options")
    {
        UIManager.ForEach<OptionsWindow>(w =>
        {
            if (w != this) w.Dispose();
        });

        SetupOptions();
        Build();

        CenterInViewPort();

        _rootWindow.Props.Resize.MaxHeight = MAX_HEIGHT;
        _rootWindow.Props.Resize.MaxWidth = MAX_WIDTH;
    }

    private void SetupOptions()
    {
        SetupGeneralOptions();
        SetupMobileOptions();
        SetupInterfaceOptions();
        SetupMiscOptions();
        SetupTerrainStatics();
        SetupSound();
        SetupVideo();
        SetupInfoBarOptions();
        SetupTooltipOptions();
        SetupSpeechOptions();
        SetupCombatOptions();
        SetupCounterOptions();
        SetupContainerOptions();
        //SetupCooldownOptions();
        SetupExperimentalOptions();
    }

    private void Build()
    {
        _mainArea.MinWidth = 400;
        _mainArea.MinHeight = 400;

        _mainArea.AddColumn(Proportion.Auto);
        _mainArea.AddColumn(Proportion.Fill);
        _mainArea.AddRow(Proportion.Auto);
        _mainArea.AddRow(Proportion.Fill);

        _searchField.HintText = "Search...";
        _searchField.TextChangedByUser += SearchFieldOnTextChangedByUser;
        _mainArea.AddWidget(_searchField, 0, 0, null, 2);

        WrapPanel categoryPanel = new()
        {
            Orientation = Orientation.Vertical,
            HorizontalSpacing = MyraStyle.STANDARD_SPACING,
            VerticalSpacing = MyraStyle.STANDARD_SPACING
        };

        _mainArea.AddWidget(categoryPanel.WrapInScroll(MAX_HEIGHT), 1, 0);

        _optionsStack.Widgets.Add(_optionsPanel);
        _mainArea.AddWidget(_optionsStack, 1, 1);

        foreach (string category in _options.Keys)
            categoryPanel.Widgets.Add(GetCategoryButton(category));

        SetRootContent(_mainArea);
    }

    private ButtonBase2 GetCategoryButton(string category)
    {
        var unstyledButton = new ToggleTextButton(category, sender =>
        {
            ShowPage(category);
            SelectedCategoryChanged?.Invoke(sender, category);
        });

        // Each button listens to the category selection event and updates its pressed state accordingly
        SelectedCategoryChanged += (sender, _) => unstyledButton.IsPressed = sender == unstyledButton;

        return ApplyTabStyleToButton(unstyledButton);
    }

    private void SearchFieldOnTextChangedByUser(object? sender, ValueChangedEventArgs<string> e)
    {
        string search = e.NewValue?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(search))
        {
            _optionsStack.Widgets.Clear();
            _optionsStack.Widgets.Add(_optionsPanel);

            if (_lastCategory.NotNullNotEmpty())
                ShowPage(_lastCategory);
        }
        else
        {
            _searchPanel.Widgets.Clear();
            foreach ((string _, List<OptionItem> items) in _options)
            foreach (OptionItem item in items.Where(item => item.MatchesSearch(search)))
                _searchPanel.Widgets.Add(item);

            _optionsStack.Widgets.Clear();
            _optionsStack.Widgets.Add(_searchPanel);
        }
    }

    private static ButtonStyle _lastUsedButtonStylesheet = null!;
    private static ButtonStyle _tabButtonStyle = null!;

    private static ButtonBase2 ApplyTabStyleToButton(ButtonBase2 tabButton)
    {
        if (_tabButtonStyle == null! || _lastUsedButtonStylesheet != Stylesheet.Current.ButtonStyle)
        {
            _lastUsedButtonStylesheet = Stylesheet.Current.ButtonStyle;
            _tabButtonStyle = new ButtonStyle(_lastUsedButtonStylesheet)
            {
                Background = new SolidBrush(Color.Transparent),
                Border = new SolidBrush(new Color(0, 0, 0, MyraStyle.STANDARD_BORDER_ALPHA)),
                BorderThickness = new Thickness(0, 0, 1, 1),
                LabelStyle = { Font = MyraStyle.UiFont },
                OverBackground = new SolidBrush(new Color(0, 0, 0, 55)),
                PressedBackground = new SolidBrush(new Color(0, 0, 0, 155)),
                MinWidth = 150
            };
        }

        tabButton.ApplyButtonStyle(_tabButtonStyle);

        return tabButton;
    }

    private void ShowPage(string category)
    {
        _searchField.Text = string.Empty;
        _optionsStack.Widgets.Clear();
        _optionsStack.Widgets.Add(_optionsPanel);

        _optionsPanel.Widgets.Clear();

        _lastCategory = category;

        foreach (OptionItem optionItem in _options[category])
            _optionsPanel.Widgets.Add(optionItem);
    }

    private void SetupGeneralOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        const string generalKey = "General";

        if (!_options.ContainsKey(generalKey))
            _options.Add(generalKey, []);

        List<OptionItem> general = _options[generalKey];

        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;

        general.Add(OptionsFactory.CreateCheckboxOption(genLang.HighlightObjects, new Accessor<bool>(() => profile.HighlightGameObjects)));

        general.Add(OptionsFactory.CreateSpacer());

        general.Add(GetPathfindingSettingsGroup());

        general.Add(OptionsFactory.CreateSpacer());

        general.Add(new OptionItem(genLang.AlwaysRun, () => new CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => profile.AlwaysRun), genLang.AlwaysRun),
            OptionsFactory.CreateCheckboxOption(genLang.RunUnlessHidden, new Accessor<bool>(() => profile.AlwaysRunUnlessHidden))
        )));

        general.Add(OptionsFactory.CreateSpacer());

        general.Add(new OptionItem(genLang.AutoOpenDoors, () => new CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => profile.AutoOpenDoors), genLang.AutoOpenDoors),
            OptionsFactory.CreateCheckboxOption(genLang.AutoOpenPathfinding, new Accessor<bool>(() => profile.SmoothDoors))
        )));

        general.Add(OptionsFactory.CreateSpacer());

        general.Add(new OptionItem(genLang.AutoOpenCorpse, () => new CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => profile.AutoOpenCorpses), genLang.AutoOpenCorpse),
            OptionsFactory.CreateSliderOption(genLang.CorpseOpenDistance, 0, 5, profile.AutoOpenCorpseRange,
                f => profile.AutoOpenCorpseRange = (int)f).SetTags("corpse, loot"),
            OptionsFactory.CreateCheckboxOption(genLang.CorpseSkipEmpty, new Accessor<bool>(() => profile.SkipEmptyCorpse),
                    "Most servers don't send corpse contents until it's opened.\nEnabling this will make this feature not work on most servers."
                )
                .SetTags("corpse, loot"),
            OptionsFactory.CreateComboBox(genLang.CorpseOpenOptions, profile.CorpseOpenOptions, [
                genLang.CorpseOptNone, genLang.CorpseOptNotTarg,
                genLang.CorpseOptNotHiding, genLang.CorpseOptBoth
            ], i => profile.CorpseOpenOptions = i).SetTags("corpse, loot")
        )).SetTags("corpse, loot"));

        general.Add(OptionsFactory.CreateSpacer());

        general.Add(OptionsFactory.CreateCheckboxOption(genLang.OutRangeColor, new Accessor<bool>(() => profile.NoColorObjectsOutOfRange)));
        general.Add(OptionsFactory.CreateCheckboxOption(genLang.SallosEasyGrab, new Accessor<bool>(() => profile.SallosEasyGrab), genLang.SallosTooltip));
        general.Add(OptionsFactory.CreateCheckboxOption(genLang.ShowHouseContent, new Accessor<bool>(() => profile.ShowHouseContent), genLang.ClientVersionLimitedTooltip));
        general.Add(OptionsFactory.CreateCheckboxOption(genLang.SmoothBoat, new Accessor<bool>(() => profile.UseSmoothBoatMovement), genLang.ClientVersionLimitedTooltip));
        //general.Add(OptionsFactory.CreateCheckboxOption(, , b =>  = b));
    }

    private static OptionItem GetPathfindingSettingsGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.General lang = Language.Instance.GetModernOptionsGumpLanguage.GetGeneral;
        const string tags = "pathfinding, pathing, path";

        return new OptionItem(
            lang.Pathfinding,
            () => new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.EnablePathfind), lang.Pathfinding),
                OptionsFactory.CreateCheckboxOption(lang.ShiftPathfinding, new Accessor<bool>(() => profile.UseShiftToPathfind)).SetTags(tags),
                OptionsFactory.CreateCheckboxOption(lang.SingleClickPathfind, new Accessor<bool>(() => profile.PathfindSingleClick)).SetTags(tags)
            ),
            tags
        );
    }

    private void SetupMobileOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;

        if (!_options.ContainsKey("Mobiles")) _options.Add("Mobiles", []);
        List<OptionItem> mobiles = _options["Mobiles"];

        mobiles.Add(
            new OptionItem(
                genLang.ShowMobileHP,
                () => new CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.ShowMobilesHP), genLang.ShowMobileHP),
                    OptionsFactory.CreateComboBox(
                        genLang.MobileHPType,
                        profile.MobileHPType,
                        [genLang.HPTypePerc, genLang.HPTypeBar, genLang.HPTypeNBoth],
                        i => profile.MobileHPType = i
                    ).SetTags("hp, health, hit point"),
                    OptionsFactory.CreateComboBox(
                        genLang.HPShowWhen,
                        profile.MobileHPShowWhen,
                        [genLang.HPShowWhen_Always, genLang.HPShowWhen_Less100, genLang.HPShowWhen_Smart],
                        i => profile.MobileHPShowWhen = i
                    ).SetTags("hp, health, hit point")
                )
            ).SetTags("hp, health, hit point")
        );

        mobiles.Add(OptionsFactory.CreateSpacer());

        mobiles.Add(
            new OptionItem(
                genLang.HighlightPoisoned,
                () => new CheckBoxGroup(
                    new PropertyBinder(
                        new Accessor<bool>(() => profile.HighlightMobilesByPoisoned),
                        genLang.HighlightPoisoned
                    ),
                    OptionsFactory.CreateHuePicker(
                        genLang.PoisonHighlightColor,
                        profile.PoisonHue,
                        h => profile.PoisonHue = h
                    )
                )
            )
        );

        mobiles.Add(
            new OptionItem(
                genLang.HighlightPara,
                () => new CheckBoxGroup(
                    new PropertyBinder(
                        new Accessor<bool>(() => profile.HighlightMobilesByParalize),
                        genLang.HighlightPara
                    ),
                    OptionsFactory.CreateHuePicker(
                        genLang.ParaHighlightColor,
                        profile.ParalyzedHue,
                        h => profile.ParalyzedHue = h
                    )
                )
            )
        );

        mobiles.Add(
            new OptionItem(
                genLang.HighlightInvul,
                () => new CheckBoxGroup(
                    new PropertyBinder(
                        new Accessor<bool>(() => profile.HighlightMobilesByInvul),
                        genLang.HighlightInvul
                    ),
                    OptionsFactory.CreateHuePicker(
                        genLang.InvulHighlightColor,
                        profile.InvulnerableHue,
                        h => profile.InvulnerableHue = h
                    )
                )
            )
        );

        mobiles.Add(OptionsFactory.CreateSpacer());

        mobiles.Add(
            OptionsFactory.CreateCheckboxOption(
                genLang.IncomingMobiles,
                new Accessor<bool>(() => profile.ShowNewMobileNameIncoming)
            )
        );
        mobiles.Add(
            OptionsFactory.CreateCheckboxOption(
                genLang.IncomingCorpses,
                new Accessor<bool>(() => profile.ShowNewCorpseNameIncoming)
            )
        );

        mobiles.Add(OptionsFactory.CreateSpacer());

        mobiles.Add(
            OptionsFactory.CreateComboBox(
                genLang.AuraUnderFeet,
                profile.AuraUnderFeetType,
                [
                    genLang.AuraOptDisabled,
                    genLang.AuroOptWarmode,
                    genLang.AuraOptCtrlShift,
                    genLang.AuraOptAlways
                ],
                i => profile.AuraUnderFeetType = i
            )
        );

        mobiles.Add(
            new OptionItem(
                genLang.AuraForParty,
                () => new CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.PartyAura), genLang.AuraForParty),
                    OptionsFactory.CreateHuePicker(
                        genLang.AuraPartyColor,
                        profile.PartyAuraHue,
                        h => profile.PartyAuraHue = h
                    )
                )
            )
        );
    }

    private void SetupInterfaceOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;

        if (!_options.ContainsKey("Interface")) _options.Add("Interface", []);
        List<OptionItem> opt = _options["Interface"];

        opt.Add(OptionsFactory.CreateCheckboxOption(genLang.DisableTopMenu, new Accessor<bool>(() => profile.TopbarGumpIsDisabled),
            "The top menu is pretty vital in TazUO, we recommend leaving this unchecked."));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(genLang.AltForAnchorsGumps,
            new Accessor<bool>(() => profile.HoldDownKeyAltToCloseAnchored)));
        opt.Add(OptionsFactory.CreateCheckboxOption(genLang.AltToMoveGumps, new Accessor<bool>(() => profile.HoldAltToMoveGumps)));
        opt.Add(OptionsFactory.CreateCheckboxOption(genLang.CloseEntireAnchorWithRClick,
            new Accessor<bool>(() => profile.CloseAllAnchoredGumpsInGroupWithRightClick)));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(genLang.OriginalSkillsGump, new Accessor<bool>(() => profile.StandardSkillsGump)));
        opt.Add(OptionsFactory.CreateCheckboxOption(genLang.OldStatusGump, new Accessor<bool>(() => profile.UseOldStatusGump)));
        opt.Add(OptionsFactory.CreateCheckboxOption(genLang.PartyInviteGump, new Accessor<bool>(() => profile.PartyInviteGump)));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(
            new OptionItem(
                genLang.ModernHealthBars,
                () => new CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.CustomBarsToggled), genLang.ModernHealthBars),
                    OptionsFactory.CreateCheckboxOption(genLang.ModernHPBlackBG, new Accessor<bool>(() => profile.CBBlackBGToggled))
                )
            )
        );

        opt.Add(OptionsFactory.CreateCheckboxOption(genLang.SaveHPBars, new Accessor<bool>(() => profile.SaveHealthbars)));
        opt.Add(OptionsFactory.CreateComboBox(genLang.CloseHPGumpsWhen, profile.CloseHealthBarType, [
            genLang.CloseHPOptDisable, genLang.CloseHPOptOOR,
            genLang.CloseHPOptDead, genLang.CloseHPOptBoth
        ], b => profile.CloseHealthBarType = b));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateComboBox(genLang.GridLoot, profile.GridLootType, [
            genLang.GridLootOptDisable, genLang.GridLootOptOnly,
            genLang.GridLootOptBoth
        ], i => profile.GridLootType = i, "This is not the same as grid containers."));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(genLang.ShiftContext, new Accessor<bool>(() => profile.HoldShiftForContext)));
        opt.Add(OptionsFactory.CreateCheckboxOption(genLang.ShiftSplit, new Accessor<bool>(() => profile.HoldShiftToSplitStack)));
    }

    private void SetupMiscOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;
        const string miscKey = "Misc";

        if (!_options.ContainsKey(miscKey))
            _options.Add(miscKey, []);

        List<OptionItem> opt = _options[miscKey];

        opt.Add(
            new OptionItem(
                genLang.EnableCOT,
                () => new CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.UseCircleOfTransparency), genLang.EnableCOT),
                    OptionsFactory.CreateSliderOption(
                        genLang.COTDistance,
                        Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS,
                        Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS,
                        profile.CircleOfTransparencyRadius,
                        f => profile.CircleOfTransparencyRadius = (int)f
                    ).SetTags("cot, circle of transparency"),
                    OptionsFactory.CreateComboBox(
                        genLang.COTType,
                        profile.CircleOfTransparencyType,
                        [
                            genLang.COTTypeOptFull,
                            genLang.COTTypeOptGrad,
                            genLang.COTTypeOptModern
                        ],
                        i => profile.CircleOfTransparencyType = i
                    ).SetTags("cot, circle of transparency")
                )
            ).SetTags("cot, circle of transparency")
        );

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                genLang.HideScreenshotMessage,
                new Accessor<bool>(() => profile.HideScreenshotStoredInMessage)
            )
        );
        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                genLang.ObjFade,
                new Accessor<bool>(() => profile.UseObjectsFading)
            )
        );
        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                genLang.TextFade,
                new Accessor<bool>(() => profile.TextFading)
            )
        );
        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                genLang.CursorRange,
                new Accessor<bool>(() => profile.ShowTargetRangeIndicator)
            )
        );

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(
            new OptionItem(
                genLang.DragSelectHP,
                () => new CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.EnableDragSelect), genLang.DragSelectHP),
                    OptionsFactory.CreateComboBox(
                        genLang.DragKeyMod,
                        profile.DragSelectModifierKey,
                        [
                            genLang.SharedNone,
                            genLang.SharedCtrl,
                            genLang.SharedShift,
                            genLang.SharedAlt
                        ],
                        i => profile.DragSelectModifierKey = i
                    ),
                    OptionsFactory.CreateComboBox(
                        genLang.DragPlayersOnly,
                        profile.DragSelect_PlayersModifier,
                        [
                            genLang.SharedNone,
                            genLang.SharedCtrl,
                            genLang.SharedShift,
                            genLang.SharedAlt
                        ],
                        i => profile.DragSelect_PlayersModifier = i
                    ),
                    OptionsFactory.CreateComboBox(
                        genLang.DragMobsOnly,
                        profile.DragSelect_MonstersModifier,
                        [
                            genLang.SharedNone,
                            genLang.SharedCtrl,
                            genLang.SharedShift,
                            genLang.SharedAlt
                        ],
                        i => profile.DragSelect_MonstersModifier = i
                    ),
                    OptionsFactory.CreateComboBox(
                        genLang.DragNameplatesOnly,
                        profile.DragSelect_NameplateModifier,
                        [
                            genLang.SharedNone,
                            genLang.SharedCtrl,
                            genLang.SharedShift,
                            genLang.SharedAlt
                        ],
                        i => profile.DragSelect_NameplateModifier = i
                    ),
                    OptionsFactory.CreateInputField(
                        genLang.DragX,
                        profile.DragSelectStartX.ToString(),
                        s =>
                        {
                            if (int.TryParse(s, out int result))
                                profile.DragSelectStartX = result;
                        }
                    ),
                    OptionsFactory.CreateInputField(
                        genLang.DragY,
                        profile.DragSelectStartY.ToString(),
                        s =>
                        {
                            if (int.TryParse(s, out int result))
                                profile.DragSelectStartY = result;
                        }
                    )
                )
            )
        );

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                genLang.ShowStatsChangedMsg,
                new Accessor<bool>(() => profile.ShowStatsChangedMessage)
            )
        );
        opt.Add(
            new OptionItem(
                genLang.ShowSkillsChangedMsg,
                () => new CheckBoxGroup(
                    new PropertyBinder(
                        new Accessor<bool>(() => profile.ShowSkillsChangedMessage),
                        genLang.ShowSkillsChangedMsg
                    ),
                    OptionsFactory.CreateSliderOption(
                        genLang.ChangeVolume,
                        0,
                        100,
                        profile.ShowSkillsChangedDeltaValue,
                        f => profile.ShowSkillsChangedDeltaValue = (int)f
                    )
                )
            )
        );
    }

    private void SetupTerrainStatics()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        if (!_options.ContainsKey("Terrain & Statics")) _options.Add("Terrain & Statics", new List<OptionItem>());
        List<OptionItem> opt = _options["Terrain & Statics"];

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.HideRoof, !profile.DrawRoofs,
            b => profile.DrawRoofs = !b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.TreesToStump, new Accessor<bool>(() => profile.TreeToStumps)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.HideVegetation, new Accessor<bool>(() => profile.HideVegetation)));
        opt.Add(OptionsFactory.CreateComboBox(lang.GetGeneral.MagicFieldType, profile.FieldsType, [
            lang.GetGeneral.MagicFieldOpt_Normal, lang.GetGeneral.MagicFieldOpt_Static,
            lang.GetGeneral.MagicFieldOpt_Tile
        ], i => profile.FieldsType = i));
    }

    private void SetupSound()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Sound soundLang = lang.GetSound;

        if (!_options.ContainsKey("Sound"))
            _options.Add("Sound", []);

        List<OptionItem> opt = _options["Sound"];

        opt.Add(
            new OptionItem(
                soundLang.EnableSound,
                () => new CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.EnableSound), soundLang.EnableSound),
                    OptionsFactory.CreateSliderOption(
                        soundLang.SharedVolume,
                        0,
                        100,
                        profile.SoundVolume,
                        f => profile.SoundVolume = (int)f
                    )
                )
            )
        );

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(
            new OptionItem(
                soundLang.EnableMusic,
                () => new CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.EnableMusic), soundLang.EnableMusic),
                    OptionsFactory.CreateSliderOption(
                        soundLang.SharedVolume,
                        0,
                        100,
                        profile.MusicVolume,
                        f => profile.MusicVolume = (int)f
                    )
                )
            )
        );

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(
            new OptionItem(
                soundLang.LoginMusic,
                () => new CheckBoxGroup(
                    new PropertyBinder(
                        new Accessor<bool>(() => Settings.GlobalSettings.LoginMusic),
                        soundLang.LoginMusic
                    ),
                    OptionsFactory.CreateSliderOption(
                        soundLang.SharedVolume,
                        0,
                        100,
                        Settings.GlobalSettings.LoginMusicVolume,
                        f => Settings.GlobalSettings.LoginMusicVolume = (int)f
                    )
                )
            )
        );

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                soundLang.PlayFootsteps,
                new Accessor<bool>(() => profile.EnableFootstepsSound)
            )
        );
        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                soundLang.CombatMusic,
                new Accessor<bool>(() => profile.EnableCombatMusic)
            )
        );
        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                soundLang.BackgroundMusic,
                new Accessor<bool>(() => profile.ReproduceSoundsInBackground)
            )
        );

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(
            new OptionItem(
                "Voice to text",
                () => new MyraButton(
                    "Create voice toggle button",
                    () =>
                    {
                        var macroManager = MacroManager.TryGetMacroManager(World.Instance);
                        if (macroManager == null)
                            return;
                        var macro = Macro.CreateFastMacro(
                            "Toggle Voice",
                            MacroType.ToggleVoiceRecognition,
                            MacroSubType.MSC_NONE
                        );
                        macroManager.PushToBack(macro);
                        UIManager.Add(
                            new MacroButtonGump(
                                World.Instance,
                                macro,
                                Mouse.Position.X,
                                Mouse.Position.Y
                            )
                        );
                    }
                )
            )
        );
        ModernOptionsGumpLanguage.TazUO voiceLang = lang.GetTazUO;
        opt.Add(
            OptionsFactory.CreateInputField(
                voiceLang.VoiceModelPath,
                profile.VoiceModelPath,
                s => profile.VoiceModelPath = s,
                voiceLang.VoiceModelPathTooltip
            )
        );
    }

    private void SetupVideo()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Video videoLang = lang.GetVideo;
        const string videoKey = "Video";

        if (!_options.ContainsKey(videoKey))
            _options.Add(videoKey, []);

        List<OptionItem> optionsList = _options[videoKey];

        optionsList.Add(
            OptionsFactory.CreateSliderOption(
                videoLang.FPSCap,
                Constants.MIN_FPS,
                Constants.MAX_FPS,
                Settings.GlobalSettings.FPS,
                f =>
                {
                    Settings.GlobalSettings.FPS = (int)f;
                    Client.Game.SetRefreshRate((int)f);
                }
            )
        );
        optionsList.Add(
            OptionsFactory.CreateCheckboxOption(
                videoLang.BackgroundFPS,
                new Accessor<bool>(() => profile.ReduceFPSWhenInactive)
            )
        );
        optionsList.Add(
            OptionsFactory.CreateCheckboxOption(
                videoLang.EnableVSync,
                profile.EnableVSync,
                b =>
                {
                    profile.EnableVSync = b;
                    Client.Game?.SetVSync(b);
                }
            )
        );

        optionsList.Add(GetViewportSettingsGroup());

        optionsList.Add(OptionsFactory.CreateSpacer());

        int cameraZoomCount = (int)(
            (Client.Game.Scene.Camera.ZoomMax - Client.Game.Scene.Camera.ZoomMin)
            / Client.Game.Scene.Camera.ZoomStep
        );
        int cameraZoomIndex =
            cameraZoomCount
            - (int)(
                (Client.Game.Scene.Camera.ZoomMax - Client.Game.Scene.Camera.Zoom)
                / Client.Game.Scene.Camera.ZoomStep
            );

        optionsList.Add(
            OptionsFactory.CreateSliderOption(
                videoLang.DefaultZoom,
                0,
                cameraZoomCount,
                cameraZoomIndex,
                f =>
                {
                    profile.DefaultScale = Client.Game.Scene.Camera.Zoom =
                        (int)f * Client.Game.Scene.Camera.ZoomStep + Client.Game.Scene.Camera.ZoomMin;
                }
            )
        );
        optionsList.Add(
            OptionsFactory.CreateCheckboxOption(
                videoLang.ZoomWheel,
                new Accessor<bool>(() => profile.EnableMousewheelScaleZoom)
            )
        );
        optionsList.Add(
            OptionsFactory.CreateCheckboxOption(
                videoLang.ReturnDefaultZoom,
                new Accessor<bool>(() => profile.RestoreScaleAfterUnpressCtrl)
            )
        );

        optionsList.Add(OptionsFactory.CreateSpacer());

        optionsList.Add(
            OptionsFactory.CreateCheckboxOption(
                videoLang.AltLights,
                new Accessor<bool>(() => profile.UseAlternativeLights)
            )
        );
        optionsList.Add(
            new OptionItem(
                videoLang.CustomLLevel,
                () => new CheckBoxGroup(
                    new PropertyBinder(
                        new Accessor<bool>(
                            () => profile.UseCustomLightLevel,
                            b =>
                            {
                                profile.UseCustomLightLevel = b;

                                if (b)
                                {
                                    World.Instance.Light.Overall = profile.LightLevelType == 1
                                        ? Math.Min(World.Instance.Light.RealOverall, profile.LightLevel)
                                        : profile.LightLevel;
                                    World.Instance.Light.Personal = 0;
                                }
                                else
                                {
                                    World.Instance.Light.Overall = World.Instance.Light.RealOverall;
                                    World.Instance.Light.Personal = World.Instance.Light.RealPersonal;
                                }
                            }
                        ),
                        videoLang.CustomLLevel
                    ),
                    OptionsFactory.CreateSliderOption(
                        videoLang.Level,
                        0,
                        0x1E,
                        0x1E - profile.LightLevel,
                        f =>
                        {
                            profile.LightLevel = (byte)(0x1E - (int)f);

                            if (profile.UseCustomLightLevel)
                            {
                                World.Instance.Light.Overall = profile.LightLevelType == 1
                                    ? Math.Min(World.Instance.Light.RealOverall, profile.LightLevel)
                                    : profile.LightLevel;
                                World.Instance.Light.Personal = 0;
                            }
                            else
                            {
                                World.Instance.Light.Overall = World.Instance.Light.RealOverall;
                                World.Instance.Light.Personal = World.Instance.Light.RealPersonal;
                            }
                        }
                    ),
                    OptionsFactory.CreateComboBox(
                        videoLang.LightType,
                        profile.LightLevelType,
                        [videoLang.LightType_Absolute, videoLang.LightType_Minimum],
                        i => profile.LightLevelType = i
                    )
                )
            )
        );

        optionsList.Add(
            OptionsFactory.CreateCheckboxOption(
                videoLang.DarkNight,
                new Accessor<bool>(() => profile.UseDarkNights)
            )
        );
        optionsList.Add(
            OptionsFactory.CreateCheckboxOption(
                videoLang.ColoredLight,
                new Accessor<bool>(() => profile.UseColoredLights)
            )
        );

        optionsList.Add(OptionsFactory.CreateSpacer());

        optionsList.Add(
            OptionsFactory.CreateCheckboxOption(
                videoLang.EnableDeathScreen,
                new Accessor<bool>(() => profile.EnableDeathScreen)
            )
        );
        optionsList.Add(
            OptionsFactory.CreateCheckboxOption(
                videoLang.BWDead,
                new Accessor<bool>(() => profile.EnableBlackWhiteEffect)
            )
        );
        optionsList.Add(
            OptionsFactory.CreateCheckboxOption(
                videoLang.MouseThread,
                new Accessor<bool>(() => Settings.GlobalSettings.RunMouseInASeparateThread)
            )
        );
        optionsList.Add(
            OptionsFactory.CreateCheckboxOption(
                videoLang.TargetAura,
                new Accessor<bool>(() => profile.AuraOnMouse)
            )
        );
        optionsList.Add(
            OptionsFactory.CreateCheckboxOption(
                videoLang.AnimWater,
                new Accessor<bool>(() => profile.AnimatedWaterEffect)
            )
        );
        optionsList.Add(
            new OptionItem(
                "Enable post processing effects",
                () => new CheckBoxGroup(
                    new PropertyBinder(
                        new Accessor<bool>(
                            () => profile.EnablePostProcessingEffects,
                            b =>
                            {
                                profile.EnablePostProcessingEffects = b;
                                GameScene.Instance?.SetPostProcessingSettings();
                            }
                        ),
                        "Enable post processing effects"
                    ),
                    OptionsFactory.CreateComboBox(
                        "Processing type",
                        profile.PostProcessingType,
                        ["point", "linear", "anisotropic", "xbr"],
                        i =>
                        {
                            profile.PostProcessingType = (ushort)i;
                            GameScene.Instance?.SetPostProcessingSettings();
                        }
                    )
                )
            )
        );

        optionsList.Add(OptionsFactory.CreateSpacer());
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.EnableShadows, new Accessor<bool>(() => profile.ShadowsEnabled)));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.RockTreeShadows, new Accessor<bool>(() => profile.ShadowsStatics)));
        optionsList.Add(OptionsFactory.CreateSliderOption(lang.GetVideo.TerrainShadowLevel,
            Constants.MIN_TERRAIN_SHADOWS_LEVEL, Constants.MAX_TERRAIN_SHADOWS_LEVEL, profile.TerrainShadowsLevel,
            f => profile.TerrainShadowsLevel = (int)f));
    }

    private static OptionItem GetViewportSettingsGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return new OptionItem(
            lang.LabelViewport,
            () =>
                new VisualContainer(
                    new VisualContainerProps { LabelText = lang.LabelViewport },
                    OptionsFactory.CreateCheckboxOption(
                        lang.GetVideo.FullsizeViewport,
                        profile.GameWindowFullSize,
                        b =>
                        {
                            profile.GameWindowFullSize = b;

                            WorldViewportGump viewport = WorldViewportGump.Instance;
                            if (viewport == null) return;

                            if (b)
                            {
                                viewport.ResizeGameWindow(new Point(Client.Game.Window.ClientBounds.Width, Client.Game.Window.ClientBounds.Height));
                                viewport.SetGameWindowPosition(new Point(0, 0));
                                profile.GameWindowPosition = new Point(0, 0);
                            }
                            else
                            {
                                viewport.ResizeGameWindow(new Point(600, 480));
                                viewport.SetGameWindowPosition(new Point(25, 25));
                                profile.GameWindowPosition = new Point(25, 25);
                            }

                            // Trigger a full update to ensure borders and positioning are correct
                            viewport.OnWindowResized();
                        }
                    ),
                    OptionsFactory.CreateCheckboxOption(
                        lang.GetVideo.FullScreen,
                        profile.WindowBorderless,
                        b =>
                        {
                            profile.WindowBorderless = b;
                            Client.Game.SetWindowBorderless(b);
                        }
                    ),
                    OptionsFactory.CreateCheckboxOption(
                        lang.GetVideo.LockViewport,
                        new Accessor<bool>(() => profile.GameWindowLock)
                    ),
                    OptionsFactory.CreateSliderOption(
                        lang.GetVideo.ViewportX,
                        0,
                        Client.Game.Window.ClientBounds.Width,
                        profile.GameWindowPosition.X,
                        f =>
                        {
                            profile.GameWindowPosition = new Point((int)f, profile.GameWindowPosition.Y);
                            WorldViewportGump.Instance?.SetGameWindowPosition(profile.GameWindowPosition);
                        }
                    ),
                    OptionsFactory.CreateSliderOption(
                        lang.GetVideo.ViewportY,
                        0,
                        Client.Game.Window.ClientBounds.Height,
                        profile.GameWindowPosition.Y,
                        f =>
                        {
                            profile.GameWindowPosition = new Point(profile.GameWindowPosition.Y, (int)f);
                            WorldViewportGump.Instance?.SetGameWindowPosition(profile.GameWindowPosition);
                        }
                    ),
                    OptionsFactory.CreateSliderOption(
                        lang.GetVideo.ViewportW,
                        0,
                        Client.Game.Window.ClientBounds.Width,
                        profile.GameWindowSize.X,
                        f =>
                        {
                            profile.GameWindowSize = new Point((int)f, profile.GameWindowSize.Y);
                            WorldViewportGump.Instance?.SetGameWindowPosition(profile.GameWindowPosition);
                        }
                    ),
                    OptionsFactory.CreateSliderOption(
                        lang.GetVideo.ViewportH,
                        0,
                        Client.Game.Window.ClientBounds.Height,
                        profile.GameWindowSize.Y,
                        f =>
                        {
                            profile.GameWindowSize = new Point(profile.GameWindowSize.X, (int)f);
                            WorldViewportGump.Instance?.SetGameWindowPosition(profile.GameWindowPosition);
                        }
                    )
                )
        );
    }

    private void SetupInfoBarOptions()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        const string infoBarKey = "Info bar";

        if (!_options.ContainsKey(infoBarKey))
            _options.Add(infoBarKey, []);

        _options[infoBarKey].Add(new OptionItem(lang.GetInfoBars.InfoBar, InfoBarOptionsContent.Build));
    }

    private void SetupTooltipOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        const string tooltipKey = "Tooltips";

        if (!_options.ContainsKey(tooltipKey))
            _options.Add(tooltipKey, []);
        List<OptionItem> opt = _options[tooltipKey];

        opt.Add(
            new OptionItem(
                lang.LabelTooltips,
                () => new CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.UseTooltip), lang.GetToolTips.EnableToolTips),
                    OptionsFactory.CreateSliderOption(
                        lang.GetToolTips.ToolTipDelay, 0, 1000, profile.TooltipDelayBeforeDisplay,
                        f => profile.TooltipDelayBeforeDisplay = (int)f
                    ),
                    OptionsFactory.CreateSliderOption(
                        lang.GetToolTips.ToolTipBG, 0, 100, profile.TooltipBackgroundOpacity,
                        f => profile.TooltipBackgroundOpacity = (int)f
                    ),
                    OptionsFactory.CreateHuePicker(
                        lang.GetToolTips.ToolTipFont, profile.TooltipTextHue, h => profile.TooltipTextHue = h
                    )
                )
            )
        );
    }

    private void SetupSpeechOptions()
    {
        const string speechKey = "Speech";

        if (!_options.ContainsKey(speechKey))
            _options.Add(speechKey, []);
        List<OptionItem> opt = _options[speechKey];

        opt.Add(SpeechTab.GetContent());
    }

    private void SetupCombatOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        const string combatKey = "Combat";

        if (!_options.ContainsKey(combatKey))
            _options.Add(combatKey, []);

        List<OptionItem> opt = _options[combatKey];

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.HoldTabForCombat, new Accessor<bool>(() => profile.HoldDownKeyTab)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.QueryBeforeAttack, new Accessor<bool>(() => profile.EnabledCriminalActionQuery)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.QueryBeforeBeneficial, new Accessor<bool>(() => profile.EnabledBeneficialCriminalActionQuery)));

        opt.Add(GetSpellSettingsGroup());

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.ShowBuffDurationOnOldStyleBuffBar, new Accessor<bool>(() => profile.BuffBarTime)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.EnableDPSCounter, new Accessor<bool>(() => profile.ShowDPS)));

        opt.Add(GetEntityHueSettingsGroup());
    }

    private void SetupCounterOptions()
    {
        const string countersKey = "Counters";

        if (!_options.ContainsKey(countersKey))
            _options.Add(countersKey, []);

        List<OptionItem> options = _options[countersKey];

        options.Add(CountersTab.GetContent());
    }

    private static OptionItem GetSpellSettingsGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return new OptionItem(
            "spell",
            () => new VisualContainer(
                new VisualContainerProps { LabelText = lang.LabelSpells },
                new CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.EnabledSpellFormat), lang.GetCombatSpells.EnableOverheadSpellFormat),
                    OptionsFactory.CreateInputField(lang.GetCombatSpells.SpellOverheadFormat, profile.SpellDisplayFormat, s => profile.SpellDisplayFormat = s)
                ),
                OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.EnableOverheadSpellHue, new Accessor<bool>(() => profile.EnabledSpellHue)),
                OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.SingleClickForSpellIcons, new Accessor<bool>(() => profile.CastSpellsByOneClick)),
                OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.EnableFastSpellHotkeyAssigning, new Accessor<bool>(() => profile.FastSpellsAssign))
            )
        );
    }

    private static OptionItem GetEntityHueSettingsGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.CombatSpells spellLang = lang.GetCombatSpells;

        return new OptionItem(
            "hue",
            () => new VisualContainer(
                new VisualContainerProps { LabelText = lang.LabelHue },
                OptionsFactory.CreateHuePicker(spellLang.InnocentColor, profile.InnocentHue, b => profile.InnocentHue = b),
                OptionsFactory.CreateHuePicker(spellLang.BeneficialSpell, profile.BeneficHue, b => profile.BeneficHue = b),
                OptionsFactory.CreateHuePicker(spellLang.FriendColor, profile.FriendHue, b => profile.FriendHue = b),
                OptionsFactory.CreateHuePicker(spellLang.HarmfulSpell, profile.HarmfulHue, b => profile.HarmfulHue = b),
                OptionsFactory.CreateHuePicker(spellLang.Criminal, profile.CriminalHue, b => profile.CriminalHue = b),
                OptionsFactory.CreateHuePicker(spellLang.NeutralSpell, profile.NeutralHue, b => profile.NeutralHue = b),
                OptionsFactory.CreateHuePicker(spellLang.CanBeAttackedHue, profile.CanAttackHue, b => profile.CanAttackHue = b),
                OptionsFactory.CreateHuePicker(spellLang.Murderer, profile.MurdererHue, b => profile.MurdererHue = b),
                OptionsFactory.CreateHuePicker(spellLang.Enemy, profile.EnemyHue, b => profile.EnemyHue = b)
            )
        );
    }

    private void SetupContainerOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Containers containerLang = lang.GetContainers;

        if (!_options.ContainsKey("Containers"))
            _options.Add("Containers", []);

        List<OptionItem> opt = _options["Containers"];

        opt.Add(ContainersTab.GetContent());
        return;

        if (Client.Game.UO.Version >= ClientVersion.CV_705301)
        {
            opt.Add(
                OptionsFactory.CreateComboBox(
                    containerLang.CharacterBackpackStyle,
                    profile.BackpackStyle,
                    [
                        containerLang.BackpackOpt_Default,
                        containerLang.BackpackOpt_Suede,
                        containerLang.BackpackOpt_PolarBear,
                        containerLang.BackpackOpt_GhoulSkin
                    ],
                    i => profile.BackpackStyle = i
                )
            );
        }

        opt.Add(
            new OptionItem(
                containerLang.ContainerScale,
                () =>
                    new VisualContainer(
                        new VisualContainerProps { LabelText = containerLang.ContainerScale },
                        OptionsFactory.CreateSliderOption(
                            containerLang.ContainerScale,
                            Constants.MIN_CONTAINER_SIZE_PERC,
                            Constants.MAX_CONTAINER_SIZE_PERC,
                            profile.ContainersScale,
                            i =>
                            {
                                profile.ContainersScale = (byte)i;
                                UIManager.ContainerScale = (byte)i / 100f;
                                UIManager.ForEach<ContainerGump>(c => c.RequestUpdateContents());
                            }
                        ),
                        OptionsFactory.CreateCheckboxOption(
                            containerLang.AlsoScaleItems,
                            new Accessor<bool>(() => profile.ScaleItemsInsideContainers)
                        )
                    )
            )
        );

        if (Client.Game.UO.Version >= ClientVersion.CV_706000)
        {
            opt.Add(
                OptionsFactory.CreateCheckboxOption(
                    containerLang.UseLargeContainerGumps,
                    new Accessor<bool>(() => profile.UseLargeContainerGumps)
                )
            );
        }

        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                containerLang.DoubleClickToLootItemsInsideContainers,
                new Accessor<bool>(() => profile.DoubleClickToLootInsideContainers)
            )
        );
        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                containerLang.RelativeDragAndDropItemsInContainers,
                new Accessor<bool>(() => profile.RelativeDragAndDropItems)
            )
        );
        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                containerLang.HighlightContainerOnGroundWhenMouseIsOverAContainerGump,
                new Accessor<bool>(() => profile.HighlightContainerWhenSelected)
            )
        );
        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                containerLang.RecolorContainerGumpByWithContainerHue,
                new Accessor<bool>(() => profile.HueContainerGumps)
            )
        );

        opt.Add(
            new OptionItem(
                containerLang.OverrideContainerGumpLocations,
                () =>
                    new CheckBoxGroup(
                        new PropertyBinder(
                            new Accessor<bool>(() => profile.OverrideContainerLocation),
                            containerLang.OverrideContainerGumpLocations
                        ),
                        OptionsFactory.CreateComboBox(
                            containerLang.OverridePosition,
                            profile.OverrideContainerLocationSetting,
                            [
                                containerLang.PositionOpt_NearContainer,
                                containerLang.PositionOpt_TopRight,
                                containerLang.PositionOpt_LastDraggedPosition,
                                containerLang.RememberEachContainer
                            ],
                            i => profile.OverrideContainerLocationSetting = i
                        )
                    )
            )
        );

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(
            new OptionItem(
                containerLang.RebuildContainersTxt,
                () =>
                    new MyraButton(
                        containerLang.RebuildContainersTxt,
                        () => World.Instance.ContainerManager.BuildContainerFile(true)
                    )
            )
        );
    }

    private void SetupExperimentalOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Experimental experimentalLang = lang.GetExperimental;

        if (!_options.ContainsKey("Experimental"))
            _options.Add("Experimental", []);

        List<OptionItem> opt = _options["Experimental"];

        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                experimentalLang.DisableDefaultUoHotkeys,
                new Accessor<bool>(() => profile.DisableDefaultHotkeys)
            )
        );
        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                experimentalLang.DisableArrowsNumlockArrowsPlayerMovement,
                new Accessor<bool>(() => profile.DisableArrowBtn)
            )
        );
        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                experimentalLang.DisableTabToggleWarmode,
                new Accessor<bool>(() => profile.DisableTabBtn)
            )
        );
        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                experimentalLang.DisableCtrlQWMessageHistory,
                new Accessor<bool>(() => profile.DisableCtrlQWBtn)
            )
        );
        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                experimentalLang.DisableRightLeftClickAutoMove,
                new Accessor<bool>(() => profile.DisableAutoMove)
            )
        );
    }
}
