using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LamaBot.Web
{
    public interface IApiKeyRepository
    {
        Task<List<ApiKey>> GetApiKeysAsync(CancellationToken cancellationToken = default);
        Task<ApiKeyInfo?> GetApiKeyInfoAsync(ulong guildId, string apiKey, CancellationToken cancellationToken = default);
        Task<ApiKeyInfo?> RevokeApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
        Task<ApiKey> CreateApiKeyAsync(ulong guildId, IEnumerable<string> roles, DateTime? expirationUtc, CancellationToken cancellationToken = default);
    }
}
