using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LamaBot.Tunnel
{
    internal static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddWebSocketTunnel(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            return serviceCollection
                .AddSingleton<IHostedService, TunnelService>()
                .Configure<TunnelSettings>(configuration);
        }
    }
}
