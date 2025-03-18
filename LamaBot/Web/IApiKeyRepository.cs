namespace LamaBot.Web
{
    public interface IApiKeyRepository
    {
        Task<ApiKeyInfo?> GetApiKeyInfoAsync(ulong guildId, string apiKey, CancellationToken cancellationToken);
    }
}
