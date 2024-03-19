using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace LamaBot
{
    internal class DebugModule : InteractionModuleBase
    {
        private readonly ILogger<DebugModule> _logger;

        public DebugModule(ILogger<DebugModule> logger)
        {
            _logger = logger;
        }

        [SlashCommand("info", "Basic slash command")]
        public async Task TestAsync()
        {
            _logger.LogDebug("Test command called");
            await RespondAsync("This is a test");
        }
    }
}
