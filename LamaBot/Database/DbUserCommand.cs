using Microsoft.EntityFrameworkCore;

namespace LamaBot.Database
{
    public class DbUserCommand
    {
        public ulong GuildId { get; set; }
        public ulong Id { get; set; }
        public required string Json { get; set; }

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entityBuilder = modelBuilder.Entity<DbUserCommand>();
            entityBuilder.HasKey(_ => new { _.GuildId, _.Id });
        }
    }
}
