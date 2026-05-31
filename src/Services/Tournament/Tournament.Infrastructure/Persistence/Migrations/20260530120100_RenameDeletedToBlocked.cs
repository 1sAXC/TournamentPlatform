using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tournament.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameDeletedToBlocked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename the existing primary-key constraint first so EF's snapshot
            // (which derives the name from the new table name) matches what's
            // in Postgres after the rename.
            migrationBuilder.Sql("ALTER TABLE \"DeletedUserProjections\" RENAME CONSTRAINT \"PK_DeletedUserProjections\" TO \"PK_BlockedUserProjections\";");

            migrationBuilder.RenameTable(
                name: "DeletedUserProjections",
                newName: "BlockedUserProjections");

            migrationBuilder.RenameColumn(
                name: "DeletedAtUtc",
                table: "BlockedUserProjections",
                newName: "BlockedAtUtc");

            migrationBuilder.RenameColumn(
                name: "deleted_at_utc",
                table: "user_projections",
                newName: "blocked_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "blocked_at_utc",
                table: "user_projections",
                newName: "deleted_at_utc");

            migrationBuilder.RenameColumn(
                name: "BlockedAtUtc",
                table: "BlockedUserProjections",
                newName: "DeletedAtUtc");

            migrationBuilder.RenameTable(
                name: "BlockedUserProjections",
                newName: "DeletedUserProjections");

            migrationBuilder.Sql("ALTER TABLE \"DeletedUserProjections\" RENAME CONSTRAINT \"PK_BlockedUserProjections\" TO \"PK_DeletedUserProjections\";");
        }
    }
}
