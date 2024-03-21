namespace LamaBot.Cron
{
    public interface ICronRepository
    {
        public event Action? MessagesUpdated;

        Task<IReadOnlyList<CronMessage>> GetMessagesAsync(ulong? guildId = null, CancellationToken cancellationToken = default);

        Task<CronMessage> AddMessageAsync(CronMessage message, CancellationToken cancellationToken = default);

        Task<bool> DeleteMessageAsync(ulong guildId, int messageId, CancellationToken cancellationToken = default);
    }
}
