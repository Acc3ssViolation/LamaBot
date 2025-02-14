namespace LamaBot
{
    public static class DateTimeExtensions
    {
        public static string ToDiscordTimestamp(this DateTime dateTime)
        {
            var unixSeconds = new DateTimeOffset(dateTime, TimeSpan.Zero).ToUnixTimeSeconds();
            return $"<t:{unixSeconds}>";
        }
    }
}
