using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Media.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MarkMediaFileIdClientGenerated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Схема не меняется. Снимок модели больше не помечает MediaFiles.Id как ValueGeneratedOnAdd:
            // иначе EF шлёт UPDATE вместо INSERT, когда сидер записывает заранее известный Guid.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
