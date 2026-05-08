using Discord;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace LamaBot
{
    internal class DiscordLogger<T>
    {
        private readonly ILogger<T> _logger;

        public DiscordLogger(ILoggerFactory factory) 
        {
            _logger = factory.CreateLogger<T>();
        }

        public Task Log(LogMessage message)
        {
            var level = message.Severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Verbose => LogLevel.Debug,
                LogSeverity.Debug => LogLevel.Trace,

                _ => LogLevel.Error,
            };
            _logger.Log(level, "{Message}", message);
            return Task.CompletedTask;
        }
    }
}
