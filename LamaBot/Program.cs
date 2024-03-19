using Discord;

namespace LamaBot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var client = new Discord.WebSocket.DiscordSocketClient();
            client.
            
        }

        private static Task DiscordLog(LogMessage message)
        {
            Console.WriteLine(message);
            return Task.CompletedTask;
        }
    }
}
