using Discord;
using Discord.WebSocket;
using System.Collections.Concurrent;
using System.Text;

namespace LamaBot.Hangman
{
    public class HangmanController
    {
        private readonly ConcurrentDictionary<ulong, HangmanGame> _games = new();

        private readonly ImageProvider _imageProvider;
        private readonly WordProvider _wordProvider;
        private readonly IDiscordFacade _discordFacade;
        private readonly ILogger<HangmanController> _logger;

        public HangmanController(ImageProvider imageProvider, WordProvider wordProvider, IDiscordFacade discordFacade, ILogger<HangmanController> logger)
        {
            _imageProvider = imageProvider;
            _wordProvider = wordProvider;
            _discordFacade = discordFacade;
            _logger = logger;

            _discordFacade.Client.MessageReceived += OnMessageAsync;
        }

        public async Task StartGameOnChannel(ulong channelId, CancellationToken cancellationToken)
        {
            var word = await _wordProvider.GetWordAsync(cancellationToken).ConfigureAwait(false);
            var game = new HangmanGame(channelId, DateTime.UtcNow, word, [], []);
            _games.AddOrUpdate(channelId, game, (_, _) => game);
            await PostGameAsync(game);
        }

        public HangmanGame? FindGameForChannel(ulong channelId)
        {
            return _games.TryGetValue(channelId, out var game) ? game : null;
        }

        private async Task OnMessageAsync(SocketMessage message)
        {
            if (message.Author.IsBot)
                return;

            var channelId = message.Channel.Id;
            if (!_games.TryGetValue(channelId, out var game))
                return;

            var trimmed = message.Content.Trim();
            if (trimmed.Length == 0)
                return;

            CleanUpOldGames();

            var guess = Rune.ToUpperInvariant(trimmed.EnumerateRunes().First()).ToString();
            if (game.Guesses.Contains(guess) || game.Errors.Contains(guess))
            {
                game = game with
                {
                    LastUsedUtc = DateTime.UtcNow
                };
                _games.AddOrUpdate(game.ChannelId, game, (_, _) => game);
                await PostGameAsync(game, $"You already guessed '{guess}'");
                return;
            }

            await OnGuessAsync(game, guess).ConfigureAwait(false);
        }

        private void CleanUpOldGames()
        {
            var cutoffTimeUtc = DateTime.UtcNow - TimeSpan.FromHours(24);
            var gamesToDelete = _games.ToList().Where(g => g.Value.LastUsedUtc < cutoffTimeUtc);
            foreach (var game in gamesToDelete)
                _games.Remove(game.Key, out _);
        }

        private async Task OnGuessAsync(HangmanGame game, string guess)
        {
            var isCorrect = game.Word.Contains(guess, StringComparison.OrdinalIgnoreCase);
            if (isCorrect)
            {
                var newGuesses = new List<string>(game.Guesses) { guess };
                game = game with
                {
                    Guesses = newGuesses,
                    LastUsedUtc = DateTime.UtcNow
                };

                _games.AddOrUpdate(game.ChannelId, game, (_, _) => game);
            }
            else
            {
                var newErrors = new List<string>(game.Errors) { guess };

                game = game with
                {
                    Errors = newErrors,
                    LastUsedUtc = DateTime.UtcNow
                };

                _games.AddOrUpdate(game.ChannelId, game, (_, _) => game);
            }

            var result = game.CalculateResult(_imageProvider.ImageCount - 1);
            if (result != GameResult.Playing)
                _games.Remove(game.ChannelId, out _);

            await PostGameAsync(game, result: result);
        }

        private async Task PostGameAsync(HangmanGame game, string? message = null, GameResult result = GameResult.Playing)
        {
            var channel = await _discordFacade.Client.GetChannelAsync(game.ChannelId).ConfigureAwait(false);
            if (channel is not IMessageChannel messageChannel)
                return;

            var sb = new StringBuilder();
            sb.Append('`');
            var isFirst = true;
            foreach (var chr in game.Word.EnumerateRunes())
            {
                if (!isFirst)
                    sb.Append(' ');
                if (result != GameResult.Playing || game.Guesses.Any(g => g.Equals(chr.ToString(), StringComparison.OrdinalIgnoreCase)))
                    sb.Append(chr);
                else
                    sb.Append('_');
                isFirst = false;
            }
            sb.Append('`');

            var title = result switch
            {
                GameResult.Win => "YOU WON!",
                GameResult.Loss => "YOU ARE DEAD",
                _ => "Raad het woord!",
            };

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(sb.ToString())
                .WithImageUrl("attachment://hangman.png")
                .WithFooter($"Wrong Guesses: {game.Errors.ToCommaSeparatedString()}");

            if (message != null)
                embed = embed.WithFields(new EmbedFieldBuilder().WithName(">").WithValue(message).WithIsInline(true));

            var index = Math.Min(_imageProvider.ImageCount - 1, game.Errors.Count);
            var stream = await _imageProvider.GetImageAsync(index, CancellationToken.None).ConfigureAwait(false);
            var fileAttachment = new FileAttachment(stream, "hangman.png");
            await messageChannel.SendFileAsync(fileAttachment, embed: embed.Build()).ConfigureAwait(false);

            _logger.LogDebug("Outputting Hangman Game {Game}", game);
        }
    }
}
