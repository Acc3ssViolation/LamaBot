namespace WebSocketProxy.Client
{
    internal static class TaskExtensions
    {
        public static void IgnoreExceptions(this Task task)
        {
            task.ContinueWith(c =>
            {
                _ = c.Exception;
            }, TaskContinuationOptions.NotOnRanToCompletion);
        }
    }
}
