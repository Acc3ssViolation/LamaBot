using Discord;
using Discord.Interactions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace LamaBot.Database
{
    [Group("database", "Like the blockchain but more sensible")]
    public class DatabaseInteractionModule : InteractionModuleBase
    {
        private readonly Func<ApplicationDbContext> _dbContextFactory;

        public DatabaseInteractionModule(Func<ApplicationDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        [RequireOwner]
        [SlashCommand("backup", "Create a non-surprise database backup")]
        public async Task BackupAsync()
        {
            await DeferAsync(ephemeral: true);
            await ModifyOriginalResponseAsync(msg => msg.Content = "Backing up database...");

            var tempFile = Path.GetTempFileName();
            using (var backup = new SqliteConnection($"Data Source={tempFile}"))
            {
                using var dbContext = _dbContextFactory();
                var databaseConnection = (SqliteConnection)dbContext.Database.GetDbConnection();
                await databaseConnection.OpenAsync();
                databaseConnection.BackupDatabase(backup);
                SqliteConnection.ClearPool(backup);
            }

            await ModifyOriginalResponseAsync(msg =>
            {
                msg.Content = "Backed up database, see attached file";
                msg.Attachments = new FileAttachment[] { new(tempFile, $"backup-{Dns.GetHostName()}-{DateTime.UtcNow:s}.db") };
            });
        }
    }
}
