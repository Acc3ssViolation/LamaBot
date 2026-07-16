using Microsoft.Extensions.DependencyInjection;

namespace LamaBot.Modules.Ping
{
    internal static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddPing(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddSingleton<ITextMessageHandler, PingResponseMessageHandler>();
        }
    }
}
