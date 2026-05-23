using System.ComponentModel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Profile;

public interface IProfile : INotifyPropertyChanged
{
    string Name { get; }
    bool Deletable { get; }
}
