using Microsoft.Extensions.DependencyInjection;

namespace LamaBot.Modules.UserCommands
{
    internal static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddUserCommands(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddSingleton<IUserCommandRepository, UserCommandRepository>()
                .AddSingleton<ITextMessageHandler, UserCommandMessageHandler>();
        }
    }
}
