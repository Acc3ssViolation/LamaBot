namespace LamaBot
{
    public static class EnumerableExtensions
    {
        public static string ToCommaSeparatedString<T>(this IEnumerable<T> list)
        {
            ArgumentNullException.ThrowIfNull(list);

            return list.Aggregate("", (str, value) => str.Length > 0 ? str + $", {value}" : value?.ToString() ?? "<null>");
        }
    }
}
