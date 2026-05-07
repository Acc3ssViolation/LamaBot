using Discord.WebSocket;

namespace LamaBot
{
    public interface ITextMessageHandler
    {
        Task HandleMessageAsync(SocketMessage message, CancellationToken cancellationToken);
    }
}
