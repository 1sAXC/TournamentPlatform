using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rating.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropPlayerTournamentStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerTournamentStatistics");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerTournamentStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DisciplineCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    Placement = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTournamentStatistics", x => x.Id);
                });

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
        }
    }
}
