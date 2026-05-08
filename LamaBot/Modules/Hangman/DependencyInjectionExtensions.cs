namespace LamaBot.Modules.Hangman
{
    internal static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddHangman(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddSingleton<ImageProvider>()
                .AddSingleton<WordProvider>()
                .AddSingleton<HangmanController>();
        }
    }
}