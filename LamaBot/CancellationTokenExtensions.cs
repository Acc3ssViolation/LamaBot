namespace LamaBot
{
    internal static class CancellationTokenExtensions
    {
        public static async Task UntilCancelled(this CancellationToken token)
        {
            var tcs = new TaskCompletionSource();
            using (token.Register(() => tcs.TrySetCanceled()))
                await tcs.Task;
        }

        public static async Task UntilCancelledNoThrow(this CancellationToken token)
        {
            try
            {
                var tcs = new TaskCompletionSource();
                using (token.Register(() => tcs.TrySetCanceled()))
                    await tcs.Task;
            }
            catch 
            {
            }
        }
    }
}
