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
        
        [SlashCommand("about", "Get info about the bot status")]
        public async Task GetAboutAsync()
        {
            var embed = new EmbedBuilder()
                .WithAuthor(Context.Client.CurrentUser.Username)
                .WithTitle("Bot info")
                .WithFields(
                    new EmbedFieldBuilder().WithName("Version").WithValue(GetVersion().AsMonospace()),
                    new EmbedFieldBuilder().WithName("Uptime").WithValue(GetUptime()),
                    new EmbedFieldBuilder().WithName("GitHub").WithValue("https://github.com/Acc3ssViolation/LamaBot")
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

        [RequireOwner]
        [SlashCommand("status", "Get info about the bot status")]
        public async Task GetStatusAsync()
        {
            var embed = new EmbedBuilder()
                .WithAuthor(Context.Client.CurrentUser.Username)
                .WithTitle("Bot info")
                .WithFields(
                    new EmbedFieldBuilder().WithName("Version").WithValue(GetVersion().AsMonospace()),
                    new EmbedFieldBuilder().WithName("Uptime").WithValue(GetUptime()),
                    new EmbedFieldBuilder().WithName("Working Set").WithValue(GetWorkingSet().AsMonospace()),
                    new EmbedFieldBuilder().WithName("Private Bytes").WithValue(GetPrivateBytes().AsMonospace()),
                    new EmbedFieldBuilder().WithName("CPU").WithValue(GetCpuLoad().AsMonospace())
                )
                .WithCurrentTimestamp()
                .Build();

            await RespondAsync(ephemeral: true, embed: embed);
        }

        private static string GetWorkingSet()
        {
            using var process = Process.GetCurrentProcess();
            return process.WorkingSet64.ToString();
        }

        private static string GetPrivateBytes()
        {
            using var process = Process.GetCurrentProcess();
            return process.PrivateMemorySize64.ToString();
        }

        private static string GetCpuLoad()
        {
            var utcNow = DateTime.UtcNow;
            using var process = Process.GetCurrentProcess();
            var uptime = utcNow - process.StartTime.ToUniversalTime();
            var cpuLoad = (process.TotalProcessorTime / (uptime * Environment.ProcessorCount)) * 100;
            return $"{cpuLoad:N2} %";
        }

        private static string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.ProductVersion ?? "Unknown";
        }

        private static string GetUptime()
        {
            using var process = Process.GetCurrentProcess();
            var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
            return $"{uptime:%d} days {uptime:h\\:mm\\:ss} hours";
        }
    }

    public static class DiscordStringExtensions
    {
        public static string AsMonospace(this string str)
            => $"`{str}`";
    }
}
