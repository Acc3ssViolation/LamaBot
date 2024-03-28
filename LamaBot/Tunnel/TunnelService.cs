
using Microsoft.Extensions.Options;
using System.Buffers;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace LamaBot.Tunnel
{
    public class TunnelService : BackgroundService
    {
        private record BridgeCommand(string Id, string Token);

        private readonly IOptions<TunnelSettings> _options;
        private readonly ILogger<TunnelService> _logger;

        public TunnelService(IOptions<TunnelSettings> options, ILogger<TunnelService> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var hasConnected = false;
                try
                {
                    using var webSocket = new ClientWebSocket();
                    var queryKey = UrlEncoder.Default.Encode(_options.Value.Key);
                    var queryId = UrlEncoder.Default.Encode(_options.Value.Id);
                    var registrationUri = new Uri(_options.Value.Endpoint + $"register?id={queryId}&key={queryKey}");
                    
                    await webSocket.ConnectAsync(registrationUri, stoppingToken).ConfigureAwait(false);
                    hasConnected = true;

                    _logger.LogInformation("Connected to WebSocket proxy server");

                    // Wait for commands from server
                    var buffer = ArrayPool<byte>.Shared.Rent(512);
                    try
                    {
                        while (true)
                        {
                            var result = await webSocket.ReceiveAsync(buffer, stoppingToken).ConfigureAwait(false);

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
                                        BridgeAsync(command, stoppingToken).LogFailure();
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
                        await TryCloseAsync(webSocket, WebSocketCloseStatus.InternalServerError, "exception", stoppingToken).ConfigureAwait(false);
                        throw;
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                        _logger.LogInformation("Disconnecting from WebSocket proxy server");
                    }
                }
                catch (Exception ex) when  (!stoppingToken.IsCancellationRequested)
                {
                    if (hasConnected)
                        _logger.LogError(ex, "Exception in WebSocket tunnel");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
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
                        using var timeoutCts = new CancellationTokenSource(_options.Value.ReadTimeout);
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
            var bridgeUri = new Uri(_options.Value.Endpoint + $"bridge?token={queryToken}");

            using var webSocket = new ClientWebSocket();
            await webSocket.ConnectAsync(bridgeUri, cancellationToken).ConfigureAwait(false);

            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync("localhost", 8080).ConfigureAwait(false);
            var tcpStream = tcpClient.GetStream();
            var tasks = new Task[2]
            {
                CopyWebSocketToStream(webSocket, tcpStream, cancellationToken),
                CopyStreamToWebSocket(tcpStream, webSocket, cancellationToken)
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
                tasks[0].IgnoreExceptions();
                tasks[1].IgnoreExceptions();

                if (webSocket.State == WebSocketState.Open)
                    await TryCloseAsync(webSocket, WebSocketCloseStatus.NormalClosure, "closed", cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Finished websocket tunnel {Command}", bridgeCommand);
            }
        }
    }
}
