using System.Net.WebSockets;

namespace LamaBot.Tunnel
{
    public class WebSocketStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private readonly WebSocket _webSocket;

        public WebSocketStream(WebSocket webSocket)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
        }

        public override void Flush()
        {
            // nop
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var result = _webSocket.ReceiveAsync(buffer.AsMemory(offset, count), CancellationToken.None).Result;
            return result.Count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _webSocket.SendAsync(buffer.AsMemory(offset, count), WebSocketMessageType.Binary, true, CancellationToken.None).AsTask().Wait();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var result = await _webSocket.ReceiveAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
            return result.Count;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _webSocket.SendAsync(buffer.AsMemory(offset, count), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
        }
    }
}
