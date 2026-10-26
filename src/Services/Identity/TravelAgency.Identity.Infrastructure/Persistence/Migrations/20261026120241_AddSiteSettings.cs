using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TravelAgency.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    SiteName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SupportPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    OfficeAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    WorkingHours = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SocialLinksJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    BannerText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BannerEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteSettingsAudit",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FieldName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OldValue = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    NewValue = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettingsAudit", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SiteSettings",
                columns: new[] { "Id", "BannerEnabled", "BannerText", "ContactEmail", "OfficeAddress", "SiteName", "SocialLinksJson", "SupportPhone", "UpdatedAt", "UpdatedByUserId", "WorkingHours" },
                values: new object[] { 1, false, null, null, null, "TravelAgency", "[]", null, new DateTimeOffset(new DateTime(2026, 10, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null });

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettingsAudit_ChangedAt",
                table: "SiteSettingsAudit",
                column: "ChangedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteSettings");

            migrationBuilder.DropTable(
                name: "SiteSettingsAudit");
        }
    }
}
