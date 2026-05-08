using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace LamaBot
{
    internal interface IReactionHook
    {
        Task OnReactionAsync(Cacheable<IUserMessage, ulong> message, SocketGuildChannel channel, SocketReaction reaction);
    }
}
