using Cronos;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LamaBot.Cron;
using Microsoft.Extensions.Logging;

namespace LamaBot.Quotes
{
    [CommandContextType(InteractionContextType.Guild)]
    [Group("cron", "A wizard is never late, nor is he early, he arrives precisely when he means to.")]
    public class CronInteractionModule : InteractionModuleBase
    {
        private readonly ICronRepository _cronRepository;
        private readonly ILogger<CronInteractionModule> _logger;

        public CronInteractionModule(ICronRepository cronRepository, ILogger<CronInteractionModule> logger)
        {
            _cronRepository = cronRepository ?? throw new ArgumentNullException(nameof(cronRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [SlashCommand("add", "Add a new cron message")]
        public async Task AddMessageAsync(
            [Summary("channel", "The channel in which to schedule the message")] SocketTextChannel channel,
            [Summary("id", "The id of the message, used to refer to it later")] string id,
            [Summary("schedule", "Scheduling, cron syntax")]string schedule,
            [Summary("message", "The message to output")] string message
            )
        {
            var guildId = Context.Interaction.GuildId;
            if (!guildId.HasValue)
            {
                await RespondAsync("This command only be run in a server");
                return;
            }

            if (!CronExpression.TryParse(schedule, CronFormat.Standard, out var scheduleExpression))
            {
                await RespondAsync("Invalid cron schedule");
                return;
            }

            await DeferAsync();

            try
            {
                var cronMessage = new CronMessage(guildId.Value, channel.Id, id, scheduleExpression, message);
                cronMessage = await _cronRepository.AddMessageAsync(cronMessage);
                await ModifyOriginalResponseAsync((msg) =>
                {
                    msg.Content = $"Added cron message `{cronMessage.Id}`";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add cron message");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [SlashCommand("list", "List all configured cron messages")]
        public async Task ListMessagesAsync()
        {
            var guildId = Context.Interaction.GuildId;
            if (!guildId.HasValue)
            {
                await RespondAsync("This command only be run in a server");
                return;
            }

            await DeferAsync();

            try
            {
                var messages = await _cronRepository.GetMessagesAsync(guildId.Value);
                // TODO: There is a max of 25 fields we can output in a single Embed, make sure we don't go over that
                await ModifyOriginalResponseAsync((msg) =>
                {
                    var embed = new EmbedBuilder()
                        .WithTitle("Configured messages");
                    foreach (var message in messages)
                        embed.AddField(message.Id, $"<#{message.ChannelId}> `{message.Schedule}` \"{message.Message}\"");

                    msg.Embed = embed.Build();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete cron message");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [SlashCommand("delete", "Delete a cron message")]
        public async Task DeleteMessageAsync(
            [Summary("id", "The id of the message to delete")] string id
            )
        {
            var guildId = Context.Interaction.GuildId;
            if (!guildId.HasValue)
            {
                await RespondAsync("This command only be run in a server");
                return;
            }

            await DeferAsync();

            try
            {
                var deleted = await _cronRepository.DeleteMessageAsync(guildId.Value, id);
                await ModifyOriginalResponseAsync((msg) =>
                {
                    if (deleted)
                        msg.Content = $"Deleted cron message `{id}`";
                    else
                        msg.Content = $"Could not find cron message `{id}`";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete cron message");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }
    }

}
