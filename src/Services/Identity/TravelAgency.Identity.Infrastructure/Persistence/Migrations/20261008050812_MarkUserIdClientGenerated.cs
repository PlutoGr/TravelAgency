using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MarkUserIdClientGenerated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Схема не меняется. Снимок модели больше не помечает Users.Id как ValueGeneratedOnAdd:
            // иначе EF шлёт UPDATE вместо INSERT, когда сидер записывает заранее известный Guid.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
