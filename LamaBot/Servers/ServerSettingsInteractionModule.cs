using Discord.Interactions;

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
    }
}
