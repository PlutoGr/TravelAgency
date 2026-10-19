using Microsoft.Extensions.Options;
using TravelAgency.Catalog.Application.Time;
using TravelAgency.Catalog.Domain;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Infrastructure.Migrations;
using TravelAgency.Catalog.Infrastructure.Time;

namespace TravelAgency.Catalog.UnitTests.Time;

public class DepartureDateRulesTests
{
    private static readonly TimeZoneInfo Moscow = Zone("Europe/Moscow");
    private static readonly TimeZoneInfo Novosibirsk = Zone("Asia/Novosibirsk");

    [Theory]
    [InlineData(" москва ", "Europe/Moscow")]
    [InlineData("Москва", "Europe/Moscow")]
    [InlineData("НОВОСИБИРСК", "Asia/Novosibirsk")]
    [InlineData("Новосибирск", "Asia/Novosibirsk")]
    [InlineData("Чёлябинск", "Asia/Yekaterinburg")]
    [InlineData("челябинск", "Asia/Yekaterinburg")]
    [InlineData("  Ростов-на-Дону  ", "Europe/Moscow")]
    public void City_MatchesTheTable_IgnoringCaseEdgesAndYo(string city, string zoneId)
    {
        DepartureCityTimeZones.FindZoneId(city).Should().Be(zoneId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Париж")]
    public void UnknownCity_HasNoZone(string? city)
    {
        DepartureCityTimeZones.FindZoneId(city).Should().BeNull();
    }

    [Fact]
    public void FoldAlphabet_HasEqualLength_AndMapsYoToE()
    {
        DepartureCityTimeZones.FoldSource.Length.Should().Be(DepartureCityTimeZones.FoldTarget.Length);
        DepartureCityTimeZones.Normalize("Ёлка").Should().Be("елка");
        DepartureCityTimeZones.Normalize("ёлка").Should().Be("елка");
    }

    [Fact]
    public void Table_HasOneZonePerNormalizedCity()
    {
        var keys = DepartureCityTimeZones.Entries.Select(entry => DepartureCityTimeZones.Normalize(entry.City)).ToList();
        keys.Should().OnlyHaveUniqueItems();
        keys.Should().HaveCount(23);
    }

    [Fact]
    public void Migration_MoscowAndNovosibirskAt0030Local_BothBecomeNovember2_WithoutUnknownLog()
    {
        var log = new List<string>();
        var moscow = new DateTime(2026, 11, 1, 21, 30, 0, DateTimeKind.Utc);
        var novosibirsk = new DateTime(2026, 11, 1, 17, 30, 0, DateTimeKind.Utc);
        var offers = new (Guid OfferId, DateTime ValidFrom, string? DepartureCity)[]
        {
            (Guid.NewGuid(), moscow, " москва "),
            (Guid.NewGuid(), novosibirsk, "Новосибирск"),
            (Guid.NewGuid(), new DateTime(2026, 11, 1, 22, 30, 0, DateTimeKind.Utc), "Сочи")
        };

        var converted = DepartureDateConversion.ConvertOffers(offers, Moscow, log);

        converted.Should().OnlyContain(row => row.DepartureDate == new DateOnly(2026, 11, 2));
        log.Should().NotContain(line => line.Contains(DepartureDateConversion.UnknownCityMarker, StringComparison.Ordinal));
        log.Should().Contain(DepartureDateConversion.UpdatedLine(3));
    }

    [Fact]
    public void Migration_UnknownCity_UsesDefaultZone_AndWritesALogLine()
    {
        var log = new List<string>();
        var instant = new DateTime(2026, 11, 1, 21, 30, 0, DateTimeKind.Utc);
        var offerId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");

        var date = DepartureDateConversion.ToLocalDate(instant, "Париж", Moscow, log, offerId);

        date.Should().Be(new DateOnly(2026, 11, 2));
        log.Should().ContainSingle()
            .Which.Should().Contain(DepartureDateConversion.UnknownCityMarker)
            .And.Contain("Париж")
            .And.Contain(offerId.ToString());
    }

    [Fact]
    public void Migration_IsIdempotentForTheSameInstant()
    {
        var instant = new DateTime(2026, 11, 1, 17, 30, 0, DateTimeKind.Utc);
        var first = new List<string>();
        var second = new List<string>();
        var row = new (Guid, DateTime, string?)[] { (Guid.NewGuid(), instant, "Новосибирск") };

        var once = DepartureDateConversion.ConvertOffers(row, Moscow, first);
        var twice = DepartureDateConversion.ConvertOffers(row, Moscow, second);

        twice.Should().Equal(once);
    }

    [Fact]
    public void Migration_MidnightUtc_KeepsTheSameCalendarDayInMoscow()
    {
        var log = new List<string>();
        var midnight = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

        var date = DepartureDateConversion.ToLocalDate(midnight, "Москва", Moscow, log);

        date.Should().Be(new DateOnly(2026, 11, 1));
        log.Should().BeEmpty();
    }

    [Fact]
    public void TypedDay_StaysTheCalendarDay_AndTimestampUsesTheCityZone()
    {
        var typed = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Unspecified);
        DepartureDateConversion.ForInput(typed, "Новосибирск", Moscow)
            .Should().Be(new DateOnly(2026, 11, 2));

        var novosibirskNight = new DateTime(2026, 11, 1, 17, 30, 0, DateTimeKind.Utc);
        DepartureDateConversion.ForInput(novosibirskNight, "Новосибирск", Moscow)
            .Should().Be(new DateOnly(2026, 11, 2));

        var moscowNight = new DateTime(2026, 11, 1, 21, 30, 0, DateTimeKind.Utc);
        DepartureDateConversion.ForInput(moscowNight, "Париж", Moscow)
            .Should().Be(new DateOnly(2026, 11, 2));
    }

    [Fact]
    public void TodayAt0030Moscow_ShowsNovember2_AndHidesNovember1()
    {
        var clock = Clock("2026-11-01T21:30:00Z", "Europe/Moscow");
        var today = clock.Today();
        today.Should().Be(new DateOnly(2026, 11, 2));

        var offers = Offers(
            (new DateOnly(2026, 11, 1), 10m),
            (new DateOnly(2026, 11, 2), 80m));

        PublicCatalogPricing.PriceFrom(offers, today).Should().Be(80m);
        PublicCatalogPricing.NearestDeparture(offers, today).Should().Be(new DateOnly(2026, 11, 2));
        DepartureListing.Includes(new DateOnly(2026, 11, 2), today, new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 2))
            .Should().BeTrue();
        DepartureListing.Includes(new DateOnly(2026, 11, 2), today, null, new DateOnly(2026, 11, 1))
            .Should().BeFalse();
        DepartureListing.Includes(new DateOnly(2026, 11, 1), today, null, new DateOnly(2026, 11, 1))
            .Should().BeFalse();
    }

    [Fact]
    public void SameInstant_IsStillNovember1InMoscow_AndAlreadyNovember2InNovosibirsk()
    {
        var instant = "2026-11-01T17:30:00Z";
        Clock(instant, "Europe/Moscow").Today().Should().Be(new DateOnly(2026, 11, 1));
        Clock(instant, "Asia/Novosibirsk").Today().Should().Be(new DateOnly(2026, 11, 2));

        var moscowToday = Clock(instant, "Europe/Moscow").Today();
        var novosibirskToday = Clock(instant, "Asia/Novosibirsk").Today();
        var departure = new DateOnly(2026, 11, 1);

        DepartureListing.Includes(departure, moscowToday, null, new DateOnly(2026, 11, 1)).Should().BeTrue();
        DepartureListing.Includes(departure, novosibirskToday, null, new DateOnly(2026, 11, 1)).Should().BeFalse();
        DepartureListing.Includes(new DateOnly(2026, 11, 2), novosibirskToday, new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 2))
            .Should().BeTrue();
    }

    [Fact]
    public void Filter_November2_MatchesItsOwnDay_AndNovember1_DoesNotMatchTheNextDay()
    {
        var today = new DateOnly(2026, 10, 1);
        var november2 = new DateOnly(2026, 11, 2);
        var november1 = new DateOnly(2026, 11, 1);

        DepartureListing.Includes(november2, today, new DateOnly(2026, 11, 2), null).Should().BeTrue();
        DepartureListing.Includes(november2, today, null, new DateOnly(2026, 11, 2)).Should().BeTrue();
        DepartureListing.Includes(november2, today, null, new DateOnly(2026, 11, 1)).Should().BeFalse();

        DepartureListing.Includes(november1, today, null, new DateOnly(2026, 11, 1)).Should().BeTrue();
        DepartureListing.Includes(november1, today, new DateOnly(2026, 11, 2), null).Should().BeFalse();
    }

    [Fact]
    public void DefaultTimeZone_IsEuropeMoscow()
    {
        var options = new CatalogOptions();
        options.DefaultTimeZone.Should().Be(CatalogOptions.DefaultTimeZoneId);
        options.ResolveZone().Id.Should().Be("Europe/Moscow");
        Novosibirsk.Id.Should().Be("Asia/Novosibirsk");
    }

    [Fact]
    public void MigrationSql_UsesTheCityTable_DefaultZone_AndUnknownCityNotice()
    {
        var sql = DepartureDateMigrationSql.PostgresBackfill();

        foreach (var entry in DepartureCityTimeZones.Entries)
        {
            sql.Should().Contain($"('{DepartureCityTimeZones.Normalize(entry.City)}', '{entry.ZoneId}')");
        }

        sql.Should().Contain("'Europe/Moscow'");
        sql.Should().Contain("DepartureDate\" IS NULL");
        sql.Should().Contain("город без пояса");
        sql.Should().Contain("обновлено % записей");
        sql.Should().Contain(DepartureCityTimeZones.FoldSource);
        sql.Should().NotContain("Париж");
    }

    private static CatalogClock Clock(string utc, string zoneId) =>
        new(new FixedTimeProvider(DateTimeOffset.Parse(utc)), Options.Create(new CatalogOptions { DefaultTimeZone = zoneId }));

    private static TimeZoneInfo Zone(string id) => TimeZoneInfo.FindSystemTimeZoneById(id);

    private static TourOffer[] Offers(params (DateOnly Day, decimal Price)[] rows)
    {
        var tourId = Guid.NewGuid();
        return rows.Select(row => TourOffer.Create(
            tourId,
            row.Day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            row.Day.AddDays(7).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            row.Price,
            "EUR",
            2,
            row.Day)).ToArray();
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
