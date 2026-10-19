using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingAssignedManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedManagerId",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            // xmin: системная колонка PostgreSQL. EF читает её как concurrency token.
            // Отдельный столбец создавать и удалять нельзя.

            migrationBuilder.CreateTable(
                name: "BookingAssignmentHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousManagerId = table.Column<Guid>(type: "uuid", nullable: true),
                    NewManagerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingAssignmentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingAssignmentHistories_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_AssignedManagerId",
                table: "Bookings",
                column: "AssignedManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingAssignmentHistories_BookingId_ChangedAt",
                table: "BookingAssignmentHistories",
                columns: new[] { "BookingId", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingAssignmentHistories");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_AssignedManagerId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AssignedManagerId",
                table: "Bookings");
        }
    }
}
