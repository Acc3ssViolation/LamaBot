using Discord.WebSocket;

namespace LamaBot
{
    internal interface IDiscordFacade
    {
        DiscordSocketClient Client { get; }
        ulong? TestGuild { get; }
        Task WaitUntilReadyAsync(CancellationToken cancellationToken);
    }
}
