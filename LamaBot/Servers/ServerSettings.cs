using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LamaBot.Servers
{
    internal class ServerSettings : IHostedService, IServerSettings
    {
        private readonly IServerSettingRepository _settingRepository;
        private readonly ILogger<ServerSettings> _logger;
        private readonly Dictionary<ulong, Dictionary<string, string>> _settings = new ();
        private readonly AsyncLock _settingsLock = new();

        public ServerSettings(IServerSettingRepository settingRepository, ILogger<ServerSettings> logger)
        {
            _settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<IReadOnlyList<ServerSetting>> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            using var l = await _settingsLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            var result = new List<ServerSetting>();
            foreach (var guildSettings in _settings)
            {
                foreach (var setting in guildSettings.Value)
                    result.Add(new ServerSetting(guildSettings.Key, setting.Key, setting.Value));
            }
            return result;
        }

        public async ValueTask<string?> GetSettingAsync(ulong guildId, string setting, CancellationToken cancellationToken = default)
        {
            using var l = await _settingsLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            if (_settings.TryGetValue(guildId, out var guildSettings) && guildSettings.TryGetValue(setting, out var value))
                return value;
            return null;
        }

        public async Task SetSettingAsync(ulong guildId, string setting, string? value, CancellationToken cancellationToken = default)
        {
            using var l = await _settingsLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            await _settingRepository.SetOrDeleteSettingAsync(guildId, setting, value, cancellationToken).ConfigureAwait(false);

            if (!_settings.TryGetValue(guildId, out var guildSettings))
            {
                guildSettings = new Dictionary<string, string>();
                _settings[guildId] = guildSettings;
            }
            if (value != null)
                guildSettings[setting] = value;
            else
                guildSettings.Remove(setting);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var l = await _settingsLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            var allSettings = await _settingRepository.GetSettingsAsync(null, cancellationToken).ConfigureAwait(false);
            foreach (var setting in allSettings)
            {
                if (!_settings.TryGetValue(setting.GuildId, out var guildSettings))
                {
                    guildSettings = new Dictionary<string, string>();
                    _settings[setting.GuildId] = guildSettings;
                }

                guildSettings[setting.Code] = setting.Value;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
