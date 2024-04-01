using LamaBot.Servers;
using Microsoft.AspNetCore.Mvc;

namespace LamaBot.Quotes
{
    [ApiController]
    [Route("api/v1/quote")]
    public class QuoteApiController : ControllerBase
    {
        [HttpGet("{guildId}")]
        public async Task<IActionResult> GetQuotesAsync([FromServices] IQuoteRepository quoteRepository, [FromServices] IDiscordFacade discord, [FromServices] IServerSettings serverSettings, [FromRoute] ulong guildId, [FromQuery] string key, [FromQuery] bool resolveUsers = true)
        {
            var requiredKey = await serverSettings.GetSettingAsync(guildId, QuoteSettings.QuoteApiKey);
            if (requiredKey == null || requiredKey != key)
                return StatusCode(403);

            var quotes = await quoteRepository.GetQuotesAsync(guildId, HttpContext.RequestAborted);

            if (resolveUsers)
            {
                var client = discord.Client;
                for (var i = 0; i < quotes.Count; i++)
                {
                    var quote = quotes[i];
                    if (quote.UserId == 0)
                        continue;

                    var user = client.GetGuild(quote.GuildId)?.GetUser(quote.UserId);
                    if (user != null)
                        quotes[i] = quote with { UserName = user.DisplayName };
                }
            }

            return new JsonResult(quotes);
        }
    }
}
