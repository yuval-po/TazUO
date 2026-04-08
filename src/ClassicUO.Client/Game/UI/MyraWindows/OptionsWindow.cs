#nullable enable
using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using Label = Myra.Graphics2D.UI.Label;

namespace ClassicUO.Game.UI.MyraWindows;

public class OptionsWindow : MyraControl
{
    /// <summary>
    /// Category, (sub category?, widget)
    /// </summary>
    private Dictionary<string, List<OptionItem>> _options = new();

    private MyraGrid _mainArea = new();
    private VerticalStackPanel _optionsPanel = new();
    private VerticalStackPanel _searchPanel = new();
    private VerticalStackPanel _optionsStack = new();
    private MyraInputBox _searchField = new();
    private string _lastCategory;

    public OptionsWindow() : base("Options")
    {
        UIManager.ForEach<OptionsWindow>(w => { if(w != this) w.Dispose(); });

        SetupOptions();
        Build();

        CenterInViewPort();
    }

    private void SetupOptions()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        if(!_options.ContainsKey("General")) _options.Add("General", new List<OptionItem>());

        #region General options

        List<OptionItem> general = _options["General"];
        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;

        general.Add(CreateCheckboxOption(genLang.HighlightObjects, profile.HighlightGameObjects,
            b => profile.HighlightGameObjects = b));

        general.Add(CreateSpacer());

        general.Add(
            CreateCheckboxOption(genLang.Pathfinding, profile.EnablePathfind, b => profile.EnablePathfind = b)
                .SetTags("pathfinding, pathing, path"));
        general.Add(
            CreateCheckboxOption(genLang.ShiftPathfinding, profile.UseShiftToPathfind,
                b => profile.UseShiftToPathfind = b).SetTags("pathfinding, pathing, path"));
        general.Add(CreateCheckboxOption(genLang.SingleClickPathfind, profile.PathfindSingleClick,
            b => profile.PathfindSingleClick = b).SetTags("pathfinding, pathing, path"));

        general.Add(CreateSpacer());

        general.Add(CreateCheckboxOption(genLang.AlwaysRun, profile.AlwaysRun, b => profile.AlwaysRun = b));
        general.Add(CreateCheckboxOption(genLang.RunUnlessHidden, profile.AlwaysRunUnlessHidden,
            b => profile.AlwaysRunUnlessHidden = b));

        general.Add(CreateSpacer());

        general.Add(CreateCheckboxOption(genLang.AutoOpenDoors, profile.AutoOpenDoors,
            b => profile.AutoOpenDoors = b));
        general.Add(CreateCheckboxOption(genLang.AutoOpenPathfinding, profile.SmoothDoors,
            b => profile.SmoothDoors = b));

        general.Add(CreateSpacer());

        general.Add(CreateCheckboxOption(genLang.AutoOpenCorpse, profile.AutoOpenCorpses,
            b => profile.AutoOpenCorpses = b).SetTags("corpse, loot"));
        general.Add(CreateSliderOption(genLang.CorpseOpenDistance, 0, 5, profile.AutoOpenCorpseRange,
            f => profile.AutoOpenCorpseRange = (int)f).SetTags("corpse, loot"));
        general.Add(CreateCheckboxOption(genLang.CorpseSkipEmpty, profile.SkipEmptyCorpse,
                b => profile.SkipEmptyCorpse = b,
                "Most servers don't send corpse contents until it's opened.\nEnabling this will make this feature not work on most servers.")
            .SetTags("corpse, loot"));
        general.Add(CreateComboBox(genLang.CorpseOpenOptions, profile.CorpseOpenOptions, [
            genLang.CorpseOptNone, genLang.CorpseOptNotTarg,
            genLang.CorpseOptNotHiding, genLang.CorpseOptBoth
        ], i => profile.CorpseOpenOptions = i).SetTags("corpse, loot"));

        general.Add(CreateSpacer());

        general.Add(CreateCheckboxOption(genLang.OutRangeColor, profile.NoColorObjectsOutOfRange,
            b => profile.NoColorObjectsOutOfRange = b));
        general.Add(CreateCheckboxOption(genLang.SallosEasyGrab, profile.SallosEasyGrab,
            b => profile.SallosEasyGrab = b, genLang.SallosTooltip));
        general.Add(CreateCheckboxOption(genLang.ShowHouseContent, profile.ShowHouseContent,
            b => profile.ShowHouseContent = b, genLang.ClientVersionLimitedTooltip));
        general.Add(CreateCheckboxOption(genLang.SmoothBoat, profile.UseSmoothBoatMovement,
            b => profile.UseSmoothBoatMovement = b, genLang.ClientVersionLimitedTooltip));
        //general.Add(CreateCheckboxOption(, , b =>  = b));

        #endregion

        if(!_options.ContainsKey("Mobiles")) _options.Add("Mobiles", new List<OptionItem>());
        List<OptionItem> mobiles = _options["Mobiles"];
        mobiles.Add(CreateCheckboxOption(lang.GetGeneral.ShowMobileHP, profile.ShowMobilesHP,b => profile.ShowMobilesHP = b));

    }

    private OptionItem CreateCheckboxOption(string label, bool enabled, Action<bool> onChange, string? tooltip = null) =>
        new (label, () => MyraCheckButton.CreateWithCallback(enabled, onChange, label, tooltip));

    private OptionItem CreateSliderOption(string label, float min, float max, float value, Action<float> onChange) =>
        new (label, () => MyraHSlider.SliderWithLabel(label, out _, onChange, min, max, value));

    private OptionItem CreateComboBox(string label, int value, string[] options, Action<int> onChange)
    {
        var comboView = new ComboView
        {
            MinWidth = 200,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        for (int i = 0; i < options.Length; i++)
        {
            string option = options[i];
            comboView.ListView.Widgets.Add(new Label { Text = option, Tag = i });
        }

        comboView.ListView.SelectedIndex = value;

        comboView.ListView.SelectedIndexChanged += (sender, args) =>
        {
            if (comboView.ListView.SelectedIndex != null)
                onChange?.Invoke(comboView.ListView.SelectedIndex.Value);
        };

        return new OptionItem(label, () => new MyraLabel(label, MyraLabel.TextStyle.P).PlaceBefore(comboView));
    }

    private OptionItem CreateSpacer() => new(string.Empty, () => new MyraSpacer(1, 3));

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

        VerticalStackPanel categoryPanel = new() { Spacing = MyraStyle.STANDARD_SPACING};
        _mainArea.AddWidget(categoryPanel.WrapInScroll(800), 1, 0);

        _optionsStack.Widgets.Add(_optionsPanel);
        _mainArea.AddWidget(_optionsStack, 1, 1);

        foreach (string category in _options.Keys) categoryPanel.Widgets.Add(GetCategoryButton(category));

        MyraButton GetCategoryButton(string category)
        {
            return ApplyTabStyleToButton(new MyraButton(category, () => { ShowPage(category); }));
        }

        SetRootContent(_mainArea);
    }

    private void SearchFieldOnTextChangedByUser(object? sender, ValueChangedEventArgs<string> e)
    {
        string search = e.NewValue?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(search))
        {
            _optionsStack.Widgets.Clear();
            _optionsStack.Widgets.Add(_optionsPanel);

            if(_lastCategory.NotNullNotEmpty())
                ShowPage(_lastCategory);
        }
        else
        {
            _searchPanel.Widgets.Clear();
            foreach (var (_, items) in _options)
            {
                foreach (OptionItem item in items)
                {
                    if (item.MatchesSearch(search))
                        _searchPanel.Widgets.Add(item.GetWidget);
                }
            }

            _optionsStack.Widgets.Clear();
            _optionsStack.Widgets.Add(_searchPanel);
        }
    }

    private static ButtonStyle _tabButtonStyle = null!;
    private MyraButton ApplyTabStyleToButton(MyraButton tabButton)
    {
        if (_tabButtonStyle == null!)
        {
            ButtonStyle tabControlStyle = Stylesheet.Current.ButtonStyle;
            _tabButtonStyle = new(tabControlStyle);

            _tabButtonStyle.Background = new SolidBrush(Color.Transparent);
            _tabButtonStyle.Border = new SolidBrush(new Color(0, 0, 0, MyraStyle.STANDARD_BORDER_ALPHA));
            _tabButtonStyle.BorderThickness = new Thickness(1);
            _tabButtonStyle.LabelStyle.Font = MyraStyle.UiFont;
            _tabButtonStyle.OverBackground = new SolidBrush(new Color(0, 0, 0, 55));
            _tabButtonStyle.PressedBackground =  new SolidBrush(new Color(0, 0, 0, 155));
            _tabButtonStyle.MinWidth = 150;
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

        foreach (OptionItem optionItem in _options[category]) _optionsPanel.Widgets.Add(optionItem.GetWidget);

        _lastCategory = category;
    }

    private class OptionItem(string searchText, Func<Widget> createWidget, string? tags = null)
    {
        private string? _tags = tags;

        public bool MatchesSearch(string text)
        {
            if (searchText.Contains(text, StringComparison.OrdinalIgnoreCase)) return true;

            return _tags.NotNullNotEmpty() && _tags!.Contains(text, StringComparison.OrdinalIgnoreCase);
        }

        public Widget GetWidget
        {
            get
            {
                if (field is null)
                    field = createWidget();

                return field;
            }
        }

        public OptionItem SetTags(string tags)
        {
            _tags = tags;
            return this;
        }
    }
}
