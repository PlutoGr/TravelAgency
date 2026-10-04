namespace TravelAgency.Booking.IntegrationTests.Bookings;

[CollectionDefinition(Name)]
public sealed class BookingPostgresCollection : ICollectionFixture<ProposalUtcWebApplicationFactory>
{
    public const string Name = "Booking Postgres";
}
