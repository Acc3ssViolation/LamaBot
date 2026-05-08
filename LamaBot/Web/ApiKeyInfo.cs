using System;
using System.Collections.Generic;

namespace LamaBot.Web
{
    public record ApiKeyInfo(ulong GuildId, ICollection<string> Roles, DateTime? ExpirationUtc);
}
