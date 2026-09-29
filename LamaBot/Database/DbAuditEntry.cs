using Microsoft.EntityFrameworkCore;
using System;

namespace LamaBot.Database
{
    public class DbAuditEntry
    {
        public DateTime TimestampUtc { get; set; }
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public string Module { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entityBuilder = modelBuilder.Entity<DbAuditEntry>();
            entityBuilder.HasKey(_ => new { _.GuildId, _.UserId, _.TimestampUtc });
        }
    }
}
