namespace LamaBot.Quotes
{
    public record QuoteWithCount(Quote Quote, int Count);

    public interface IQuoteRepository
    {
        Task RegisterQuoteRequestAsync(Quote quote, ulong userId, CancellationToken cancellationToken = default);

        Task<int> GetQuoteRequestCountAsync(ulong guildId, int quoteId, CancellationToken cancellationToken = default);

        Task<List<QuoteWithCount>> GetQuotesByRequestCountAsync(ulong guildId, int max = 10, CancellationToken cancellationToken = default);

        Task<Quote?> GetQuoteAsync(ulong guildId, int quoteId, CancellationToken cancellationToken = default);

        Task<Quote?> GetRandomQuoteAsync(ulong guildId, string? author = null, CancellationToken cancellationToken = default);

        Task<Quote> AddQuoteAsync(Quote quote, CancellationToken cancellationToken = default);

        Task InsertQuoteAsync(Quote quote, CancellationToken cancellationToken = default);

        Task<bool> DeleteQuoteAsync(ulong guildId, int id, CancellationToken cancellationToken = default);

        Task<List<Quote>> GetQuotesAsync(ulong guildId, CancellationToken cancellationToken = default);
    }
}
