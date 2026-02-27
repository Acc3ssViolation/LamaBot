using Discord;
using Discord.WebSocket;

namespace LamaBot.Components
{
    public class InteractiveComponentService : BackgroundService, IInteractiveComponentService
    {
        private record InteractionRegistration(object? Data, ButtonInteractionCallback Callback, DateTime ExpirationUtc);

        private readonly IDiscordFacade _discord;

        private readonly Dictionary<string, InteractionRegistration> _customDataCache = [];

        public InteractiveComponentService(IDiscordFacade discord)
        {
            _discord = discord ?? throw new ArgumentNullException(nameof(discord));
        }

        public string Register<T>(T? data, ButtonInteractionCallback callback)
        {
            var customId = Guid.NewGuid().ToString();
            var expirationUtc = DateTime.UtcNow + TimeSpan.FromMinutes(5);
            var registration = new InteractionRegistration(data, callback, expirationUtc);

            lock (_customDataCache)
            {
                Clean();
                _customDataCache[customId] = registration;
            }
                
            return customId;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _discord.WaitUntilReadyAsync(stoppingToken).ConfigureAwait(false);

            var client = _discord.Client;
            client.ButtonExecuted += Client_ButtonExecuted;
            await stoppingToken.UntilCancelledNoThrow();
            client.ButtonExecuted -= Client_ButtonExecuted;
        }

        private async Task Client_ButtonExecuted(SocketMessageComponent component)
        {
            InteractionRegistration? registration;

            lock (_customDataCache)
            {
                Clean();

                if (!_customDataCache.TryGetValue(component.Data.CustomId, out registration))
                {
                    return;
                }
            }

            await registration.Callback(component, registration.Data).ConfigureAwait(false);
        }

        private void Clean()
        {
            var utcNow = DateTime.UtcNow;
            var toRemove = new List<string>();
            foreach (var kvp in _customDataCache)
                if (kvp.Value.ExpirationUtc < utcNow)
                    toRemove.Add(kvp.Key);
            foreach (var key in toRemove)
                _customDataCache.Remove(key);
        }
    }
}
