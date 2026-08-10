using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuestLog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserXp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserXps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalXp = table.Column<int>(type: "integer", nullable: false),
                    CurrentLevel = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserXps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserXps_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserXps_UserId",
                table: "UserXps",
                column: "UserId");

            // Seed existing users with 0 XP so they start at Level 1
            migrationBuilder.Sql(
                """
                INSERT INTO "UserXps" ("UserId", "TotalXp", "CurrentLevel", "UpdatedAt")
                SELECT "Id", 0, 1, NOW() AT TIME ZONE 'UTC'
                FROM "Users"
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserXps");
        }
    }
}
