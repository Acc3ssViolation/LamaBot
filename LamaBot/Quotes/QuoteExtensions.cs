using Discord;
using System.Text;

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
                text.Append(quote.UserName);

            text.Append($"[(Jump)](https://discord.com/channels/{quote.GuildId}/{quote.ChannelId}/{quote.MessageId})");

            var embed = new EmbedBuilder()
                .WithTitle($"#{quote.Id}")
                .WithTimestamp(quote.TimestampUtc)
                .WithDescription(text.ToString())
                .Build();
            return embed;
        }
    }
}
