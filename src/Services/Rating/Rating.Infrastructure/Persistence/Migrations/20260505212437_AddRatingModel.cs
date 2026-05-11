using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rating.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisciplineCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Elo = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    MatchesPlayed = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerRatings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerTournamentStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisciplineCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Placement = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTournamentStatistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RatingHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisciplineCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OldElo = table.Column<int>(type: "integer", nullable: false),
                    NewElo = table.Column<int>(type: "integer", nullable: false),
                    Delta = table.Column<int>(type: "integer", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatingHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRatings_DisciplineCode",
                table: "PlayerRatings",
                column: "DisciplineCode");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRatings_IsDeleted",
                table: "PlayerRatings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRatings_PlayerId",
                table: "PlayerRatings",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRatings_PlayerId_DisciplineCode",
                table: "PlayerRatings",
                columns: new[] { "PlayerId", "DisciplineCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTournamentStatistics_DisciplineCode",
                table: "PlayerTournamentStatistics",
                column: "DisciplineCode");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTournamentStatistics_PlayerId",
                table: "PlayerTournamentStatistics",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTournamentStatistics_PlayerId_TournamentId_Discipline~",
                table: "PlayerTournamentStatistics",
                columns: new[] { "PlayerId", "TournamentId", "DisciplineCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTournamentStatistics_TournamentId",
                table: "PlayerTournamentStatistics",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_RatingHistories_DisciplineCode",
                table: "RatingHistories",
                column: "DisciplineCode");

            migrationBuilder.CreateIndex(
                name: "IX_RatingHistories_MatchId",
                table: "RatingHistories",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_RatingHistories_PlayerId",
                table: "RatingHistories",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_RatingHistories_TournamentId",
                table: "RatingHistories",
                column: "TournamentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerRatings");

            migrationBuilder.DropTable(
                name: "PlayerTournamentStatistics");

            migrationBuilder.DropTable(
                name: "RatingHistories");
        }
    }
}
