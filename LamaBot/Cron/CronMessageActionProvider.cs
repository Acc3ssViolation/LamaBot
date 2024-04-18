
using Cronos;
using Discord;

namespace LamaBot.Cron
{
    public class CronMessageActionProvider : ICronActionProvider, IDisposable
    {
        private class CronMessageAction : ICronAction
        {
            private readonly CronMessage _message;
            private readonly IDiscordFacade _discord;
            private readonly ILogger _logger;

            public CronMessageAction(CronMessage message, IDiscordFacade discord, ILogger logger)
            {
                _message = message;
                _discord = discord;
                _logger = logger;
            }

            public CronExpression Schedule => _message.Schedule;

            public async Task ActionAsync()
            {
                try
                {
                    _logger.LogDebug("Executing cron message {Message}", _message);

                    if (_discord.Client.ConnectionState != ConnectionState.Connected)
                        return;

                    var task = _discord.Client.GetGuild(_message.GuildId)?.GetTextChannel(_message.ChannelId)?.SendMessageAsync(_message.Message);
                    if (task != null)
                        await task.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while processing cron message {Message}", _message);
                }
            }
        }

        private readonly IDiscordFacade _discord;
        private readonly ICronRepository _cronRepository;
        private readonly ILogger<CronMessageActionProvider> _logger;
        private bool _disposed;

        public CronMessageActionProvider(IDiscordFacade discord, ICronRepository cronRepository, ILogger<CronMessageActionProvider> logger)
        {
            _discord = discord ?? throw new ArgumentNullException(nameof(discord));
            _cronRepository = cronRepository ?? throw new ArgumentNullException(nameof(cronRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _cronRepository.MessagesUpdated += OnMessagesUpdated;
        }

        public event Action? ActionsUpdated;

        public async Task<IEnumerable<ICronAction>> GetActionsAsync(CancellationToken cancellationToken)
        {
            var messages = await _cronRepository.GetMessagesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return messages.Select(m => new CronMessageAction(m, _discord, _logger));
        }

        private void OnMessagesUpdated()
        {
            ActionsUpdated?.Invoke();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cronRepository.MessagesUpdated -= OnMessagesUpdated;
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
