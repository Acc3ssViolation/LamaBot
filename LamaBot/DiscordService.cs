using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamaBot
{
    internal class DiscordService : BackgroundService, IDiscordFacade
    {
        private readonly IOptions<DiscordOptions> _options;
        private readonly ILogger<DiscordService> _logger;
        private readonly DiscordSocketClient _client;
        private readonly TaskCompletionSource _ready;

        public DiscordService(IOptions<DiscordOptions> options, ILogger<DiscordService> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                UseInteractionSnowflakeDate = false,
            });
            _ready = new TaskCompletionSource();
        }

        public DiscordSocketClient Client => _client;

        public ulong? TestGuild => _options.Value.TestGuildId != null ? ulong.Parse(_options.Value.TestGuildId!) : null;

        public async Task WaitUntilReadyAsync(CancellationToken cancellationToken)
        {
            if (_ready.Task.IsCompleted)
                await _ready.Task;

            var result = await Task.WhenAny(_ready.Task, cancellationToken.UntilCancelledNoThrow());
            if (result != _ready.Task)
                throw new OperationCanceledException();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting discord service");
            try
            {
                _client.Log += DiscordLog;
                _client.Ready += OnReady;

                await _client.LoginAsync(TokenType.Bot, _options.Value.Token);
                await _client.StartAsync();

                await stoppingToken.UntilCancelledNoThrow();
            }
            finally
            {
                _ready.TrySetCanceled();
                _logger.LogInformation("Stopped discord service");
            }
        }

        private Task OnReady()
        {
            _ready.TrySetResult();
            return Task.CompletedTask;
        }

        private Task DiscordLog(LogMessage message)
        {
            var level = message.Severity switch {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Verbose => LogLevel.Debug,
                LogSeverity.Debug => LogLevel.Trace,

                _ => LogLevel.Error,
            };
            _logger.Log(level, "{0}", message);
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            base.Dispose();
            _client?.Dispose();
        }
    }
}
