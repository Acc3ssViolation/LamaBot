using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection.Metadata;
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

        [SlashCommand("list", "Show all registered user commands")]
        public async Task ListAsync()
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
    }
}
