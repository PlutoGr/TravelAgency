using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Core;
using Serilog.Events;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.Features.Clients;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Enums;
using TravelAgency.Booking.Infrastructure.Persistence;
using TravelAgency.Booking.IntegrationTests.Helpers;
using TravelAgency.Shared.Contracts.Authorization;
using BookingEntity = TravelAgency.Booking.Domain.Entities.Booking;

namespace TravelAgency.Booking.IntegrationTests.Clients;

public class ClientCardsHttpTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Phone = "+79991234567";
    private const string Note = "просит вылет после 15 мая";

    private readonly CustomWebApplicationFactory _factory;

    public ClientCardsHttpTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDbCreated();
    }

    [Fact]
    public async Task GetCardAndList_AreNoStore_AndReadAgainInsteadOfReplaying()
    {
        var clientId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var newer = Seed(clientId, managerId, new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc), BookingStatus.Closed);
        var older = Seed(clientId, null, new DateTime(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc), BookingStatus.New);
        StubClient(clientId, phone: Phone);
        StubAuthor(managerId, "Марина Иванова");

        using var http = As(managerId, AppRoles.Manager);
        var first = await http.GetAsync($"/bookings/clients/{clientId}");
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        first.Headers.CacheControl!.NoStore.Should().BeTrue();
        using (var json = JsonDocument.Parse(await first.Content.ReadAsStringAsync()))
        {
            json.RootElement.GetProperty("phone").GetString().Should().Be(Phone);
            json.RootElement.GetProperty("bookings")[0].GetProperty("id").GetGuid().Should().Be(newer);
            json.RootElement.GetProperty("bookings")[1].GetProperty("id").GetGuid().Should().Be(older);
            json.RootElement.GetProperty("notes").GetArrayLength().Should().Be(0);
        }

        var created = await http.PostAsJsonAsync($"/bookings/clients/{clientId}/notes", new { text = Note });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Headers.CacheControl!.NoStore.Should().BeTrue();

        StubClient(clientId, phone: "+79990000000");
        var second = await http.GetAsync($"/bookings/clients/{clientId}");
        using var again = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        again.RootElement.GetProperty("phone").GetString().Should().Be("+79990000000");
        again.RootElement.GetProperty("notes")[0].GetProperty("text").GetString().Should().Be(Note);
        _factory.IdentityGrpcClientMock.Verify(
            client => client.ReadUserPhoneAsync(clientId, It.IsAny<CancellationToken>()),
            Times.AtLeast(2));

        var list = await http.GetAsync("/bookings/clients");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        list.Headers.CacheControl!.NoStore.Should().BeTrue();
        using var listJson = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        listJson.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        listJson.RootElement[0].TryGetProperty("notes", out _).Should().BeFalse();
        listJson.RootElement[0].TryGetProperty("bookings", out _).Should().BeFalse();

        var booking = await http.GetAsync($"/bookings/{newer}");
        booking.StatusCode.Should().Be(HttpStatusCode.OK);
        using var bookingJson = JsonDocument.Parse(await booking.Content.ReadAsStringAsync());
        bookingJson.RootElement.GetProperty("clientPhone").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GuestIs401_ClientIs403_AdminPassesWhereManagerPasses()
    {
        var clientId = Guid.NewGuid();
        Seed(clientId, null, DateTime.UtcNow, BookingStatus.New);
        StubClient(clientId, phone: Phone);

        using var guest = _factory.CreateClient();
        var anonymous = await guest.GetAsync("/bookings/clients");
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.Content.ReadAsStringAsync()).Should().NotContain("displayName").And.NotContain("authorName");

        using var client = As(Guid.NewGuid(), AppRoles.Client);
        foreach (var path in new[] { "/bookings/clients", $"/bookings/clients/{clientId}" })
        {
            var forbidden = await client.GetAsync(path);
            forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await forbidden.Content.ReadAsStringAsync()).Should().NotContain("displayName").And.NotContain(Note);
        }

        var post = await client.PostAsJsonAsync($"/bookings/clients/{clientId}/notes", new { text = Note });
        post.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        CountNotes(clientId).Should().Be(0);

        using var admin = As(Guid.NewGuid(), AppRoles.Admin);
        var ok = await admin.GetAsync($"/bookings/clients/{clientId}");
        ok.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await ok.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("phone").GetString().Should().Be(Phone);
    }

    [Fact]
    public async Task MissingCardAndBadNote_Use404_503_400_And405DoesNotChangeTheNote()
    {
        var clientId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        Seed(clientId, null, DateTime.UtcNow, BookingStatus.New);
        StubClient(clientId, phone: Phone);
        StubAuthor(managerId, "Марина Иванова");
        using var http = As(managerId, AppRoles.Manager);

        await AssertNotFound(http, "/bookings/clients/not-a-guid");
        await AssertNotFound(http, $"/bookings/clients/{Guid.NewGuid()}");

        var staffId = Guid.NewGuid();
        Seed(staffId, null, DateTime.UtcNow, BookingStatus.New);
        StubClient(staffId, role: AppRoles.Manager);
        await AssertNotFound(http, $"/bookings/clients/{staffId}");
        var staffPost = await http.PostAsJsonAsync($"/bookings/clients/{staffId}/notes", new { text = Note });
        staffPost.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Detail(staffPost)).Should().Be(ClientCardMessages.NotFound);
        CountNotes(staffId).Should().Be(0);

        var emptyClient = Guid.NewGuid();
        StubClient(emptyClient, phone: Phone);
        await AssertNotFound(http, $"/bookings/clients/{emptyClient}");
        _factory.IdentityGrpcClientMock.Verify(
            client => client.ReadCardProfileAsync(emptyClient, It.IsAny<CancellationToken>()),
            Times.Never);

        _factory.IdentityGrpcClientMock
            .Setup(client => client.ReadCardProfileAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityCardProfileRead(IdentityReadKind.Unavailable, null));
        var unavailable = await http.GetAsync($"/bookings/clients/{clientId}");
        unavailable.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var unavailableBody = await unavailable.Content.ReadAsStringAsync();
        (await Detail(unavailable)).Should().Be("Upstream service unavailable");
        unavailableBody.Should().NotContain(Phone).And.NotContain("Анна");
        StubClient(clientId, phone: Phone);

        foreach (var body in new object?[] { new { }, new { text = (string?)null }, new { text = "" }, new { text = "   " } })
        {
            var response = await http.PostAsJsonAsync($"/bookings/clients/{clientId}/notes", body);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            json.RootElement.GetProperty("title").GetString().Should().Be("Validation Error");
            json.RootElement.GetProperty("errors").GetProperty("text")[0].GetString().Should().Be(ClientCardMessages.EmptyNote);
        }

        var longText = " " + new string('я', 501) + " ";
        var tooLong = await http.PostAsJsonAsync($"/bookings/clients/{clientId}/notes", new { text = longText });
        tooLong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using (var json = JsonDocument.Parse(await tooLong.Content.ReadAsStringAsync()))
        {
            json.RootElement.GetProperty("errors").GetProperty("text")[0].GetString().Should().Be(ClientCardMessages.NoteTooLong);
        }
        CountNotes(clientId).Should().Be(0);

        var outboxBefore = CountOutbox();
        var created = await http.PostAsJsonAsync(
            $"/bookings/clients/{clientId}/notes",
            new { text = "  просит  вылет после 15 мая  " });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        createdJson.RootElement.GetProperty("text").GetString().Should().Be("просит  вылет после 15 мая");
        createdJson.RootElement.GetProperty("authorName").GetString().Should().Be("Марина Иванова");
        createdJson.RootElement.TryGetProperty("email", out _).Should().BeFalse();
        CountNotes(clientId).Should().Be(1);
        CountOutbox().Should().Be(outboxBefore);

        var noteUrl = $"/bookings/clients/{clientId}/notes";
        (await http.PutAsJsonAsync(noteUrl, new { text = "правка" })).StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        (await http.PatchAsJsonAsync(noteUrl, new { text = "правка" })).StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        (await http.DeleteAsync(noteUrl)).StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        (await http.PostAsync("/bookings/clients", JsonContent.Create(new { text = Note }))).StatusCode
            .Should().Be(HttpStatusCode.MethodNotAllowed);
        (await http.PutAsync($"/bookings/clients/{clientId}", JsonContent.Create(new { }))).StatusCode
            .Should().Be(HttpStatusCode.MethodNotAllowed);
        CountNotes(clientId).Should().Be(1);
        _factory.UseDbContext(db =>
        {
            db.ClientNotes.Single(note => note.ClientId == clientId).Text.Should().Be("просит  вылет после 15 мая");
        });
    }

    [Fact]
    public async Task RequestLog_WritesThePath_AndNotThePhoneOrNote()
    {
        await using var factory = new ClientCardLogFactory();
        factory.EnsureDbCreated();
        var clientId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var booking = BookingEntity.Create(clientId, Guid.NewGuid(), null, managerId);
        factory.UseDbContext(db =>
        {
            db.Bookings.Add(booking);
            db.SaveChanges();
        });
        factory.IdentityGrpcClientMock
            .Setup(client => client.ReadCardProfileAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile(AppRoles.Client, "Анна Соколова", "/api/v1/media/files/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/a256"));
        factory.IdentityGrpcClientMock
            .Setup(client => client.ReadCardProfileAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile(AppRoles.Manager, "Марина Иванова", null));
        factory.IdentityGrpcClientMock
            .Setup(client => client.ReadUserPhoneAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPhoneRead(IdentityReadKind.Found, Phone));

        using var http = factory.CreateClient();
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateToken(managerId, AppRoles.Manager));
        (await http.GetAsync($"/bookings/clients/{clientId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await http.PostAsJsonAsync($"/bookings/clients/{clientId}/notes", new { text = Note })).StatusCode
            .Should().Be(HttpStatusCode.Created);

        var log = string.Join('\n', factory.Sink.Lines);
        log.Should().Contain($"/bookings/clients/{clientId}");
        log.Should().NotContain(Phone);
        log.Should().NotContain(Note);
    }

    private async Task AssertNotFound(HttpClient http, string path)
    {
        var response = await http.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Detail(response)).Should().Be(ClientCardMessages.NotFound);
    }

    private static async Task<string> Detail(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("detail").GetString() ?? string.Empty;
    }

    private HttpClient As(Guid userId, string role)
    {
        var http = _factory.CreateClient();
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateToken(userId, role));
        return http;
    }

    private Guid Seed(Guid clientId, Guid? managerId, DateTime createdAt, BookingStatus status)
    {
        var booking = BookingEntity.Create(clientId, Guid.NewGuid(), null, managerId);
        booking.GetType().GetProperty(nameof(BookingEntity.CreatedAt))!.SetValue(booking, createdAt);
        booking.GetType().GetProperty(nameof(BookingEntity.Status))!.SetValue(booking, status);
        _factory.UseDbContext(db =>
        {
            db.Bookings.Add(booking);
            db.SaveChanges();
        });
        return booking.Id;
    }

    private void StubClient(Guid clientId, string role = AppRoles.Client, string phone = Phone)
    {
        _factory.IdentityGrpcClientMock
            .Setup(client => client.ReadCardProfileAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile(role, "Анна Соколова", null));
        _factory.IdentityGrpcClientMock
            .Setup(client => client.ReadUserPhoneAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPhoneRead(IdentityReadKind.Found, phone));
    }

    private void StubAuthor(Guid authorId, string name)
    {
        _factory.IdentityGrpcClientMock
            .Setup(client => client.ReadCardProfileAsync(authorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile(AppRoles.Manager, name, null));
    }

    private static IdentityCardProfileRead Profile(string role, string name, string? avatar) =>
        new(IdentityReadKind.Found, new IdentityCardProfile("anna@example.com", name, role, avatar));

    private int CountNotes(Guid clientId)
    {
        var count = 0;
        _factory.UseDbContext(db => count = db.ClientNotes.Count(note => note.ClientId == clientId));
        return count;
    }

    private int CountOutbox()
    {
        var count = 0;
        _factory.UseDbContext(db => count = db.OutboxMessages.Count());
        return count;
    }

    private sealed class ClientCardLogFactory : CustomWebApplicationFactory
    {
        public ListSink Sink { get; } = new();

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.AddOptions<RequestLoggingOptions>()
                    .Configure<Serilog.ILogger>((options, logger) => options.Logger = logger);
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseSerilog(
                (_, configuration) => configuration.MinimumLevel.Information().WriteTo.Sink(Sink),
                preserveStaticLogger: true,
                writeToProviders: false);
            return base.CreateHost(builder);
        }
    }

    private sealed class ListSink : ILogEventSink
    {
        public List<string> Lines { get; } = new();

        public void Emit(LogEvent logEvent) => Lines.Add(logEvent.RenderMessage());
    }
}
