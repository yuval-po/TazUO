using Avalonia.Controls;
using TazUO.Avalonia.ViewModels;

namespace TazUO.Avalonia.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        DataContext = new SettingsWindowViewModel();
        InitializeComponent();
    }
}

