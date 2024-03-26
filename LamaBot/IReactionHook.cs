using Discord;
using Discord.WebSocket;

namespace LamaBot
{
    internal interface IReactionHook
    {
        Task OnReactionAsync(Cacheable<IUserMessage, ulong> message, SocketGuildChannel channel, SocketReaction reaction);
    }
}
