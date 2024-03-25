using LamaBot.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LamaBot.Servers
{
    internal class ServerSettingRepository : IServerSettingRepository
    {
        private readonly Func<ApplicationDbContext> _dbContextFactory;
        private readonly ILogger<ServerSettingRepository> _logger;

        public ServerSettingRepository(Func<ApplicationDbContext> dbContextFactory, ILogger<ServerSettingRepository> logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyList<ServerSetting>> GetSettingsAsync(ulong? guildId, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();

            var dbSettingsQuery = dbContext.ServerSettings.AsNoTracking();
            if (guildId.HasValue)
                dbSettingsQuery = dbSettingsQuery.Where(s => s.GuildId == guildId);
            var dbSettings = await dbSettingsQuery.ToListAsync(cancellationToken).ConfigureAwait(false);
            return dbSettings.Select(ToModel).ToList();
        }

        public async Task SetOrDeleteSettingAsync(ulong guildId, string setting, string? value, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();
            var dbSetting = await dbContext.ServerSettings.SingleOrDefaultAsync(s => s.GuildId == guildId && s.Code == setting).ConfigureAwait(false);
            if (value == null)
            {
                if (dbSetting != null)
                {
                    dbContext.ServerSettings.Remove(dbSetting);
                    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                return;
            }

            if (dbSetting == null)
            {
                dbSetting = new DbServerSetting { GuildId = guildId, Code = setting };
                dbContext.ServerSettings.Add(dbSetting);
            }
            dbSetting.Value = value;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        private static ServerSetting ToModel(DbServerSetting setting)
            => new ServerSetting(setting.GuildId, setting.Code, setting.Value);
    }
}
