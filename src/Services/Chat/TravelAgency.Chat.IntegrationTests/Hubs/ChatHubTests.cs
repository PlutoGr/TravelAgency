using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using TravelAgency.Chat.Application.DTOs;
using TravelAgency.Chat.IntegrationTests.Helpers;
using TravelAgency.Shared.Contracts.Authorization;
using Xunit;

namespace TravelAgency.Chat.IntegrationTests.Hubs;

/// <summary>
/// Integration tests for Chat SignalR Hub at /hubs/chat.
/// Uses Microsoft.AspNetCore.SignalR.Client to connect; IBookingAccessService is mocked to return true.
/// </summary>
[Collection("Chat API")]
public class ChatHubTests
{
    private readonly ChatApiFixture _fixture;

    public ChatHubTests(ChatApiFixture fixture)
    {
        _fixture = fixture;
    }

    private static HubConnection CreateHubConnection(ChatApiApplicationFactory factory, string accessToken)
    {
        var baseAddress = factory.Server.BaseAddress ?? new Uri("http://localhost");
        var hubUrl = new Uri(baseAddress, "hubs/chat");

        return new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            .Build();
    }

    [Fact]
    public async Task Connect_WithValidJWT_Succeeds()
    {
        var token = JwtTokenHelper.GenerateToken(role: AppRoles.Client);
        await using var connection = CreateHubConnection(_fixture.Factory, token);

        var act = async () => await connection.StartAsync();

        await act.Should().NotThrowAsync();
        connection.State.Should().Be(HubConnectionState.Connected);
    }

    [Fact]
    public async Task JoinBookingGroup_WithAccess_JoinsGroup()
    {
        var token = JwtTokenHelper.GenerateToken(role: AppRoles.Client);
        await using var connection = CreateHubConnection(_fixture.Factory, token);
        await connection.StartAsync();

        var bookingId = Guid.NewGuid();

        var act = async () => await connection.InvokeAsync("JoinBookingGroup", bookingId);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendMessage_WithAccess_BroadcastsToGroup()
    {
        var token = JwtTokenHelper.GenerateToken(role: AppRoles.Client);
        await using var connection = CreateHubConnection(_fixture.Factory, token);
        await connection.StartAsync();

        var bookingId = Guid.NewGuid();
        await connection.InvokeAsync("JoinBookingGroup", bookingId);

        var tcs = new TaskCompletionSource<ChatMessageDto>();
        connection.On<ChatMessageDto>("MessageReceived", msg => tcs.TrySetResult(msg));

        var text = "Hello from integration test";
        await connection.InvokeAsync("SendMessage", bookingId, text, (IReadOnlyList<string>?)null);

        var receivedMessage = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        receivedMessage.Text.Should().Be(text);
        receivedMessage.BookingId.Should().Be(bookingId);
    }
}
