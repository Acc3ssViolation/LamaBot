using LamaBot.Servers;
using Microsoft.AspNetCore.Mvc;

namespace LamaBot.Quotes
{
    [ApiController]
    [Route("api/v1/quote")]
    public class QuoteApiController : ControllerBase
    {
        [HttpGet("{guildId}")]
        public async Task<IActionResult> GetQuotesAsync([FromServices] IQuoteRepository quoteRepository, [FromServices] IServerSettings serverSettings, [FromRoute] ulong guildId, [FromQuery] string key)
        {
            var requiredKey = await serverSettings.GetSettingAsync(guildId, QuoteSettings.QuoteApiKey);
            if (requiredKey == null || requiredKey != key)
                return StatusCode(403);

            var quotes = await quoteRepository.GetQuotesAsync(guildId, HttpContext.RequestAborted);
            return new JsonResult(quotes);
        }
    }
}
