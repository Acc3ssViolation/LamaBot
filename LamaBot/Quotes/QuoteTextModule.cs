using Discord.Commands;
using Microsoft.Extensions.Logging;

namespace LamaBot.Quotes
{
    public class QuoteTextModule : ModuleBase<SocketCommandContext>
    {
        private readonly IDiscordFacade _discord;
        private readonly IQuoteRepository _quoteRepository;
        private readonly ILogger<QuoteTextModule> _logger;

        public QuoteTextModule(IDiscordFacade discord, IQuoteRepository quoteRepository, ILogger<QuoteTextModule> logger)
        {
            _discord = discord ?? throw new ArgumentNullException(nameof(discord));
            _quoteRepository = quoteRepository ?? throw new ArgumentNullException(nameof(quoteRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [RequireContext(ContextType.Guild)]
        [Command("quote")]
        [Summary("Gets a quote.")]
        public async Task GetQuoteAsync([Remainder] string? quoteSearch = null)
        {
            var guildId = Context.Guild.Id;

            // TODO: This should be moved up the stack
            if (_discord.TestGuild.HasValue && guildId != _discord.TestGuild)
                return;

            Quote? quote = null;
            if (string.IsNullOrWhiteSpace(quoteSearch))
            {
                quote = await _quoteRepository.GetRandomQuoteAsync(guildId);
            }
            else if (int.TryParse(quoteSearch, out var quoteId))
            {
                quote = await _quoteRepository.GetQuoteAsync(guildId, quoteId);
            }
            else
            {
                var resolvedUser = Context.Guild.Users.FirstOrDefault(u =>
                    quoteSearch.Equals(u.Username, StringComparison.OrdinalIgnoreCase) ||
                    quoteSearch.Equals(u.Nickname, StringComparison.OrdinalIgnoreCase)
                );
                if (resolvedUser != null)
                    quote = await _quoteRepository.GetRandomQuoteAsync(guildId, resolvedUser.Username);
                else
                    quote = await _quoteRepository.GetRandomQuoteAsync(guildId, quoteSearch);
            }

            if (quote == null)
            {
                await ReplyAsync("No quote found. Wow, chat more, losers.");
                return;
            }
            await ReplyAsync(embed: quote.CreateEmbed());
        }
    }
}
