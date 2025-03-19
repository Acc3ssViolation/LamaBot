
namespace LamaBot.Hangman
{
    public class WordProvider
    {
        const string WordListUrl = "https://raw.githubusercontent.com/OpenTaal/opentaal-wordlist/refs/heads/master/elements/wordlist-ascii.txt";
        const int MinLength = 4;
        const int MaxLength = 150;

        private List<string>? _words;

        private readonly HttpClient _httpClient;

        public WordProvider(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<string> GetWordAsync(CancellationToken cancellationToken)
        {
            if (_words == null)
            {
                using var stream = await _httpClient.GetStreamAsync(WordListUrl, cancellationToken).ConfigureAwait(false);
                using var reader = new StreamReader(stream);
                var line = "";
                var words = new HashSet<string>();
                while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null) 
                {
                    if (line.Length < MinLength || line.Length > MaxLength || line.Any(c => !char.IsAsciiLetterLower(c)))
                        continue;

                    words.Add(line.ToUpperInvariant());
                }
                _words = words.ToList();
            }

            return _words[Random.Shared.Next(_words.Count)];
        }
    }
}
