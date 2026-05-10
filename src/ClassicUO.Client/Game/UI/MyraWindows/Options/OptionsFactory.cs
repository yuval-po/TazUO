
#nullable enable

using System;
using ClassicUO.Common;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options;

public static class OptionsFactory
{
    internal static OptionItem CreateCheckboxOption(string label, bool enabled, Action<bool> onChange,
        string? tooltip = null) =>
        new(label, () => MyraCheckButton.CreateWithCallback(enabled, onChange, label, tooltip));

    internal static OptionItem CreateCheckboxOption(string label, Accessor<bool> backingProperty, string? tooltip = null) =>
        new(label, () => MyraCheckButton.CreatePropBoundCheckButton(backingProperty, label, tooltip));

    internal static OptionItem CreateSliderOption(string label, float min, float max, float value,
        Action<float> onChange) =>
        new(label, () => MyraHSlider.SliderWithLabel(label, out _, onChange, min, max, value));

    internal static OptionItem CreateComboBox(string label, int value, string[] options, Action<int> onChange,
        string? tooltip = null)
    {
        var comboView = new ComboView
        {
            MinWidth = 200,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        if (tooltip != null) comboView.Tooltip = tooltip;

        for (int i = 0; i < options.Length; i++)
        {
            string option = options[i];
            comboView.ListView.Widgets.Add(new Label { Text = option, Tag = i });
        }

        comboView.ListView.SelectedIndex = value;

        comboView.ListView.SelectedIndexChanged += (_, _) =>
        {
            if (comboView.ListView.SelectedIndex != null)
                onChange(comboView.ListView.SelectedIndex.Value);
        };

        return new OptionItem(label, () => new MyraLabel(label, MyraLabel.TextStyle.P).PlaceBefore(comboView));
    }

    internal static OptionItem CreateHuePicker(string label, ushort hue, Action<ushort> onChange) =>
        new(label, () =>
        {
            var item = new MyraArtTexture(0x0FAB) { Tooltip = $"Current hue: {hue}" };
            item.TouchUp += (_, _) =>
            {
                UIManager.GetGump<ModernColorPicker>()?.Dispose();
                UIManager.Add(new ModernColorPicker(World.Instance, onChange));
            };

            return item.PlaceBefore(new MyraLabel(label, MyraLabel.TextStyle.P));
        });

    internal static OptionItem CreateInputField(string label, string text, Action<string> onChange,
        string? tooltip = null) => new(label, () =>
    {
        WrapPanel wid =
            MyraInputBox.LabeledHorizontalStackPanel(label, out MyraInputBox inputBox, text: text, tooltip: tooltip);
        inputBox.TextChangedByUser += (_, _) => onChange(inputBox.Text);
        return wid;
    });

    internal static OptionItem CreateSpacer() => new(string.Empty, () => new MyraSpacer(1, 4), skipSearch: true);
}

