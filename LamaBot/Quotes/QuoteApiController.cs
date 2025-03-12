using Discord.WebSocket;
using LamaBot.Servers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace LamaBot.Quotes
{
    [ApiController]
    [Route("api/v1/quote")]
    [Authorize]
    public partial class QuoteApiController : ControllerBase
    {
        [HttpGet("{guildId}")]
        public async Task<IActionResult> GetQuotesAsync([FromServices] IQuoteRepository quoteRepository, [FromServices] IDiscordFacade discord, [FromRoute] ulong guildId, [FromQuery] string key)
        {
            var quotes = await quoteRepository.GetQuotesAsync(guildId, HttpContext.RequestAborted);

            var client = discord.Client;
            for (var i = 0; i < quotes.Count; i++)
            {
                var quote = quotes[i];
                if (quote.UserId == 0)
                    continue;

                quotes[i] = ResolveReferences(quote, client);
            }

            return new JsonResult(quotes);
        }

        private static Quote ResolveReferences(Quote quote, DiscordSocketClient client)
        {
            var guild = client.GetGuild(quote.GuildId);
            if (guild == null)
                return quote;

            var content = UserRefRegex().Replace(quote.Content, (match) =>
            {
                var userId = ulong.Parse(match.Groups[1].ValueSpan);
                var user = guild.GetUser(userId);
                if (user != null)
                    return $"@{user.DisplayName}";
                return match.Value;
            });
            content = ChannelRefRegex().Replace(content, (match) =>
            {
                var channelId = ulong.Parse(match.Groups[1].ValueSpan);
                var channel = guild.GetChannel(channelId);
                if (channel != null)
                    return $"#{channel.Name}";
                return match.Value;
            });

            var user = guild.GetUser(quote.UserId);
            var username = user?.DisplayName ?? quote.UserName;
            return quote with { Content = content, UserName = username };
        }

        [GeneratedRegex("<@(\\d+)>")]
        private static partial Regex UserRefRegex();
        [GeneratedRegex("<#(\\d+)>")]
        private static partial Regex ChannelRefRegex();
    }
}
