using Discord.WebSocket;
using LamaBot.Quotes;
using LamaBot.Database;
using LamaBot.Cron;
using LamaBot.Servers;
using LamaBot.Tunnel;
using System.Diagnostics;
using System.Reflection;
using LamaBot.Web;

namespace LamaBot
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            using var app = CreateAppBuilder(args).Build();

            await app.Services.GetRequiredService<DatabaseStorage>().InitializeAsync();
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            TaskExtensions.Initialize(logger);

            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            var version = fileVersionInfo.ProductVersion ?? "Unknown";
            logger.LogInformation("Version {Version}", version);

            app.MapControllers();
            
            await app.RunAsync();
        }

        public static WebApplicationBuilder CreateAppBuilder(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.UseKestrel(k =>
            {
                k.ListenLocalhost(80, o =>
                {
                    o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
                });
                k.ListenLocalhost(8080, o =>
                {
                    o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
                });
            });

            builder.Services.AddControllers();

            builder.Host
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
                        .AddServerSettings()
                        .AddWebSocketTunnel(hostContext.Configuration.GetSection("Tunnel"))
                        .AddSingleton<HttpClient>()
                        .AddSingleton(discordConfig)
                        .AddSingleton<DiscordSocketClient>()
                        .AddSingleton<DiscordService>()
                        .AddSingleton<IHostedService>(sp => sp.GetRequiredService<DiscordService>())
                        .AddSingleton<IDiscordFacade>(sp => sp.GetRequiredService<DiscordService>())
                        .AddSingleton<DiscordCommandService>()
                        .AddSingleton<IHostedService>(sp => sp.GetRequiredService<DiscordCommandService>())
                        .AddAuthentication()
                        .AddScheme<ApiKeyOptions, ApiKeyHandler>("ApiKey", (_) => { })
                        ;
                });
            return builder;
        }
    }
}
