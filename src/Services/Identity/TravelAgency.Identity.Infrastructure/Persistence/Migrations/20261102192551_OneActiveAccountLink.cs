using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OneActiveAccountLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens");

            migrationBuilder.DropIndex(
                name: "IX_EmailChangeRequests_UserId",
                table: "EmailChangeRequests");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_OneActive",
                table: "PasswordResetTokens",
                column: "UserId",
                unique: true,
                filter: "\"ConsumedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeRequests_OneActive",
                table: "EmailChangeRequests",
                column: "UserId",
                unique: true,
                filter: "\"ConsumedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PasswordResetTokens_OneActive",
                table: "PasswordResetTokens");

            migrationBuilder.DropIndex(
                name: "IX_EmailChangeRequests_OneActive",
                table: "EmailChangeRequests");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeRequests_UserId",
                table: "EmailChangeRequests",
                column: "UserId");
        }
    }
}
