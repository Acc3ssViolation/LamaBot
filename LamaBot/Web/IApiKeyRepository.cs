using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LamaBot.Web
{
    public interface IApiKeyRepository
    {
        Task<List<ApiKey>> GetApiKeysAsync(CancellationToken cancellationToken = default);
        Task<ApiKeyInfo?> GetApiKeyInfoAsync(ulong guildId, string apiKey, CancellationToken cancellationToken = default);
    }
}
