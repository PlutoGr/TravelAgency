using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTourPriceToTourOffer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // xmin — системная колонка PostgreSQL. EF читает её как concurrency token,
            // отдельный столбец создавать нельзя.

            migrationBuilder.AddColumn<string>(
                name: "AccommodationText",
                table: "Tours",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepartureCity",
                table: "Tours",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MealPlan",
                table: "Tours",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Tours",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Tours",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                table: "Tours",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Tours",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Tours",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            // OwnerId остаётся NULL: Identity создаёт manager@test.com через Guid.NewGuid()
            // в другой базе, Catalog не может узнать этот id детерминированно.
            migrationBuilder.Sql(
                """
                UPDATE "Tours"
                SET "Status" = CASE WHEN "IsActive" THEN 'Published' ELSE 'Unpublished' END,
                    "Source" = 'Manager',
                    "PublishedAt" = CASE WHEN "IsActive" THEN COALESCE("UpdatedAt", "CreatedAt") ELSE NULL END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Tours",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "Tours",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            // SQLite пересобирает таблицу по модели и копирует xmin. В PostgreSQL xmin системный,
            // отдельный столбец не создаём. Для SQLite-тестов колонка должна уже быть в старой таблице.
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.AddColumn<uint>(
                    name: "xmin",
                    table: "Tours",
                    type: "xid",
                    rowVersion: true,
                    nullable: false,
                    defaultValue: 0u);
            }

            migrationBuilder.DropIndex(
                name: "IX_Tours_IsActive",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Tours");

            migrationBuilder.RenameTable(
                name: "TourPrices",
                newName: "TourOffers");

            migrationBuilder.RenameIndex(
                name: "IX_TourPrices_TourId",
                table: "TourOffers",
                newName: "IX_TourOffers_TourId");

            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(
                    """
                    ALTER TABLE "TourOffers" RENAME CONSTRAINT "PK_TourPrices" TO "PK_TourOffers";
                    ALTER TABLE "TourOffers" RENAME CONSTRAINT "FK_TourPrices_Tours_TourId" TO "FK_TourOffers_Tours_TourId";
                    """);
            }

            migrationBuilder.CreateTable(
                name: "TourComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TourId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourComponents_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TourId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsCover = table.Column<bool>(type: "boolean", nullable: false),
                    Alt = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    WidthPx = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourImages_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TourId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ComponentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourDays_TourComponents_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "TourComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TourDays_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourInclusions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TourId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ComponentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourInclusions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourInclusions_TourComponents_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "TourComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TourInclusions_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tours_Source",
                table: "Tours",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_Tours_Status",
                table: "Tours",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TourComponents_TourId",
                table: "TourComponents",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_TourDays_ComponentId",
                table: "TourDays",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_TourDays_TourId_DayNumber",
                table: "TourDays",
                columns: new[] { "TourId", "DayNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourImages_OneCoverPerTour",
                table: "TourImages",
                column: "TourId",
                unique: true,
                filter: "\"IsCover\"");

            migrationBuilder.CreateIndex(
                name: "IX_TourImages_TourId_SortOrder",
                table: "TourImages",
                columns: new[] { "TourId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TourInclusions_ComponentId",
                table: "TourInclusions",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_TourInclusions_TourId",
                table: "TourInclusions",
                column: "TourId");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TourDays");

            migrationBuilder.DropTable(
                name: "TourImages");

            migrationBuilder.DropTable(
                name: "TourInclusions");

            migrationBuilder.DropTable(
                name: "TourComponents");

            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(
                    """
                    ALTER TABLE "TourOffers" RENAME CONSTRAINT "FK_TourOffers_Tours_TourId" TO "FK_TourPrices_Tours_TourId";
                    ALTER TABLE "TourOffers" RENAME CONSTRAINT "PK_TourOffers" TO "PK_TourPrices";
                    """);
            }

            migrationBuilder.RenameIndex(
                name: "IX_TourOffers_TourId",
                table: "TourOffers",
                newName: "IX_TourPrices_TourId");

            migrationBuilder.RenameTable(
                name: "TourOffers",
                newName: "TourPrices");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Tours",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Tours"
                SET "IsActive" = ("Status" = 'Published');
                """);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Tours",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_Tours_Source",
                table: "Tours");

            migrationBuilder.DropIndex(
                name: "IX_Tours_Status",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "AccommodationText",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "DepartureCity",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "MealPlan",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "ShortDescription",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tours");

            migrationBuilder.CreateIndex(
                name: "IX_Tours_IsActive",
                table: "Tours",
                column: "IsActive");
        }
    }
}
