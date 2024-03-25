namespace LamaBot.Servers
{
    internal interface IServerSettingRepository
    {
        Task<IReadOnlyList<ServerSetting>> GetSettingsAsync(ulong? guildId, CancellationToken cancellationToken = default);
        Task SetOrDeleteSettingAsync(ulong guildId, string setting, string? value, CancellationToken cancellationToken = default);
    }
}
