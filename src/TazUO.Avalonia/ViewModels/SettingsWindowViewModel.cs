using ClassicUO.Configuration;

namespace TazUO.Avalonia.ViewModels;

public partial class SettingsWindowViewModel : ViewModelBase
{
    public Profile CurrentProfile { get; private set; } = ProfileManager.CurrentProfile;

    public SettingsWindowViewModel()
    {
    }
}
