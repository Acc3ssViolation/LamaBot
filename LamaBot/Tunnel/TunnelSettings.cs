namespace LamaBot.Tunnel
{
    public class TunnelSettings
    {
        public Uri Endpoint { get; set; } = new Uri("http://localhost");
        public string Key { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }
}
