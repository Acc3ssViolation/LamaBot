using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LamaBot.Modules.UserCommands
{
    public class UserCommandMessageHandler : ITextMessageHandler
    {
        private class CompiledUserCommand
        {
            public UserCommand Command { get; }

            public CompiledUserCommand(UserCommand command)
            {
                Command = command ?? throw new ArgumentNullException(nameof(command));
            }

            public bool Matches(string message)
            {
                // TODO: Compile this to regex?
                var comparison = Command.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                return Command.MatchType switch
                {
                    MatchType.Equals => string.Equals(message, Command.Trigger, comparison),
                    MatchType.Contains => message.Contains(Command.Trigger, comparison),
                    MatchType.StartsWith => message.StartsWith(Command.Trigger, comparison),
                    MatchType.EndsWith => message.EndsWith(Command.Trigger, comparison),
                    _ => false,
                };
            }

            public async Task RespondAsync(SocketTextChannel channel)
            {
                await channel.SendMessageAsync(text: Command.Response).ConfigureAwait(false);
            }
        }

        private record GuildCommands(List<CompiledUserCommand> Commands)
        {
            public int LoadedVersion { get; set; } = 0;
            public int DesiredVersion { get; set; } = 1;
        }

        private readonly IUserCommandRepository _repository;
        private readonly ILogger<UserCommandMessageHandler> _logger;

        private Dictionary<ulong, GuildCommands> _commands = new();

        public UserCommandMessageHandler(IUserCommandRepository repository, ILogger<UserCommandMessageHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleMessageAsync(SocketMessage message, CancellationToken cancellationToken)
        {
            if (message.Channel is not SocketTextChannel guildChannel)
                return;

            if (string.IsNullOrWhiteSpace(message.Content))
                return;

            var guildCommands = await GetCommandsAsync(guildChannel.Guild.Id).ConfigureAwait(false);

            foreach (var command in guildCommands.Commands)
            {
                if (command.Matches(message.Content))
                    await command.RespondAsync(guildChannel).ConfigureAwait(false);
            }
        }

        private async ValueTask<GuildCommands> GetCommandsAsync(ulong guildId)
        {
            if (!_commands.TryGetValue(guildId, out var guildCommands))
            {
                guildCommands = new GuildCommands(new List<CompiledUserCommand>());
                _commands[guildId] = guildCommands;
            }

            if (guildCommands.LoadedVersion != guildCommands.DesiredVersion)
            {
                guildCommands.LoadedVersion = guildCommands.DesiredVersion;

                var commands = await _repository.GetUserCommandsAsync(guildId).ConfigureAwait(false);
                guildCommands.Commands.Clear();
                foreach (var command in commands)
                    guildCommands.Commands.Add(new CompiledUserCommand(command));
            }

            return guildCommands;
        }
    }
}
