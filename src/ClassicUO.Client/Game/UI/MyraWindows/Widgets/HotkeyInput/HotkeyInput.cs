#nullable enable

using System;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Keyboard = ClassicUO.Input.Keyboard;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.HotkeyInput;

public class SelectionChangedEventArgs : EventArgs
{
    public HotkeySelection OldValue { get; init; }
    public HotkeySelection NewValue { get; init; }
}

public class HotkeyInput : Panel
{
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    private readonly TextBox _input;
    private readonly Action<SelectionChangedEventArgs>? _onSelectionChanged;
    private readonly Color _defaultTextColor;

    private bool _isRecording;

    private HotkeySelection _selection;
    public HotkeySelection Selection
    {
        get => _selection;
        set
        {
            if (_selection == value)
                return;

            _selection = value;

            var eventArgs = new SelectionChangedEventArgs { OldValue = _selection, NewValue = value };
            _onSelectionChanged?.Invoke(eventArgs);
            SelectionChanged?.Invoke(this, eventArgs);
        }
    }

    public HotkeyInput(
        string? labelText = null,
        HotkeySelection? existingSelection = null,
        Action<SelectionChangedEventArgs>? onSelectionChanged = null
    )
    {
        _onSelectionChanged = onSelectionChanged;

        if (existingSelection.HasValue)
            _selection = existingSelection.Value;


        StackPanel panel = OptionTabCommons.StyledStackPanel(Orientation.Horizontal);
        if (!string.IsNullOrEmpty(labelText))
        {
            var label = new Label { Text = labelText, VerticalAlignment = VerticalAlignment.Center };
            panel.Widgets.Add(label);
        }

        _input = new TextBox
        {
            Width = 150,
            Text = existingSelection?.IsEmpty == false ? existingSelection.Value.ToString() : "No hotkey set",
            Cursor = null,
            Selection = null,
            VerticalAlignment = VerticalAlignment.Center
        };

        _input.TouchDown += (s, e) => StartRecording();
        _defaultTextColor = _input.TextColor;

        panel.Widgets.Add(_input);
        panel.Widgets.Add(new MyraButton("Clear", Clear));

        Children.Add(panel);
    }

    protected override void OnPlacedChanged() => DetachAsNecessary();

    public override void OnVisibleChanged() => DetachAsNecessary();

    private void DetachAsNecessary()
    {
        if (_isRecording && (Desktop == null || !Visible))
            StopRecording();
    }

    private void StartRecording()
    {
        if (_isRecording)
            return;
        _isRecording = true;

        _input.Text = "Press a key...";
        _input.TextColor = Color.DarkGoldenrod;
        Keyboard.KeyDownEvent += OnGlobalKeyDown;
    }

    public void Clear()
    {
        _input.Text = "No hotkey set";
        Selection = new HotkeySelection();
        _isRecording = false;
        _input.TextColor = _defaultTextColor;
        Keyboard.KeyDownEvent -= OnGlobalKeyDown;
    }

    private void OnGlobalKeyDown(string key)
    {
        if (!_isRecording)
            return;

        StopRecording();

        _input.Text = key;
        _input.TextColor = Color.White;

        Selection = HotkeySelection.FromString(key);
    }

    private void StopRecording()
    {
        _isRecording = false;
        Keyboard.KeyDownEvent -= OnGlobalKeyDown;
    }
}
