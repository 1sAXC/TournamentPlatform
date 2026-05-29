using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tournament.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchMapsAndDropPubg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Code",
                keyValue: "PUBG");

            migrationBuilder.AddColumn<int>(
                name: "LoserMaps",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WinnerMaps",
                table: "Matches",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoserMaps",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "WinnerMaps",
                table: "Matches");

            migrationBuilder.InsertData(
                table: "Disciplines",
                columns: new[] { "Code", "AllowedTeamSizes", "IsActive", "Name" },
                values: new object[] { "PUBG", new[] { 1, 2, 5 }, true, "PUBG" });
        }
    }
}
