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

        private static readonly string QuoteEmoji = "💬";

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

        public async Task OnReactionAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            if (reaction.Emote.Name != QuoteEmoji || reaction.User.Value.IsBot)
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

                    // TODO: This should be moved up the stack
                    if (_discord.TestGuild.HasValue && textChannel.Guild.Id != _discord.TestGuild)
                        return;

                    var enabled = await _serverSettings.GetBoolAsync(textChannel.Guild.Id, QuoteSettings.QuoteWithReactionEnabled, false, stoppingToken).ConfigureAwait(false);
                    if (!enabled)
                        continue;

                    var message = await textChannel.GetMessageAsync(job.MessageId).ConfigureAwait(false);
                    var quote = new Quote(textChannel.Guild.Id, 0, message.Author.Id, message.Author.Username, channel.Id, channel.Name, message.Content, message.Id, message.Timestamp.UtcDateTime);
                    quote = await _quoteRepository.AddQuoteAsync(quote).ConfigureAwait(false);

                    await message.AddReactionAsync(new Emoji(QuoteEmoji));
                    await textChannel.SendMessageAsync($"<@{job.Quoter}> added quote #{quote.Id}", allowedMentions: AllowedMentions.None);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Failed to quote message via reaction");
                }
            }
        }
    }
}
