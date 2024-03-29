using LamaBot.Events;

namespace LamaBot.Servers
{
    public enum CrudEventType
    {
        Updated,
        Deleted,
    }
    public record ServerSettingEvent(ServerSetting Setting, CrudEventType Type);
    public interface IServerSettings
    {
        AsyncEvent<ServerSettingEvent> SettingChanged { get; }
        ValueTask<IReadOnlyList<ServerSetting>> GetSettingsAsync(CancellationToken cancellationToken = default);
        ValueTask<string?> GetSettingAsync(ulong guildId, string setting, CancellationToken cancellationToken = default);
        Task SetSettingAsync(ulong guildId, string setting, string? value, CancellationToken cancellationToken = default);
    }
}
