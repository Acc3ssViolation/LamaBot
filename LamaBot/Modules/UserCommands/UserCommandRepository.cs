using LamaBot.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LamaBot.Modules.UserCommands
{
    public class UserCommandRepository : IUserCommandRepository
    {
        private readonly Func<ApplicationDbContext> _dbContextFactory;
        private readonly ILogger<UserCommandRepository> _logger;

        public event Action<ulong>? CommandsUpdated;

        public UserCommandRepository(Func<ApplicationDbContext> dbContextFactory, ILogger<UserCommandRepository> logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<UserCommand>> GetUserCommandsAsync(ulong guildId, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();

            var dbCommands = await dbContext
                .UserCommands
                .AsNoTracking()
                .Where(c => c.GuildId == guildId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            return [.. dbCommands.Select(Map)];
        }

        public async Task<UserCommand> AddCommandAsync(UserCommand command, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();

            var maxId = await dbContext
                .UserCommands
                .AsNoTracking()
                .Where(c => c.GuildId == command.GuildId)
                .OrderByDescending(c => (long)c.Id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            command = command with { Id = (maxId?.Id ?? 0) + 1 };

            var dbCommand = Map(command);
            dbContext.UserCommands.Add(dbCommand);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Added user command {Command}", command);

            RunCommandsUpdated(dbCommand.GuildId);

            return Map(dbCommand);
        }

        public async Task<bool> DeleteCommandAsync(ulong guildId, ulong commandId, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();

            _logger.LogInformation("Deleting user command {GuildId}:{Id}", guildId, commandId);

            var removed = await dbContext
                .UserCommands
                .Where(c => c.GuildId == guildId && c.Id == commandId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (removed > 0)
                RunCommandsUpdated(guildId);

            return removed > 0;
        }

        public async Task<UserCommand> UpdateCommandAsync(UserCommand command, CancellationToken cancellationToken = default)
        {
            using var dbContext = _dbContextFactory();

            var dbCommand = await dbContext
                .UserCommands
                .Where(c => c.GuildId == command.GuildId && c.Id == command.Id)
                .FirstOrDefaultAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (dbCommand == null)
                throw new ArgumentException("Command not found", nameof(command));

            dbCommand.Json = Map(command).Json;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Updated user command {Command}", command);

            RunCommandsUpdated(dbCommand.GuildId);

            return Map(dbCommand);
        }

        private static UserCommand Map(DbUserCommand dbCommand)
        {
            var command = JsonSerializer.Deserialize<UserCommand>(dbCommand.Json);
            return command! with { GuildId = dbCommand.GuildId, Id =  command.Id };
        }

        private static DbUserCommand Map(UserCommand command)
        {
            var dbCommand = new DbUserCommand
            {
                GuildId = command.GuildId,
                Id = command.Id,
                Json = JsonSerializer.Serialize<UserCommand>(command),
            };
            return dbCommand;
        }

        private void RunCommandsUpdated(ulong guildId)
        {
            if (CommandsUpdated == null)
                return;

            try
            {
                CommandsUpdated.Invoke(guildId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in CommandsUpdated handler");
            }
        }
    }
}
