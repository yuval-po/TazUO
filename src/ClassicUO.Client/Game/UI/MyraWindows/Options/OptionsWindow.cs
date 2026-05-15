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
        SetupCounterOptions();
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

        general.Add(OptionsFactory.CreateCheckboxOption(genLang.AlwaysRun, new Accessor<bool>(() => profile.AlwaysRun)));
        general.Add(OptionsFactory.CreateCheckboxOption(genLang.RunUnlessHidden, new Accessor<bool>(() => profile.AlwaysRunUnlessHidden)));

        general.Add(OptionsFactory.CreateSpacer());

        general.Add(OptionsFactory.CreateCheckboxOption(genLang.AutoOpenDoors, new Accessor<bool>(() => profile.AutoOpenDoors)));
        general.Add(OptionsFactory.CreateCheckboxOption(genLang.AutoOpenPathfinding, new Accessor<bool>(() => profile.SmoothDoors)));

        general.Add(OptionsFactory.CreateSpacer());

        general.Add(OptionsFactory
            .CreateCheckboxOption(genLang.AutoOpenCorpse, new Accessor<bool>(() => profile.AutoOpenCorpses))
            .SetTags("corpse, loot"));
        general.Add(OptionsFactory.CreateSliderOption(genLang.CorpseOpenDistance, 0, 5, profile.AutoOpenCorpseRange,
            f => profile.AutoOpenCorpseRange = (int)f).SetTags("corpse, loot"));
        general.Add(OptionsFactory.CreateCheckboxOption(genLang.CorpseSkipEmpty, new Accessor<bool>(() => profile.SkipEmptyCorpse),
                "Most servers don't send corpse contents until it's opened.\nEnabling this will make this feature not work on most servers."
            )
            .SetTags("corpse, loot")
        );
        general.Add(OptionsFactory.CreateComboBox(genLang.CorpseOpenOptions, profile.CorpseOpenOptions, [
            genLang.CorpseOptNone, genLang.CorpseOptNotTarg,
            genLang.CorpseOptNotHiding, genLang.CorpseOptBoth
        ], i => profile.CorpseOpenOptions = i).SetTags("corpse, loot"));

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

        if (!_options.ContainsKey("Mobiles")) _options.Add("Mobiles", new List<OptionItem>());
        List<OptionItem> mobiles = _options["Mobiles"];
        mobiles.Add(
            OptionsFactory.CreateCheckboxOption(lang.GetGeneral.ShowMobileHP, new Accessor<bool>(() => profile.ShowMobilesHP))
                .SetTags("hp, health, hit point"));
        mobiles.Add(OptionsFactory.CreateComboBox(lang.GetGeneral.MobileHPType, profile.MobileHPType,
            [lang.GetGeneral.HPTypePerc, lang.GetGeneral.HPTypeBar, lang.GetGeneral.HPTypeNBoth],
            i => profile.MobileHPType = i).SetTags("hp, health, hit point"));
        mobiles.Add(OptionsFactory.CreateComboBox(lang.GetGeneral.HPShowWhen, profile.MobileHPShowWhen,
            [lang.GetGeneral.HPShowWhen_Always, lang.GetGeneral.HPShowWhen_Less100, lang.GetGeneral.HPShowWhen_Smart],
            i => profile.MobileHPShowWhen = i).SetTags("hp, health, hit point"));

        mobiles.Add(OptionsFactory.CreateSpacer());

        mobiles.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.HighlightPoisoned,
            new Accessor<bool>(() => profile.HighlightMobilesByPoisoned)));
        mobiles.Add(
            OptionsFactory.CreateHuePicker(lang.GetGeneral.PoisonHighlightColor, profile.PoisonHue,
                h => profile.PoisonHue = h));

        mobiles.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.HighlightPara,
            new Accessor<bool>(() => profile.HighlightMobilesByParalize)));
        mobiles.Add(OptionsFactory.CreateHuePicker(lang.GetGeneral.ParaHighlightColor, profile.ParalyzedHue,
            h => profile.ParalyzedHue = h));

        mobiles.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.HighlightInvul, new Accessor<bool>(() => profile.HighlightMobilesByInvul)));
        mobiles.Add(OptionsFactory.CreateHuePicker(lang.GetGeneral.InvulHighlightColor, profile.InvulnerableHue,
            h => profile.InvulnerableHue = h));

        mobiles.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.IncomingMobiles,
            new Accessor<bool>(() => profile.ShowNewMobileNameIncoming)));
        mobiles.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.IncomingCorpses,
            new Accessor<bool>(() => profile.ShowNewCorpseNameIncoming)));

        mobiles.Add(OptionsFactory.CreateComboBox(lang.GetGeneral.AuraUnderFeet, profile.AuraUnderFeetType, [
                lang.GetGeneral.AuraOptDisabled, lang.GetGeneral.AuroOptWarmode,
                lang.GetGeneral.AuraOptCtrlShift, lang.GetGeneral.AuraOptAlways
            ],
            i => profile.AuraUnderFeetType = i));

        mobiles.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.AuraForParty, new Accessor<bool>(() => profile.PartyAura)));
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

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.DisableTopMenu, new Accessor<bool>(() => profile.TopbarGumpIsDisabled),
            "The top menu is pretty vital in TazUO, we recommend leaving this unchecked."));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.AltForAnchorsGumps,
            new Accessor<bool>(() => profile.HoldDownKeyAltToCloseAnchored)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.AltToMoveGumps, new Accessor<bool>(() => profile.HoldAltToMoveGumps)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.CloseEntireAnchorWithRClick,
            new Accessor<bool>(() => profile.CloseAllAnchoredGumpsInGroupWithRightClick)));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.OriginalSkillsGump, new Accessor<bool>(() => profile.StandardSkillsGump)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.OldStatusGump, new Accessor<bool>(() => profile.UseOldStatusGump)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.PartyInviteGump, new Accessor<bool>(() => profile.PartyInviteGump)));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.ModernHealthBars, new Accessor<bool>(() => profile.CustomBarsToggled)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.ModernHPBlackBG, new Accessor<bool>(() => profile.CBBlackBGToggled)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.SaveHPBars, new Accessor<bool>(() => profile.SaveHealthbars)));
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

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.ShiftContext, new Accessor<bool>(() => profile.HoldShiftForContext)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.ShiftSplit, new Accessor<bool>(() => profile.HoldShiftToSplitStack)));
    }

    private void SetupMiscOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        const string miscKey = "Misc";

        if (!_options.ContainsKey(miscKey))
            _options.Add(miscKey, []);

        List<OptionItem> opt = _options[miscKey];

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.EnableCOT, new Accessor<bool>(() => profile.UseCircleOfTransparency))
            .SetTags("cot, circle of transparency"));
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
            new Accessor<bool>(() => profile.HideScreenshotStoredInMessage)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.ObjFade, new Accessor<bool>(() => profile.UseObjectsFading)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.TextFade, new Accessor<bool>(() => profile.TextFading)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.CursorRange, new Accessor<bool>(() => profile.ShowTargetRangeIndicator)));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.DragSelectHP, new Accessor<bool>(() => profile.EnableDragSelect)));
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
            new Accessor<bool>(() => profile.ShowStatsChangedMessage)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetGeneral.ShowSkillsChangedMsg,
            new Accessor<bool>(() => profile.ShowSkillsChangedMessage)));
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

        if (!_options.ContainsKey("Sound"))
            _options.Add("Sound", []);

        List<OptionItem> opt = _options["Sound"];

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSound.EnableSound, new Accessor<bool>(() => profile.EnableSound)));
        opt.Add(OptionsFactory.CreateSliderOption(lang.GetSound.SharedVolume, 0, 100, profile.SoundVolume,
            f => profile.SoundVolume = (int)f));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSound.EnableMusic, new Accessor<bool>(() => profile.EnableMusic)));
        opt.Add(OptionsFactory.CreateSliderOption(lang.GetSound.SharedVolume, 0, 100, profile.MusicVolume,
            f => profile.MusicVolume = (int)f));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSound.LoginMusic, new Accessor<bool>(() => Settings.GlobalSettings.LoginMusic)));
        opt.Add(OptionsFactory.CreateSliderOption(lang.GetSound.SharedVolume, 0, 100,
            Settings.GlobalSettings.LoginMusicVolume, f => Settings.GlobalSettings.LoginMusicVolume = (int)f));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSound.PlayFootsteps, new Accessor<bool>(() => profile.EnableFootstepsSound)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSound.CombatMusic, new Accessor<bool>(() => profile.EnableCombatMusic)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSound.BackgroundMusic, new Accessor<bool>(() => profile.ReproduceSoundsInBackground)));

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
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.BackgroundFPS, new Accessor<bool>(() => profile.ReduceFPSWhenInactive)));
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
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.ZoomWheel, new Accessor<bool>(() => profile.EnableMousewheelScaleZoom)));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.ReturnDefaultZoom,
            new Accessor<bool>(() => profile.RestoreScaleAfterUnpressCtrl)));

        optionsList.Add(OptionsFactory.CreateSpacer());

        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.AltLights, new Accessor<bool>(() => profile.UseAlternativeLights)));
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
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.DarkNight, new Accessor<bool>(() => profile.UseDarkNights)));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.ColoredLight, new Accessor<bool>(() => profile.UseColoredLights)));

        optionsList.Add(OptionsFactory.CreateSpacer());

        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.EnableDeathScreen, new Accessor<bool>(() => profile.EnableDeathScreen)));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.BWDead, new Accessor<bool>(() => profile.EnableBlackWhiteEffect)));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.MouseThread,
            new Accessor<bool>(() => Settings.GlobalSettings.RunMouseInASeparateThread)));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.TargetAura, new Accessor<bool>(() => profile.AuraOnMouse)));
        optionsList.Add(OptionsFactory.CreateCheckboxOption(lang.GetVideo.AnimWater, new Accessor<bool>(() => profile.AnimatedWaterEffect)));
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

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSpeech.ScaleSpeechDelay, new Accessor<bool>(() => profile.ScaleSpeechDelay)));
        opt.Add(OptionsFactory.CreateSliderOption(lang.GetSpeech.SpeechDelay, 0, 1000, profile.SpeechDelay,
            f => profile.SpeechDelay = (int)f));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSpeech.SaveJournalE, new Accessor<bool>(() => profile.SaveJournalToFile)));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSpeech.ChatEnterActivation, new Accessor<bool>(() => profile.ActivateChatAfterEnter)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSpeech.ChatEnterSpecial,
            new Accessor<bool>(() => profile.ActivateChatAdditionalButtons)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSpeech.ShiftEnterChat,
            new Accessor<bool>(() => profile.ActivateChatShiftEnterSupport)));

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSpeech.ChatGradient, new Accessor<bool>(() => profile.HideChatGradient)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSpeech.HideGuildChat, new Accessor<bool>(() => profile.IgnoreGuildMessages)));
        opt.Add(OptionsFactory.CreateCheckboxOption(lang.GetSpeech.HideAllianceChat, new Accessor<bool>(() => profile.IgnoreAllianceMessages)));

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

        options.Add(GetCountersSettingsSection());
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

    private static OptionItem GetCountersSettingsSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Counters counterLang = lang.GetCounters;

        return new OptionItem(
            "counters",
            () => new VisualContainer(
                new VisualContainerProps { LabelText = lang.LabelCounters },
                new CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.CounterBarEnabled), counterLang.EnableCounters),
                    OptionsFactory.CreateCheckboxOption(counterLang.HighlightItemsOnUse, new Accessor<bool>(() => profile.CounterBarHighlightOnUse)),
                    new CheckBoxGroup(
                        new PropertyBinder(new Accessor<bool>(() => profile.CounterBarDisplayAbbreviatedAmount), counterLang.AbbreviatedValues),
                        new LabeledIntegerInput(
                            counterLang.AbbreviateIfAmountExceeds,
                            new Accessor<int>(() => profile.CounterBarAbbreviatedAmount)
                        ) { InputBoxWidth = 80, MinValue = 999, MaxValue = 999999999 }
                    ),
                    new CheckBoxGroup(
                        new PropertyBinder(new Accessor<bool>(() => profile.CounterBarHighlightOnAmount), counterLang.HighlightRedWhenAmountIsLow),
                        new LabeledIntegerInput(
                            counterLang.HighlightRedIfAmountIsBelow,
                            new Accessor<int>(() => profile.CounterBarHighlightAmount)
                        ) { InputBoxWidth = 80, MinValue = 1, MaxValue = 60000 }
                    )
                )
            )
        );
    }
}
