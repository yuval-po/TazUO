using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ClassicUO.Ipc;
using ClassicUO.Utility.Logging;
using TazUO.Avalonia.Views;

namespace TazUO.Avalonia;

public class AvaloniaUiHost
{
    private readonly AppBuilder _appBuilder;

    private readonly Dictionary<Type, Window> _windows = new();

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly ManualResetEventSlim _stopEvent = new(false);

    private readonly IIpcHost<IUiToCoreMessage, ICoreToUiMessage> _ipc;

    public AvaloniaUiHost(IIpcHost<IUiToCoreMessage, ICoreToUiMessage> ipc)
    {
        ArgumentNullException.ThrowIfNull(ipc);
        _ipc = ipc;

        _appBuilder = AppBuilder.Configure<TazUi>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }

    public void Start()
    {
        StartIpcListener();
        _appBuilder.StartWithClassicDesktopLifetime([]);
    }

    private async Task StartIpcListener()
    {
        while (!_stopEvent.IsSet)
            try
            {
                await IpcReaderLoop();
            }
            catch (Exception e)
            {
                Log.Error($"Avalonia IPC listener encountered an error: {e}");
            }
    }

    public void WaitForExit(CancellationToken? cToken = null) => _stopEvent.Wait(cToken ?? CancellationToken.None);

    private async Task IpcReaderLoop()
    {
        try
        {
            await foreach (ICoreToUiMessage msg in _ipc.Receive.ReadAllAsync(_cancellationTokenSource.Token))
                switch (msg)
                {
                    case ShowSettingsMessage:
                        ShowWindow<SettingsWindow>();
                        break;
                }
        }
        catch (OperationCanceledException)
        {
            _stopEvent.Set();
        }
    }

    public async Task StopIpcListener()
    {
        await _cancellationTokenSource.CancelAsync();
        await Task.Run(() => _stopEvent.Wait());
    }

    private static Task Dispatch(Action action) => Task.Run(() => Dispatcher.UIThread.InvokeAsync(action).GetTask());

    private static void Post(Action action) => Dispatcher.UIThread.Post(action);

    private void ShowWindow<T>() where T : Window, new() =>
        Post(() =>
        {
            Window settingsWin = GetWindow<SettingsWindow>();

            if (!settingsWin.IsVisible)
                settingsWin.Show();

            settingsWin.Activate();
        });

    private Window GetWindow<T>() where T : Window, new()
    {
        Type wType = typeof(T);
        ArgumentNullException.ThrowIfNull(wType);
        if (_windows.TryGetValue(wType, out Window window))
            return window;


        var newWindow = new T();
        _windows[wType] = new T();

        return newWindow;
    }
}
