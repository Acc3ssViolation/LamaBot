using Discord;

namespace LamaBot.Quotes
{
    internal static class QuoteInteractionExtensions
    {
        public static Embed CreateEmbed(this Quote quote)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"#{quote.Id}")
                //.WithFooter($"{quote.TimestampUtc:dd MMM yyyy @ HH:mm:ss} UTC")
                .WithTimestamp(quote.TimestampUtc)
                .WithDescription($"{quote.Content}\n- <@{quote.UserId}> [(Jump)](https://discord.com/channels/{quote.GuildId}/{quote.ChannelName}/{quote.MessageId})")
                .Build();
            return embed;
        }
    }
}
