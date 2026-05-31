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
    private const int MAX_HEIGHT = 880;
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
        SetupVideo();
        SetupSound();
        SetupGameplayTab();
        SetupMobileOptions();
        SetupInterfaceOptions();
        SetupMiscOptions();
        SetupChatOptions();
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
    }

    private void SetupMobileOptions()
    {
        const string mobilesKey = "Mobiles";
        if (!_options.ContainsKey(mobilesKey)) _options.Add(mobilesKey, []);
        List<OptionItem> mobiles = _options[mobilesKey];
        mobiles.Add(MobilesTab.GetContent());
    }

    private void SetupInterfaceOptions()
    {
        const string interfaceKey = "Interface";
        if (!_options.ContainsKey(interfaceKey)) _options.Add(interfaceKey, []);
        List<OptionItem> opt = _options[interfaceKey];

        opt.Add(InterfaceTab.GetContent());
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

    private void SetupGameplayTab()
    {
        const string gameplayKey = "Gameplay";
        if (!_options.ContainsKey(gameplayKey)) _options.Add(gameplayKey, []);
        List<OptionItem> opt = _options[gameplayKey];

        opt.Add(GameplayTab.GetContent());
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
        const string videoKey = "Video";

        if (!_options.ContainsKey(videoKey))
            _options.Add(videoKey, []);

        List<OptionItem> optionsList = _options[videoKey];
        optionsList.Add(VideoTab.GetContent());
    }

    private void SetupChatOptions()
    {
        const string speechKey = "Chat";

        if (!_options.ContainsKey(speechKey))
            _options.Add(speechKey, []);
        List<OptionItem> opt = _options[speechKey];

        opt.Add(ChatTab.GetContent());
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
