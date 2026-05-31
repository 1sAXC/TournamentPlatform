using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameDeletedToBlocked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DeletedAtUtc",
                table: "Users",
                newName: "BlockedAtUtc");

            migrationBuilder.Sql("UPDATE \"Users\" SET \"Status\" = 'Blocked' WHERE \"Status\" = 'Deleted';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Status\" = 'Deleted' WHERE \"Status\" = 'Blocked';");

            migrationBuilder.RenameColumn(
                name: "BlockedAtUtc",
                table: "Users",
                newName: "DeletedAtUtc");
        }
    }
}
