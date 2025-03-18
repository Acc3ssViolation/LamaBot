namespace LamaBot.Hangman
{
    public record HangmanGame(ulong ChannelId, DateTime LastUsedUtc, string Word, IReadOnlyList<string> Guesses, IReadOnlyList<string> Errors)
    {
        public GameResult CalculateResult(int errorKillCount)
        {
            var allMatched = true;
            foreach (var rune in Word)
            {
                var matched = false;
                foreach (var g in Guesses)
                {
                    if (rune.ToString().Equals(g, StringComparison.OrdinalIgnoreCase))
                    {
                        matched = true;
                        break;
                    }
                }
                if (!matched)
                {
                    allMatched = false;
                    break;
                }
            }

            if (allMatched)
                return GameResult.Win;

            if (Errors.Count >= errorKillCount)
                return GameResult.Loss;

            return GameResult.Playing;
        }
    }
}
