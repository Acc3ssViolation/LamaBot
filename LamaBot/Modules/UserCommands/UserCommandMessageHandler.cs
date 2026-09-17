using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LamaBot.Modules.UserCommands
{
    public class UserCommandMessageHandler : DisposableBase, ITextMessageHandler
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
                // TODO: We want this to be in the server timezone and language really, but who cares
                var now = DateTime.Now;
                var response = Command.Response
                    .Replace("$datum", $"$dag {now.Day} $maand {now.Year}")
                    .Replace("$dag", now.DayOfWeek switch
                    {
                        DayOfWeek.Monday => "maandag",
                        DayOfWeek.Tuesday => "dinsdag",
                        DayOfWeek.Wednesday => "woensdag",
                        DayOfWeek.Thursday => "donderdag",
                        DayOfWeek.Friday => "vrijdag",
                        DayOfWeek.Saturday => "zaterdag",
                        DayOfWeek.Sunday => "zondag",
                        _ => "wtf"
                    })
                    .Replace("$maand", now.Month switch
                    {
                        1 => "januari",
                        2 => "februari",
                        3 => "maart",
                        4 => "april",
                        5 => "mei",
                        6 => "juni",
                        7 => "juli",
                        8 => "augustus",
                        9 => "september",
                        10 => "oktober",
                        11 => "november",
                        12 => "december",
                        _ => "wtf"
                    })
                    .Replace("$week", System.Globalization.ISOWeek.GetWeekOfYear(now).ToString())
                    .Replace("$jaar", now.Year.ToString());

                await channel.SendMessageAsync(text: response).ConfigureAwait(false);
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
        private readonly AsyncLock _lock = new();

        public UserCommandMessageHandler(IUserCommandRepository repository, ILogger<UserCommandMessageHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _repository.CommandsUpdated += OnCommandsUpdated;
        }

        public async Task HandleMessageAsync(SocketMessage message, CancellationToken cancellationToken)
        {
            if (message.Channel is not SocketTextChannel guildChannel)
                return;

            if (string.IsNullOrWhiteSpace(message.Content))
                return;

            using var l = await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

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

                _logger.LogInformation("Loaded user commands version {Version} for guild {GuildId}", guildCommands.LoadedVersion, guildId);
            }

            return guildCommands;
        }

        protected override void OnDisposing()
        {
            _repository.CommandsUpdated -= OnCommandsUpdated;
        }

        private void OnCommandsUpdated(ulong guildId)
        {
            using var l = _lock.WaitBlocking(CancellationToken.None);

            if (_commands.TryGetValue(guildId, out var commands))
                commands.DesiredVersion++;
        }
    }
}
