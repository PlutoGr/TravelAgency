using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.DTOs.Requests;
using TravelAgency.Booking.Infrastructure.GrpcClients;
using TravelAgency.Booking.IntegrationTests.Helpers;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Booking.IntegrationTests.Bookings;

[Collection(BookingPostgresCollection.Name)]
public class CreateProposalSnapshotUtcTests
{
    private readonly ProposalUtcWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();

    public CreateProposalSnapshotUtcTests(ProposalUtcWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProposal_WhenCatalogSendsRoundtripUtc_PersistsTheSameUtcInstant()
    {
        ProposalUtcWebApplicationFactory.CatalogSnapshotTakenAtWire.Should().Be("2026-04-04T10:15:30.0000000Z");

        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<ICatalogGrpcClient>()
                .Should().BeOfType<CatalogGrpcClient>();
        }

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateToken(_clientId, AppRoles.Client));
        var tourId = Guid.NewGuid();
        var createBooking = await _client.PostAsJsonAsync("/bookings", new CreateBookingRequest(tourId, null));
        createBooking.StatusCode.Should().Be(HttpStatusCode.Created);
        var booking = await createBooking.Content.ReadFromJsonAsync<BookingDto>();
        booking.Should().NotBeNull();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateToken(_managerId, AppRoles.Manager));
        var proposalResponse = await _client.PostAsJsonAsync(
            $"/bookings/{booking!.Id}/proposal",
            new CreateProposalRequest("Снимок в UTC"));
        var proposalBody = await proposalResponse.Content.ReadAsStringAsync();
        proposalResponse.StatusCode.Should().Be(HttpStatusCode.Created, proposalBody);

        var stored = await _factory.ReadSnapshotTakenAtAsync(booking.Id);
        stored.Kind.Should().Be(DateTimeKind.Utc);
        stored.Should().Be(ProposalUtcWebApplicationFactory.CatalogSnapshotTakenAtUtc);
    }
}
