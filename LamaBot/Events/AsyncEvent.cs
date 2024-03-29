namespace LamaBot.Events
{
    public class AsyncEvent<T> 
    {
        private class AsyncEventSubscription : DisposableBase, IDisposable
        {
            private readonly AsyncEvent<T> _event;
            private readonly Func<T, CancellationToken, Task> _handler;

            public AsyncEventSubscription(AsyncEvent<T> @event, Func<T, CancellationToken, Task> handler)
            {
                _event = @event ?? throw new ArgumentNullException(nameof(@event));
                _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            }

            protected override void OnDisposing()
            {
                lock (_event._handlers)
                    _event._handlers.Remove(_handler);
            }
        }

        private readonly List<Func<T, CancellationToken, Task>> _handlers = new();

        public async Task InvokeAsync(T data, CancellationToken cancellationToken)
        {
            List<Func<T, CancellationToken, Task>> handlers;
            lock (_handlers)
                handlers = _handlers.ToList();

            foreach (var handler in handlers)
            {
                try
                {
                    await handler(data, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Ignored :(
                }
            }
        }

        public IDisposable Subscribe(Func<T, CancellationToken, Task> handler)
        {
            lock (_handlers)
            {
                _handlers.Add(handler);
            }
            return new AsyncEventSubscription(this, handler);
        }
    }
}
