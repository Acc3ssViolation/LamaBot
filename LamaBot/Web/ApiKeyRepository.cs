using LamaBot.Database;
using LamaBot.Servers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public async Task<ApiKeyInfo?> GetApiKeyInfoAsync(ulong guildId, string apiKey, CancellationToken cancellationToken)
        {
            using var dbContext = _dbContextFactory();

            var dbApiKey = await dbContext.ApiKeys
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Key == apiKey, cancellationToken)
                .ConfigureAwait(false);

            if (dbApiKey == null)
            {
                var dbSettingKey = await dbContext.ServerSettings
                    .FirstOrDefaultAsync(k => k.GuildId == guildId && k.Code == LegacyApiKeySetting, cancellationToken)
                    .ConfigureAwait(false);
                if (dbSettingKey != null)
                {
                    // Perform migration from legacy format to new api key record
                    dbApiKey = CreateApiKey(guildId, [WebRoles.QuoteReader, WebRoles.ForumChannelReader], null);
                    dbApiKey.Key = dbSettingKey.Value;
                    dbContext.ApiKeys.Add(dbApiKey);
                    dbContext.ServerSettings.Remove(dbSettingKey);
                    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            if (dbApiKey == null)
                return null;

            Debug.Assert(dbApiKey.GuildId == guildId);
            Debug.Assert(dbApiKey.Key == apiKey);

            var roles = dbApiKey.Content.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return new ApiKeyInfo(guildId, roles, dbApiKey.ExpiresUtc);
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
