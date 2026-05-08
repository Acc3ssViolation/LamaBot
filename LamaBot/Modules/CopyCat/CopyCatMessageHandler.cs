using Discord.WebSocket;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LamaBot.Modules.CopyCat
{
    public class CopyCatMessageHandler : ITextMessageHandler
    {
        private record ChannelStats()
        {
            public required string LastMessage { get; set; }
            public required int Count { get; set; }
        }

        private readonly ILogger<CopyCatMessageHandler> _logger;
        private readonly IMemoryCache _memoryCache;

        public CopyCatMessageHandler(ILogger<CopyCatMessageHandler> logger, IMemoryCache memoryCache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        public async Task HandleMessageAsync(SocketMessage message, CancellationToken cancellationToken)
        {
            if (message.Channel is not SocketTextChannel guildChannel)
                return;

            if (string.IsNullOrWhiteSpace(message.Content))
                return;

            var stats = _memoryCache.GetOrCreate($"{guildChannel.Guild.Id}:{guildChannel.Id}", (cache) =>
            {
                cache.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return new ChannelStats { LastMessage = message.Content, Count = 0 };
            })!;
            if (!string.Equals(stats.LastMessage, message.Content, StringComparison.OrdinalIgnoreCase))
            {
                stats.Count = 0;
                stats.LastMessage = message.Content;
            }

            stats.Count++;

            if (stats.Count > 3)
            {
                if (Random.Shared.Next(10) < stats.Count)
                {
                    _logger.LogInformation("Copy Cat ᓚ₍ ^. ̫ .^₎: {Message}", message.Content);
                    await guildChannel.SendMessageAsync(message.Content);
                    stats.Count = 0;
                }
            }
        }
    }
}
