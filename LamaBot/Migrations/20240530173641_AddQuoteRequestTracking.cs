using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LamaBot.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteRequestTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuoteRequests",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteRequests", x => new { x.GuildId, x.Id, x.TimestampUtc });
                    table.ForeignKey(
                        name: "FK_QuoteRequests_Quotes_GuildId_Id",
                        columns: x => new { x.GuildId, x.Id },
                        principalTable: "Quotes",
                        principalColumns: new[] { "GuildId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuoteRequests");
        }
    }
}
