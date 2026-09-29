using System;

namespace LamaBot.Modules.Audit
{
    public record AuditEntry(DateTime TimestampUtc, ulong GuildId, ulong UserId, string Module, string Message);
}
