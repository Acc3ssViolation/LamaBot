using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LamaBot.Modules.UserCommands
{
    public interface IUserCommandRepository
    {
        Task<List<UserCommand>> GetUserCommandsAsync(ulong guildId, CancellationToken cancellationToken = default);

        Task<UserCommand> AddCommandAsync(UserCommand command, CancellationToken cancellationToken = default);

        Task<UserCommand> UpdateCommandAsync(UserCommand command, CancellationToken cancellationToken = default);

        Task DeleteCommandAsync(ulong guildId, ulong commandId, CancellationToken cancellationToken = default);
    }
}
