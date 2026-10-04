using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Media.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTourImagePublication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SizeCode",
                table: "MediaFileThumbnails",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "MediaFiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "MediaFiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "MediaFiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "general");

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "MediaFiles",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SizeCode",
                table: "MediaFileThumbnails");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "MediaFiles");
        }
    }
}
