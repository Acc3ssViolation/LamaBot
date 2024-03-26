namespace LamaBot.Quotes
{
    public record Quote(ulong GuildId, int Id, ulong UserId, string UserName, ulong ChannelId, string ChannelName, string Content, ulong MessageId, DateTime TimestampUtc)
    {
        public string GetMessageLink()
            => $"[(Jump)](https://discord.com/channels/{GuildId}/{ChannelId}/{MessageId})";
    }
}
