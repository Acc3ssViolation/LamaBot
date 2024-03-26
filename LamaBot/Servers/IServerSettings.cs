namespace LamaBot.Servers
{
    public interface IServerSettings
    {
        ValueTask<IReadOnlyList<ServerSetting>> GetSettingsAsync(CancellationToken cancellationToken = default);
        ValueTask<string?> GetSettingAsync(ulong guildId, string setting, CancellationToken cancellationToken = default);
        Task SetSettingAsync(ulong guildId, string setting, string? value, CancellationToken cancellationToken = default);
    }
}
