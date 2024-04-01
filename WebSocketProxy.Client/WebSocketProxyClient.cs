using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace WebSocketProxy.Client
{
    public class WebSocketProxyClient : IDisposable
    {
        private record BridgeCommand(string Id, string Token);

        private readonly ILogger<WebSocketProxyClient> _logger;
        private Uri? _server;
        private ClientWebSocket? _webSocket;
        private bool _disposed;

        public TimeSpan BridgeCloseTimeout { get; set; } = TimeSpan.FromMinutes(3);
        public int ForwardingPort { get; set; } = 80;

        public WebSocketProxyClient(ILogger<WebSocketProxyClient> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ConnectAsync(Uri server, string id, string key, CancellationToken cancellationToken)
        {
            if (_webSocket != null)
                throw new InvalidOperationException("Already in use");

            _server = server;
            _webSocket = new ClientWebSocket();

            var queryKey = UrlEncoder.Default.Encode(key);
            var queryId = UrlEncoder.Default.Encode(id);
            var registrationUri = new Uri(_server + $"register?id={queryId}&key={queryKey}");
            await _webSocket.ConnectAsync(registrationUri, cancellationToken).ConfigureAwait(false);
        }

        public async Task AcceptBridgesAsync(CancellationToken cancellationToken)
        {
            if (_webSocket == null)
                throw new InvalidOperationException("Cannot accept bridges before connecting to the server");

            // Wait for commands from server
            var buffer = ArrayPool<byte>.Shared.Rent(512);
            try
            {
                while (true)
                {
                    var result = await _webSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Close:
                            {
                                // Websocket closed
                                break;
                            }
                        case WebSocketMessageType.Text:
                            {
                                // Got a JSON command from the server
                                var command = JsonSerializer.Deserialize<BridgeCommand>(buffer.AsSpan(0, result.Count));
                                if (command == null)
                                    throw new InvalidOperationException($"Got null bridge command");

                                // Kick off the bridge
                                BridgeAsync(command, cancellationToken).IgnoreExceptions();
                                break;
                            }
                        default:
                            {
                                throw new InvalidOperationException($"Got unexpected message type {result.MessageType}");
                            }
                    }
                }
            }
            catch (Exception)
            {
                await TryCloseAsync(_webSocket, WebSocketCloseStatus.InternalServerError, "exception", cancellationToken).ConfigureAwait(false);
                _webSocket = null;
                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                _logger.LogInformation("Disconnecting from WebSocket proxy server");
            }
        }

        private async Task TryCloseAsync(WebSocket webSocket, WebSocketCloseStatus status, string reason, CancellationToken cancellationToken)
        {
            try
            {
                await webSocket.CloseOutputAsync(status, reason, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Ignore
            }
        }

        private async Task BridgeAsync(BridgeCommand bridgeCommand, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting websocket tunnel {Command}", bridgeCommand);

            async Task CopyWebSocketToStream(WebSocket webSocket, Stream stream, CancellationToken cancellationToken)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(4 * 1024 * 1024);
                try
                {
                    while (true)
                    {
                        // We want to close the websocket if no requests come in for some time
                        using var timeoutCts = new CancellationTokenSource(BridgeCloseTimeout);
                        using var readCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                        var result = await webSocket.ReceiveAsync(buffer, readCts.Token).ConfigureAwait(false);
                        if (result.MessageType == WebSocketMessageType.Binary)
                        {
                            // Binary data should be forwarded
                            await stream.WriteAsync(buffer, 0, result.Count, cancellationToken).ConfigureAwait(false);
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            // Other end closed the connection
                            await TryCloseAsync(webSocket, WebSocketCloseStatus.NormalClosure, "closed", cancellationToken).ConfigureAwait(false);
                            return;
                        }
                        else
                        {
                            // Other message types are not allowed
                            await TryCloseAsync(webSocket, WebSocketCloseStatus.ProtocolError, "bad-data", cancellationToken).ConfigureAwait(false);
                            return;
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            async Task CopyStreamToWebSocket(Stream stream, WebSocket webSocket, CancellationToken cancellationToken)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(4 * 1024 * 1024);
                try
                {
                    while (true)
                    {
                        var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                        if (read == 0)
                        {
                            // Stream closed, close websocket
                            await TryCloseAsync(webSocket, WebSocketCloseStatus.NormalClosure, "closed", cancellationToken).ConfigureAwait(false);
                            return;
                        }

                        await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, read), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            var queryToken = UrlEncoder.Default.Encode(bridgeCommand.Token);
            var bridgeUri = new Uri(_server! + $"bridge?token={queryToken}");

            using var webSocket = new ClientWebSocket();
            await webSocket.ConnectAsync(bridgeUri, cancellationToken).ConfigureAwait(false);

            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync("localhost", ForwardingPort).ConfigureAwait(false);
            var tcpStream = tcpClient.GetStream();

            using var cts = new CancellationTokenSource();
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
            var tasks = new Task[2]
            {
                CopyWebSocketToStream(webSocket, tcpStream, combinedCts.Token),
                CopyStreamToWebSocket(tcpStream, webSocket, combinedCts.Token)
            };

            try
            {
                await Task.WhenAny(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during bridge of {Command}", bridgeCommand);
            }
            finally
            {
                cts.Cancel();
                tasks[0].IgnoreExceptions();
                tasks[1].IgnoreExceptions();

                if (webSocket.State == WebSocketState.Open)
                    await TryCloseAsync(webSocket, WebSocketCloseStatus.NormalClosure, "closed", cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Finished websocket tunnel {Command}", bridgeCommand);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _webSocket?.Dispose();
                    _webSocket = null;
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
