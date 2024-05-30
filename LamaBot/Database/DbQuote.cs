using Microsoft.EntityFrameworkCore;

namespace LamaBot.Database
{
    public class DbQuote
    {
        public ulong GuildId { get; set; }
        public int Id { get; set; }
        public ulong UserId { get; set; }
        public string UserName { get; set; }
        public ulong ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string Content { get; set; }
        public ulong MessageId { get; set; }
        public DateTime TimestampUtc { get; set; }

        public List<DbQuoteRequest> Requests { get; set; }

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entityBuilder = modelBuilder.Entity<DbQuote>();
            entityBuilder.HasKey(_ => new { _.GuildId, _.Id });
            entityBuilder.HasIndex(_ => _.UserName);
        }
    }
}
