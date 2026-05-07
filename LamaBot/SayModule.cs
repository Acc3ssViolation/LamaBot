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
        public async Task SayAsync(
            [Summary("channel", "Where to say something")] SocketTextChannel channel,
            [Summary("message", "What to say")] string message
            )
        {
            await DeferAsync(ephemeral: true);
            await channel.SendMessageAsync(message);
            await ModifyOriginalResponseAsync(msg => msg.Content = $"Message delivered to {channel}");
        }
    }
}
