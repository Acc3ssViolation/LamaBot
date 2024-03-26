using Discord;
using Discord.WebSocket;
using LamaBot.Servers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace LamaBot.Quotes
{
    internal class QuoteReactionHook : BackgroundService, IReactionHook
    {
        private record ReactionJob(ulong MessageId, ulong ChannelId, ulong Quoter);

        private readonly Channel<ReactionJob> _reactionQueue;
        private readonly IQuoteRepository _quoteRepository;
        private readonly IServerSettings _serverSettings;
        private readonly IDiscordFacade _discord;
        private readonly ILogger<QuoteReactionHook> _logger;

        public QuoteReactionHook(IQuoteRepository quoteRepository, IServerSettings serverSettings, IDiscordFacade discordFacade, ILogger<QuoteReactionHook> logger)
        {
            _quoteRepository = quoteRepository ?? throw new ArgumentNullException(nameof(quoteRepository));
            _discord = discordFacade ?? throw new ArgumentNullException(nameof(discordFacade));
            _serverSettings = serverSettings ?? throw new ArgumentNullException(nameof(serverSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _reactionQueue = Channel.CreateBounded<ReactionJob>(new BoundedChannelOptions(10)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
            });
        }

        public async Task OnReactionAsync(Cacheable<IUserMessage, ulong> message, SocketGuildChannel channel, SocketReaction reaction)
        {
            var quoteEmoji = await GetQuoteEmoji(channel.Guild.Id, CancellationToken.None);
            if (reaction.Emote.ToString() != quoteEmoji)
                return;

            await _reactionQueue.Writer.WriteAsync(new ReactionJob(message.Id, channel.Id, reaction.UserId)).ConfigureAwait(false);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var job = await _reactionQueue.Reader.ReadAsync(stoppingToken).ConfigureAwait(false);

                    var channel = await _discord.Client.GetChannelAsync(job.ChannelId).ConfigureAwait(false);
                    if (channel is not SocketTextChannel textChannel)
                        continue;

                    var message = await textChannel.GetMessageAsync(job.MessageId).ConfigureAwait(false);
                    var quote = new Quote(textChannel.Guild.Id, 0, message.Author.Id, message.Author.Username, channel.Id, channel.Name, message.Content, message.Id, message.Timestamp.UtcDateTime);
                    quote = await _quoteRepository.AddQuoteAsync(quote).ConfigureAwait(false);

                    var emojiText = await GetQuoteEmoji(textChannel.Guild.Id, stoppingToken).ConfigureAwait(false);

                    if (Emote.TryParse(emojiText, out var emote))
                        await message.AddReactionAsync(emote);
                    else
                        await message.AddReactionAsync(new Emoji(emojiText));

                    await textChannel.SendMessageAsync($"<@{job.Quoter}> added quote #{quote.Id} {quote.GetMessageLink()}", allowedMentions: AllowedMentions.None);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Failed to quote message via reaction");
                }
            }
        }

        private async ValueTask<string?> GetQuoteEmoji(ulong guildId, CancellationToken cancellationToken)
        {
            return await _serverSettings.GetSettingAsync(guildId, QuoteSettings.QuoteEmoji, cancellationToken).ConfigureAwait(false);
        }
    }
}
