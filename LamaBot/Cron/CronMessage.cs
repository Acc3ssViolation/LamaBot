using Cronos;

namespace LamaBot.Cron
{
    public record CronMessage(ulong GuildId, ulong ChannelId, int Id, CronExpression Schedule, string Message);
}
