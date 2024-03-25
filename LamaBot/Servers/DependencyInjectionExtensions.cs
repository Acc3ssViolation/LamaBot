using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LamaBot.Servers
{
    internal static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddServerSettings(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddSingleton<IServerSettingRepository, ServerSettingRepository>()
                .AddSingleton<ServerSettings>()
                .AddSingleton<IHostedService>(sp => sp.GetRequiredService<ServerSettings>())
                .AddSingleton<IServerSettings>(sp => sp.GetRequiredService<ServerSettings>());
        }
    }
}
