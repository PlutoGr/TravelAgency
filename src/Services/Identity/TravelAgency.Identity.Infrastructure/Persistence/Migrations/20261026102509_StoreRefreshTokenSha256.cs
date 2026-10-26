using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreRefreshTokenSha256 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Существующие сессии остаются рабочими: значение заменяется на SHA-256,
            // поиск идёт по тому же хешу. pgcrypto есть в образе PostgreSQL, пользователь БД его включает.
            migrationBuilder.Sql(
                """
                CREATE EXTENSION IF NOT EXISTS pgcrypto;

                UPDATE "RefreshTokens"
                SET "Token" = encode(sha256(convert_to("Token", 'UTF8')), 'hex'),
                    "ReplacedByToken" = CASE
                        WHEN "ReplacedByToken" IS NULL THEN NULL
                        ELSE encode(sha256(convert_to("ReplacedByToken", 'UTF8')), 'hex')
                    END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "RefreshTokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "ReplacedByToken",
                table: "RefreshTokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Колонки снова вмещают длинную строку. Открытые значения не восстанавливаются.
            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "RefreshTokens",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "ReplacedByToken",
                table: "RefreshTokens",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }
    }
}
