using Discord;
using Discord.WebSocket;
using LamaBot.Servers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LamaBot.Modules.Ping
{
    public class PingResponseMessageHandler : ITextMessageHandler
    {
        private readonly IServerSettings _settings;

        public PingResponseMessageHandler(IServerSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task HandleMessageAsync(SocketMessage message, CancellationToken cancellationToken)
        {
            if (message.Channel is not SocketTextChannel guildChannel)
                return;

            var emoji = await _settings.GetSettingAsync(guildChannel.Guild.Id, PingSettings.PingResponseEmoji, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(emoji))
                return;

            if (message.Content.Contains("@everyone", StringComparison.Ordinal))
            {
                if (Emote.TryParse(emoji, out var emote))
                    await message.AddReactionAsync(emote);
                else
                    await message.AddReactionAsync(new Emoji(emoji));
            }
        }
    }
}
