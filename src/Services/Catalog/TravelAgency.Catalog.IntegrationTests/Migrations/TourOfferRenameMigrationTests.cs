using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Infrastructure.Persistence;

namespace TravelAgency.Catalog.IntegrationTests.Migrations;

public class TourOfferRenameMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("catalog_migration")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Migration_RenamesTourPricesWithoutLosingRows_AndMapsIsActiveToStatus()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var activeTourId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var inactiveTourId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
        var firstOfferId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        var secondOfferId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
        var inactiveOfferId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3");
        var createdAt = new DateTime(2026, 3, 1, 8, 30, 0, DateTimeKind.Utc);
        var firstFrom = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var firstTo = new DateTime(2026, 7, 8, 0, 0, 0, DateTimeKind.Utc);
        var secondFrom = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var secondTo = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc);
        var pastFrom = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var pastTo = new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc);

        await using (var db = new CatalogDbContext(options))
        {
            await db.Database.MigrateAsync("20260315095716_InitialCreate");

            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Tours"
                    ("Id", "Title", "Description", "TourType", "DirectionId", "Country", "DurationDays", "ImageUrl", "IsActive", "CreatedAt", "UpdatedAt")
                VALUES
                    ({activeTourId}, {"Мальдивы"}, {"Описание активного тура"}, {"Beach"}, {(Guid?)null}, {"Мальдивы"}, {7}, {"https://images.example/maldives.jpg"}, {true}, {createdAt}, {(DateTime?)null}),
                    ({inactiveTourId}, {"Скрытый тур"}, {"Описание скрытого тура"}, {"City"}, {(Guid?)null}, {"Италия"}, {4}, {"https://images.example/hidden.jpg"}, {false}, {createdAt}, {(DateTime?)null});
                """);

            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "TourPrices"
                    ("Id", "TourId", "ValidFrom", "ValidTo", "PricePerPerson", "Currency", "AvailableSeats")
                VALUES
                    ({firstOfferId}, {activeTourId}, {firstFrom}, {firstTo}, {340000.50m}, {"RUB"}, {12}),
                    ({secondOfferId}, {activeTourId}, {secondFrom}, {secondTo}, {289000.00m}, {"USD"}, {8}),
                    ({inactiveOfferId}, {inactiveTourId}, {pastFrom}, {pastTo}, {99.90m}, {"EUR"}, {3});
                """);

            await db.Database.MigrateAsync();
        }

        await using var verify = new CatalogDbContext(options);
        var tours = await verify.Tours.AsNoTracking().ToDictionaryAsync(t => t.Id);
        var offers = await verify.TourOffers.AsNoTracking().OrderBy(o => o.PricePerPerson).ToListAsync();

        tours.Should().HaveCount(2);

        var active = tours[activeTourId];
        active.Status.Should().Be(TourStatus.Published);
        active.Source.Should().Be(TourSource.Manager);
        active.OwnerId.Should().BeNull();
        active.ImageUrl.Should().Be("https://images.example/maldives.jpg");
        active.Title.Should().Be("Мальдивы");
        active.PublishedAt.Should().Be(createdAt);
        active.IsActive.Should().BeTrue();

        var inactive = tours[inactiveTourId];
        inactive.Status.Should().Be(TourStatus.Unpublished);
        inactive.Source.Should().Be(TourSource.Manager);
        inactive.OwnerId.Should().BeNull();
        inactive.ImageUrl.Should().Be("https://images.example/hidden.jpg");
        inactive.PublishedAt.Should().BeNull();
        inactive.IsActive.Should().BeFalse();

        offers.Should().HaveCount(3);
        offers.Select(o => o.Id).Should().BeEquivalentTo([firstOfferId, secondOfferId, inactiveOfferId]);

        var cheapest = offers.Single(o => o.Id == inactiveOfferId);
        cheapest.TourId.Should().Be(inactiveTourId);
        cheapest.ValidFrom.Should().Be(pastFrom);
        cheapest.ValidTo.Should().Be(pastTo);
        cheapest.PricePerPerson.Should().Be(99.90m);
        cheapest.Currency.Should().Be("EUR");
        cheapest.AvailableSeats.Should().Be(3);

        var usd = offers.Single(o => o.Id == secondOfferId);
        usd.TourId.Should().Be(activeTourId);
        usd.ValidFrom.Should().Be(secondFrom);
        usd.ValidTo.Should().Be(secondTo);
        usd.PricePerPerson.Should().Be(289000.00m);
        usd.Currency.Should().Be("USD");
        usd.AvailableSeats.Should().Be(8);

        var rub = offers.Single(o => o.Id == firstOfferId);
        rub.TourId.Should().Be(activeTourId);
        rub.ValidFrom.Should().Be(firstFrom);
        rub.ValidTo.Should().Be(firstTo);
        rub.PricePerPerson.Should().Be(340000.50m);
        rub.Currency.Should().Be("RUB");
        rub.AvailableSeats.Should().Be(12);

        var connection = verify.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                to_regclass('public."TourPrices"') IS NULL,
                to_regclass('public."TourOffers"') IS NOT NULL;
            """;
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        reader.GetBoolean(0).Should().BeTrue();
        reader.GetBoolean(1).Should().BeTrue();
    }
}
