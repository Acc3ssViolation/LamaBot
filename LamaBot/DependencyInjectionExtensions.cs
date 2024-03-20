using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace LamaBot
{
    internal interface IModuleProvider
    {
        IEnumerable<Type> GetModuleTypes();
    }

    internal static class DependencyInjectionExtensions
    {
        // Ewwww
        private static List<Type> _moduleTypes = new List<Type>();

        private class ModuleProvider : IModuleProvider
        {
            public IEnumerable<Type> GetModuleTypes() => _moduleTypes;
        }

        public static IServiceCollection AddModules(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddSingleton<IModuleProvider, ModuleProvider>();
        }

        public static IServiceCollection AddModule<T>(this IServiceCollection serviceCollection) where T : InteractionModuleBase
        {
            _moduleTypes.Add(typeof(T));
            return serviceCollection;
        }
    }
}
