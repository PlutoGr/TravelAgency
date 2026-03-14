using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.DTOs.Requests;
using TravelAgency.Booking.Domain.Enums;
using TravelAgency.Booking.IntegrationTests.Helpers;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Booking.IntegrationTests.Bookings;

public class BookingsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();

    public BookingsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.EnsureDbCreated();
    }

    private void AuthorizeAsClient() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateToken(_clientId, AppRoles.Client));

    private void AuthorizeAsManager() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateToken(_managerId, AppRoles.Manager));

    private void ClearAuthorization() =>
        _client.DefaultRequestHeaders.Authorization = null;

    private async Task<BookingDto> CreateBookingAsClientAsync(Guid? tourId = null, string? comment = null)
    {
        AuthorizeAsClient();
        var request = new CreateBookingRequest(tourId ?? Guid.NewGuid(), comment);
        var response = await _client.PostAsJsonAsync("/bookings", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookingDto>())!;
    }

    [Fact]
    public async Task CreateBooking_AsClient_ShouldReturn201WithNewStatus()
    {
        AuthorizeAsClient();
        var request = new CreateBookingRequest(Guid.NewGuid(), "I want this tour");

        var response = await _client.PostAsJsonAsync("/bookings", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<BookingDto>();
        result.Should().NotBeNull();
        result!.ClientId.Should().Be(_clientId);
        result.Status.Should().Be(BookingStatus.New);
        result.Comment.Should().Be("I want this tour");
    }

    [Fact]
    public async Task CreateBooking_Unauthenticated_ShouldReturn401()
    {
        ClearAuthorization();
        var request = new CreateBookingRequest(Guid.NewGuid(), null);

        var response = await _client.PostAsJsonAsync("/bookings", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateBooking_AsManager_ShouldReturn201()
    {
        // Managers satisfy the RequireClient policy (Client | Manager | Admin)
        AuthorizeAsManager();
        var request = new CreateBookingRequest(Guid.NewGuid(), null);

        var response = await _client.PostAsJsonAsync("/bookings", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetMyBookings_AsClient_ShouldReturn200WithList()
    {
        AuthorizeAsClient();
        await CreateBookingAsClientAsync();

        var response = await _client.GetAsync("/bookings/my");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<BookingDto>>();
        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();
        result.Should().AllSatisfy(b => b.ClientId.Should().Be(_clientId));
    }

    [Fact]
    public async Task GetMyBookings_Unauthenticated_ShouldReturn401()
    {
        ClearAuthorization();

        var response = await _client.GetAsync("/bookings/my");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBookingById_AsOwnerClient_ShouldReturn200()
    {
        var created = await CreateBookingAsClientAsync();

        var response = await _client.GetAsync($"/bookings/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BookingDto>();
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetBookingById_AsManager_ShouldReturn200ForAnyBooking()
    {
        var created = await CreateBookingAsClientAsync();

        AuthorizeAsManager();
        var response = await _client.GetAsync($"/bookings/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBookingById_AsDifferentClient_ShouldReturn403()
    {
        var created = await CreateBookingAsClientAsync();

        // Switch to a completely different client identity
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateToken(Guid.NewGuid(), AppRoles.Client));

        var response = await _client.GetAsync($"/bookings/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetBookingById_NonExistent_ShouldReturn404WithProblemDetails()
    {
        AuthorizeAsClient();

        var response = await _client.GetAsync($"/bookings/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Not Found");
    }

    [Fact]
    public async Task ChangeBookingStatus_AsManager_NewToInProgress_ShouldReturn200()
    {
        var created = await CreateBookingAsClientAsync();

        AuthorizeAsManager();
        var statusRequest = new ChangeBookingStatusRequest(BookingStatus.InProgress);
        var response = await _client.PatchAsJsonAsync($"/bookings/{created.Id}/status", statusRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BookingDto>();
        result!.Status.Should().Be(BookingStatus.InProgress);
    }

    [Fact]
    public async Task ChangeBookingStatus_AsClient_CancelOwnBooking_ShouldReturn200()
    {
        var created = await CreateBookingAsClientAsync();

        AuthorizeAsClient();
        var statusRequest = new ChangeBookingStatusRequest(BookingStatus.Cancelled);
        var response = await _client.PatchAsJsonAsync($"/bookings/{created.Id}/status", statusRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BookingDto>();
        result!.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task ChangeBookingStatus_AsClient_NonCancelTransition_ShouldReturn403()
    {
        // Clients can only cancel their bookings; any other transition is forbidden
        var created = await CreateBookingAsClientAsync();

        AuthorizeAsClient();
        var statusRequest = new ChangeBookingStatusRequest(BookingStatus.InProgress);
        var response = await _client.PatchAsJsonAsync($"/bookings/{created.Id}/status", statusRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeBookingStatus_InvalidDomainTransition_ShouldReturn422()
    {
        // New → Confirmed is not a valid domain transition
        var created = await CreateBookingAsClientAsync();

        AuthorizeAsManager();
        var statusRequest = new ChangeBookingStatusRequest(BookingStatus.Confirmed);
        var response = await _client.PatchAsJsonAsync($"/bookings/{created.Id}/status", statusRequest);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Unprocessable Entity");
    }

    [Fact]
    public async Task ChangeBookingStatus_NonExistentBooking_ShouldReturn404()
    {
        AuthorizeAsManager();
        var statusRequest = new ChangeBookingStatusRequest(BookingStatus.InProgress);

        var response = await _client.PatchAsJsonAsync($"/bookings/{Guid.NewGuid()}/status", statusRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProposal_AsManager_OnInProgressBooking_ShouldReturn201()
    {
        var created = await CreateBookingAsClientAsync();

        AuthorizeAsManager();
        var statusRequest = new ChangeBookingStatusRequest(BookingStatus.InProgress);
        await _client.PatchAsJsonAsync($"/bookings/{created.Id}/status", statusRequest);

        var proposalRequest = new CreateProposalRequest("Special deal just for you!");
        var response = await _client.PostAsJsonAsync($"/bookings/{created.Id}/proposals", proposalRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ProposalDto>();
        result.Should().NotBeNull();
        result!.BookingId.Should().Be(created.Id);
        result.ManagerId.Should().Be(_managerId);
        result.Notes.Should().Be("Special deal just for you!");
        result.IsConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task CreateProposal_AsClient_ShouldReturn403()
    {
        var created = await CreateBookingAsClientAsync();

        // Still authorized as the client who created the booking
        var proposalRequest = new CreateProposalRequest("hack");
        var response = await _client.PostAsJsonAsync($"/bookings/{created.Id}/proposals", proposalRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProposal_NonExistentBooking_ShouldReturn404()
    {
        AuthorizeAsManager();
        var proposalRequest = new CreateProposalRequest("notes");

        var response = await _client.PostAsJsonAsync($"/bookings/{Guid.NewGuid()}/proposals", proposalRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProposal_OnCancelledBooking_ShouldReturn422()
    {
        var created = await CreateBookingAsClientAsync();

        // Cancel the booking as client
        AuthorizeAsClient();
        await _client.PatchAsJsonAsync($"/bookings/{created.Id}/status",
            new ChangeBookingStatusRequest(BookingStatus.Cancelled));

        // Attempt to add proposal to a cancelled booking
        AuthorizeAsManager();
        var proposalRequest = new CreateProposalRequest("too late");
        var response = await _client.PostAsJsonAsync($"/bookings/{created.Id}/proposals", proposalRequest);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
