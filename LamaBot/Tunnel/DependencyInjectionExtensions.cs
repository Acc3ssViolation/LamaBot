using WebSocketProxy.Client;

namespace LamaBot.Tunnel
{
    internal static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddWebSocketTunnel(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            return serviceCollection
                .AddSingleton<IHostedService, TunnelService>()
                .AddSingleton<Func<WebSocketProxyClient>>(sp => () =>
                {
                    var logger = sp.GetRequiredService<ILogger<WebSocketProxyClient>>();
                    return new WebSocketProxyClient(logger);
                })
                .Configure<TunnelSettings>(configuration);
        }
    }
}
