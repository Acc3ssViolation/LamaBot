using LamaBot.Modules.Audit;
using LamaBot.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace LamaBot.Modules.UserCommands
{
    [ApiController]
    [Route("api/v1/usercommands")]
    [Authorize]
    public partial class UserCommandApiController : ControllerBase
    {
        private readonly IAuditLog _audit;

        public UserCommandApiController(IAuditLog audit)
        {
            _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        }

        [HttpGet("{guildId}")]
        [RequireRole(WebRoles.UserCommandReader)]
        public async Task<IActionResult> GetCommandsAsync([FromServices] IUserCommandRepository repository, [FromRoute] ulong guildId, [FromQuery] string key)
        {
            var userCommands = await repository.GetUserCommandsAsync(guildId, HttpContext.RequestAborted);
            return new JsonResult(userCommands);
        }

        [HttpPost("{guildId}")]
        [RequireRole(WebRoles.UserCommandEditor)]
        public async Task<IActionResult> AddCommandAsync([FromServices] IUserCommandRepository repository, [FromRoute] ulong guildId, [FromBody] UserCommand command, [FromQuery] string key)
        {
            command = command with { GuildId = guildId, Id = 0 };
            command = await repository.AddCommandAsync(command, HttpContext.RequestAborted);
            await _audit.LogAsync(guildId, 0, nameof(LamaBot.Modules.UserCommands), $"Command {command.Id} added via Web API");
            return new JsonResult(command);
        }

        [HttpPut("{guildId}/{commandId}")]
        [RequireRole(WebRoles.UserCommandEditor)]
        public async Task<IActionResult> UpdateCommandAsync([FromServices] IUserCommandRepository repository, [FromRoute] ulong guildId, [FromRoute] ulong commandId, [FromBody] UserCommand command, [FromQuery] string key)
        {
            command = command with { GuildId = guildId, Id = commandId };
            command = await repository.UpdateCommandAsync(command, HttpContext.RequestAborted);
            await _audit.LogAsync(guildId, 0, nameof(LamaBot.Modules.UserCommands), $"Command {command.Id} updated via Web API");
            return new JsonResult(command);
        }

        [HttpDelete("{guildId}/{commandId}")]
        [RequireRole(WebRoles.UserCommandEditor)]
        public async Task<IActionResult> DeleteCommandAsync([FromServices] IUserCommandRepository repository, [FromRoute] ulong guildId, [FromRoute] ulong commandId, [FromQuery] string key)
        {
            await repository.DeleteCommandAsync(guildId, commandId, HttpContext.RequestAborted);
            await _audit.LogAsync(guildId, 0, nameof(LamaBot.Modules.UserCommands), $"Command {commandId} deleted via Web API");
            return NoContent();
        }
    }
}
