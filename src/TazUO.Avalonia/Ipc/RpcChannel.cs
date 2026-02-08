using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TazUO.Avalonia.Ipc.Services.Ui;

namespace TazUO.Host.Ipc;

public class RpcChannel(string? socketPath = null)
{
    private readonly string _socketPath = socketPath ?? Path.Combine(Path.GetTempPath(), "tazui.avalonia.tmp");
    private WebApplication _app;

    public void Start()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddGrpc();
        builder.Services.AddLogging(logging =>
        {
            logging.AddConsole();
            if (builder.Environment.IsDevelopment())
                logging.SetMinimumLevel(LogLevel.Debug);
        });

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ListenUnixSocket(
                _socketPath,
                listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                }
            );
        });

        _app = builder.Build();
        if (_app.Environment.IsDevelopment())
            _app.UseDeveloperExceptionPage();

        _app.UseRouting();

        _app.MapGrpcService<UiService>();

        // Health check endpoint
        _app.MapGet("/", () => "gRPC Channel is up and running");

        _app.Run();
    }

    public Task Stop() => _app.StopAsync();

    public GrpcChannel CreateChannel()
    {
        var udsEndPoint = new UnixDomainSocketEndPoint(_socketPath);
        var connectionFactory = new UdsFactory(udsEndPoint);
        var socketsHttpHandler = new SocketsHttpHandler { ConnectCallback = connectionFactory.ConnectAsync };

        return GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpHandler = socketsHttpHandler });
    }
}
