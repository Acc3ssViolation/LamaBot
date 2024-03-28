namespace LamaBot.Cron
{
    internal class CronService : BackgroundService
    {
        private readonly IReadOnlyList<ICronActionProvider> _actionProviders;
        private readonly IDiscordFacade _discordFacade;
        private readonly ILogger<CronService> _logger;

        private CancellationTokenSource? _cts;
        private readonly object _lock = new object();

        public CronService(IEnumerable<ICronActionProvider> actionProviders, IDiscordFacade discordFacade, ILogger<CronService> logger)
        {
            _actionProviders = actionProviders.ToList();
            _discordFacade = discordFacade ?? throw new ArgumentNullException(nameof(discordFacade));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _discordFacade.WaitUntilReadyAsync(stoppingToken);

            foreach (var actionProvider in _actionProviders)
                actionProvider.ActionsUpdated += OnActionsUpdated;

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Fetching cron messages from repository");
                    IEnumerable<ICronAction> actions = Array.Empty<ICronAction>();
                    foreach (var actionProvider in _actionProviders)
                        actions = actions.Concat(await actionProvider.GetActionsAsync(stoppingToken).ConfigureAwait(false));

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
                    await RunSchedulesAsync(actions, combinedSource.Token).ConfigureAwait(false);
                }
            }
            finally
            {
                foreach (var actionProvider in _actionProviders)
                    actionProvider.ActionsUpdated -= OnActionsUpdated;
            }
        }

        private void OnActionsUpdated()
        {
            lock (_lock)
            {
                _cts?.Cancel();
            }
        }

        private async Task RunSchedulesAsync(IEnumerable<ICronAction> actions, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // TODO: We should actually make sure this is run in the guild's timezone, but whatever
                    var now = DateTimeOffset.Now;
                    var timezone = TimeZoneInfo.Local;
                    var scheduleGroups = actions.GroupBy(m => m.Schedule.GetNextOccurrence(now, timezone)).Where(g => g.Key.HasValue).OrderBy(g => g.Key).ToList();
                    var firstGroup = scheduleGroups.FirstOrDefault();
                    if (firstGroup != null)
                    {
                        var scheduleTime = firstGroup.Key!.Value;
                        var delay = scheduleTime - DateTimeOffset.Now;

                        _logger.LogDebug("Waiting until {Time}", scheduleTime);
                        if (delay > TimeSpan.Zero)
                            await DelayLong(delay, cancellationToken).ConfigureAwait(false);

                        // Run first group, do not allow cancellations during this process
                        foreach (var action in firstGroup)
                            await ExecuteActionAsync(action).ConfigureAwait(false);
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

        private async Task ExecuteActionAsync(ICronAction action)
        {
            try
            {
                await action.ActionAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while processing cron action {Message}", action);
            }
        }
    }
}
