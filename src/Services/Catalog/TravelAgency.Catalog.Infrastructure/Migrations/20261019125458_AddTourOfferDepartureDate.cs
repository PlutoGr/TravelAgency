using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTourOfferDepartureDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Сначала пустой столбец: значение 0001-01-01 нельзя принять за день вылета.
            migrationBuilder.AddColumn<DateOnly>(
                name: "DepartureDate",
                table: "TourOffers",
                type: "date",
                nullable: true);

            // ValidFrom (timestamptz) переводится в местный день города вылета.
            // Города нет в таблице: Europe/Moscow и NOTICE «без пояса».
            // Повтор SQL не переписывает уже заполненные строки.
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(DepartureDateMigrationSql.PostgresBackfill());
            }
            else
            {
                migrationBuilder.Sql(DepartureDateMigrationSql.SqliteBackfill);
            }

            migrationBuilder.AlterColumn<DateOnly>(
                name: "DepartureDate",
                table: "TourOffers",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourOffers_DepartureDate",
                table: "TourOffers",
                column: "DepartureDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TourOffers_DepartureDate",
                table: "TourOffers");

            migrationBuilder.DropColumn(
                name: "DepartureDate",
                table: "TourOffers");
        }
    }
}
