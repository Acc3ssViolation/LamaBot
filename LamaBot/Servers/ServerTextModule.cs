using Discord.Commands;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

namespace LamaBot.Servers
{
    [Group("server")]
    public class ServerTextModule : ModuleBase<SocketCommandContext>
    {
        private readonly IServerSettings _serverSettings;
        private readonly ILogger<ServerTextModule> _logger;

        public ServerTextModule(IServerSettings serverSettings, ILogger<ServerTextModule> logger)
        {
            _serverSettings = serverSettings ?? throw new ArgumentNullException(nameof(serverSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [RequireOwner]
        [Command("instances")]
        public async Task GetInfoAsync()
        {
            await ReplyAsync($"Instance `{GetInstanceName()}`");
        }

        [RequireOwner]
        [Command("enable")]
        [Summary("Enable a server")]
        public async Task EnableServerAsync(string instance, ulong guildId)
        {
            if (instance != GetInstanceName())
                return;

            await _serverSettings.EnableServerAsync(guildId);
            await ReplyAsync($"Enabled server `{guildId}` on instance `{instance}`");
        }

        [RequireOwner]
        [Command("disable")]
        [Summary("Disable a server")]
        public async Task DisableServerAsync(string instance, ulong guildId)
        {
            if (instance != GetInstanceName())
                return;

            await _serverSettings.DisableServerAsync(guildId);
            await ReplyAsync($"Disabled server `{guildId}` on instance `{instance}`");
        }

        [RequireOwner]
        [Command("list")]
        [Summary("List servers registered on each instance")]
        public async Task ListerversAsync(string? instance = null)
        {
            if (instance != null && instance != GetInstanceName())
                return;

            var allSettings = await _serverSettings.GetSettingsAsync();
            var sb = new StringBuilder();
            sb.Append("Servers enabled on instance `");
            sb.Append(GetInstanceName());
            sb.AppendLine("`");
            foreach (var setting in allSettings)
            {
                if (setting.Code == "enabled")
                {
                    sb.Append("`");
                    sb.Append(setting.GuildId);
                    sb.AppendLine("`");
                }
            }
            await ReplyAsync(sb.ToString());
        }

        private static string GetInstanceName()
            => Dns.GetHostName();
    }
}
