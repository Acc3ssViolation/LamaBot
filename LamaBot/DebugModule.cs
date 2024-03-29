using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace LamaBot
{
    public class DebugModule : InteractionModuleBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DebugModule> _logger;

        public DebugModule(HttpClient httpClient, ILogger<DebugModule> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            _logger.LogWarning("Rebooting for command!");
            Environment.Exit(0);
        }

        [RequireOwner]
        [SlashCommand("update", "Apply update")]
        public async Task UpdateAsync(IAttachment attachment)
        {
            await DeferAsync();

            _logger.LogWarning("Got bot update command");

            var sb = new StringBuilder();
            await ModifyOriginalResponseAsync(msg =>
            {
                sb.AppendLine("Downloading update...");
                msg.Content = sb.ToString();
            });
            try
            {
                var tempFilePath = Path.GetTempFileName();
                using (var httpResponse = await _httpClient.GetAsync(attachment.Url).ConfigureAwait(false))
                {
                    using (var fileStream = File.Create("update.zip"))
                    {
                        await httpResponse.Content.CopyToAsync(fileStream).ConfigureAwait(false);
                    }
                }

                await ModifyOriginalResponseAsync(msg =>
                {
                    sb.AppendLine("Update downloaded!");
                    sb.AppendLine("Rebooting bot...");
                    msg.Content = sb.ToString();
                });

                await Task.Delay(1000);
                _logger.LogWarning("Rebooting for update!");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                await ModifyOriginalResponseAsync(msg =>
                {
                    sb.Append("Exception: ");
                    sb.AppendLine(ex.Message);
                    msg.Content = sb.ToString();
                });
            }
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
