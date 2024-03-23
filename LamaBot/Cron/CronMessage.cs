using Cronos;

namespace LamaBot.Cron
{
    public record CronMessage(ulong GuildId, ulong ChannelId, string Id, CronExpression Schedule, string Message);
}
