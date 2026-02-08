using System;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using TazUO.Host.Ipc.Services.Ui;

namespace TazUO.Avalonia.Ipc.Services.Ui;

public class UiService
{
    private readonly ILogger<UiService> _logger;

    private event EventHandler<UiService, ShowWindowRequest> ShowWindowRequested;

    public UiService(ILogger<UiService> logger)
    {
        _logger = logger;
    }

    public void ShowWindow(ShowWindowRequest request, ServerCallContext context)
    {
        _logger.LogInformation($"Showing UI window '{request.Name}");
        ShowWindowRequested.Invoke(this, request);
    }
}
