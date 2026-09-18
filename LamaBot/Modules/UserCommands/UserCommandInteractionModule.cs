using Discord;
using Discord.Interactions;
using LamaBot.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LamaBot.Modules.UserCommands
{
    [CommandContextType(InteractionContextType.Guild)]
    [Group("commands", "Because all good things are customized by end users")]
    public class UserCommandInteractionModule : InteractionModuleBase
    {
        private readonly IInteractiveComponentService _componentService;
        private readonly IUserCommandRepository _repository;
        private readonly ILogger<UserCommandInteractionModule> _logger;

        public UserCommandInteractionModule(IInteractiveComponentService componentService, IUserCommandRepository repository, ILogger<UserCommandInteractionModule> logger)
        {
            _componentService = componentService ?? throw new ArgumentNullException(nameof(componentService));
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
                await ModifyOriginalResponseAsync(async (msg) =>
                {
                    var helper = new UserCommandHelper(_repository, _componentService);
                    await helper.ShowFirstPage(msg, guildId.Value, this);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user commands");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }

        private class UserCommandHelper : PagedResponseHelper<UserCommand, object>
        {
            private readonly IUserCommandRepository _repository;

            public UserCommandHelper(IUserCommandRepository repository, IInteractiveComponentService componentService) : base(componentService)
            {
                _repository = repository;
            }

            protected override EmbedFieldBuilder GetField(UserCommand item)
            {
                return new EmbedFieldBuilder()
                    .WithName($"#{item.Id} - {item.Trigger} - {item.MatchType}")
                    .WithValue(item.Response);
            }

            protected override async Task<IReadOnlyList<UserCommand>> GetItemsAsync(ulong guildId, object options)
            {
                return await _repository.GetUserCommandsAsync(guildId);
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
