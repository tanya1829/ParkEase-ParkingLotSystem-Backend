using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkEase.AuthService.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Users",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                newName: "RefreshTokens",
                newSchema: "auth");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Users",
                schema: "auth",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                schema: "auth",
                newName: "RefreshTokens");
        }
    }
}
