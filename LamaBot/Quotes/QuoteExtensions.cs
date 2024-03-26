using Discord;
using System.Text;
using System.Threading.Channels;
using static System.Net.Mime.MediaTypeNames;

namespace LamaBot.Quotes
{
    internal static class QuoteInteractionExtensions
    {
        public static Embed CreateEmbed(this Quote quote)
        {
            var text = new StringBuilder()
                .Append(quote.Content)
                .AppendLine()
                .Append("- ");

            if (quote.UserId != 0)
                text.Append("<@").Append(quote.UserId).Append('>');
            else
                text.Append(quote.GetQuoteAuthor());

            text.Append(' ');
            text.Append(quote.GetMessageLink());

            var embed = new EmbedBuilder()
                .WithTitle($"#{quote.Id}")
                .WithTimestamp(quote.TimestampUtc)
                .WithDescription(text.ToString())
                .Build();
            return embed;
        }

        public static string GetMessageLink(this Quote quote)
            => $"[(Jump)](https://discord.com/channels/{quote.GuildId}/{quote.ChannelId}/{quote.MessageId})";

        public static string GetQuoteAuthor(this Quote quote)
        {
            if (quote.UserId != 0)
                return $"<@{quote.UserId}>";
            else
                return quote.UserName;
        }
    }
}
