using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TravelAgency.Identity.Infrastructure.Persistence;

#nullable disable

namespace TravelAgency.Identity.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20261027060000_AddUserProfileAndAvatar")]
    public partial class AddUserProfileAndAvatar : Migration
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

            migrationBuilder.AddColumn<Guid>(
                name: "AvatarMediaFileId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LanguagesJson",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Specialization",
                table: "Users",
                type: "character varying(300)",
                maxLength: 300,
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
            migrationBuilder.DropColumn(name: "AvatarMediaFileId", table: "Users");
            migrationBuilder.DropColumn(name: "City", table: "Users");
            migrationBuilder.DropColumn(name: "LanguagesJson", table: "Users");
            migrationBuilder.DropColumn(name: "Specialization", table: "Users");
            migrationBuilder.DropColumn(name: "TourismSince", table: "Users");
        }
    }
}
