using Discord;
using Discord.Interactions;
using LamaBot.Components;
using LamaBot.Web;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LamaBot.Modules.Bot
{
    [RequireOwner]
    [Group("apikeys", "Web API key management")]
    public class ApiKeysInteractionModule : InteractionModuleBase
    {
        private readonly IApiKeyRepository _apiKeyRepository;
        private readonly IInteractiveComponentService _componentService;

        public ApiKeysInteractionModule(IApiKeyRepository apiKeyRepository, IInteractiveComponentService componentService)
        {
            _apiKeyRepository = apiKeyRepository ?? throw new ArgumentNullException(nameof(apiKeyRepository));
            _componentService = componentService ?? throw new ArgumentNullException(nameof(componentService));
        }

        [SlashCommand("list", "Show all registered API keys")]
        public async Task ListApiKeysAsync()
        {
            await DeferAsync(ephemeral: true);

            await ModifyOriginalResponseAsync(async msg =>
            {
                var helper = new ApiKeyHelper(_apiKeyRepository, _componentService);
                await helper.ShowFirstPage(msg, 0, this);
            });
        }

        private class ApiKeyHelper : PagedResponseHelper<ApiKey, object>
        {
            private readonly IApiKeyRepository _apiKeyRepository;

            public ApiKeyHelper(IApiKeyRepository apiKeyRepository, IInteractiveComponentService componentService) : base(componentService)
            {
                _apiKeyRepository = apiKeyRepository ?? throw new ArgumentNullException(nameof(apiKeyRepository));
            }

            protected override EmbedFieldBuilder GetField(ApiKey item)
            {
                return new EmbedFieldBuilder()
                    .WithName(item.Key)
                    .WithValue($"Guild: {item.GuildId}\nRoles: {item.Roles.ToCommaSeparatedString()}\nExpiration: {item.ExpirationUtc?.ToString("s") ?? "never"}");
            }

            protected override async Task<IReadOnlyList<ApiKey>> GetItemsAsync(ulong guildId, object options)
            {
                return await _apiKeyRepository.GetApiKeysAsync();
            }
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
