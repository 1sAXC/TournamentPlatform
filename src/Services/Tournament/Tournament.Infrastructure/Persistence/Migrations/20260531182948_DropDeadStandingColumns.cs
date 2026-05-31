using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tournament.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropDeadStandingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuchholzScore",
                table: "SwissStandings");

            migrationBuilder.DropColumn(
                name: "OpponentsJson",
                table: "SwissStandings");

            migrationBuilder.DropColumn(
                name: "IsEliminated",
                table: "DoubleEliminationStandings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "BuchholzScore",
                table: "SwissStandings",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpponentsJson",
                table: "SwissStandings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEliminated",
                table: "DoubleEliminationStandings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
