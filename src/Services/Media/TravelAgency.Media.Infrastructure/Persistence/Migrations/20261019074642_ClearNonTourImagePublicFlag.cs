using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Media.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClearNonTourImagePublicFlag : Migration
    {
        /// <summary>
        /// Фото туров. Регистр не важен: Tour-Image и TOUR-IMAGE тоже остаются публичными.
        /// Новое назначение фото тура нужно добавить и в этот список.
        /// </summary>
        private const string TourPhotoPurposesSql = """
            'tour-image'
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
                return;

            // Меняется только IsPublic. Строки MediaFiles не удаляются.
            // Id снятых флагов лежат в MediaFilePublicFlagRollback, чтобы Down вернул флаг ровно им.
            // Повторный запуск видит эту таблицу и ничего не меняет.
            migrationBuilder.Sql(
                $"""
                DO $migration$
                DECLARE
                    changed_count integer := 0;
                BEGIN
                    IF to_regclass('public."MediaFilePublicFlagRollback"') IS NOT NULL THEN
                        RAISE WARNING 'MediaFiles.IsPublic cleared: %', 0;
                        RETURN;
                    END IF;

                    CREATE TABLE "MediaFilePublicFlagRollback" (
                        "MediaFileId" uuid NOT NULL,
                        CONSTRAINT "PK_MediaFilePublicFlagRollback" PRIMARY KEY ("MediaFileId")
                    );

                    INSERT INTO "MediaFilePublicFlagRollback" ("MediaFileId")
                    SELECT "Id"
                    FROM "MediaFiles"
                    WHERE "IsPublic" = TRUE
                      AND (
                          "Purpose" IS NULL
                          OR lower("Purpose") NOT IN (
                              {TourPhotoPurposesSql}
                          )
                      );

                    UPDATE "MediaFiles"
                    SET "IsPublic" = FALSE
                    WHERE "Id" IN (
                        SELECT "MediaFileId" FROM "MediaFilePublicFlagRollback"
                    );

                    GET DIAGNOSTICS changed_count = ROW_COUNT;
                    RAISE WARNING 'MediaFiles.IsPublic cleared: %', changed_count;
                END
                $migration$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
                return;

            migrationBuilder.Sql(
                """
                DO $migration$
                DECLARE
                    restored_count integer := 0;
                BEGIN
                    IF to_regclass('public."MediaFilePublicFlagRollback"') IS NULL THEN
                        RAISE WARNING 'MediaFiles.IsPublic restored: %', 0;
                        RETURN;
                    END IF;

                    UPDATE "MediaFiles"
                    SET "IsPublic" = TRUE
                    WHERE "Id" IN (
                        SELECT "MediaFileId" FROM "MediaFilePublicFlagRollback"
                    );

                    GET DIAGNOSTICS restored_count = ROW_COUNT;
                    RAISE WARNING 'MediaFiles.IsPublic restored: %', restored_count;

                    DROP TABLE "MediaFilePublicFlagRollback";
                END
                $migration$;
                """);
        }
    }
}
