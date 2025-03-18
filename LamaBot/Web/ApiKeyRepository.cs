
using LamaBot.Servers;

namespace LamaBot.Web
{
    public class ApiKeyRepository : IApiKeyRepository
    {
        private readonly IServerSettings _serverSettings;

        public ApiKeyRepository(IServerSettings serverSettings)
        {
            _serverSettings = serverSettings ?? throw new ArgumentNullException(nameof(serverSettings));
        }

        public async Task<ApiKeyInfo?> GetApiKeyInfoAsync(ulong guildId, string apiKey, CancellationToken cancellationToken)
        {
            var savedKey = await _serverSettings.GetSettingAsync(guildId, Quotes.QuoteSettings.QuoteApiKey, cancellationToken).ConfigureAwait(false);
            if (savedKey != null && savedKey.Equals(apiKey, StringComparison.Ordinal))
                return new ApiKeyInfo(guildId, ["QuoteReader"]);
            return null;
        }
    }
}
