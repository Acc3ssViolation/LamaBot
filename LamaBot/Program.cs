using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace LamaBot
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureAppConfiguration((hostContext, configuration) =>
                {
                    configuration
                        .AddIniFile("config.ini")
                        .AddIniFile($"config.{hostContext.HostingEnvironment.EnvironmentName}.ini", true);
                })
                .ConfigureServices((hostContext, services) => {
                    var discordConfig = new DiscordSocketConfig
                    {
                        UseInteractionSnowflakeDate = false,
                    };
                    services.Configure<DiscordOptions>(hostContext.Configuration.GetSection("Discord"))
                        .AddSingleton(discordConfig)
                        .AddSingleton<DiscordSocketClient>()
                        .AddSingleton<DiscordService>()
                        .AddSingleton<IHostedService>(sp => sp.GetRequiredService<DiscordService>())
                        .AddSingleton<IDiscordFacade>(sp => sp.GetRequiredService<DiscordService>())
                        .AddSingleton<CommandService>()
                        .AddSingleton<IHostedService>(sp => sp.GetRequiredService<CommandService>())
                        ;
                });

            using var host = builder.Build();

            await host.RunAsync();
        }
    }
}
