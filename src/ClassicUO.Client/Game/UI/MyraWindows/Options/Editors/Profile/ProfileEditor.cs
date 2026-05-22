using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
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
    private ConfirmationModal _confirmationModal;

    private readonly List<TProfile> _profileRefs = [];

    public ObservableCollection<TProfile> Profiles { get; } = [];

    public ProfileEditor(Func<TProfile, Widget> getConfigUiForProfile, IEnumerable<TProfile> profiles = null)
    {
        ArgumentNullException.ThrowIfNull(getConfigUiForProfile);
        _configUiGetter = getConfigUiForProfile;

        foreach (TProfile profile in profiles ?? [])
            AddProfile(profile);

        ChildrenLayout = new WrapPanelLayout();

        if (Profiles.Count > 0)
            ChangeOrUpdateProfile(Profiles.First());
        else
            Children.Add(Build());

        Profiles.CollectionChanged += OnProfilesCollectionChanged;
    }

    private void AddProfile(TProfile profile)
    {
        profile.PropertyChanged += OnProfilePropertyChanged;
        Profiles.Add(profile);
        _profileRefs.Add(profile);
    }

    private void RemoveProfile(int index)
    {
        if (index <= 0 || index >= Profiles.Count)
            return;

        Profiles[index].PropertyChanged -= OnProfilePropertyChanged;
        Profiles.RemoveAt(index);
        _profileRefs.RemoveAt(index);
    }

    private void OnProfilePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (!sender.Equals(_selectedProfile))
            return;

        // Re-render with the updated content
        ChangeOrUpdateProfile(_selectedProfile);
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
        if (_selectedProfile == null)
            return;

        _confirmationModal?.Dispose();
        _confirmationModal = new ConfirmationModal(
            "Delete Profile?",
            $"Delete profile {_selectedProfile.Name}?",
            confirmed => { }
        );

        UIManager.Add(_confirmationModal);
    }

    private void OnProfileSelected(string selectedName)
    {
        TProfile newValue = Profiles.FirstOrDefault(p => p.Name == selectedName);
        ChangeOrUpdateProfile(newValue);
    }

    private void ChangeOrUpdateProfile(TProfile profile)
    {
        _selectedProfile = profile;
        _currentConfigUi = _configUiGetter(profile);
        RebuildUi();
    }

    private void OnProfilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                OnProfilesAddedToCollection(e);
                break;

            case NotifyCollectionChangedAction.Remove:
            case NotifyCollectionChangedAction.Replace:
                OnProfilesRemovedFromCollection(e);
                break;
            case NotifyCollectionChangedAction.Reset:
                OnProfilesCollectionCleared();
                break;
        }

        RebuildUi();
    }

    private void OnProfilesAddedToCollection(NotifyCollectionChangedEventArgs e)
    {
        if (!(e.NewItems?.Count > 0))
            return;

        foreach (TProfile newProfile in e.NewItems ?? Array.Empty<TProfile>())
        {
            newProfile.PropertyChanged += OnProfilePropertyChanged;
            _profileRefs.Add(newProfile);
        }

        if (!(e.OldItems?.Count > 0))
            OnProfileSelected(Profiles.First().Name);
    }

    private void OnProfilesRemovedFromCollection(NotifyCollectionChangedEventArgs e)
    {
        foreach (TProfile removedProfile in e.OldItems ?? Array.Empty<TProfile>())
        {
            removedProfile.PropertyChanged -= OnProfilePropertyChanged;
            _profileRefs.Remove(removedProfile);
        }
    }

    private void OnProfilesCollectionCleared()
    {
        foreach (TProfile profile in _profileRefs)
            profile.PropertyChanged -= OnProfilePropertyChanged;
        _profileRefs.Clear();
    }

    private void RebuildUi()
    {
        Children.Clear();
        Children.Add(Build());
    }
}
