using System.Text.Json.Serialization;

namespace LamaBot.Modules.UserCommands
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MatchType
    {
        [JsonStringEnumMemberName("equals")]
        Equals,
        [JsonStringEnumMemberName("contains")]
        Contains,
        [JsonStringEnumMemberName("ends")]
        StartsWith,
        [JsonStringEnumMemberName("starts")]
        EndsWith,
    }

    public record UserCommand(ulong GuildId, ulong Id, string Trigger, MatchType MatchType, bool CaseSensitive, string Response);
}
