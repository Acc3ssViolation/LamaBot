using Discord.WebSocket;

namespace LamaBot
{
    public interface IDiscordFacade
    {
        DiscordSocketClient Client { get; }
        Task WaitUntilReadyAsync(CancellationToken cancellationToken);
    }
}
