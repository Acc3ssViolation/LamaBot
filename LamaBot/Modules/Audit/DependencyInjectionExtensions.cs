using Microsoft.Extensions.DependencyInjection;

namespace LamaBot.Modules.Audit
{
    internal static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddAudit(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddSingleton<AuditRepository>()
                .AddSingleton<IAuditLog>(sp => sp.GetRequiredService<AuditRepository>())
                .AddSingleton<IAuditRepository>(sp => sp.GetRequiredService<AuditRepository>())
                ;
        }
    }
}
