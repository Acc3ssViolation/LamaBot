namespace LamaBot.Modules.UserCommands
{
    public enum MatchType
    {
        Equals,
        Contains,
        StartsWith,
        EndsWith,
    }

    public record UserCommand(ulong GuildId, ulong Id, string Trigger, MatchType MatchType, bool CaseSensitive, string Response);
}
