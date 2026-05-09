using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace LamaBot.Modules.Say
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

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [SlashCommand("reply", "Say something in response to something!")]
        public async Task ReplyAsync(
            [Summary("message", "Discord message link of the message to reply to")] string messageLink,
            [Summary("reply", "What it is that you want to say")] string reply
            )
        {
            await DeferAsync(ephemeral: true);
            var parts = messageLink.Split('/');
            if (parts.Length < 3 || !ulong.TryParse(parts[^1], out var messageId) || !ulong.TryParse(parts[^2], out var channelId) || !ulong.TryParse(parts[^3], out var guildId))
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = "400 Invalid message link");
                return;
            }

            var channel = (await Context.Client.GetChannelAsync(channelId)) as SocketTextChannel;
            if (channel == null)
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = "404 Channel not found");
                return;
            }

            var message = await channel.GetMessageAsync(messageId);
            if (message == null)
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = "404 Message not found");
                return;
            }

            await channel.SendMessageAsync(messageReference: new MessageReference(messageId: messageId, channelId: channelId, guildId: guildId), text: reply);
            await ModifyOriginalResponseAsync(msg => msg.Content = $"Reply delivered to {channel}");
        }
    }
}
