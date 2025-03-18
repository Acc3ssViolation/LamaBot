namespace LamaBot.Hangman
{
    public class ImageProvider
    {
        public int ImageCount => 12;

        public async Task<Stream> GetImageAsync(int index, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, ImageCount);

            try
            {
                return File.OpenRead($"Hangman/Images/{index}.png");
            }
            catch
            {
                return File.OpenRead($"Hangman/Images/{ImageCount - 1}.png");
            }
        }
    }
}
