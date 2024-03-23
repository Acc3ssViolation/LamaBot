using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace LamaBot
{
    [CommandContextType(InteractionContextType.Guild)]
    public class SayModule : InteractionModuleBase
    {
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [SlashCommand("say", "Say something!")]
        public async Task SaySync(
            [Summary("channel", "Where to say something")] SocketTextChannel channel,
            [Summary("message", "What to say")] string message
            )
        {
            await channel.SendMessageAsync(message).ConfigureAwait(false);
            await RespondAsync($"Message delivered to {channel}");
        }
    }
}
