﻿using Microsoft.EntityFrameworkCore;

namespace LamaBot.Database
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<DbQuote> Quotes { get; set; }
        public DbSet<DbCronMessage> CronMessages { get; set; }
        public DbSet<DbServerSetting> ServerSettings { get; set; }


        public ApplicationDbContext() : base(DefaultOptions)
        {
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) 
        { 
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            DbQuote.OnModelCreating(modelBuilder);
            DbCronMessage.OnModelCreating(modelBuilder);
            DbServerSetting.OnModelCreating(modelBuilder);
        }

        public static DbContextOptions<ApplicationDbContext> DefaultOptions
        {
            get
            {
                var dbContextOptionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                dbContextOptionsBuilder.UseSqlite("Data Source=llama.db");
                return dbContextOptionsBuilder.Options;
            }
        }
    }
}
