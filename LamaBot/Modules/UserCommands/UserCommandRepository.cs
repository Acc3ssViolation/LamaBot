using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LamaBot.Modules.UserCommands
{
    public class UserCommandRepository : IUserCommandRepository
    {
        private readonly List<UserCommand> _commands = [
            new UserCommand(0, 1, "daglimiet", MatchType.Contains, false, "https://tenor.com/view/creepy-old-lady-laugh-evil-witch-gif-5182128"),
            new UserCommand(0, 2, "bara bada bastu", MatchType.Equals, false, "https://tenor.com/view/bara-bada-bastu-kaj-kaj-sauna-gif-7128029960041048937"),
            ];

        public Task DeleteCommandAsync(ulong guildId, ulong commandId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<List<UserCommand>> GetUserCommandsAsync(ulong guildId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_commands);
        }

        public Task<UserCommand> UpsertCommandAsync(UserCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(command);
        }
    }
}
