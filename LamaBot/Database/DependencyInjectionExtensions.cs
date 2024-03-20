using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamaBot.Database
{
    internal static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            return serviceCollection
                .AddSingleton<Func<ApplicationDbContext>>(CreateDatabase)
                .AddTransient<DatabaseStorage>()
                .Configure<DatabaseOptions>(configuration);
        }

        private static Func<ApplicationDbContext> CreateDatabase(IServiceProvider serviceProvider)
        {
            var settings = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>();
            var storagePath = settings.Value.Path;
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = storagePath,
            };
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            dbContextOptionsBuilder.UseSqlite(builder.ToString());
#if DEBUG
            dbContextOptionsBuilder.UseLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>());
            dbContextOptionsBuilder.EnableSensitiveDataLogging(true);
#endif
            var options = dbContextOptionsBuilder.Options;

            return () => new ApplicationDbContext(options);
        }
    }
}
