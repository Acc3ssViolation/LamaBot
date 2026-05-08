using Discord.WebSocket;
using System.Threading;
using System.Threading.Tasks;

namespace LamaBot
{
    public interface IDiscordFacade
    {
        DiscordSocketClient Client { get; }
        Task WaitUntilReadyAsync(CancellationToken cancellationToken);
    }
}
