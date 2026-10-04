using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.DTOs.Requests;
using TravelAgency.Booking.Domain.Enums;
using TravelAgency.Booking.IntegrationTests.Helpers;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Booking.IntegrationTests.Bookings;

[Collection(BookingPostgresCollection.Name)]
public class BookingStatusAndConfirmPostgresTests
{
    private readonly ProposalUtcWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();

    public BookingStatusAndConfirmPostgresTests(ProposalUtcWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ChangeStatus_ThenConfirm_OnExistingBooking_PersistsStatusHistory()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateToken(_clientId, AppRoles.Client));
        var created = await _client.PostAsJsonAsync("/bookings", new CreateBookingRequest(Guid.NewGuid(), null));
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var booking = await created.Content.ReadFromJsonAsync<BookingDto>();
        booking.Should().NotBeNull();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateToken(_managerId, AppRoles.Manager));
        var inProgress = await _client.PatchAsJsonAsync(
            $"/bookings/{booking!.Id}/status",
            new ChangeBookingStatusRequest(BookingStatus.InProgress));
        var inProgressBody = await inProgress.Content.ReadAsStringAsync();
        inProgress.StatusCode.Should().Be(HttpStatusCode.OK, inProgressBody);

        var proposalResponse = await _client.PostAsJsonAsync(
            $"/bookings/{booking.Id}/proposal",
            new CreateProposalRequest("Нужно для подтверждения"));
        var proposalBody = await proposalResponse.Content.ReadAsStringAsync();
        proposalResponse.StatusCode.Should().Be(HttpStatusCode.Created, proposalBody);
        var proposal = JsonSerializer.Deserialize<ProposalDto>(
            proposalBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        proposal.Should().NotBeNull();

        var proposalSent = await _client.PatchAsJsonAsync(
            $"/bookings/{booking.Id}/status",
            new ChangeBookingStatusRequest(BookingStatus.ProposalSent));
        var proposalSentBody = await proposalSent.Content.ReadAsStringAsync();
        proposalSent.StatusCode.Should().Be(HttpStatusCode.OK, proposalSentBody);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateToken(_clientId, AppRoles.Client));
        var confirm = await _client.PostAsJsonAsync(
            $"/bookings/{booking.Id}/confirm",
            new ConfirmProposalRequest(proposal!.Id));
        var confirmBody = await confirm.Content.ReadAsStringAsync();
        confirm.StatusCode.Should().Be(HttpStatusCode.OK, confirmBody);

        var history = await _factory.ReadStatusHistoryAsync(booking.Id);
        history.Should().Equal(
            (int)BookingStatus.New,
            (int)BookingStatus.InProgress,
            (int)BookingStatus.ProposalSent,
            (int)BookingStatus.Confirmed);
    }
}
