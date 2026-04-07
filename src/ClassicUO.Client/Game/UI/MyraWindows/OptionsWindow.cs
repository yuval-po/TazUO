#nullable enable
using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

public class OptionsWindow : MyraControl
{
    /// <summary>
    /// Category, (sub category?, widget)
    /// </summary>
    private Dictionary<string, List<OptionItem>> _options = new();

    private MyraGrid _mainArea = new();
    private VerticalStackPanel _optionsPanel = new();

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

        _options["General"].Add(new (null,
            () => MyraCheckButton.CreateWithCallback(
            profile.HighlightGameObjects,
            b => profile.HighlightGameObjects = b,
            lang.GetGeneral.HighlightObjects))
            );
    }

    private void Build()
    {
        _mainArea.MinWidth = 400;
        _mainArea.MinHeight = 400;

        _mainArea.AddColumn(Proportion.Auto);
        _mainArea.AddColumn(Proportion.Fill);

        VerticalStackPanel categoryPanel = new();
        _mainArea.AddWidget(categoryPanel, 0, 0);

        var optionsStack = new VerticalStackPanel() { Height = 500 };
        optionsStack.Widgets.Add(_optionsPanel);
        _mainArea.AddWidget(optionsStack, 0, 1);

        foreach (string category in _options.Keys) categoryPanel.Widgets.Add(new MyraButton(category, () => { ShowPage(category); }));

        SetRootContent(_mainArea);
    }

    private void ShowPage(string category)
    {
        _optionsPanel.Widgets.Clear();

        foreach (OptionItem optionItem in _options[category]) _optionsPanel.Widgets.Add(optionItem.GetWidget);
    }

    private class OptionItem(string? subcategory, Func<Widget> createWidget)
    {
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
