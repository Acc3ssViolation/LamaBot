using Discord.Commands;
using Microsoft.Extensions.Logging;

namespace LamaBot.Quotes
{
    public class QuoteTextModule : ModuleBase<SocketCommandContext>
    {
        private readonly IQuoteRepository _quoteRepository;
        private readonly ILogger<QuoteTextModule> _logger;

        public QuoteTextModule(IQuoteRepository quoteRepository, ILogger<QuoteTextModule> logger)
        {
            _quoteRepository = quoteRepository ?? throw new ArgumentNullException(nameof(quoteRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [RequireContext(ContextType.Guild)]
        [Command("quote")]
        [Summary("Gets a quote.")]
        public async Task GetQuoteAsync([Remainder] string? quoteSearch = null)
        {
            if (string.IsNullOrWhiteSpace(quoteSearch))
            {
                var guildId = Context.Guild.Id;
                var quote = await _quoteRepository.GetRandomQuoteAsync(guildId);
                if (quote == null)
                {
                    await ReplyAsync("No quote found. Wow, chat more, losers.");
                    return;
                }
                await ReplyAsync(embed: quote.CreateEmbed());
            }
        }
    }
}
