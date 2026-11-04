using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TravelAgency.Identity.Infrastructure.Persistence;

#nullable disable

namespace TravelAgency.Identity.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20261104120000_AddManagerCardFields")]
    public partial class AddManagerCardFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "About",
                table: "Users",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "Users",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TagsJson",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TourismSince",
                table: "Users",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "About", table: "Users");
            migrationBuilder.DropColumn(name: "Position", table: "Users");
            migrationBuilder.DropColumn(name: "TagsJson", table: "Users");
            migrationBuilder.DropColumn(name: "TourismSince", table: "Users");
        }
    }
}
