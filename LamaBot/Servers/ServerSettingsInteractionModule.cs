using Discord;
using Discord.Interactions;
using System.Text;

namespace LamaBot.Servers
{
    [Group("setting", "Server specific settings")]
    public class ServerSettingsInteractionModule : InteractionModuleBase
    {
        private readonly IServerSettings _serverSettings;

        public ServerSettingsInteractionModule(IServerSettings serverSettings)
        {
            _serverSettings = serverSettings;
        }

        [RequireOwner]
        [SlashCommand("set", "Set the value of a setting")]
        public async Task SetSettingAsync(
            [Summary("code", "The code of the setting")] string setting,
            [Summary("value", "The value to set it to")] string value
            )
        {
            await DeferAsync(true);
            await _serverSettings.SetSettingAsync(Context.Guild.Id, setting, value);
            await ModifyOriginalResponseAsync((msg) =>
            {
                msg.Content = $"Updated setting `{setting}` to `{value}`";
            });
        }

        [RequireOwner]
        [SlashCommand("clear", "Delete setting")]
        public async Task DeleteSettingAsync(
            [Summary("code", "The code of the setting")] string setting
            )
        {
            await DeferAsync(true);
            await _serverSettings.ClearAsync(Context.Guild.Id, setting);
            await ModifyOriginalResponseAsync((msg) =>
            {
                msg.Content = $"Setting `{setting}` cleared";
            });
        }

        [RequireOwner]
        [SlashCommand("get", "Get setting value")]
        public async Task GetSettingAsync(
            [Summary("code", "The code of the setting")] string setting
            )
        {
            await DeferAsync(true);
            var value = await _serverSettings.GetSettingAsync(Context.Guild.Id, setting);
            await ModifyOriginalResponseAsync((msg) =>
            {
                msg.Content = $"Setting `{setting}` is `{value ?? "null"}`";
            });
        }

        [RequireOwner]
        [SlashCommand("list", "List setting values")]
        public async Task ListSettingsAsync()
        {
            await DeferAsync(true);
            var settings = await _serverSettings.GetSettingsAsync();

            await ModifyOriginalResponseAsync((msg) =>
            {
                var sb = new StringBuilder();
                sb.AppendLine("Configured settings");
                sb.AppendLine("```");
                foreach (var setting in settings)
                    sb.AppendLine($"<{setting.GuildId}> <{setting.Code}> <{setting.Value}>");
                sb.AppendLine("```");
                msg.Content = sb.ToString();
            });
        }

        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [SlashCommand("prefix", "Set the server's command prefix")]
        public async Task SetCommandPrefixAsync(
            [Summary("prefix", "The prefix to use before a command")] string prefix
        )
        {
            prefix = prefix.Trim();
            if (string.IsNullOrWhiteSpace(prefix) || prefix.Length > 1)
            {
                await RespondAsync($"Prefix `{prefix}` is invalid", ephemeral: true);
                return;
            }

            await DeferAsync();
            await _serverSettings.SetCommandPrefixAsync(Context.Guild.Id, prefix[0]);
            await ModifyOriginalResponseAsync((msg) =>
            {
                msg.Content = $"Text commands now use `{prefix}` (e.g. `{prefix}quote`)";
            });
        }
    }
}
