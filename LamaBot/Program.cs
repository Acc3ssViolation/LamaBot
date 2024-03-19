using Discord;
using Discord.WebSocket;

namespace LamaBot
{
    internal class Program
    {
        private static DiscordSocketClient _client;

        public static async Task Main(string[] args)
        {
            var discordConfig = await GetConfigSectionAsync("discord");

            using var client = new DiscordSocketClient(new DiscordSocketConfig
            {
                UseInteractionSnowflakeDate = false,
            });
            client.Log += DiscordLog;

            await client.LoginAsync(TokenType.Bot, discordConfig["Token"]);
            await client.StartAsync();

            _client = client;
            client.Ready += Client_Ready;

            await Task.Delay(-1);
        }

        private static async Task Client_Ready()
        {
            var discordConfig = await GetConfigSectionAsync("discord");
            var testGuild = _client.GetGuild(ulong.Parse(discordConfig["TestGuildId"]));

            var registry = new CommandRegistry(_client, testGuild);
            await registry.RegisterCommandsAsync();
        }

        private static Task DiscordLog(LogMessage message)
        {
            Console.WriteLine(message);
            return Task.CompletedTask;
        }

        private static async Task<Dictionary<string, string>> GetConfigSectionAsync(string section)
        {
            var configText = await File.ReadAllLinesAsync("config.ini").ConfigureAwait(false);
            var foundSection = false;
            var result = new Dictionary<string, string>();
            foreach (var rawLine in configText)
            {
                var line = rawLine.Trim();
                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    var sectionName = line.Substring(1, line.Length - 2);
                    if (sectionName.Equals(section, StringComparison.OrdinalIgnoreCase))
                        foundSection = true;
                    else if (foundSection)
                        break;
                }

                if (foundSection)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length != 2)
                        continue;

                    result[parts[0]] = parts[1];
                }
            }
            return result;
        }
    }
}
