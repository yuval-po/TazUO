using Avalonia;
using System;

namespace TazUO.Avalonia;

sealed class Program
{
    private static readonly AvaloniaUiHost _avaloniaHost;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        _avaloniaHost = new AvaloniaUiHost();
    }
}
