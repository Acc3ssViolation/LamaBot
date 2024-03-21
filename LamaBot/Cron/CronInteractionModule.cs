using Cronos;
using Discord;
using Discord.Interactions;
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
            [Summary("channel", "The channel in which to schedule the message")] IGuildChannel channel,
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
                var cronMessage = new CronMessage(guildId.Value, channel.Id, 0, scheduleExpression, message);
                cronMessage = await _cronRepository.AddMessageAsync(cronMessage);
                await ModifyOriginalResponseAsync((msg) =>
                {
                    msg.Content = $"Added cron message {cronMessage.Id}";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add cron message");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [SlashCommand("delete", "Delete a cron message")]
        public async Task DeleteMessageAsync(
            [Summary("id", "The id of the message to delete")] int id
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
                        msg.Content = $"Deleted cron message {id}";
                    else
                        msg.Content = $"Could not find cron message {id}";
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
