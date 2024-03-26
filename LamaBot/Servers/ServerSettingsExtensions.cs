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

        public static ValueTask<bool> IsServerEnabledAsync(this IServerSettings serverSettings, ulong guildId, CancellationToken cancellationToken = default)
            => serverSettings.GetBoolAsync(guildId, "enabled", false, cancellationToken);

        public static Task EnableServerAsync(this IServerSettings serverSettings, ulong guildId, CancellationToken cancellationToken = default)
            => serverSettings.SetBoolAsync(guildId, "enabled", true, cancellationToken);

        public static Task DisableServerAsync(this IServerSettings serverSettings, ulong guildId, CancellationToken cancellationToken = default)
            => serverSettings.ClearAsync(guildId, "enabled", cancellationToken);

        public static async ValueTask<char> GetCommandPrefixAsync(this IServerSettings serverSettings, ulong guildId, CancellationToken cancellationToken = default)
        {
            var value = await serverSettings.GetSettingAsync(guildId, "prefix", cancellationToken);
            if (value == null)
                return '!';
            return value[0];
        }

        public static async Task SetCommandPrefixAsync(this IServerSettings serverSettings, ulong guildId, char prefix, CancellationToken cancellationToken = default)
        {
            await serverSettings.SetSettingAsync(guildId, "prefix", prefix.ToString(), cancellationToken);
        }
    }
}
