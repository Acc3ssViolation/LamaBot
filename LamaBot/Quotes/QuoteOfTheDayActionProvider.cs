using Cronos;
using LamaBot.Cron;
using LamaBot.Servers;
using System.Globalization;

namespace LamaBot.Quotes
{
    public class QuoteOfTheDayActionProvider : ICronActionProvider
    {
        private class QuoteOfTheDayAction : ICronAction
        {
            public CronExpression Schedule {get;}
            private readonly IDiscordFacade _discordFacade;
            private readonly ulong _guildId;
            private readonly ulong _channelId;
            private readonly IQuoteRepository _quoteRepository;
            private readonly ILogger _logger;

            public QuoteOfTheDayAction(CronExpression schedule, IDiscordFacade discordFacade, ulong guildId, ulong channelId, IQuoteRepository quoteRepository, ILogger logger)
            {
                Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
                _discordFacade = discordFacade ?? throw new ArgumentNullException(nameof(discordFacade));
                _guildId = guildId;
                _channelId = channelId;
                _quoteRepository = quoteRepository ?? throw new ArgumentNullException(nameof(quoteRepository));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public async Task ActionAsync()
            {
                // TODO: Make quote of the day not random
                var quote = await _quoteRepository.GetRandomQuoteAsync(_guildId).ConfigureAwait(false);
                if (quote == null)
                {
                    _logger.LogWarning("No QOTD for guild {GuildId}", _guildId);
                    return;
                }

                var guild = _discordFacade.Client.GetGuild(_guildId);
                var channel = guild?.GetTextChannel(_channelId);

                if (guild == null || channel == null)
                {
                    _logger.LogWarning("Cannot find channel {Channel} in guild {GuildId}", _channelId, guild?.Name ?? _guildId.ToString());
                    return;
                }

                _logger.LogInformation("Sending QOTD for guild {Guild} in channel {Channel}", guild.Name, channel.Name);
                await channel.SendMessageAsync(embed: quote.CreateEmbed($"Quote Of The Day #{quote.Id}")).ConfigureAwait(false);
            }
        }

        public event Action? ActionsUpdated;

        private readonly IDiscordFacade _discordFacade;
        private readonly IQuoteRepository _quoteRepository;
        private readonly IServerSettings _serverSettings;
        private readonly ILogger<QuoteOfTheDayActionProvider> _logger;

        public QuoteOfTheDayActionProvider(IDiscordFacade discordFacade, IQuoteRepository quoteRepository, IServerSettings serverSettings, ILogger<QuoteOfTheDayActionProvider> logger)
        {
            _discordFacade = discordFacade ?? throw new ArgumentNullException(nameof(discordFacade));
            _quoteRepository = quoteRepository ?? throw new ArgumentNullException(nameof(quoteRepository));
            _serverSettings = serverSettings ?? throw new ArgumentNullException(nameof(serverSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ICronAction>> GetActionsAsync(CancellationToken cancellationToken)
        {
            var settings = await _serverSettings.GetSettingsAsync(cancellationToken).ConfigureAwait(false);
            var channelSettings = settings.Where(s => s.Code == QuoteSettings.QuoteOfTheDayChannel);
            var actions = new List<ICronAction>();
            foreach (var channelSetting in channelSettings)
            {
                var timeSetting = settings.Where(s => s.GuildId == channelSetting.GuildId && s.Code == QuoteSettings.QuoteOfTheDayTime).FirstOrDefault();
                var time = TimeOnly.Parse(timeSetting?.Value ?? "7:00", CultureInfo.InvariantCulture);

                var schedule = CronExpression.Parse($"{time.Minute} {time.Hour} * * *");
                var action = new QuoteOfTheDayAction(schedule, _discordFacade, channelSetting.GuildId, ulong.Parse(channelSetting.Value), _quoteRepository, _logger);
                actions.Add(action);
            }
            return actions;
        }
    }
}
