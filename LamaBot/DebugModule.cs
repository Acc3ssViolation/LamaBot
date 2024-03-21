using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

namespace LamaBot
{
    public class DebugModule : InteractionModuleBase
    {
        private readonly ILogger<DebugModule> _logger;

        public DebugModule(ILogger<DebugModule> logger)
        {
            _logger = logger;
        }

        [RequireOwner]
        [SlashCommand("status", "Get info about the bot status")]
        public async Task GetStatusAsync()
        {
            var embed = new EmbedBuilder()
                .WithAuthor(Context.Client.CurrentUser.Username)
                .WithTitle("Bot info")
                .WithFields(
                    new EmbedFieldBuilder().WithName("Version").WithValue(GetVersion()),
                    new EmbedFieldBuilder().WithName("Uptime").WithValue(GetUptime())
                )
                .WithCurrentTimestamp()
                .Build();

            await RespondAsync(embed: embed);
        }

        [RequireOwner]
        [SlashCommand("restart", "Restart the bot")]
        public async Task RestartAsync()
        {
            await RespondAsync("Rebooting bot...");
            await Task.Delay(1000);
            Environment.Exit(0);
        }

        private static string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.ProductVersion ?? "Unknown";
        }

        private static string GetUptime()
        {
            var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            return $"{uptime:%d} days {uptime:h\\:mm\\:ss} hours";
        }
    }
}
