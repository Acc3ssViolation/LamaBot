using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LamaBot.Modules.Audit
{
    public interface IAuditRepository
    {
        Task<List<AuditEntry>> GetAuditEntriesAsync(ulong? guildId, ulong? userId, string? module, CancellationToken cancellationToken = default);
    }
}
