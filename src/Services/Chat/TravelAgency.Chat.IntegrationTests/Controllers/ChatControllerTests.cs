using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TravelAgency.Chat.Application.DTOs;
using TravelAgency.Chat.IntegrationTests.Helpers;
using Xunit;

namespace TravelAgency.Chat.IntegrationTests.Controllers;

[Collection("Chat API")]
public class ChatControllerTests
{
    private readonly ChatApiFixture _fixture;
    private readonly HttpClient _client;

    public ChatControllerTests(ChatApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    private void AuthorizeWithValidToken() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateToken());

    private void ClearAuthorization() =>
        _client.DefaultRequestHeaders.Authorization = null;

    [Fact]
    public async Task GetMessages_WithValidJWT_Returns200AndMessages()
    {
        AuthorizeWithValidToken();
        var bookingId = Guid.NewGuid();

        var response = await _client.GetAsync($"/chat/booking/{bookingId}/messages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<ChatMessageDto>>();
        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessages_WithoutAuth_Returns401()
    {
        ClearAuthorization();
        var bookingId = Guid.NewGuid();

        var response = await _client.GetAsync($"/chat/booking/{bookingId}/messages");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMessages_WithInvalidBooking_Returns403()
    {
        var forbiddenBookingId = Guid.NewGuid();
        _fixture.Factory.BookingAccessServiceMock
            .Setup(x => x.CanAccessBookingAsync(
                It.Is<Guid>(id => id == forbiddenBookingId),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        AuthorizeWithValidToken();

        var response = await _client.GetAsync($"/chat/booking/{forbiddenBookingId}/messages");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
