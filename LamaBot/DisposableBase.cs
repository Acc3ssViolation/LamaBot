namespace LamaBot
{
    public abstract class DisposableBase : IDisposable
    {
        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    OnDisposing();
                }

                _disposed = true;
            }
        }

        protected abstract void OnDisposing();

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
