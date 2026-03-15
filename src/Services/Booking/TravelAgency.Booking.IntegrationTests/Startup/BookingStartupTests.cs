namespace TravelAgency.Booking.IntegrationTests.Startup;

/// <summary>
/// Integration tests for Booking API startup (AUDIT-003).
/// Verifies the service starts successfully without OutboxProcessorBackgroundService.
/// </summary>
public class BookingStartupTests
{
    [Fact]
    public async Task Service_StartsSuccessfully_WithoutOutboxProcessor()
    {
        await using var factory = new CustomWebApplicationFactory();
        factory.EnsureDbCreated();

        var client = factory.CreateClient();
        var response = await client.GetAsync("/health/live");

        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
