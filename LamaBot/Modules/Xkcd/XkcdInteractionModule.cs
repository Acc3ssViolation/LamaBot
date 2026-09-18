using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LamaBot.Modules.Xkcd
{
    public partial class XkcdInteractionModule : InteractionModuleBase
    {
        private record Comic(int Id, string Title, string Image, string Link);

        private readonly HttpClient _httpClient;
        private readonly ILogger<XkcdInteractionModule> _logger;

        public XkcdInteractionModule(HttpClient httpClient, ILogger<XkcdInteractionModule> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [SlashCommand("xkcd", "Because there is always a relevant XKCD")]
        public async Task GetXkcdAsync([Summary("id", "The number of the XKCD to get. Leave empty to get a random one!")] int? id = null)
        {
            await DeferAsync();

            try
            {
                if (id.HasValue)
                {
                    var comic = await GetComicAsync(id.Value);

                    await ModifyOriginalResponseAsync(msg => msg.Embed = 
                        new EmbedBuilder()
                            .WithTitle($"{comic.Id} - {comic.Title}")
                            .WithImageUrl(comic.Image)
                            .WithUrl(comic.Link)
                            .Build()
                        );
                }
                else
                {
                    var url = "https://c.xkcd.com/random/comic/";
                    var response = await _httpClient.GetAsync(url);
                    var targetUrl = response.Headers.Location;
                    if (targetUrl == null)
                        throw new HttpRequestException("Could not find Location header to get random XKCD");

                    var comic = await GetComicAsync(targetUrl.ToString());

                    await ModifyOriginalResponseAsync(msg => msg.Embed =
                        new EmbedBuilder()
                            .WithTitle($"{comic.Id} - {comic.Title}")
                            .WithImageUrl(comic.Image)
                            .WithUrl(comic.Link)
                            .Build()
                        );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get XKCD");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }

        private async Task<Comic> GetComicAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);
            var text = await response.Content.ReadAsStringAsync();
            var image = ImageRegex().Match(text).Groups[^1].Value;
            var title = TitleRegex().Match(text).Groups[^1].Value;
            var link = UrlRegex().Match(text).Groups[^1].Value;

            var id = int.Parse(IdRegex().Match(link).Groups[^1].Value);
            return new Comic(id, title, image, link);
        }

        private Task<Comic> GetComicAsync(int id)
        {
            var url = $"https://xkcd.com/{id}";
            return GetComicAsync(url);
        }

        [GeneratedRegex(".+\\D(\\d+)")]
        private static partial Regex IdRegex();

        [GeneratedRegex("<meta property=\"og:image\" content=\"(.+)\">")]
        private static partial Regex ImageRegex();

        [GeneratedRegex("<meta property=\"og:title\" content=\"(.+)\">")]
        private static partial Regex TitleRegex();

        [GeneratedRegex("<meta property=\"og:url\" content=\"(.+)\">")]
        private static partial Regex UrlRegex();
    }
}
