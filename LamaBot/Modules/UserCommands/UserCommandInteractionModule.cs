using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;

namespace LamaBot.Modules.UserCommands
{
    [CommandContextType(InteractionContextType.Guild)]
    [Group("commands", "Because all good things are customized by end users")]
    public class UserCommandInteractionModule : InteractionModuleBase
    {
        private readonly IUserCommandRepository _repository;
        private readonly ILogger<UserCommandInteractionModule> _logger;

        public UserCommandInteractionModule(IUserCommandRepository repository, ILogger<UserCommandInteractionModule> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [SlashCommand("list", "Show all registered user commands")]
        public async Task ListAsync()
        {
            var guildId = Context.Interaction.GuildId;
            if (!guildId.HasValue)
            {
                await RespondAsync("This command only be run in a server");
                return;
            }

            await DeferAsync(ephemeral: true);

            try
            {
                var commands = await _repository.GetUserCommandsAsync(guildId.Value);

                await ModifyOriginalResponseAsync((msg) =>
                {
                    // TODO: Something better lol
                    var sb = new StringBuilder();

                    foreach (var command in commands)
                        sb.AppendLine(command.ToString());

                    msg.Content = sb.ToString();
                    msg.Flags = MessageFlags.SuppressEmbeds;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user commands");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }

        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [SlashCommand("delete", "Delete a user command")]
        public async Task DeleteAsync([Summary("id", "The id of the user command to delete")] ulong commandId)
        {
            var guildId = Context.Interaction.GuildId;
            if (!guildId.HasValue)
            {
                await RespondAsync("This command only be run in a server");
                return;
            }

            await DeferAsync(ephemeral: true);

            try
            {
                var removed = await _repository.DeleteCommandAsync(guildId.Value, commandId);
                
                await ModifyOriginalResponseAsync((msg) =>
                {
                    if (removed)
                        msg.Content = $"Removed user command {commandId}";
                    else
                        msg.Content = $"Unable to find user command {commandId}";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete user commands");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }

        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [SlashCommand("add", "Add a user command")]
        public async Task AddAsync(
            [Summary("trigger", "Which words to trigger on")] string trigger,
            [Summary("response", "The response to reply with, may be an image URL")] string response,
            [Summary("match", "How to match the trigger, defaults to Contains")] MatchType match = MatchType.Contains,
            [Summary("case", "Should matching be case sensitive, defaults to false")] bool caseSensitive = false)
        {
            var guildId = Context.Interaction.GuildId;
            if (!guildId.HasValue)
            {
                await RespondAsync("This command only be run in a server");
                return;
            }

            await DeferAsync(ephemeral: true);

            try
            {
                var command = new UserCommand(guildId.Value, 0, trigger, match, caseSensitive, response);
                command = await _repository.AddCommandAsync(command);

                await ModifyOriginalResponseAsync((msg) =>
                {
                    msg.Content = $"Command {command.Id} \"{command.Trigger}\" was added";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add user command");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }
    }
}
