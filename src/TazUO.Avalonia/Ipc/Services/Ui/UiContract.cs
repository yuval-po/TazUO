using System;

namespace TazUO.Host.Ipc.Services.Ui;

public class ShowWindowRequest
{
    public readonly string Name;

    public ShowWindowRequest(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}
