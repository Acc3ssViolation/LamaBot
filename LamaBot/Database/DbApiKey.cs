using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace LamaBot.Database
{
    public class DbApiKey
    {
        public ulong GuildId { get; set; }
        public string Key { get; set; }
        public string Content { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? ExpiresUtc { get; set; }

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entityBuilder = modelBuilder.Entity<DbApiKey>();
            entityBuilder.HasKey(_ => new { _.GuildId, _.Key });
            entityBuilder.HasIndex(_ => _.Key);
        }
    }
}
