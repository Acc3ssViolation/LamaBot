using Microsoft.EntityFrameworkCore;

namespace LamaBot.Database
{
    public class DbServerSetting
    {
        public ulong GuildId { get; set; }
        public string Code { get; set; }
        public string Value { get; set; }

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entityBuilder = modelBuilder.Entity<DbServerSetting>();
            entityBuilder.HasKey(_ => new { _.GuildId, _.Code });
        }
    }
}
