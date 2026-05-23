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
using Microsoft.Xna.Framework;
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
    private bool _isRenaming;
    private readonly MyraInputBox _renameInputBox = new();

    private readonly List<TProfile> _profileRefs = [];
    private readonly Func<string, TProfile> _createProfile;
    private readonly Action<TProfile> _onDeleteProfile;

    private const int PROFILE_COMBO_WIDTH = 225;
    private readonly Thickness _profileBomboMargins = new(0, 0, 20, 0);

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

    private WrapPanel Build()
    {
        Widget content = _currentConfigUi ?? new Panel();
        content.Enabled = !_isRenaming;

        return OptionTabCommons.StyledVerticalWrapPanel(
            OptionsFactory.CreateSpacer(),
            GetToolbar(),
            OptionsFactory.CreateSpacer(),
            OptionTabCommons.StyledHorizontalWrapPanel(
                content
            )
        );
    }

    private StackPanel GetToolbar() => _isRenaming ? GetRenamingToolbar() : GetNormalToolbar();

    private StackPanel GetNormalToolbar()
    {
        ProfileEditorLanguage lang = Language.Instance.UiCommons.ProfileEditor;

        bool canEdit = _selectedProfile is { Deletable: true };
        StackPanel panel = OptionTabCommons.StyledStackPanel(
            Orientation.Horizontal,
            GetProfilesCombo(),
            new MyraButton(lang.Add, OnAdd),
            new MyraButton(lang.Rename, OnRename) { Enabled = canEdit, Tooltip = lang.CannotRenameBuiltInProfile },
            new MyraButton(lang.Delete, OnDelete) { Enabled = canEdit, Tooltip = lang.CannotDeleteBuiltInProfile }
        );

        panel.Margin = new Thickness(0, 0, 0, 10);

        return OptionTabCommons.StyledStackPanel(
            Orientation.Vertical,
            panel,
            OptionTabCommons.StyledHorizontalSeparator()
        );
    }

    private StackPanel GetRenamingToolbar()
    {
        ProfileEditorLanguage lang = Language.Instance.UiCommons.ProfileEditor;

        StackPanel panel = OptionTabCommons.StyledStackPanel(
            Orientation.Horizontal,
            GetRenameProfileInput(),
            new MyraButton(lang.Save, OnRenameSave),
            new MyraButton(lang.Cancel, OnRenameCancel)
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

        combo.Width = PROFILE_COMBO_WIDTH;
        combo.Margin = _profileBomboMargins;

        return combo;
    }

    private void OnAdd()
    {
        TProfile newProfile = _createProfile(GetNextProfileName());
        AddProfile(newProfile);
        ChangeOrUpdateProfile(newProfile);
    }

    private void OnRename()
    {
        _isRenaming = true;
        RebuildUi();
    }

    private void OnDelete()
    {
        if (_selectedProfile?.Deletable != true)
            return;

        ProfileEditorLanguage lang = Language.Instance.UiCommons.ProfileEditor;

        IGui prevTopmost = UIManager.TopMostControl;

        _confirmationModal?.Dispose();
        _confirmationModal = new ConfirmationModal(
            lang.DeleteProfile,
            string.Format(lang.DeleteProfileX, _selectedProfile.Name),
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

    private StackPanel GetRenameProfileInput()
    {
        ProfileEditorLanguage lang = Language.Instance.UiCommons.ProfileEditor;

        var panelLabel = new MyraLabel(lang.Profile, MyraLabel.TextStyle.P);
        _renameInputBox.Text = _selectedProfile?.Name;

        // Ultimately this should always yield 200, but we keep this for the dynamic calculation
        _renameInputBox.Width = PROFILE_COMBO_WIDTH - (panelLabel.Measure(new Point(PROFILE_COMBO_WIDTH, 60)).X + MyraStyle.STANDARD_SPACING);
        _renameInputBox.OnGotKeyboardFocus();
        _renameInputBox.CursorPosition = _renameInputBox?.Text?.Length ?? 0;

        StackPanel panel = OptionTabCommons.StyledStackPanel(
            Orientation.Horizontal,
            panelLabel,
            _renameInputBox
        );

        panel.Width = PROFILE_COMBO_WIDTH;
        panel.Margin = _profileBomboMargins;

        return panel;
    }

    private void OnRenameSave()
    {
        if (_selectedProfile == null)
            return;

        string newName = _renameInputBox.Text;
        if (string.IsNullOrWhiteSpace(newName))
            return;

        _selectedProfile.Name = newName;

        _isRenaming = false;
        RebuildUi();
    }

    private void OnRenameCancel()
    {
        _isRenaming = false;
        RebuildUi();
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
        ProfileEditorLanguage lang = Language.Instance.UiCommons.ProfileEditor;

        int index = 1;
        while (Profiles.Any(p => p.Name == $"{lang.Profile} {index}"))
            index++;
        return $"{lang.Profile} {index}";
    }
}
