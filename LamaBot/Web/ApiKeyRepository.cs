using LamaBot.Database;
using LamaBot.Servers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace LamaBot.Web
{
    internal class ApiKeyRepository : IApiKeyRepository
    {
        private const string LegacyApiKeySetting = "quote:key";

        private readonly Func<ApplicationDbContext> _dbContextFactory;
        private readonly ILogger<ServerSettingRepository> _logger;

        public ApiKeyRepository(Func<ApplicationDbContext> dbContextFactory, ILogger<ServerSettingRepository> logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<ApiKey>> GetApiKeysAsync(CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();

            var dbApiKeys = await dbContext.ApiKeys
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return dbApiKeys.Select(MapKey).ToList();
        }

        public async Task<ApiKeyInfo?> GetApiKeyInfoAsync(ulong guildId, string apiKey, CancellationToken cancellationToken)
        {
            using var dbContext = _dbContextFactory();

            var dbApiKey = await dbContext.ApiKeys
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.GuildId == guildId && k.Key == apiKey, cancellationToken)
                .ConfigureAwait(false);

            if (dbApiKey == null)
                return null;

            Debug.Assert(dbApiKey.GuildId == guildId);
            Debug.Assert(dbApiKey.Key == apiKey);

            return MapInfo(dbApiKey);
        }

        public async Task<ApiKeyInfo?> RevokeApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();

            var dbApiKey = await dbContext.ApiKeys
                .FirstOrDefaultAsync(k => k.Key == apiKey, cancellationToken)
                .ConfigureAwait(false);

            if (dbApiKey == null)
                return null;

            if (dbApiKey.ExpiresUtc < DateTime.UtcNow)
                return MapInfo(dbApiKey);

            dbApiKey.ExpiresUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return MapInfo(dbApiKey);
        }

        public async Task<ApiKey> CreateApiKeyAsync(ulong guildId, IEnumerable<string> roles, DateTime? expirationUtc, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();

            var dbApiKey = CreateApiKey(guildId, roles, expirationUtc);
            dbContext.ApiKeys.Add(dbApiKey);

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return MapKey(dbApiKey);
        }

        private static ApiKey MapKey(DbApiKey dbApiKey)
        {
            var roles = dbApiKey.Content.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return new ApiKey(dbApiKey.Key, dbApiKey.GuildId, roles, dbApiKey.ExpiresUtc);
        }

        private static ApiKeyInfo MapInfo(DbApiKey dbApiKey)
        {
            var roles = dbApiKey.Content.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return new ApiKeyInfo(dbApiKey.GuildId, roles, dbApiKey.ExpiresUtc);
        }

        private static DbApiKey CreateApiKey(ulong guildId, IEnumerable<string> roles, DateTime? expiresUtc)
        {
            var key = RandomNumberGenerator.GetHexString(48, true);
            return new DbApiKey
            {
                GuildId = guildId,
                Key = key,
                Content = roles.ToCommaSeparatedString(),
                CreatedUtc = DateTime.UtcNow,
                ExpiresUtc = expiresUtc,
            };
        }
    }
}
