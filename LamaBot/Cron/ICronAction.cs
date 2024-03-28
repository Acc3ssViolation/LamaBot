using Cronos;

namespace LamaBot.Cron
{
    public interface ICronActionProvider
    {
        event Action? ActionsUpdated;
        Task<IEnumerable<ICronAction>> GetActionsAsync(CancellationToken cancellationToken);
    }

    public interface ICronAction
    {
        CronExpression Schedule { get; }
        Task ActionAsync();
    }
}
