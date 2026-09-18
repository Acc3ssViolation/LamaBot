using System;
using System.Collections.Generic;

namespace LamaBot.Web
{
    public record ApiKeyInfo(ulong GuildId, ICollection<string> Roles, DateTime? ExpirationUtc);

    public record ApiKey(string Key, ulong GuildId, ICollection<string> Roles, DateTime? ExpirationUtc) : ApiKeyInfo(GuildId, Roles, ExpirationUtc);
}
