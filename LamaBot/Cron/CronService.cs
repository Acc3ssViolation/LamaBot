using Discord;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LamaBot.Cron
{
    internal class CronService : BackgroundService
    {
        private readonly ICronRepository _cronRepository;
        private readonly IDiscordFacade _discordFacade;
        private readonly ILogger<CronService> _logger;

        private CancellationTokenSource? _cts;
        private readonly object _lock = new object();

        public CronService(ICronRepository cronRepository, IDiscordFacade discordFacade, ILogger<CronService> logger)
        {
            _cronRepository = cronRepository ?? throw new ArgumentNullException(nameof(cronRepository));
            _discordFacade = discordFacade ?? throw new ArgumentNullException(nameof(discordFacade));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _discordFacade.WaitUntilReadyAsync(stoppingToken);

            _cronRepository.MessagesUpdated += OnMessagesUpdated;

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("Fetching cron messages from repository");
                var messages = await _cronRepository.GetMessagesAsync(cancellationToken: stoppingToken);

                lock (_lock)
                {
                    if (_cts != null)
                    {
                        _cts.Dispose();
                        _cts = null;
                    }
                    _cts = new CancellationTokenSource();
                }

                using var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _cts.Token);
                await RunSchedulesAsync(messages, combinedSource.Token).ConfigureAwait(false);
            }
        }

        private void OnMessagesUpdated()
        {
            lock (_lock)
            {
                _cts?.Cancel();
            }
        }

        private async Task RunSchedulesAsync(IReadOnlyList<CronMessage> messages, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // TODO: We should actually make sure this is run in the guild's timezone, but whatever
                    var now = DateTimeOffset.Now;
                    var timezone = TimeZoneInfo.Local;
                    var scheduleGroups = messages.GroupBy(m => m.Schedule.GetNextOccurrence(now, timezone)).Where(g => g.Key.HasValue).OrderBy(g => g.Key).ToList();
                    var firstGroup = scheduleGroups.FirstOrDefault();
                    if (firstGroup != null)
                    {
                        var scheduleTime = firstGroup.Key!.Value;
                        var delay = scheduleTime - DateTimeOffset.Now;

                        _logger.LogDebug("Waiting until {Time}", scheduleTime);
                        if (delay > TimeSpan.Zero)
                            await DelayLong(delay, cancellationToken).ConfigureAwait(false);

                        // Run first group, do not allow cancellations during this process
                        foreach (var message in firstGroup)
                            await ExecuteMessageAsync(message).ConfigureAwait(false);
                    }
                    else
                    {
                        // No messages available
                        _logger.LogDebug("No messages available");
                        await cancellationToken.UntilCancelledNoThrow();
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private async Task DelayLong(TimeSpan timeSpan, CancellationToken cancellationToken)
        {
            while (timeSpan.TotalMilliseconds > 4294967294)
            {
                timeSpan = timeSpan - TimeSpan.FromMilliseconds(4294967294);
                await Task.Delay(timeSpan, cancellationToken).ConfigureAwait(false);
            }
            await Task.Delay(timeSpan, cancellationToken).ConfigureAwait(false);
        }

        private async Task ExecuteMessageAsync(CronMessage message)
        {
            try
            {
                _logger.LogDebug("Executing cron message {Message}", message);

                if (_discordFacade.Client.ConnectionState != ConnectionState.Connected)
                    return;

                var task = _discordFacade.Client.GetGuild(message.GuildId)?.GetTextChannel(message.ChannelId)?.SendMessageAsync(message.Message);
                if (task != null)
                    await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while processing cron message {Message}", message);
            }
        }
    }
}
