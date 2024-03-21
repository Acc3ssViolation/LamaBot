using LamaBot.Quotes;
using Microsoft.Extensions.DependencyInjection;

namespace LamaBot.Cron
{
    internal static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddCronMessages(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddSingleton<ICronRepository, CronRepository>()
                .AddHostedService<CronService>();
        }
    }
}
