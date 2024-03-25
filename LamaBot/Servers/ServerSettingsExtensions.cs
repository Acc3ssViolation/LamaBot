namespace LamaBot.Servers
{
    internal static class ServerSettingsExtensions
    {
        public static async ValueTask<bool?> GetBoolAsync(this IServerSettings serverSettings, ulong guildId, string setting, CancellationToken cancellationToken = default)
        {
            var value = await serverSettings.GetSettingAsync(guildId, setting, cancellationToken).ConfigureAwait(false);
            if (bool.TryParse(value, out var result))
                return result;
            return null;
        }

        public static async ValueTask<bool> GetBoolAsync(this IServerSettings serverSettings, ulong guildId, string setting, bool defaultValue, CancellationToken cancellationToken = default)
        {
            var value = await serverSettings.GetSettingAsync(guildId, setting, cancellationToken).ConfigureAwait(false);
            if (bool.TryParse(value, out var result))
                return result;
            return defaultValue;
        }

        public static async Task SetBoolAsync(this IServerSettings serverSettings, ulong guildId, string setting, bool value, CancellationToken cancellationToken = default)
        {
            await serverSettings.SetSettingAsync(guildId, setting, value.ToString(), cancellationToken).ConfigureAwait(false);
        }

        public static async Task ClearAsync(this IServerSettings serverSettings, ulong guildId, string setting, CancellationToken cancellationToken = default)
        {
            await serverSettings.SetSettingAsync(guildId, setting, null, cancellationToken).ConfigureAwait(false);
        }
    }
}
