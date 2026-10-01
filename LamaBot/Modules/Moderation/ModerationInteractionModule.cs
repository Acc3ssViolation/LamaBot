using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LamaBot.Modules.Audit;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LamaBot.Modules.Moderation
{
    public class ModerationInteractionModule : InteractionModuleBase
    {
        private readonly IAuditLog _audit;

        public ModerationInteractionModule(IAuditLog audit)
        {
            _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        }

        [SlashCommand("clean", "Throw out the trash, or in this case, the last messages posted in this channel")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task CleanChannelAsync([Summary("count", "How many messages to delete")][MaxValue(50)] int count)
        {
            if (Context.Channel is not SocketTextChannel textChannel)
            {
                await RespondAsync("Can only clean text channels, sorry");
                return;
            }

            await DeferAsync(true);

            var messages = await textChannel.GetMessagesAsync(count + 10).FlattenAsync();
            await textChannel.DeleteMessagesAsync(messages.Where(m => !(m.Flags ?? MessageFlags.None).HasFlag(MessageFlags.Ephemeral) && m.Source != MessageSource.System).Take(count));
            await _audit.LogAsync(Context.Interaction.GuildId ?? 0, Context.Interaction.User.Id, nameof(Say), $"Cleared {count} messages from channel: [link](https://discord.com/channels/{textChannel.Guild.Id}/{textChannel.Id})");
            await ModifyOriginalResponseAsync(msg => msg.Content = $"Deleted {count} messages from this channel");
        }
    }
}
