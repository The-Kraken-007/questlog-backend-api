using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuestLog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "HabitEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Icon = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAchievements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAchievements_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAchievements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "Id", "Category", "Description", "Icon", "Key", "Name" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-0001-4000-8000-000000000001"), 0, "Complete any habit for the first time", "🔥", "first_flame", "First Flame" },
                    { new Guid("a1b2c3d4-0002-4000-8000-000000000002"), 0, "Maintain a 7-day streak on any habit", "⚔️", "week_warrior", "Week Warrior" },
                    { new Guid("a1b2c3d4-0003-4000-8000-000000000003"), 0, "Maintain a 30-day streak on any habit", "🛡️", "monthly_master", "Monthly Master" },
                    { new Guid("a1b2c3d4-0004-4000-8000-000000000004"), 0, "Maintain a 100-day streak on any habit", "👑", "century_club", "Century Club" },
                    { new Guid("a1b2c3d4-0005-4000-8000-000000000005"), 1, "Complete your first goal", "🎯", "goal_getter", "Goal Getter" },
                    { new Guid("a1b2c3d4-0006-4000-8000-000000000006"), 1, "Complete 10 goals", "🏆", "overachiever", "Overachiever" },
                    { new Guid("a1b2c3d4-0007-4000-8000-000000000007"), 1, "Complete 5 goal milestones", "📌", "milestone_marker", "Milestone Marker" },
                    { new Guid("a1b2c3d4-0008-4000-8000-000000000008"), 1, "Complete at least one habit every day for 7 consecutive days", "⭐", "perfect_week", "Perfect Week" },
                    { new Guid("a1b2c3d4-0009-4000-8000-000000000009"), 2, "Complete habits 5 days in a week", "📅", "dedicated", "Dedicated" },
                    { new Guid("a1b2c3d4-0010-4000-8000-000000000010"), 2, "Complete habits 20 days in a month", "🗓️", "committed", "Committed" },
                    { new Guid("a1b2c3d4-0011-4000-8000-000000000011"), 2, "Complete habits 100 total times", "💪", "unstoppable", "Unstoppable" },
                    { new Guid("a1b2c3d4-0012-4000-8000-000000000012"), 2, "Reach Level 10", "🌟", "rising_star", "Rising Star" },
                    { new Guid("a1b2c3d4-0013-4000-8000-000000000013"), 3, "Complete a habit after midnight (UTC)", "🦉", "night_owl", "Night Owl" },
                    { new Guid("a1b2c3d4-0014-4000-8000-000000000014"), 3, "Complete a habit before 7 AM (UTC)", "🐦", "early_bird", "Early Bird" },
                    { new Guid("a1b2c3d4-0015-4000-8000-000000000015"), 3, "Write 30 daily log entries", "📝", "journal_keeper", "Journal Keeper" },
                    { new Guid("a1b2c3d4-0016-4000-8000-000000000016"), 3, "Reach Level 50", "🎖️", "level_legend", "Level Legend" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_Key",
                table: "Achievements",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_AchievementId",
                table: "UserAchievements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_UserId_AchievementId",
                table: "UserAchievements",
                columns: new[] { "UserId", "AchievementId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAchievements");

            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "HabitEntries");
        }
    }
}
