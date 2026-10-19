using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using TravelAgency.Catalog.Application.Time;
using TravelAgency.Catalog.Infrastructure.Migrations;
using TravelAgency.Catalog.Infrastructure.Persistence;

namespace TravelAgency.Catalog.IntegrationTests.Migrations;

public class DepartureDateMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("catalog_departure_date")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Migration_ConvertsValidFromInTheDepartureCityZone_AndDownDropsTheColumn()
    {
        var logs = new CollectingLogger();
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .AddInterceptors(new PostgresNoticeLoggingInterceptor(logs))
            .Options;

        var moscowTour = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa11");
        var novosibirskTour = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa12");
        var yoTour = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa13");
        var unknownTour = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa14");
        var moscowOffer = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb11");
        var novosibirskOffer = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb12");
        var yoOffer = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb13");
        var unknownOffer = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb14");
        var createdAt = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);
        var moscowNight = new DateTime(2026, 11, 1, 21, 30, 0, DateTimeKind.Utc);
        var novosibirskNight = new DateTime(2026, 11, 1, 17, 30, 0, DateTimeKind.Utc);
        var yekaterinburgNight = new DateTime(2026, 11, 1, 19, 30, 0, DateTimeKind.Utc);
        var validTo = new DateTime(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc);

        await using (var db = new CatalogDbContext(options))
        {
            await db.Database.MigrateAsync("20261004112548_AddTourVersion");
            await InsertTourAsync(db, moscowTour, " москва ", createdAt);
            await InsertTourAsync(db, novosibirskTour, "Новосибирск", createdAt);
            await InsertTourAsync(db, yoTour, "Чёлябинск", createdAt);
            await InsertTourAsync(db, unknownTour, "Париж", createdAt);
            await InsertOfferAsync(db, moscowOffer, moscowTour, moscowNight, validTo);
            await InsertOfferAsync(db, novosibirskOffer, novosibirskTour, novosibirskNight, validTo);
            await InsertOfferAsync(db, yoOffer, yoTour, yekaterinburgNight, validTo);
            await InsertOfferAsync(db, unknownOffer, unknownTour, moscowNight, validTo);

            await db.Database.MigrateAsync();

            var offers = await db.TourOffers.AsNoTracking().ToDictionaryAsync(offer => offer.Id);
            offers[moscowOffer].DepartureDate.Should().Be(new DateOnly(2026, 11, 2));
            offers[novosibirskOffer].DepartureDate.Should().Be(new DateOnly(2026, 11, 2));
            offers[yoOffer].DepartureDate.Should().Be(new DateOnly(2026, 11, 2));
            offers[unknownOffer].DepartureDate.Should().Be(new DateOnly(2026, 11, 2));
            offers[moscowOffer].ValidFrom.Should().Be(moscowNight);

            var unknownLines = logs.Messages.Where(line => line.Contains(DepartureDateConversion.UnknownCityMarker, StringComparison.Ordinal)).ToList();
            unknownLines.Should().ContainSingle();
            unknownLines[0].Should().Contain("Париж");
            unknownLines[0].Should().NotContain("Новосибирск");
            unknownLines[0].Should().NotContain("москва");
            logs.Messages.Should().Contain(line => line.Contains("обновлено 4", StringComparison.Ordinal));

            var logged = logs.Messages.Count;
            await db.Database.ExecuteSqlRawAsync(DepartureDateMigrationSql.PostgresBackfill());
            offers = await db.TourOffers.AsNoTracking().ToDictionaryAsync(offer => offer.Id);
            offers[moscowOffer].DepartureDate.Should().Be(new DateOnly(2026, 11, 2));
            logs.Messages.Skip(logged).Should().NotContain(line => line.Contains(DepartureDateConversion.UnknownCityMarker, StringComparison.Ordinal));
            logs.Messages.Skip(logged).Should().Contain(line => line.Contains("обновлено 0", StringComparison.Ordinal));

            await db.Database.MigrateAsync("20261004112548_AddTourVersion");
        }

        await using var afterDown = new CatalogDbContext(options);
        var dropped = afterDown.Database.GetDbConnection();
        await dropped.OpenAsync();
        await using var command = dropped.CreateCommand();
        command.CommandText = """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_name = 'TourOffers' AND column_name = 'DepartureDate';
            """;
        var stillThere = await command.ExecuteScalarAsync();
        stillThere.Should().BeNull();
    }

    private static Task InsertTourAsync(CatalogDbContext db, Guid id, string city, DateTime createdAt) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Tours"
                ("Id", "Title", "Description", "TourType", "Country", "DurationDays", "CreatedAt", "Status", "Source", "Version", "DepartureCity")
            VALUES
                ({id}, {"Тур"}, {"Описание тура для миграции даты вылета."}, {"Beach"}, {"Россия"}, {7}, {createdAt}, {"Published"}, {"Manager"}, {0}, {city});
            """);

    private static Task InsertOfferAsync(
        CatalogDbContext db,
        Guid id,
        Guid tourId,
        DateTime validFrom,
        DateTime validTo) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "TourOffers"
                ("Id", "TourId", "ValidFrom", "ValidTo", "PricePerPerson", "Currency", "AvailableSeats")
            VALUES
                ({id}, {tourId}, {validFrom}, {validTo}, {1000m}, {"RUB"}, {4});
            """);

    private sealed class CollectingLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
