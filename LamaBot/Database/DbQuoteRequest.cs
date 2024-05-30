using Microsoft.EntityFrameworkCore;

namespace LamaBot.Database
{
    public class DbQuoteRequest
    {
        public ulong GuildId { get; set; }
        public int Id { get; set; }

        public ulong UserId { get; set; }
        public DateTime TimestampUtc { get; set; }

        public DbQuote Quote { get; set; }

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entityBuilder = modelBuilder.Entity<DbQuoteRequest>();
            entityBuilder.HasKey(_ => new { _.GuildId, _.Id, _.TimestampUtc });
            entityBuilder
                .HasOne(_ => _.Quote)
                .WithMany(_ => _.Requests)
                .HasForeignKey(_ => new { _.GuildId, _.Id })
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
