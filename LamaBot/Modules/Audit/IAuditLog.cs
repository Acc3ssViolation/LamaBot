using System.Threading.Tasks;

namespace LamaBot.Modules.Audit
{
    public interface IAuditLog
    {
        Task LogAsync(ulong guildId, ulong userId, string module, string message);
    }
}
