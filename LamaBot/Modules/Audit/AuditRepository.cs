using LamaBot.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LamaBot.Modules.Audit
{
    public class AuditRepository : IAuditRepository, IAuditLog
    {
        private readonly Func<ApplicationDbContext> _dbContextFactory;
        private readonly ILogger<AuditRepository> _logger;

        public AuditRepository(Func<ApplicationDbContext> dbContextFactory, ILogger<AuditRepository> logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<AuditEntry>> GetAuditEntriesAsync(ulong? guildId, ulong? userId, string? module, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();

            var entries = dbContext.AuditEntries.AsNoTracking();
            if (guildId.HasValue)
                entries = entries.Where(a => a.GuildId == guildId.Value);
            if (userId.HasValue)
                entries = entries.Where(a => a.UserId == userId.Value);
            if (module != null)
                entries = entries.Where(a => a.Module == module);
            entries = entries.OrderByDescending(a => a.TimestampUtc).ThenBy(a => a.GuildId).ThenBy(a => a.UserId).ThenBy(a => a.Module).ThenBy(a => a.Message);

            return await entries
                .Select(a => new AuditEntry(a.TimestampUtc, a.GuildId, a.UserId, a.Module, a.Message))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task LogAsync(ulong guildId, ulong userId, string module, string message)
        {
            using var dbContext = _dbContextFactory();

            var entry = new DbAuditEntry
            {
                TimestampUtc = DateTime.UtcNow,
                GuildId = guildId,
                UserId = userId,
                Module = module,
                Message = message,
            };

            dbContext.AuditEntries.Add(entry);

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
