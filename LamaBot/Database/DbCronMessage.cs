using Microsoft.EntityFrameworkCore;

namespace LamaBot.Database
{
    public class DbCronMessage
    {
        public ulong GuildId { get; set; }
        public string Id { get; set; }
        public ulong ChannelId { get; set; }
        public string Schedule { get; set; }
        public string Content { get; set; }

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entityBuilder = modelBuilder.Entity<DbCronMessage>();
            entityBuilder.HasKey(_ => new { _.GuildId, _.Id });
            entityBuilder.Property(_ => _.Id);
        }
    }
}
