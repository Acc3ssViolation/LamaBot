
using Microsoft.Extensions.Options;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace LamaBot.Tunnel
{
    public class TunnelService : BackgroundService
    {
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
                try
                {
                    using var webSocket = new ClientWebSocket();
                    await webSocket.ConnectAsync(_options.Value.Endpoint, stoppingToken).ConfigureAwait(false);

                    _logger.LogInformation("Connected to WebSocket proxy server");

                    var webSocketStream = new WebSocketStream(webSocket);
                    using var tcpClient = new TcpClient();
                    await tcpClient.ConnectAsync("localhost", 8080).ConfigureAwait(false);
                    var tcpStream = tcpClient.GetStream();
                    await Task.WhenAll(
                        webSocketStream.CopyToAsync(tcpStream, stoppingToken), 
                        tcpStream.CopyToAsync(webSocketStream, stoppingToken)
                        ).ConfigureAwait(false);
                }
                catch (Exception ex) when  (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Exception in WebSocket tunnel");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        }
    }
}
