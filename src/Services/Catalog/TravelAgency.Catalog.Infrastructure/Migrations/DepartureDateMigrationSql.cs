using TravelAgency.Catalog.Application.Time;

namespace TravelAgency.Catalog.Infrastructure.Migrations;

/// <summary>
/// Заполнение DepartureDate из ValidFrom в поясе города вылета.
/// Повторный запуск не трогает уже заполненные строки: условие DepartureDate IS NULL.
/// </summary>
public static class DepartureDateMigrationSql
{
    public static string PostgresBackfill()
    {
        var values = string.Join(",\n                ",
            DepartureCityTimeZones.Entries.Select(entry =>
                $"({SqlLiteral(DepartureCityTimeZones.Normalize(entry.City))}, {SqlLiteral(entry.ZoneId)})"));
        var foldSource = SqlLiteral(DepartureCityTimeZones.FoldSource);
        var foldTarget = SqlLiteral(DepartureCityTimeZones.FoldTarget);
        var fallback = SqlLiteral(CatalogOptions.DefaultTimeZoneId);

        return $$"""
            DO $departure$
            DECLARE
                updated_count integer := 0;
                rec record;
            BEGIN
                CREATE TEMP TABLE departure_date_backfill ON COMMIT DROP AS
                SELECT
                    o."Id" AS offer_id,
                    t."DepartureCity" AS city,
                    COALESCE(z.zone_id, {{fallback}}) AS zone_id,
                    z.zone_id IS NULL AS unknown
                FROM "TourOffers" AS o
                INNER JOIN "Tours" AS t ON t."Id" = o."TourId"
                LEFT JOIN (VALUES
                    {{values}}
                ) AS z(city_key, zone_id)
                    ON z.city_key = translate(btrim(COALESCE(t."DepartureCity", '')), {{foldSource}}, {{foldTarget}})
                WHERE o."DepartureDate" IS NULL;

                UPDATE "TourOffers" AS o
                SET "DepartureDate" = (o."ValidFrom" AT TIME ZONE b.zone_id)::date
                FROM departure_date_backfill AS b
                WHERE o."Id" = b.offer_id;

                GET DIAGNOSTICS updated_count = ROW_COUNT;

                FOR rec IN
                    SELECT offer_id, city
                    FROM departure_date_backfill
                    WHERE unknown
                LOOP
                    RAISE NOTICE 'DepartureDate: город без пояса, предложение %, город "%"', rec.offer_id, COALESCE(rec.city, '');
                END LOOP;

                RAISE NOTICE 'DepartureDate: обновлено % записей', updated_count;
            END
            $departure$;
            """;
    }

    public const string SqliteBackfill = """
        UPDATE "TourOffers"
        SET "DepartureDate" = date("ValidFrom")
        WHERE "DepartureDate" IS NULL;
        """;

    private static string SqlLiteral(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
}
