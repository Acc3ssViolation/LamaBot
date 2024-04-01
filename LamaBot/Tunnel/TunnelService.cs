
using Microsoft.Extensions.Options;
using WebSocketProxy.Client;

namespace LamaBot.Tunnel
{
    public class TunnelService : BackgroundService
    {
        private readonly Func<WebSocketProxyClient> _clientFactory;
        private readonly IOptions<TunnelSettings> _options;
        private readonly ILogger<TunnelService> _logger;

        public TunnelService(Func<WebSocketProxyClient> clientFactory, IOptions<TunnelSettings> options, ILogger<TunnelService> logger)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
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
                    using var webSocket = _clientFactory();
                    webSocket.ForwardingPort = 80;

                    await webSocket.ConnectAsync(_options.Value.Endpoint, _options.Value.Id, _options.Value.Key, stoppingToken).ConfigureAwait(false);
                    await webSocket.AcceptBridgesAsync(stoppingToken).ConfigureAwait(false);
                    hasConnected = true;
                    _logger.LogInformation("Connected to WebSocket proxy server");
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    if (hasConnected)
                        _logger.LogError(ex, "Exception in WebSocket tunnel");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        }
    }
}