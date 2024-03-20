using Microsoft.Extensions.DependencyInjection;

namespace LamaBot.Quotes
{
    internal static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddQuotes(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddSingleton<IQuoteRepository, QuoteRepository>()
                .AddModule<QuoteInteractionModule>();
        }
    }
}
