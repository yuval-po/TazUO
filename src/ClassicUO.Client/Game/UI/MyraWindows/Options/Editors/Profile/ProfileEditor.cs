using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Profile;

public class ProfileEditor<TProfile> : Widget where TProfile : IProfile
{
    private readonly Func<TProfile, Widget> _configUiGetter;
    private TProfile _selectedProfile;
    private Widget _currentConfigUi;

    public ObservableCollection<TProfile> Profiles { get; } = [];

    public ProfileEditor(Func<TProfile, Widget> getConfigUiForProfile, IEnumerable<TProfile> profiles = null)
    {
        ArgumentNullException.ThrowIfNull(getConfigUiForProfile);
        _configUiGetter = getConfigUiForProfile;

        foreach (TProfile profile in profiles ?? [])
            Profiles.Add(profile);

        ChildrenLayout = new WrapPanelLayout();

        if (Profiles.Count > 0)
            ChangeProfile(Profiles.First());
        else
            Children.Add(Build());

        Profiles.CollectionChanged += OnProfilesCollectionChanged;
    }

    private Widget Build() =>
        OptionTabCommons.StyledVerticalWrapPanel(
            OptionsFactory.CreateSpacer(),
            GetToolbar(),
            OptionsFactory.CreateSpacer(),
            OptionTabCommons.StyledHorizontalWrapPanel(
                _currentConfigUi ?? new Panel()
            )
        );

    private Widget GetToolbar()
    {
        ProfileEditorLanguage lang = Language.Instance.UiCommons.ProfileEditor;

        StackPanel panel = OptionTabCommons.StyledStackPanel(
            Orientation.Horizontal,
            GetProfilesCombo(),
            new MyraButton(lang.Add, OnAdd),
            new MyraButton(lang.Edit, OnEdit) { Enabled = _selectedProfile != null },
            new MyraButton(lang.Delete, OnDelete)
        );

        panel.Margin = new Thickness(0, 0, 0, 10);

        return OptionTabCommons.StyledStackPanel(
            Orientation.Vertical,
            panel,
            OptionTabCommons.StyledHorizontalSeparator()
        );
    }

    private Widget GetProfilesCombo()
    {
        ProfileEditorLanguage lang = Language.Instance.UiCommons.ProfileEditor;

        string selectedProfileName = _selectedProfile?.Name ?? Profiles.FirstOrDefault()?.Name ?? string.Empty;

        OptionItem combo = OptionsFactory.CreateComboBox(
            lang.Profile,
            selectedProfileName,
            Profiles?.Select(p => p.Name) ?? [],
            OnProfileSelected
        );

        combo.Width = 225;
        combo.Margin = new Thickness(0, 0, 20, 0);

        return combo;
    }

    private void OnAdd()
    {
    }

    private void OnEdit()
    {
    }

    private void OnDelete()
    {
    }

    private void OnProfileSelected(string selectedName)
    {
        TProfile newValue = Profiles.FirstOrDefault(p => p.Name == selectedName);
        ChangeProfile(newValue);
    }

    private void ChangeProfile(TProfile profile)
    {
        _selectedProfile = profile;
        _currentConfigUi = _configUiGetter(profile);
        RebuildUi();
    }

    private void OnProfilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && !(e.OldItems?.Count > 0) && e.NewItems?.Count > 0)
            OnProfileSelected(Profiles.First().Name);

        RebuildUi();
    }

    private void RebuildUi()
    {
        Children.Clear();
        Children.Add(Build());
    }
}
