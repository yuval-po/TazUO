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
    private const int MAX_HEIGHT = 1000;
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

        general.Add(OptionsFactory.CreateCheckboxOption(genLang.HighlightObjects, profile.HighlightGameObjects,
            b => profile.HighlightGameObjects = b));

        general.Add(OptionsFactory.CreateSpacer());

        general.Add(GetPathfindingSettingsGroup());

        general.Add(OptionsFactory.CreateSpacer());

        general.Add(OptionsFactory.CreateCheckboxOption(genLang.AlwaysRun, profile.AlwaysRun,
            b => profile.AlwaysRun = b));
        general.Add(OptionsFactory.CreateCheckboxOption(genLang.RunUnlessHidden, profile.AlwaysRunUnlessHidden,
            b => profile.AlwaysRunUnlessHidden = b));

        general.Add(OptionsFactory.CreateSpacer());

        general.Add(OptionsFactory.CreateCheckboxOption(genLang.AutoOpenDoors, profile.AutoOpenDoors,
            b => profile.AutoOpenDoors = b));
        general.Add(OptionsFactory.CreateCheckboxOption(genLang.AutoOpenPathfinding, profile.SmoothDoors,
            b => profile.SmoothDoors = b));

        general.Add(OptionsFactory.CreateSpacer());

        general.Add(OptionsFactory
            .CreateCheckboxOption(genLang.AutoOpenCorpse, profile.AutoOpenCorpses, b => profile.AutoOpenCorpses = b)
            .SetTags("corpse, loot"));
        general.Add(OptionsFactory.CreateSliderOption(genLang.CorpseOpenDistance, 0, 5, profile.AutoOpenCorpseRange,
            f => profile.AutoOpenCorpseRange = (int)f).SetTags("corpse, loot"));
        general.Add(OptionsFactory.CreateCheckboxOption(genLang.CorpseSkipEmpty, profile.SkipEmptyCorpse,
                b => profile.SkipEmptyCorpse = b,
                "Most servers don't send corpse contents until it's opened.\nEnabling this will make this feature not work on most servers."
            )
            .SetTags("corpse, loot")
        );
        general.Add(OptionsFactory.CreateComboBox(genLang.CorpseOpenOptions, profile.CorpseOpenOptions, [
            genLang.CorpseOptNone, genLang.CorpseOptNotTarg,
            genLang.CorpseOptNotHiding, genLang.CorpseOptBoth
        ], i => profile.CorpseOpenOptions = i).SetTags("corpse, loot"));

        general.Add(OptionsFactory.CreateSpacer());

        general.Add(OptionsFactory.CreateCheckboxOption(genLang.OutRangeColor, profile.NoColorObjectsOutOfRange,
            b => profile.NoColorObjectsOutOfRange = b));
        general.Add(OptionsFactory.CreateCheckboxOption(genLang.SallosEasyGrab, profile.SallosEasyGrab,
            b => profile.SallosEasyGrab = b, genLang.SallosTooltip));
        general.Add(OptionsFactory.CreateCheckboxOption(genLang.ShowHouseContent, profile.ShowHouseContent,
            b => profile.ShowHouseContent = b, genLang.ClientVersionLimitedTooltip));
        general.Add(OptionsFactory.CreateCheckboxOption(genLang.SmoothBoat, profile.UseSmoothBoatMovement,
            b => profile.UseSmoothBoatMovement = b, genLang.ClientVersionLimitedTooltip));
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
                OptionsFactory.CreateCheckboxOption(lang.ShiftPathfinding, profile.UseShiftToPathfind, b => profile.UseShiftToPathfind = b).SetTags(tags),
                OptionsFactory.CreateCheckboxOption(lang.SingleClickPathfind, profile.PathfindSingleClick, b => profile.PathfindSingleClick = b).SetTags(tags)
            ),
            tags
        );
    }

    private void SetupMobileOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        if (!_options.ContainsKey("Mobiles")) _options.Add("Mobiles", new List<OptionItem>());
        List<OptionItem> mobiles = _options["Mobiles"];
        mobiles.Add(
            OptionsFactory.CreateCheckboxOption(lang.GetGeneral.ShowMobileHP, profile.ShowMobilesHP,
                    b => profile.ShowMobilesHP = b)
                .SetTags("hp, health, hit point"));
        mobiles.Add(OptionsFactory.CreateComboBox(lang.GetGeneral.MobileHPType, profile.MobileHPType,
            [lang.GetGeneral.HPTypePerc, lang.GetGeneral.HPTypeBar, lang.GetGeneral.HPTypeNBoth],
            i => profile.MobileHPType = i).SetTags("hp, health, hit point"));
        mobiles.Add(OptionsFactory.CreateComboBox(lang.GetGeneral.HPShowWhen, profile.MobileHPShowWhen,
            [lang.GetGeneral.HPShowWhen_Always, lang.GetGeneral.HPShowWhen_Less100, lang.GetGeneral.HPShowWhen_Smart],
            i => profile.MobileHPShowWhen = i).SetTags("hp, health, hit point"));

        mobiles.Add(OptionsFactory.CreateSpacer());

        mobiles.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.HighlightPoisoned,
            profile.HighlightMobilesByPoisoned,
            b => profile.HighlightMobilesByPoisoned = b));
        mobiles.Add(
            OptionsFactory.CreateHuePicker(lang.GetGeneral.PoisonHighlightColor, profile.PoisonHue,
                h => profile.PoisonHue = h));

        mobiles.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.HighlightPara,
            profile.HighlightMobilesByParalize,
            b => profile.HighlightMobilesByParalize = b));
        mobiles.Add(OptionsFactory.CreateHuePicker(lang.GetGeneral.ParaHighlightColor, profile.ParalyzedHue,
            h => profile.ParalyzedHue = h));

        mobiles.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.HighlightInvul, profile.HighlightMobilesByInvul,
            b => profile.HighlightMobilesByInvul = b));
        mobiles.Add(OptionsFactory.CreateHuePicker(lang.GetGeneral.InvulHighlightColor, profile.InvulnerableHue,
            h => profile.InvulnerableHue = h));

        mobiles.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.IncomingMobiles,
            profile.ShowNewMobileNameIncoming,
            b => profile.ShowNewMobileNameIncoming = b));
        mobiles.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.IncomingCorpses,
            profile.ShowNewCorpseNameIncoming,
            b => profile.ShowNewCorpseNameIncoming = b));

        mobiles.Add(OptionsFactory.CreateComboBox(lang.GetGeneral.AuraUnderFeet, profile.AuraUnderFeetType, [
                lang.GetGeneral.AuraOptDisabled, lang.GetGeneral.AuroOptWarmode,
                lang.GetGeneral.AuraOptCtrlShift, lang.GetGeneral.AuraOptAlways
            ],
            i => profile.AuraUnderFeetType = i));

        mobiles.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.AuraForParty, profile.PartyAura,
            b => profile.PartyAura = b));
        mobiles.Add(
            OptionsFactory.CreateHuePicker(lang.GetGeneral.AuraPartyColor, profile.PartyAuraHue,
                h => profile.PartyAuraHue = h));
    }

    private void SetupInterfaceOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        if (!_options.ContainsKey("Interface")) _options.Add("Interface", new List<OptionItem>());
        List<OptionItem> opt = _options["Interface"];

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.DisableTopMenu, profile.TopbarGumpIsDisabled,
            b => profile.TopbarGumpIsDisabled = b,
            "The top menu is pretty vital in TazUO, we recommend leaving this unchecked."));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.AltForAnchorsGumps,
            profile.HoldDownKeyAltToCloseAnchored,
            b => profile.HoldDownKeyAltToCloseAnchored = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.AltToMoveGumps, profile.HoldAltToMoveGumps,
            b => profile.HoldAltToMoveGumps = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.CloseEntireAnchorWithRClick,
            profile.CloseAllAnchoredGumpsInGroupWithRightClick,
            b => profile.CloseAllAnchoredGumpsInGroupWithRightClick = b));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.OriginalSkillsGump, profile.StandardSkillsGump,
            b => profile.StandardSkillsGump = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.OldStatusGump, profile.UseOldStatusGump,
            b => profile.UseOldStatusGump = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.PartyInviteGump, profile.PartyInviteGump,
            b => profile.PartyInviteGump = b));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.ModernHealthBars, profile.CustomBarsToggled,
            b => profile.CustomBarsToggled = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.ModernHPBlackBG, profile.CBBlackBGToggled,
            b => profile.CBBlackBGToggled = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.SaveHPBars, profile.SaveHealthbars,
            b => profile.SaveHealthbars = b));
        opt.Add(OptionsFactory.CreateComboBox(lang.GetGeneral.CloseHPGumpsWhen, profile.CloseHealthBarType, [
            lang.GetGeneral.CloseHPOptDisable, lang.GetGeneral.CloseHPOptOOR,
            lang.GetGeneral.CloseHPOptDead, lang.GetGeneral.CloseHPOptBoth
        ], b => profile.CloseHealthBarType = b));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateComboBox(lang.GetGeneral.GridLoot, profile.GridLootType, [
            lang.GetGeneral.GridLootOptDisable, lang.GetGeneral.GridLootOptOnly,
            lang.GetGeneral.GridLootOptBoth
        ], i => profile.GridLootType = i, "This is not the same as grid containers."));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.ShiftContext, profile.HoldShiftForContext,
            b => profile.HoldShiftForContext = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.ShiftSplit, profile.HoldShiftToSplitStack,
            b => profile.HoldShiftToSplitStack = b));
    }

    private void SetupMiscOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        const string miscKey = "Misc";

        if (!_options.ContainsKey(miscKey))
            _options.Add(miscKey, []);

        List<OptionItem> opt = _options[miscKey];

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.EnableCOT, profile.UseCircleOfTransparency,
            b => profile.UseCircleOfTransparency = b).SetTags("cot, circle of transparency"));
        opt.Add(OptionsFactory.CreateSliderOption(lang.GetGeneral.COTDistance,
            Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS,
            Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS, profile.CircleOfTransparencyRadius,
            f => profile.CircleOfTransparencyRadius = (int)f).SetTags("cot, circle of transparency"));
        opt.Add(OptionsFactory.CreateComboBox(lang.GetGeneral.COTType, profile.CircleOfTransparencyType, [
            lang.GetGeneral.COTTypeOptFull, lang.GetGeneral.COTTypeOptGrad,
            lang.GetGeneral.COTTypeOptModern
        ], i => profile.CircleOfTransparencyType = i).SetTags("cot, circle of transparency"));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.HideScreenshotMessage,
            profile.HideScreenshotStoredInMessage,
            b => profile.HideScreenshotStoredInMessage = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.ObjFade, profile.UseObjectsFading,
            b => profile.UseObjectsFading = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.TextFade, profile.TextFading,
            b => profile.TextFading = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.CursorRange, profile.ShowTargetRangeIndicator,
            b => profile.ShowTargetRangeIndicator = b));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.DragSelectHP, profile.EnableDragSelect,
            b => profile.EnableDragSelect = b));
        opt.Add(OptionsFactory.CreateComboBox(lang.GetGeneral.DragKeyMod, profile.DragSelectModifierKey, [
            lang.GetGeneral.SharedNone, lang.GetGeneral.SharedCtrl, lang.GetGeneral.SharedShift,
            lang.GetGeneral.SharedAlt
        ], i => profile.DragSelectModifierKey = i));
        opt.Add(OptionsFactory.CreateComboBox(lang.GetGeneral.DragPlayersOnly, profile.DragSelect_PlayersModifier, [
            lang.GetGeneral.SharedNone, lang.GetGeneral.SharedCtrl, lang.GetGeneral.SharedShift,
            lang.GetGeneral.SharedAlt
        ], i => profile.DragSelect_PlayersModifier = i));
        opt.Add(OptionsFactory.CreateComboBox(lang.GetGeneral.DragMobsOnly, profile.DragSelect_MonstersModifier, [
            lang.GetGeneral.SharedNone, lang.GetGeneral.SharedCtrl, lang.GetGeneral.SharedShift,
            lang.GetGeneral.SharedAlt
        ], i => profile.DragSelect_MonstersModifier = i));
        opt.Add(OptionsFactory.CreateComboBox(lang.GetGeneral.DragNameplatesOnly, profile.DragSelect_NameplateModifier,
        [
            lang.GetGeneral.SharedNone, lang.GetGeneral.SharedCtrl, lang.GetGeneral.SharedShift,
            lang.GetGeneral.SharedAlt
        ], i => profile.DragSelect_NameplateModifier = i));
        opt.Add(OptionsFactory.CreateInputField(lang.GetGeneral.DragX, profile.DragSelectStartX.ToString(), s =>
        {
            if (int.TryParse(s, out int result)) profile.DragSelectStartX = result;
        }));
        opt.Add(OptionsFactory.CreateInputField(lang.GetGeneral.DragY, profile.DragSelectStartY.ToString(), s =>
        {
            if (int.TryParse(s, out int result)) profile.DragSelectStartY = result;
        }));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.ShowStatsChangedMsg,
            profile.ShowStatsChangedMessage,
            b => profile.ShowStatsChangedMessage = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.ShowSkillsChangedMsg,
            profile.ShowSkillsChangedMessage,
            b => profile.ShowSkillsChangedMessage = b));
        opt.Add(OptionsFactory.CreateSliderOption(lang.GetGeneral.ChangeVolume, 0, 100,
            profile.ShowSkillsChangedDeltaValue,
            f => profile.ShowSkillsChangedDeltaValue = (int)f));
    }

    private void SetupTerrainStatics()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        if (!_options.ContainsKey("Terrain & Statics")) _options.Add("Terrain & Statics", new List<OptionItem>());
        List<OptionItem> opt = _options["Terrain & Statics"];

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.HideRoof, !profile.DrawRoofs,
            b => profile.DrawRoofs = !b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.TreesToStump, profile.TreeToStumps,
            b => profile.TreeToStumps = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.HideVegetation, profile.HideVegetation,
            b => profile.HideVegetation = b));
        opt.Add(OptionsFactory.CreateComboBox(lang.GetGeneral.MagicFieldType, profile.FieldsType, [
            lang.GetGeneral.MagicFieldOpt_Normal, lang.GetGeneral.MagicFieldOpt_Static,
            lang.GetGeneral.MagicFieldOpt_Tile
        ], i => profile.FieldsType = i));
    }

    private void SetupSound()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        if (!_options.ContainsKey("Sound"))
            _options.Add("Sound", []);

        List<OptionItem> opt = _options["Sound"];

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSound.EnableSound, profile.EnableSound,
            b => profile.EnableSound = b));
        opt.Add(OptionsFactory.CreateSliderOption(lang.GetSound.SharedVolume, 0, 100, profile.SoundVolume,
            f => profile.SoundVolume = (int)f));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSound.EnableMusic, profile.EnableMusic,
            b => profile.EnableMusic = b));
        opt.Add(OptionsFactory.CreateSliderOption(lang.GetSound.SharedVolume, 0, 100, profile.MusicVolume,
            f => profile.MusicVolume = (int)f));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSound.LoginMusic, Settings.GlobalSettings.LoginMusic,
            b => Settings.GlobalSettings.LoginMusic = b));
        opt.Add(OptionsFactory.CreateSliderOption(lang.GetSound.SharedVolume, 0, 100,
            Settings.GlobalSettings.LoginMusicVolume, f => Settings.GlobalSettings.LoginMusicVolume = (int)f));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSound.PlayFootsteps, profile.EnableFootstepsSound,
            b => profile.EnableFootstepsSound = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSound.CombatMusic, profile.EnableCombatMusic,
            b => profile.EnableCombatMusic = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSound.BackgroundMusic, profile.ReproduceSoundsInBackground,
            b => profile.ReproduceSoundsInBackground = b));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(new OptionItem("Voice to text", () => new MyraButton("Create voice toggle button", () =>
        {
            var macroManager = MacroManager.TryGetMacroManager(World.Instance);
            if (macroManager == null) return;
            var macro = Macro.CreateFastMacro("Toggle Voice", MacroType.ToggleVoiceRecognition,
                MacroSubType.MSC_NONE);
            macroManager.PushToBack(macro);
            UIManager.Add(new MacroButtonGump(World.Instance, macro, Mouse.Position.X, Mouse.Position.Y));
        })));
        ModernOptionsGumpLanguage.TazUO voiceLang = lang.GetTazUO;
        opt.Add(OptionsFactory.CreateInputField(voiceLang.VoiceModelPath, profile.VoiceModelPath,
            s => profile.VoiceModelPath = s, voiceLang.VoiceModelPathTooltip));
    }

    private void SetupVideo()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        const string videoKey = "Video";

        if (!_options.ContainsKey(videoKey))
            _options.Add(videoKey, []);

        List<OptionItem> optionsList = _options[videoKey];

        optionsList.Add(OptionsFactory.CreateSliderOption(lang.GetVideo.FPSCap, Constants.MIN_FPS, Constants.MAX_FPS,
            Settings.GlobalSettings.FPS,
            f =>
            {
                Settings.GlobalSettings.FPS = (int)f;
                Client.Game.SetRefreshRate((int)f);
            }));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.BackgroundFPS, profile.ReduceFPSWhenInactive,
            b => profile.ReduceFPSWhenInactive = b));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.EnableVSync, profile.EnableVSync, b =>
        {
            profile.EnableVSync = b;
            Client.Game?.SetVSync(b);
        }));

        optionsList.Add(GetViewportSettingsGroup());

        optionsList.Add(OptionsFactory.CreateSpacer());

        int cameraZoomCount = (int)((Client.Game.Scene.Camera.ZoomMax - Client.Game.Scene.Camera.ZoomMin) /
                                    Client.Game.Scene.Camera.ZoomStep);
        int cameraZoomIndex = cameraZoomCount -
                              (int)((Client.Game.Scene.Camera.ZoomMax - Client.Game.Scene.Camera.Zoom) /
                                    Client.Game.Scene.Camera.ZoomStep);

        optionsList.Add(OptionsFactory.CreateSliderOption(lang.GetVideo.DefaultZoom, 0, cameraZoomCount,
            cameraZoomIndex, f =>
            {
                profile.DefaultScale = Client.Game.Scene.Camera.Zoom =
                    (int)f * Client.Game.Scene.Camera.ZoomStep + Client.Game.Scene.Camera.ZoomMin;
            }));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.ZoomWheel, profile.EnableMousewheelScaleZoom,
            b => profile.EnableMousewheelScaleZoom = b));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.ReturnDefaultZoom,
            profile.RestoreScaleAfterUnpressCtrl, b => profile.RestoreScaleAfterUnpressCtrl = b));

        optionsList.Add(OptionsFactory.CreateSpacer());

        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.AltLights, profile.UseAlternativeLights,
            b => profile.UseAlternativeLights = b));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.CustomLLevel, profile.UseCustomLightLevel,
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
            }));
        optionsList.Add(OptionsFactory.CreateSliderOption(lang.GetVideo.Level, 0, 0x1E, 0x1E - profile.LightLevel, f =>
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
        }));
        optionsList.Add(OptionsFactory.CreateComboBox(lang.GetVideo.LightType, profile.LightLevelType, [
            lang.GetVideo.LightType_Absolute, lang.GetVideo.LightType_Minimum
        ], i => profile.LightLevelType = i));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.DarkNight, profile.UseDarkNights,
            b => profile.UseDarkNights = b));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.ColoredLight, profile.UseColoredLights,
            b => profile.UseColoredLights = b));

        optionsList.Add(OptionsFactory.CreateSpacer());

        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.EnableDeathScreen, profile.EnableDeathScreen,
            b => profile.EnableDeathScreen = b));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.BWDead, profile.EnableBlackWhiteEffect,
            b => profile.EnableBlackWhiteEffect = b));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.MouseThread,
            Settings.GlobalSettings.RunMouseInASeparateThread,
            b => Settings.GlobalSettings.RunMouseInASeparateThread = b));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.TargetAura, profile.AuraOnMouse,
            b => profile.AuraOnMouse = b));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.AnimWater, profile.AnimatedWaterEffect,
            b => profile.AnimatedWaterEffect = b));
        optionsList.Add(OptionsFactory.CreateCheckboxOption("Enable post processing effects",
            profile.EnablePostProcessingEffects, b =>
            {
                profile.EnablePostProcessingEffects = b;
                GameScene.Instance?.SetPostProcessingSettings();
            }));
        optionsList.Add(OptionsFactory.CreateComboBox("Processing type", profile.PostProcessingType,
            ["point", "linear", "anisotropic", "xbr"], i =>
            {
                profile.PostProcessingType = (ushort)i;
                GameScene.Instance?.SetPostProcessingSettings();
            }));

        optionsList.Add(OptionsFactory.CreateSpacer());
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.EnableShadows, profile.ShadowsEnabled,
            b => profile.ShadowsEnabled = b));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.RockTreeShadows, profile.ShadowsStatics,
            b => profile.ShadowsStatics = b));
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
                        profile.GameWindowLock,
                        b => profile.GameWindowLock = b
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

        opt.Add(OptionsFactory.CreateSliderOption(lang.GetToolTips.ToolTipDelay, 0, 1000,
            profile.TooltipDelayBeforeDisplay, f => profile.TooltipDelayBeforeDisplay = (int)f));
        opt.Add(OptionsFactory.CreateSliderOption(lang.GetToolTips.ToolTipBG, 0, 100, profile.TooltipBackgroundOpacity,
            f => profile.TooltipBackgroundOpacity = (int)f));
        opt.Add(OptionsFactory.CreateHuePicker(lang.GetToolTips.ToolTipFont, profile.TooltipTextHue,
            h => profile.TooltipTextHue = h));
    }

    private void SetupSpeechOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        const string speechKey = "Speech";

        if (!_options.ContainsKey(speechKey))
            _options.Add(speechKey, []);
        List<OptionItem> opt = _options[speechKey];

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSpeech.ScaleSpeechDelay, profile.ScaleSpeechDelay,
            b => profile.ScaleSpeechDelay = b));
        opt.Add(OptionsFactory.CreateSliderOption(lang.GetSpeech.SpeechDelay, 0, 1000, profile.SpeechDelay,
            f => profile.SpeechDelay = (int)f));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSpeech.SaveJournalE, profile.SaveJournalToFile,
            b => profile.SaveJournalToFile = b));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSpeech.ChatEnterActivation, profile.ActivateChatAfterEnter,
            b => profile.ActivateChatAfterEnter = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSpeech.ChatEnterSpecial,
            profile.ActivateChatAdditionalButtons, b => profile.ActivateChatAdditionalButtons = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSpeech.ShiftEnterChat,
            profile.ActivateChatShiftEnterSupport, b => profile.ActivateChatShiftEnterSupport = b));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSpeech.ChatGradient, profile.HideChatGradient,
            b => profile.HideChatGradient = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSpeech.HideGuildChat, profile.IgnoreGuildMessages,
            b => profile.IgnoreGuildMessages = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSpeech.HideAllianceChat, profile.IgnoreAllianceMessages,
            b => profile.IgnoreAllianceMessages = b));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(GetSpeechSettingGroups());
    }

    private static OptionItem GetSpeechSettingGroups()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return new OptionItem(
            "speech",
            () => new VisualContainer(
                new VisualContainerProps { LabelText = lang.LabelSpeech },
                OptionsFactory.CreateHuePicker(lang.GetSpeech.SpeechColor, profile.SpeechHue,
                    b => profile.SpeechHue = b),
                OptionsFactory.CreateHuePicker(lang.GetSpeech.YellColor, profile.YellHue, b => profile.YellHue = b),
                OptionsFactory.CreateHuePicker(lang.GetSpeech.PartyColor, profile.PartyMessageHue,
                    b => profile.PartyMessageHue = b),
                OptionsFactory.CreateHuePicker(lang.GetSpeech.AllianceColor, profile.AllyMessageHue,
                    b => profile.AllyMessageHue = b),
                OptionsFactory.CreateHuePicker(lang.GetSpeech.EmoteColor, profile.EmoteHue, b => profile.EmoteHue = b),
                OptionsFactory.CreateHuePicker(lang.GetSpeech.WhisperColor, profile.WhisperHue,
                    b => profile.WhisperHue = b),
                OptionsFactory.CreateHuePicker(lang.GetSpeech.GuildColor, profile.GuildMessageHue,
                    b => profile.GuildMessageHue = b),
                OptionsFactory.CreateHuePicker(lang.GetSpeech.CharColor, profile.ChatMessageHue,
                    b => profile.ChatMessageHue = b)
            )
        );
    }

    private void SetupCombatOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        const string combatKey = "Combat";

        if (!_options.ContainsKey(combatKey))
            _options.Add(combatKey, []);

        List<OptionItem> opt = _options[combatKey];

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.HoldTabForCombat, profile.HoldDownKeyTab,
            b => profile.HoldDownKeyTab = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.QueryBeforeAttack,
            profile.EnabledCriminalActionQuery, b => profile.EnabledCriminalActionQuery = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.QueryBeforeBeneficial,
            profile.EnabledBeneficialCriminalActionQuery, b => profile.EnabledBeneficialCriminalActionQuery = b));
        opt.Add(GetSpellSettingsGroup());

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.EnableOverheadSpellHue,
            profile.EnabledSpellHue, b => profile.EnabledSpellHue = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.SingleClickForSpellIcons,
            profile.CastSpellsByOneClick, b => profile.CastSpellsByOneClick = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.ShowBuffDurationOnOldStyleBuffBar,
            profile.BuffBarTime, b => profile.BuffBarTime = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.EnableFastSpellHotkeyAssigning,
            profile.FastSpellsAssign, b => profile.FastSpellsAssign = b));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.EnableDPSCounter, profile.ShowDPS,
            b => profile.ShowDPS = b));

        opt.Add(GetEntityHueSettingsGroup());
    }

    private static OptionItem GetSpellSettingsGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return new OptionItem(
            "spell",
            () => new VisualContainer(
                new VisualContainerProps { LabelText = lang.LabelSpells },
                OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.EnableOverheadSpellFormat,
                    profile.EnabledSpellFormat, b => profile.EnabledSpellFormat = b),
                OptionsFactory.CreateInputField(lang.GetCombatSpells.SpellOverheadFormat, profile.SpellDisplayFormat,
                    s => profile.SpellDisplayFormat = s)
            )
        );
    }

    private static OptionItem GetEntityHueSettingsGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return new OptionItem(
            "hue",
            () => new VisualContainer(
                new VisualContainerProps { LabelText = lang.LabelHue },
                OptionsFactory.CreateHuePicker(lang.GetCombatSpells.InnocentColor, profile.InnocentHue,
                    b => profile.InnocentHue = b),
                OptionsFactory.CreateHuePicker(lang.GetCombatSpells.BeneficialSpell, profile.BeneficHue,
                    b => profile.BeneficHue = b),
                OptionsFactory.CreateHuePicker(lang.GetCombatSpells.FriendColor, profile.FriendHue,
                    b => profile.FriendHue = b),
                OptionsFactory.CreateHuePicker(lang.GetCombatSpells.HarmfulSpell, profile.HarmfulHue,
                    b => profile.HarmfulHue = b),
                OptionsFactory.CreateHuePicker(lang.GetCombatSpells.Criminal, profile.CriminalHue,
                    b => profile.CriminalHue = b),
                OptionsFactory.CreateHuePicker(lang.GetCombatSpells.NeutralSpell, profile.NeutralHue,
                    b => profile.NeutralHue = b),
                OptionsFactory.CreateHuePicker(lang.GetCombatSpells.CanBeAttackedHue, profile.CanAttackHue,
                    b => profile.CanAttackHue = b),
                OptionsFactory.CreateHuePicker(lang.GetCombatSpells.Murderer, profile.MurdererHue,
                    b => profile.MurdererHue = b),
                OptionsFactory.CreateHuePicker(lang.GetCombatSpells.Enemy, profile.EnemyHue, b => profile.EnemyHue = b)
            )
        );
    }
}
