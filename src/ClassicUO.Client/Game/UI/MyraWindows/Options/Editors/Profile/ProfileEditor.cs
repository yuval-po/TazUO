using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility.Logging;
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
    private readonly Func<string, TProfile> _createProfile;
    private readonly Action<TProfile> _onDeleteProfile;

    public ObservableCollection<TProfile> Profiles { get; } = [];

    public ProfileEditor(
        Func<TProfile, Widget> getConfigUiForProfile,
        Func<string, TProfile> createProfile,
        Action<TProfile> onDeleteProfile,
        IEnumerable<TProfile> profiles = null
    )
    {
        ArgumentNullException.ThrowIfNull(getConfigUiForProfile);
        ArgumentNullException.ThrowIfNull(createProfile);
        ArgumentNullException.ThrowIfNull(onDeleteProfile);

        _configUiGetter = getConfigUiForProfile;
        _createProfile = createProfile;
        _onDeleteProfile = onDeleteProfile;

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

    private void RemoveProfile(TProfile profile)
    {
        if (profile == null)
            return;

        profile.PropertyChanged -= OnProfilePropertyChanged;
        Profiles.Remove(profile);
        _profileRefs.Remove(profile);

        // Since we've deleted a profile, we may need to display an empty state
        if (Profiles.Count > 0)
            ChangeOrUpdateProfile(Profiles.First());
        else
            Children.Add(Build());
    }

    private void OnProfilePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (!sender.Equals(_selectedProfile))
            return;

        // Re-render with the updated content
        ChangeOrUpdateProfile(_selectedProfile);
    }

    private WrapPanel Build() =>
        OptionTabCommons.StyledVerticalWrapPanel(
            OptionsFactory.CreateSpacer(),
            GetToolbar(),
            OptionsFactory.CreateSpacer(),
            OptionTabCommons.StyledHorizontalWrapPanel(
                _currentConfigUi ?? new Panel()
            )
        );

    private StackPanel GetToolbar()
    {
        ProfileEditorLanguage lang = Language.Instance.UiCommons.ProfileEditor;

        StackPanel panel = OptionTabCommons.StyledStackPanel(
            Orientation.Horizontal,
            GetProfilesCombo(),
            new MyraButton(lang.Add, OnAdd),
            new MyraButton(lang.Delete, OnDelete) { Enabled = _selectedProfile is { Deletable: true } }
        );

        panel.Margin = new Thickness(0, 0, 0, 10);

        return OptionTabCommons.StyledStackPanel(
            Orientation.Vertical,
            panel,
            OptionTabCommons.StyledHorizontalSeparator()
        );
    }

    private StackPanel GetProfilesCombo()
    {
        ProfileEditorLanguage lang = Language.Instance.UiCommons.ProfileEditor;

        string selectedProfileName = _selectedProfile?.Name ?? Profiles.FirstOrDefault()?.Name ?? string.Empty;

        StackPanel combo = OptionTabCommons.CreateOptionsComboBox(lang.Profile,
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
        TProfile newProfile = _createProfile(GetNextProfileName());
        AddProfile(newProfile);
        ChangeOrUpdateProfile(newProfile);
    }

    private void OnDelete()
    {
        if (_selectedProfile?.Deletable != true)
            return;

        IGui prevTopmost = UIManager.TopMostControl;

        _confirmationModal?.Dispose();
        _confirmationModal = new ConfirmationModal(
            "Delete Profile?",
            $"Delete profile \"{_selectedProfile.Name}\"?",
            confirmed =>
            {
                if (!confirmed)
                    return;

                if (_selectedProfile?.Deletable != true)
                {
                    Log.Warn($"Profile {nameof(TProfile)} is not deletable. This is a logical bug, please report it via GitHub or Discord.");
                    return;
                }

                TProfile removedProfile = _selectedProfile;
                // RemoveProfile updates _selectedProfile so we need to track it first
                RemoveProfile(removedProfile);

                // Invoke the user callback
                _onDeleteProfile(removedProfile);

                // Restore focus back to the parent control
                UIManager.MakeTopMostGump(prevTopmost);
            }
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

    private string GetNextProfileName()
    {
        int index = 1;
        while (Profiles.Any(p => p.Name == $"Profile {index}"))
            index++;
        return $"Profile {index}";
    }
}
