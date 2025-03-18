using Discord;
using Discord.Interactions;

namespace LamaBot.Hangman
{
    [CommandContextType(InteractionContextType.Guild | InteractionContextType.BotDm)]
    public class HangmanInteractionModule : InteractionModuleBase
    {
        private readonly HangmanController _controller;
        private readonly ILogger<HangmanInteractionModule> _logger;

        public HangmanInteractionModule(HangmanController controller, ILogger<HangmanInteractionModule> logger)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [SlashCommand("hangman", "Guess like your life depends on it!")]
        public async Task StartGameAsync(
            [Summary("difficulty", "The difficulty of the game")] Difficulty difficulty = Difficulty.Easy
            )
        {
            var channelId = Context.Interaction.ChannelId;
            if (!channelId.HasValue)
            {
                await RespondAsync("This command only be run in a channel");
                return;
            }

            await RespondAsync("Starting a game of hangman!");

            await _controller.StartGameOnChannel(channelId.Value, difficulty, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
