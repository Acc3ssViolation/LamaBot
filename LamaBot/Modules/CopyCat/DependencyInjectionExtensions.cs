using Microsoft.Extensions.DependencyInjection;

namespace LamaBot.Modules.CopyCat
{
    internal static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddCopyCat(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddSingleton<ITextMessageHandler, CopyCatMessageHandler>();
        }
    }
}
