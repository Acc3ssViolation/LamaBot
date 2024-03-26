using Discord;
using Discord.Commands;
using LamaBot.Database;
using LamaBot.Servers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

namespace LamaBot
{
    [Group("bot")]
    public class BotTextModule : ModuleBase<SocketCommandContext>
    {
        private readonly Func<ApplicationDbContext> _dbContextFactory;
        private readonly IServerSettings _serverSettings;
        private readonly ILogger<BotTextModule> _logger;

        public BotTextModule(Func<ApplicationDbContext> dbContextFactory, IServerSettings serverSettings, ILogger<BotTextModule> logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
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
                    sb.Append('`');
                    var guild = Context.Client.Guilds.FirstOrDefault(g => g.Id == setting.GuildId);
                    if (guild != null)
                        sb.Append(' ').Append(guild.Name);
                    sb.AppendLine();
                }
            }
            await ReplyAsync(sb.ToString());
        }

        [RequireOwner]
        [Command("backup")]
        [Summary("Create a backup of an instance's database")]
        public async Task BackupAsync(string instance)
        {
            if (instance != GetInstanceName())
                return;

            var msg = await ReplyAsync("Backing up database...");

            var tempFile = Path.GetTempFileName();
            using (var backup = new SqliteConnection($"Data Source={tempFile}"))
            {
                using var dbContext = _dbContextFactory();
                var databaseConnection = (SqliteConnection)dbContext.Database.GetDbConnection();
                await databaseConnection.OpenAsync();
                databaseConnection.BackupDatabase(backup);
                SqliteConnection.ClearPool(backup);
            }

            await msg.ModifyAsync(msg =>
            {
                msg.Content = "Backed up database, see attached file";
                msg.Attachments = new FileAttachment[] { new(tempFile, $"backup-{GetInstanceName()}-{DateTime.UtcNow:s}.db") };
            });
        }

        private static string GetInstanceName()
            => Dns.GetHostName();
    }
}
