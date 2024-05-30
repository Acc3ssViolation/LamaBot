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

        public async Task<Quote?> GetQuoteAsync(ulong guildId, int quoteId, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();

            var quote = await dbContext.Quotes.AsNoTracking().OfGuild(guildId).SingleOrDefaultAsync(q => q.Id == quoteId, cancellationToken).ConfigureAwait(false);
            if (quote == null)
                return null;

            return ToModel(quote);
        }

        public async Task RegisterQuoteRequestAsync(Quote quote, ulong userId, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();
            var dbQuoteRequest = new DbQuoteRequest
            {
                Id = quote.Id,
                GuildId = quote.GuildId,
                UserId = userId,
                TimestampUtc = DateTime.UtcNow,
            };
            dbContext.QuoteRequests.Add(dbQuoteRequest);

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<int> GetQuoteRequestCountAsync(ulong guildId, int quoteId, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();

            return await dbContext.QuoteRequests.AsNoTracking().CountAsync(q => q.Id == quoteId && q.GuildId == guildId, cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<QuoteWithCount>> GetQuotesByRequestCountAsync(ulong guildId, int max = 10, ulong? userId = null, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();

            // TODO: Rewrite this as a single LINQ query that gets handled as SQL instead of this monstrosity
            var qbQuoteRequests = dbContext.QuoteRequests.AsNoTracking()
                .Where(q => q.GuildId == guildId);
            if (userId.HasValue)
                qbQuoteRequests = qbQuoteRequests.Where(q => q.UserId == userId);

            var quoteStats = await qbQuoteRequests
                .GroupBy(q => q.Id)
                .OrderByDescending(q => q.Count())
                .Take(max)
                .Select(q => new { Id = q.Key, Count = q.Count() })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var quotes = await dbContext.Quotes.AsNoTracking()
                .Where(q => q.GuildId == guildId)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var result = new List<QuoteWithCount>(quoteStats.Count);

            foreach (var stat in quoteStats)
            {
                var quote = quotes.FirstOrDefault(q => q.Id == stat.Id);
                if (quote != null)
                    result.Add(new QuoteWithCount(ToModel(quote), stat.Count));
            }

            return result;
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

            // Make sure this message is not quoted already
            var quoteExists = await dbContext.Quotes.AsNoTracking().OfGuild(quote.GuildId).Where(q => q.MessageId == quote.MessageId && q.ChannelId == quote.ChannelId).AnyAsync(cancellationToken).ConfigureAwait(false);
            if (quoteExists)
                throw new ArgumentException("Message has already been quoted");

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

        public async Task InsertQuoteAsync(Quote quote, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();

            var dbQuote = await dbContext.Quotes.OfGuild(quote.GuildId).SingleOrDefaultAsync(q => q.Id == quote.Id, cancellationToken).ConfigureAwait(false);
            if (dbQuote == null)
            {
                dbQuote = new DbQuote
                {
                    GuildId = quote.GuildId,
                    Id = quote.Id,
                };
                dbContext.Quotes.Add(dbQuote);
            }
            FromModel(dbQuote, quote);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> DeleteQuoteAsync(ulong guildId, int id, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();
            var count = await dbContext.Quotes.Where(q => q.GuildId == guildId && q.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            return count > 0;
        }

        public async Task<List<Quote>> GetQuotesAsync(ulong guildId, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();
            var quotes = await dbContext.Quotes.AsNoTracking().OfGuild(guildId).ToListAsync(cancellationToken).ConfigureAwait(false);
            return quotes.Select(ToModel).ToList();
        }

        private static void FromModel(DbQuote dbQuote, Quote model)
        {
            if (dbQuote.Id != model.Id && dbQuote.GuildId != model.GuildId)
                throw new ArgumentException();

            dbQuote.UserId = model.UserId;
            dbQuote.UserName = model.UserName;
            dbQuote.Content = model.Content;
            dbQuote.ChannelId = model.ChannelId;
            dbQuote.ChannelName = model.ChannelName;
            dbQuote.MessageId = model.MessageId;
            dbQuote.TimestampUtc = model.TimestampUtc;
        }

        private static Quote ToModel(DbQuote quote)
        {
            return new Quote(quote.GuildId, quote.Id, quote.UserId, quote.UserName, quote.ChannelId, quote.ChannelName, quote.Content, quote.MessageId, DateTime.SpecifyKind(quote.TimestampUtc, DateTimeKind.Utc));
        }
    }

    internal static class QuoteExtensions
    {
        public static IQueryable<DbQuote> OfGuild(this IQueryable<DbQuote> quotes, ulong guildId)
            => quotes.Where(q => q.GuildId == guildId);
    }
}
