using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LamaBot.Quotes;
using LamaBot.Database;
using LamaBot.Cron;

namespace LamaBot
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            using var host = CreateHostBuilder(args).Build();

            await host.Services.GetRequiredService<DatabaseStorage>().InitializeAsync();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            TaskExtensions.Initialize(logger);
            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
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
                        .AddDatabase(hostContext.Configuration.GetSection("Database"))
                        .AddQuotes()
                        .AddCronMessages()
                        .AddSingleton(discordConfig)
                        .AddSingleton<DiscordSocketClient>()
                        .AddSingleton<DiscordService>()
                        .AddSingleton<IHostedService>(sp => sp.GetRequiredService<DiscordService>())
                        .AddSingleton<IDiscordFacade>(sp => sp.GetRequiredService<DiscordService>())
                        .AddSingleton<DiscordCommandService>()
                        .AddSingleton<IHostedService>(sp => sp.GetRequiredService<DiscordCommandService>())
                        ;
                });
            return builder;
        }
    }
}
