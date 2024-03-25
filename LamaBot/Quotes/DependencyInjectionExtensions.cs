using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LamaBot.Quotes
{
    internal static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddQuotes(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddSingleton<IQuoteRepository, QuoteRepository>()
                .AddSingleton<QuoteReactionHook>()
                .AddSingleton<IHostedService>(sp => sp.GetRequiredService<QuoteReactionHook>())
                .AddSingleton<IReactionHook>(sp => sp.GetRequiredService<QuoteReactionHook>());
        }
    }
}
