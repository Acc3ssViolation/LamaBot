using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using LamaBot.Servers;
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
        private readonly IServerSettings _serverSettings;

        public DiscordCommandService(ILogger<DiscordCommandService> logger, ILoggerFactory loggerFactory, IDiscordFacade discord, IServiceProvider serviceProvider, IServerSettings serverSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _discord = discord ?? throw new ArgumentNullException(nameof(discord));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _serverSettings = serverSettings ?? throw new ArgumentNullException(nameof(serverSettings));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var registeredGuilds = new HashSet<ulong>();

            _logger.LogDebug("Starting command service");

            _logger.LogDebug("Creating interactions");
            // Set up service with logging
            var interactionService = new InteractionService(_discord.Client, new InteractionServiceConfig
            {
                LogLevel = Discord.LogSeverity.Debug,
            });
            interactionService.Log += new DiscordLogger<InteractionService>(_loggerFactory).Log;

            // Load all modules
            await interactionService.AddModulesAsync(System.Reflection.Assembly.GetEntryAssembly(), _serviceProvider).ConfigureAwait(false);

            // Register error handler
            interactionService.InteractionExecuted += async (command, context, result) =>
            {
                if (!result.IsSuccess)
                {
                    if (context.Interaction.HasResponded)
                        await context.Interaction.ModifyOriginalResponseAsync(msg =>
                        {
                            var orig = msg.Content.GetValueOrDefault("");
                            msg.Content = orig + '\n' + result.ErrorReason;
                        }).ConfigureAwait(false);
                    else
                        await context.Interaction.RespondAsync(text: result.ErrorReason, ephemeral: true).ConfigureAwait(false);
                }
            };

            // Handle interactions from the client
            _discord.Client.InteractionCreated += async (x) =>
            {
                // Only handle interactions from guilds that are enabled
                if (x.GuildId.HasValue && !await _serverSettings.IsServerEnabledAsync(x.GuildId.Value))
                    return;

                var ctx = new SocketInteractionContext(_discord.Client, x);
                await interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
            };

            // Auto register interactions on guilds
            _discord.Client.GuildAvailable += async (guild) =>
            {
                // No need to register for the same guild twice
                if (registeredGuilds.Contains(guild.Id))
                    return;

                await interactionService.RegisterCommandsToGuildAsync(guild.Id).ConfigureAwait(false);
                registeredGuilds.Add(guild.Id);
                _logger.LogInformation("Registered interactions on guild {Name}", guild.Name);
            };
            _discord.Client.Disconnected += (exc) =>
            {
                _logger.LogInformation(exc, "Disconnect reason");
                return Task.CompletedTask;
            };

            _logger.LogDebug("Interactions created!");

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
                if (x is not SocketUserMessage userMessage || userMessage.Author.IsBot)
                    return;

                // If this is a guild message we need to check if we're supposed to handle commands from it
                var prefix = '!';
                if (userMessage.Channel is SocketGuildChannel channel)
                {
                    if (!await _serverSettings.IsServerEnabledAsync(channel.Guild.Id))
                        return;

                    prefix = await _serverSettings.GetCommandPrefixAsync(channel.Guild.Id);
                }

                var argPos = 0;
                // Determine if the message is a command based on the prefix
                if (!userMessage.HasCharPrefix(prefix, ref argPos))
                    return;

                var ctx = new SocketCommandContext(_discord.Client, userMessage);

                var result = await commandService.ExecuteAsync(ctx, argPos, _serviceProvider).ConfigureAwait(false);
                if (!result.IsSuccess)
                    _logger.LogWarning("Error during command execution: {Error}", result.ErrorReason);
            };

            _logger.LogDebug("Text commands registered!");

            _discord.Client.ReactionAdded += async (message, channel, reaction) =>
            {
                // Don't respond to bots (including our own emoji based responses)
                if (reaction.User.Value.IsBot)
                    return;

                // We only care about guilds
                if ((await channel.GetOrDownloadAsync()) is not SocketGuildChannel guildChannel)
                    return;

                // Guild must be enabled on this instance
                if (!await _serverSettings.IsServerEnabledAsync(guildChannel.Guild.Id))
                    return;

                using var scope = _serviceProvider.CreateScope();
                var hooks = scope.ServiceProvider.GetServices<IReactionHook>();
                foreach (var hook in hooks)
                    await hook.OnReactionAsync(message, guildChannel, reaction);
            };

            await _discord.WaitUntilReadyAsync(stoppingToken).ConfigureAwait(false);

            await stoppingToken.UntilCancelledNoThrow().ConfigureAwait(false);
            _logger.LogDebug("Stopped command service");
        }
    }
}
