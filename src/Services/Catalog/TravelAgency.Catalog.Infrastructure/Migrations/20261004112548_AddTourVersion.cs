using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTourVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Tours",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Tours");
        }
    }
}
