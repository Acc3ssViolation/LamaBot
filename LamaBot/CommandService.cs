using Discord.Interactions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LamaBot
{
    internal class CommandService : BackgroundService
    {
        private readonly ILogger<CommandService> _logger;
        private readonly IDiscordFacade _discord;
        private readonly IServiceProvider _serviceProvider;

        public CommandService(ILogger<CommandService> logger, IDiscordFacade discord, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _discord = discord ?? throw new ArgumentNullException(nameof(discord));
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug("Starting command service");
            await _discord.WaitUntilReadyAsync(stoppingToken).ConfigureAwait(false);

            _logger.LogDebug("Registering interactions");
            var interactionService = new InteractionService(_discord.Client);
            await interactionService.AddModuleAsync<DebugModule>(_serviceProvider);

            _discord.Client.InteractionCreated += async (x) =>
            {
                var ctx = new SocketInteractionContext(_discord.Client, x);
                await interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
            };

            if (_discord.TestGuild.HasValue)
                await interactionService.RegisterCommandsToGuildAsync(_discord.TestGuild.Value).ConfigureAwait(false);
            
            _logger.LogDebug("Interactions registered!");

            await stoppingToken.UntilCancelledNoThrow().ConfigureAwait(false);
            _logger.LogDebug("Stopped command service");
        }
    }
}
