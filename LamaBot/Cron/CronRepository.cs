using Cronos;
using LamaBot.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LamaBot.Cron
{
    internal class CronRepository : ICronRepository
    {
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
            if (message.Id != 0)
                throw new ArgumentException("Message id should be 0", nameof(message));

            using var dbContext = _dbContextFactory();

            var messageCount = await dbContext.CronMessages.AsNoTracking().OfGuild(message.GuildId).CountAsync(cancellationToken).ConfigureAwait(false);
            var messageNumber = messageCount == 0 ? 0 : await dbContext.CronMessages.AsNoTracking().OfGuild(message.GuildId).MaxAsync(q => q.Id, cancellationToken).ConfigureAwait(false);
            var dbMessage = new DbCronMessage
            {
                GuildId = message.GuildId,
                Id = messageNumber + 1,
            };
            FromModel(dbMessage, message);
            dbContext.CronMessages.Add(dbMessage);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            RunMessagesUpdated();
            return ToModel(dbMessage);
        }

        public async Task<bool> DeleteMessageAsync(ulong guildId, int messageId, CancellationToken cancellationToken = default)
        {
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
