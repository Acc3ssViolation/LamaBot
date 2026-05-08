using Cronos;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LamaBot.Modules.Cron
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
