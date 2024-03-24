using Discord.WebSocket;

namespace LamaBot
{
    public interface IDiscordFacade
    {
        DiscordSocketClient Client { get; }
        ulong? TestGuild { get; }
        Task WaitUntilReadyAsync(CancellationToken cancellationToken);
    }
}
