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

        _options["General"].Add(CreateCheckboxOption(lang.GetGeneral.HighlightObjects, profile.HighlightGameObjects, b => profile.HighlightGameObjects = b));

        _options["General"].Add(CreateSpacer());

        _options["General"].Add(CreateCheckboxOption(lang.GetGeneral.Pathfinding, profile.EnablePathfind, b => profile.EnablePathfind = b));
        _options["General"].Add(CreateCheckboxOption(lang.GetGeneral.ShiftPathfinding, profile.UseShiftToPathfind, b => profile.UseShiftToPathfind = b));
        _options["General"].Add(CreateCheckboxOption(lang.GetGeneral.SingleClickPathfind, profile.PathfindSingleClick, b => profile.PathfindSingleClick = b));

        _options["General"].Add(CreateSpacer());

        _options["General"].Add(CreateCheckboxOption(lang.GetGeneral.AlwaysRun, profile.AlwaysRun, b => profile.AlwaysRun = b));
        _options["General"].Add(CreateCheckboxOption(lang.GetGeneral.RunUnlessHidden, profile.AlwaysRunUnlessHidden, b => profile.AlwaysRunUnlessHidden = b));

        _options["General"].Add(CreateSpacer());

        _options["General"].Add(CreateCheckboxOption(lang.GetGeneral.AutoOpenDoors, profile.AutoOpenDoors, b => profile.AutoOpenDoors = b));
        _options["General"].Add(CreateCheckboxOption(lang.GetGeneral.AutoOpenPathfinding, profile.SmoothDoors, b => profile.SmoothDoors = b));

        _options["General"].Add(CreateSpacer());

        _options["General"].Add(CreateCheckboxOption(lang.GetGeneral.AutoOpenCorpse, profile.AutoOpenCorpses, b => profile.AutoOpenCorpses = b));
        _options["General"].Add(CreateSliderOption(lang.GetGeneral.CorpseOpenDistance, 0, 5, profile.AutoOpenCorpseRange, f => profile.AutoOpenCorpseRange = (int)f));
        _options["General"].Add(CreateCheckboxOption(lang.GetGeneral.CorpseSkipEmpty, profile.SkipEmptyCorpse, b => profile.SkipEmptyCorpse = b,
            "Most servers don't send corpse contents until it's opened.\nEnabling this will make this feature not work on most servers."));

        //_options["General"].Add(CreateCheckboxOption(, , b =>  = b));
        //_options["General"].Add(CreateCheckboxOption(, , b =>  = b));
    }

    private OptionItem CreateCheckboxOption(string label, bool enabled, Action<bool> onChange, string? tooltip = null) =>
        new (label, () => MyraCheckButton.CreateWithCallback(enabled, onChange, label, tooltip));

    private OptionItem CreateSliderOption(string label, float min, float max, float value, Action<float> onChange) =>
        new (label, () => MyraHSlider.SliderWithLabel(label, out _, onChange, min, max, value));

    private OptionItem CreateSpacer() => new(string.Empty, () => new MyraSpacer(1, 3));

    private void Build()
    {
        _mainArea.MinWidth = 400;
        _mainArea.MinHeight = 400;

        _mainArea.AddColumn(Proportion.Auto);
        _mainArea.AddColumn(Proportion.Fill);
        _mainArea.AddRow(Proportion.Auto);
        _mainArea.AddRow(Proportion.Fill);

        var searchField = new MyraInputBox();
        searchField.HintText = "Search...";
        searchField.TextChangedByUser += SearchFieldOnTextChangedByUser;
        _mainArea.AddWidget(searchField, 0, 0, null, 2);

        VerticalStackPanel categoryPanel = new();
        _mainArea.AddWidget(categoryPanel, 1, 0);

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
                    if (item.SearchText.Contains(search, StringComparison.OrdinalIgnoreCase))
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
        }

        tabButton.MinWidth = 200;
        tabButton.ApplyButtonStyle(_tabButtonStyle);

        return tabButton;
    }

    private void ShowPage(string category)
    {
        _optionsPanel.Widgets.Clear();

        foreach (OptionItem optionItem in _options[category]) _optionsPanel.Widgets.Add(optionItem.GetWidget);

        _lastCategory = category;
    }

    private class OptionItem(string searchText, Func<Widget> createWidget)
    {
        public string SearchText { get; } = searchText;

        public Widget GetWidget
        {
            get
            {
                if (field is null)
                    field = createWidget();

                return field;
            }
        }
    }
}
