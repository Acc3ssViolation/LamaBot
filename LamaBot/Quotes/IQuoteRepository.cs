namespace LamaBot.Quotes
{
    public interface IQuoteRepository
    {
        Task<Quote?> GetRandomQuoteAsync(ulong guildId, string? author = null, CancellationToken cancellationToken = default);

        Task<Quote> AddQuoteAsync(Quote quote, CancellationToken cancellationToken = default);

        Task<bool> DeleteQuoteAsync(ulong guildId, int id, CancellationToken cancellationToken = default);
    }
}
