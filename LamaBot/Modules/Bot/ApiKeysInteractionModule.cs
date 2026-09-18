using Discord;
using Discord.Interactions;
using LamaBot.Web;
using System;
using System.Threading.Tasks;

namespace LamaBot.Modules.Bot
{
    [RequireOwner]
    [Group("apikeys", "Web API key management")]
    public class ApiKeysInteractionModule : InteractionModuleBase
    {
        private readonly IApiKeyRepository _apiKeyRepository;

        public ApiKeysInteractionModule(IApiKeyRepository apiKeyRepository)
        {
            _apiKeyRepository = apiKeyRepository ?? throw new ArgumentNullException(nameof(apiKeyRepository));
        }

        [SlashCommand("list", "Show all registered API keys")]
        public async Task ListApiKeysAsync()
        {
            await DeferAsync(ephemeral: true);

            var apiKeys = await _apiKeyRepository.GetApiKeysAsync();

            await ModifyOriginalResponseAsync(msg =>
            {
                var embed = new EmbedBuilder()
                    .WithTitle("API Keys")
                    .WithCurrentTimestamp();

                foreach (var apiKey in apiKeys)
                    embed.AddField(apiKey.Key, $"Guild: {apiKey.GuildId}\nRoles: {apiKey.Roles.ToCommaSeparatedString()}\nExpiration: {apiKey.ExpirationUtc?.ToString("s") ?? "never"}");

                msg.Embed = embed.Build();
            });
        }

        [SlashCommand("create", "Create a new API key")]
        public async Task CreateApiKeyAsync(
            [Summary("guildId", "Which guild the key provides access to")] string guildId, 
            [Summary("roles", "Comma separated list of roles to give the key")] string roles,
            [Summary("expiration", "Expiration time at which the key stops working")] DateTime? expirationUtc = null)
        {
            await DeferAsync(ephemeral: true);

            var guildIdUlong = ulong.Parse(guildId);

            var apiKey = await _apiKeyRepository.CreateApiKeyAsync(guildIdUlong, roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), expirationUtc);

            await ModifyOriginalResponseAsync(msg =>
            {
                var embed = new EmbedBuilder()
                    .WithTitle("API Key created")
                    .WithCurrentTimestamp();

                embed.AddField(apiKey.Key, $"Guild: {apiKey.GuildId}\nRoles: {apiKey.Roles.ToCommaSeparatedString()}\nExpiration: {apiKey.ExpirationUtc?.ToString("s") ?? "never"}");

                msg.Embed = embed.Build();
            });
        }

        [SlashCommand("revoke", "Revoke an existing API key")]
        public async Task RevokeApiKeyAsync([Summary("key", "The key to revoke")] string key)
        {
            await DeferAsync(ephemeral: true);

            var apiKey = await _apiKeyRepository.RevokeApiKeyAsync(key);

            await ModifyOriginalResponseAsync(msg =>
            {
                if (apiKey == null)
                {
                    msg.Content = "Could not find API key to revoke";
                    return;
                }

                var embed = new EmbedBuilder()
                    .WithTitle("API Key revoked")
                    .WithCurrentTimestamp();

                embed.AddField(key, $"Guild: {apiKey.GuildId}\nRoles: {apiKey.Roles.ToCommaSeparatedString()}\nExpiration: {apiKey.ExpirationUtc?.ToString("s") ?? "never"}");

                msg.Embed = embed.Build();
            });
        }
    }
}
