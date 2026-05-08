using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LamaBot.Components
{
    internal static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddInteractiveComponents(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddSingleton<InteractiveComponentService>()
                .AddSingleton<IHostedService>(sp => sp.GetRequiredService<InteractiveComponentService>())
                .AddSingleton<IInteractiveComponentService>(sp => sp.GetRequiredService<InteractiveComponentService>());
        }
    }
}
