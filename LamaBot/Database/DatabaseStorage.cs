using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamaBot.Database
{
    internal class DatabaseStorage
    {
        private string _connectionString;
        private readonly ILogger<DatabaseStorage> _logger;

        public DatabaseStorage(IOptions<DatabaseOptions> databaseOptions, ILogger<DatabaseStorage> logger)
        {
            var storagePath = databaseOptions.Value.Path;
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = storagePath,
            };
            _connectionString = builder.ToString();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InitializeAsync()
        {
            try
            {
                var dbContextOptionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                dbContextOptionsBuilder.UseSqlite(_connectionString);

                await using var dbContext = new ApplicationDbContext(dbContextOptionsBuilder.Options);

                var dbDatabase = dbContext.Database;
                foreach (var migration in dbDatabase.GetPendingMigrations())
                    _logger.LogInformation("Migration {Name} will be applied", migration);

                await dbDatabase.MigrateAsync().ConfigureAwait(false);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error initializing database");
            }
        }
    }
}
