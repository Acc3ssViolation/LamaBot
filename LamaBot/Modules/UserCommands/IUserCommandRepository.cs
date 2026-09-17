using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LamaBot.Modules.UserCommands
{
    public interface IUserCommandRepository
    {
        public event Action<ulong>? CommandsUpdated;

        Task<List<UserCommand>> GetUserCommandsAsync(ulong guildId, CancellationToken cancellationToken = default);

        Task<UserCommand> AddCommandAsync(UserCommand command, CancellationToken cancellationToken = default);

        Task<UserCommand> UpdateCommandAsync(UserCommand command, CancellationToken cancellationToken = default);

        Task<bool> DeleteCommandAsync(ulong guildId, ulong commandId, CancellationToken cancellationToken = default);
    }
}
