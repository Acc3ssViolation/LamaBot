using Discord.WebSocket;
using System.Text.RegularExpressions;

namespace LamaBot
{
    public static partial class SocketGuildExtensions
    {
        [GeneratedRegex("<@(\\d+)>")]
        private static partial Regex UserRefRegex();

        public static SocketGuildUser? ResolveUser(this SocketGuild guild, string userRef)
        {
            ArgumentNullException.ThrowIfNull(guild);
            ArgumentNullException.ThrowIfNull(userRef);

            var match = UserRefRegex().Match(userRef);
            if (!match.Success)
                return null;

            var userId = ulong.Parse(match.Groups[1].ValueSpan);
            return guild.GetUser(userId);
        }
    }
}
