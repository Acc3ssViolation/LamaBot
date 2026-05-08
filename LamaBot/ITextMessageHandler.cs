using Discord.WebSocket;
using System.Threading;
using System.Threading.Tasks;

namespace LamaBot
{
    public interface ITextMessageHandler
    {
        Task HandleMessageAsync(SocketMessage message, CancellationToken cancellationToken);
    }
}
