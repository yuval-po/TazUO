using System;
using ClassicUO.Common;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class PropertyBinder
{
    public Accessor<bool> BackingProperty { get; init; }

    public string Label { get; init; }

    public PropertyBinder(Accessor<bool> backingProperty, string label)
    {
        ArgumentNullException.ThrowIfNull(backingProperty);
        BackingProperty = backingProperty;
        Label = label;
    }
}

public class CheckBoxGroup : Panel
{
    private readonly PropertyBinder _primaryControlProp;
    private readonly VerticalStackPanel _primaryPanel = new();
    private readonly VerticalStackPanel _dependentsPanel = new() { Margin = new Thickness(20, 0, 0, 0) };

    public CheckBoxGroup(PropertyBinder controlProp, params Widget[] widgets)
    {
        ArgumentNullException.ThrowIfNull(controlProp);
        _primaryControlProp = controlProp;

        var primaryCheckBox = MyraCheckButton.CreateWithCallback(
            _primaryControlProp.BackingProperty.Get(),
            OnPrimaryCheckBoxChanged,
            _primaryControlProp.Label
        );

        _primaryPanel.Widgets.Add(primaryCheckBox);
        _primaryPanel.Widgets.Add(_dependentsPanel);

        if (widgets?.Length > 0)
            Add(widgets);

        UpdateChildrenEnablement(_primaryControlProp.BackingProperty.Get());
        EnabledChanged += (_, _) => UpdateChildrenEnablement(Enabled && primaryCheckBox.IsChecked);

        Children.Add(_primaryPanel);
        ChildrenLayout = new StackPanelLayout(Orientation.Vertical);
    }

    public void Add(params Widget[] widgets)
    {
        foreach (Widget widget in widgets)
            _dependentsPanel.Widgets.Add(widget);
    }

    private void OnPrimaryCheckBoxChanged(bool isChecked)
    {
        _primaryControlProp.BackingProperty.Set(isChecked);
        UpdateChildrenEnablement(isChecked);
    }

    private void UpdateChildrenEnablement(bool enabled)
    {
        // When the primary checkbox is checked, enable all dependent widgets.
        // Note this will miss changes done to the property outside of this control flow.
        //
        // Can be expanded with INotifyPropertyChanged for more robust handling but not necessary for now.
        foreach (Widget widget in _dependentsPanel.Widgets)
            widget.Enabled = enabled;
    }
}
