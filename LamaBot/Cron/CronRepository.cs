using Cronos;
using LamaBot.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LamaBot.Cron
{
    internal class CronRepository : ICronRepository
    {
        private const int MaxMessagesPerGuild = 16;

        private readonly Func<ApplicationDbContext> _dbContextFactory;
        private readonly ILogger<CronRepository> _logger;

        public event Action? MessagesUpdated;

        public CronRepository(Func<ApplicationDbContext> dbContextFactory, ILogger<CronRepository> logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CronMessage> AddMessageAsync(CronMessage message, CancellationToken cancellationToken = default)
        {
            var normalId = NormalizeId(message.Id);
            if (string.IsNullOrWhiteSpace(normalId))
                throw new ArgumentException("Message id should not be empty", nameof(message));

            using var dbContext = _dbContextFactory();

            var messageCount = await dbContext.CronMessages.AsNoTracking().OfGuild(message.GuildId).CountAsync(cancellationToken).ConfigureAwait(false);
            if (messageCount >= MaxMessagesPerGuild)
                throw new ArgumentException("Cannot add more messages for this guild", nameof(message));

            var dbMessage = await dbContext.CronMessages.AsNoTracking().OfGuild(message.GuildId).Where(m => m.Id == normalId).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (dbMessage != null)
                throw new ArgumentException($"Message with id '{normalId}' already exists", nameof(message));

            dbMessage = new DbCronMessage
            {
                GuildId = message.GuildId,
                Id = normalId,
            };
            FromModel(dbMessage, message);
            dbContext.CronMessages.Add(dbMessage);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            RunMessagesUpdated();
            return ToModel(dbMessage);
        }

        public async Task<bool> DeleteMessageAsync(ulong guildId, string messageId, CancellationToken cancellationToken = default)
        {
            messageId = NormalizeId(messageId);
            using var dbContext = _dbContextFactory();
            var count = await dbContext.CronMessages.Where(q => q.GuildId == guildId && q.Id == messageId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            if (count > 0)
            {
                RunMessagesUpdated();
                return true;
            }
            return false;
        }

        public async Task<IReadOnlyList<CronMessage>> GetMessagesAsync(ulong? guildId = null, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();
            var messages = dbContext.CronMessages.AsNoTracking();
            if (guildId.HasValue)
                messages = messages.OfGuild(guildId.Value);
            var dbMessages = await messages.ToListAsync(cancellationToken).ConfigureAwait(false);
            return dbMessages.Select(ToModel).ToList();
        }

        private void RunMessagesUpdated()
        {
            if (MessagesUpdated == null)
                return;

            try
            {
                MessagesUpdated.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in MessagesUpdated handler");
            }
        }

        private static string NormalizeId(string id)
            => id.Trim().Replace(' ', '-').ToLowerInvariant();

        private static void FromModel(DbCronMessage dbMessage, CronMessage model)
        {
            if (dbMessage.Id != model.Id && dbMessage.GuildId != model.GuildId)
                throw new ArgumentException();

            dbMessage.Content = model.Message;
            dbMessage.ChannelId = model.ChannelId;
            dbMessage.Schedule = model.Schedule.ToString();
        }

        private static CronMessage ToModel(DbCronMessage message)
        {
            return new CronMessage(message.GuildId, message.ChannelId, message.Id, CronExpression.Parse(message.Schedule), message.Content);
        }
    }

    internal static class CronMessageExtensions
    {
        public static IQueryable<DbCronMessage> OfGuild(this IQueryable<DbCronMessage> messages, ulong guildId)
            => messages.Where(q => q.GuildId == guildId);
    }
}
