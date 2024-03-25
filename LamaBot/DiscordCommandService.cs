using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LamaBot
{
    internal class DiscordCommandService : BackgroundService
    {
        private readonly ILogger<DiscordCommandService> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDiscordFacade _discord;
        private readonly IServiceProvider _serviceProvider;

        public DiscordCommandService(ILogger<DiscordCommandService> logger, ILoggerFactory loggerFactory, IDiscordFacade discord, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _discord = discord ?? throw new ArgumentNullException(nameof(discord));
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug("Starting command service");
            await _discord.WaitUntilReadyAsync(stoppingToken).ConfigureAwait(false);

            _logger.LogDebug("Registering interactions");
            var interactionService = new InteractionService(_discord.Client, new InteractionServiceConfig
            {
                LogLevel = Discord.LogSeverity.Debug,
            });
            interactionService.Log += new DiscordLogger<InteractionService>(_loggerFactory).Log;

            await interactionService.AddModulesAsync(System.Reflection.Assembly.GetEntryAssembly(), _serviceProvider).ConfigureAwait(false);

            _discord.Client.InteractionCreated += async (x) =>
            {
                if (_discord.TestGuild.HasValue && x.GuildId.HasValue && x.GuildId != _discord.TestGuild)
                    return;

                var ctx = new SocketInteractionContext(_discord.Client, x);
                await interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
            };

            if (_discord.TestGuild.HasValue)
                await interactionService.RegisterCommandsToGuildAsync(_discord.TestGuild.Value).ConfigureAwait(false);
            
            _logger.LogDebug("Interactions registered!");

            _logger.LogDebug("Registering text commands");
            var commandService = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                LogLevel = Discord.LogSeverity.Debug,
            });
            commandService.Log += new DiscordLogger<CommandService>(_loggerFactory).Log;
            await commandService.AddModulesAsync(System.Reflection.Assembly.GetEntryAssembly(), _serviceProvider).ConfigureAwait(false);

            _discord.Client.MessageReceived += async (x) =>
            {
                if (x is not SocketUserMessage userMessage)
                    return;

                var argPos = 0;
                // Determine if the message is a command based on the prefix and make sure no bots trigger commands
                if (!userMessage.HasCharPrefix('!', ref argPos) ||
                    userMessage.Author.IsBot)
                    return;

                var ctx = new SocketCommandContext(_discord.Client, userMessage);

                var result = await commandService.ExecuteAsync(ctx, argPos, _serviceProvider).ConfigureAwait(false);
                if (!result.IsSuccess)
                    _logger.LogWarning("Error during command execution: {Error}", result.ErrorReason);
            };

            _logger.LogDebug("Text commands registered!");

            await stoppingToken.UntilCancelledNoThrow().ConfigureAwait(false);
            _logger.LogDebug("Stopped command service");
        }
    }
}
