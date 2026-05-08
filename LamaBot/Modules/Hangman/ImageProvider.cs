namespace LamaBot.Modules.Hangman
{
    public class ImageProvider
    {
        public int ImageCount => 12;

        public Task<Stream> GetImageAsync(int index, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, ImageCount);

            try
            {
                return Task.FromResult<Stream>(File.OpenRead($"Modules/Hangman/Images/{index}.png"));
            }
            catch
            {
                return Task.FromResult<Stream>(File.OpenRead($"Modules/Hangman/Images/{ImageCount - 1}.png"));
            }
        }
    }
}
