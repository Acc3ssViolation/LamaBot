using LamaBot.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LamaBot.Quotes
{
    internal class QuoteRepository : IQuoteRepository
    {
        private readonly Func<ApplicationDbContext> _dbContextFactory;
        private readonly ILogger<QuoteRepository> _logger;

        public QuoteRepository(Func<ApplicationDbContext> dbContextFactory, ILogger<QuoteRepository> logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Quote?> GetRandomQuoteAsync(ulong guildId, string? author = null, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();

            var quotes = dbContext.Quotes.AsNoTracking().OfGuild(guildId);
            if (author != null)
                quotes = quotes.Where(q => q.UserName == author);
            quotes = quotes.OrderBy(q => q.Id);
            var quoteCount = await quotes.CountAsync(cancellationToken).ConfigureAwait(false);
            if (quoteCount == 0)
                return null;

            var randomOffset = Random.Shared.Next(quoteCount);
            var dbQuote = await quotes.Skip(randomOffset).FirstAsync(cancellationToken).ConfigureAwait(false);

            return ToModel(dbQuote);
        }

        public async Task<Quote> AddQuoteAsync(Quote quote, CancellationToken cancellationToken = default)
        {
            if (quote.Id != 0)
                throw new ArgumentException("Quote id should be 0", nameof(quote));
            
            using var dbContext = _dbContextFactory();

            // Two datbase queries to get the amount of quotes, great
            var quoteCount = await dbContext.Quotes.AsNoTracking().OfGuild(quote.GuildId).CountAsync(cancellationToken).ConfigureAwait(false);
            var quoteNumber = quoteCount == 0 ? 0 : await dbContext.Quotes.AsNoTracking().OfGuild(quote.GuildId).MaxAsync(q => q.Id, cancellationToken).ConfigureAwait(false);
            var dbQuote = new DbQuote
            {
                GuildId = quote.GuildId,
                Id = quoteNumber + 1,
            };
            FromModel(dbQuote, quote);
            dbContext.Quotes.Add(dbQuote);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ToModel(dbQuote);
        }

        public async Task<bool> DeleteQuoteAsync(ulong guildId, int id, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();
            var count = await dbContext.Quotes.Where(q => q.GuildId == guildId && q.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            return count > 0;
        }

        private static void FromModel(DbQuote dbQuote, Quote model)
        {
            if (dbQuote.Id != model.Id && dbQuote.GuildId != model.GuildId)
                throw new ArgumentException();

            dbQuote.UserId = model.UserId;
            dbQuote.UserName = model.UserName;
            dbQuote.Content = model.Content;
            dbQuote.ChannelName = model.ChannelName;
            dbQuote.MessageId = model.MessageId;
            dbQuote.TimestampUtc = model.TimestampUtc;
        }

        private static Quote ToModel(DbQuote quote)
        {
            return new Quote(quote.GuildId, quote.Id, quote.UserId, quote.UserName, quote.ChannelName, quote.Content, quote.MessageId, quote.TimestampUtc);
        }
    }

    internal static class QuoteExtensions
    {
        public static IQueryable<DbQuote> OfGuild(this IQueryable<DbQuote> quotes, ulong guildId)
            => quotes.Where(q => q.GuildId == guildId);
    }
}
