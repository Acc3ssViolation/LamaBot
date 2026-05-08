using Cronos;

namespace LamaBot.Modules.Cron
{
    public record CronMessage(ulong GuildId, ulong ChannelId, string Id, CronExpression Schedule, string Message);
}
