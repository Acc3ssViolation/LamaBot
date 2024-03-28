using Microsoft.Extensions.Logging;

namespace LamaBot
{
    internal static class TaskExtensions
    {
        private static ILogger? _logger;

        public static void Initialize(ILogger logger) 
        {
            _logger = logger;
        }

        public static void LogFailure(this Task task)
        {
            task.ContinueWith(c => {
                _logger?.LogError(c.Exception, "Task failed");
            }, TaskContinuationOptions.NotOnRanToCompletion);
        }

        public static void IgnoreExceptions(this Task task)
        {
            task.ContinueWith(c => {
                _ = c.Exception;
                _logger?.LogTrace("Ignored exception {Message}", c.Exception?.Message);
            }, TaskContinuationOptions.NotOnRanToCompletion);
        }
    }
}
