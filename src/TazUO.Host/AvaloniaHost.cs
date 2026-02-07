using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ClassicUO.Ipc;
using TazUO.Avalonia;
using TazUO.Avalonia.Views;

namespace ClassicUO;

public class AvaloniaUiHost
{
    private AppBuilder _appBuilder;

    private readonly Dictionary<Type, Window> _windows = new();

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly ManualResetEventSlim _stopEvent = new(false);

    public void Init()
    {
        if (_appBuilder != null) return;

        _appBuilder = AppBuilder.Configure<TazUi>()
            .UsePlatformDetect()
            .LogToTrace();

        _appBuilder.SetupWithoutStarting();
    }

    public async Task StartIpcListener(ChannelReader<IUiToCoreMessage> channelReader)
    {
        try
        {
            await foreach (IUiToCoreMessage msg in channelReader.ReadAllAsync(_cancellationTokenSource.Token))
                switch (msg)
                {
                    case ShowSettingsMessage:
                        await ShowWindow<SettingsWindow>();
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

    private static Task Dispatch(Action action) => Dispatcher.UIThread.InvokeAsync(action).GetTask();

    private Task ShowWindow<T>() where T : Window, new() =>
        Dispatch(() =>
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
