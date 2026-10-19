using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.DTOs.Requests;
using TravelAgency.Booking.Domain.Exceptions;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Booking.Infrastructure.GrpcServices;
using TravelAgency.Booking.Infrastructure.Persistence;
using TravelAgency.Booking.IntegrationTests.Helpers;
using TravelAgency.Contracts.Grpc.Booking;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Booking.IntegrationTests.Bookings;

[Collection(BookingPostgresCollection.Name)]
public class BookingAssignmentPostgresTests
{
    private readonly ProposalUtcWebApplicationFactory _factory;

    public BookingAssignmentPostgresTests(ProposalUtcWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ParallelTake_OneSucceeds_AndListFiltersQueueAndMe()
    {
        var clientId = Guid.NewGuid();
        var managerA = Guid.NewGuid();
        var managerB = Guid.NewGuid();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(clientId, AppRoles.Client);
        var booking = await CreateBookingAsync(client);

        var forbidden = await client.PostAsJsonAsync(
            $"/bookings/{booking.Id}/take",
            new TakeBookingRequest(null));
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var httpA = _factory.CreateClient();
        using var httpB = _factory.CreateClient();
        httpA.DefaultRequestHeaders.Authorization = Bearer(managerA, AppRoles.Manager);
        httpB.DefaultRequestHeaders.Authorization = Bearer(managerB, AppRoles.Manager);

        var queued = await httpA.GetFromJsonAsync<List<BookingDto>>("/bookings?assigned=queue");
        var mineBefore = await httpA.GetFromJsonAsync<List<BookingDto>>("/bookings?assigned=me");
        queued.Should().Contain(b => b.Id == booking.Id);
        mineBefore.Should().NotContain(b => b.Id == booking.Id);

        var responses = await Task.WhenAll(
            httpA.PostAsJsonAsync($"/bookings/{booking.Id}/take", new TakeBookingRequest(null)),
            httpB.PostAsJsonAsync($"/bookings/{booking.Id}/take", new TakeBookingRequest(null)));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
        var conflict = responses.Single(r => r.StatusCode == HttpStatusCode.Conflict);
        using var conflictJson = JsonDocument.Parse(await conflict.Content.ReadAsStringAsync());
        conflictJson.RootElement.GetProperty("detail").GetString()
            .Should().Be(BookingAssignmentConflictException.DefaultMessage);

        var winner = responses[0].StatusCode == HttpStatusCode.OK ? httpA : httpB;
        var queuedAfter = await winner.GetFromJsonAsync<List<BookingDto>>("/bookings?assigned=queue");
        var mineAfter = await winner.GetFromJsonAsync<List<BookingDto>>("/bookings?assigned=me");
        queuedAfter.Should().NotContain(b => b.Id == booking.Id);
        mineAfter.Should().Contain(b => b.Id == booking.Id && b.AssignedManagerId != null);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var historyCount = await db.BookingAssignmentHistories.CountAsync(h => h.BookingId == booking.Id);
        historyCount.Should().Be(1);
    }

    [Fact]
    public async Task ValidateBookingAccess_CanWrite_DependsOnResponsibleManager()
    {
        var clientId = Guid.NewGuid();
        var responsibleId = Guid.NewGuid();
        var previousId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        StubUser(previousId, AppRoles.Manager);
        StubUser(responsibleId, AppRoles.Manager);
        StubUser(otherId, AppRoles.Manager);
        StubUser(adminId, AppRoles.Admin);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(clientId, AppRoles.Client);
        var booking = await CreateBookingAsync(client);

        await using (var beforeScope = _factory.Services.CreateAsyncScope())
        {
            var beforeGrpc = CreateGrpc(beforeScope.ServiceProvider);
            var before = await beforeGrpc.ValidateBookingAccess(
                Access(booking.Id, otherId), new TestServerCallContext());
            before.HasAccess.Should().BeTrue();
            before.CanWrite.Should().BeFalse();
        }

        client.DefaultRequestHeaders.Authorization = Bearer(previousId, AppRoles.Manager);
        var taken = await client.PostAsJsonAsync(
            $"/bookings/{booking.Id}/take",
            new TakeBookingRequest(null));
        taken.StatusCode.Should().Be(HttpStatusCode.OK);

        client.DefaultRequestHeaders.Authorization = Bearer(adminId, AppRoles.Admin);
        var reassigned = await client.PutAsJsonAsync(
            $"/bookings/{booking.Id}/assigned-manager",
            new AssignManagerRequest(responsibleId, previousId, null));
        var reassignedBody = await reassigned.Content.ReadAsStringAsync();
        reassigned.StatusCode.Should().Be(HttpStatusCode.OK, reassignedBody);

        await using var scope = _factory.Services.CreateAsyncScope();
        var grpc = CreateGrpc(scope.ServiceProvider);
        var context = new TestServerCallContext();

        await AssertAccess(grpc, context, booking.Id, responsibleId, hasAccess: true, canWrite: true);
        await AssertAccess(grpc, context, booking.Id, previousId, hasAccess: true, canWrite: false);
        await AssertAccess(grpc, context, booking.Id, otherId, hasAccess: true, canWrite: false);
        await AssertAccess(grpc, context, booking.Id, adminId, hasAccess: true, canWrite: true);
        await AssertAccess(grpc, context, booking.Id, clientId, hasAccess: true, canWrite: true);

        var assignment = await grpc.GetBookingAssignment(
            new GetBookingAssignmentRequest { BookingId = booking.Id.ToString() },
            context);
        assignment.ClientId.Should().Be(clientId.ToString());
        assignment.AssignedManagerId.Should().Be(responsibleId.ToString());
        assignment.PreviousManagerIds.Should().ContainSingle().Which.Should().Be(previousId.ToString());
    }

    [Fact]
    public async Task AddBookingAssignedManager_KeepsExistingBookingsInQueue()
    {
        var database = "travel_booking_legacy_" + Guid.NewGuid().ToString("N");
        var adminCs = _factory.PostgresConnectionString;
        var legacyCs = new NpgsqlConnectionStringBuilder(adminCs) { Database = database }.ConnectionString;

        await using (var admin = new NpgsqlConnection(adminCs))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var options = new DbContextOptionsBuilder<BookingDbContext>().UseNpgsql(legacyCs).Options;
            var firstId = Guid.NewGuid();
            var secondId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;

            await using (var db = new BookingDbContext(options))
            {
                await db.Database.MigrateAsync("20260315111537_InitialCreate");
                await db.Database.ExecuteSqlRawAsync(
                    """
                    INSERT INTO "Bookings" ("Id", "ClientId", "TourId", "Comment", "Status", "CreatedAt", "UpdatedAt")
                    VALUES ({0}, {1}, {2}, NULL, 0, {3}, NULL),
                           ({4}, {5}, {6}, NULL, 4, {3}, NULL)
                    """,
                    firstId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    createdAt,
                    secondId,
                    Guid.NewGuid(),
                    Guid.NewGuid());
            }

            int countBefore;
            await using (var connection = new NpgsqlConnection(legacyCs))
            {
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand("SELECT COUNT(*) FROM \"Bookings\"", connection);
                countBefore = Convert.ToInt32(await command.ExecuteScalarAsync());
            }

            await using var migrated = new BookingDbContext(options);
            await migrated.Database.MigrateAsync();

            var bookings = await migrated.Bookings.OrderBy(b => b.CreatedAt).ToListAsync();
            bookings.Should().HaveCount(countBefore);
            bookings.Should().HaveCount(2);
            bookings.Should().OnlyContain(b => b.AssignedManagerId == null && b.AssignedAt == null);
            (await migrated.BookingAssignmentHistories.CountAsync()).Should().Be(0);
        }
        finally
        {
            await using var admin = new NpgsqlConnection(adminCs);
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private void StubUser(Guid userId, string role)
    {
        _factory.IdentityGrpcClientMock
            .Setup(c => c.GetUserSummaryAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSummary(userId.ToString(), "user@example.com", "Test", role, role));
        _factory.IdentityGrpcClientMock
            .Setup(c => c.GetUserRoleAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRoleLookup(true, role));
    }

    private static BookingGrpcService CreateGrpc(IServiceProvider services) =>
        new(
            services.GetRequiredService<IBookingRepository>(),
            services.GetRequiredService<IIdentityGrpcClient>());

    private static async Task<BookingDto> CreateBookingAsync(HttpClient client)
    {
        var created = await client.PostAsJsonAsync("/bookings", new CreateBookingRequest(Guid.NewGuid(), null));
        var body = await created.Content.ReadAsStringAsync();
        created.StatusCode.Should().Be(HttpStatusCode.Created, body);
        var booking = JsonSerializer.Deserialize<BookingDto>(body, JsonOptions);
        booking.Should().NotBeNull();
        return booking!;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static AuthenticationHeaderValue Bearer(Guid userId, string role) =>
        new("Bearer", JwtTokenHelper.GenerateToken(userId, role));

    private static ValidateBookingAccessRequest Access(Guid bookingId, Guid userId) =>
        new()
        {
            BookingId = bookingId.ToString(),
            UserId = userId.ToString()
        };

    private static async Task AssertAccess(
        BookingGrpcService grpc,
        ServerCallContext context,
        Guid bookingId,
        Guid userId,
        bool hasAccess,
        bool canWrite)
    {
        var response = await grpc.ValidateBookingAccess(Access(bookingId, userId), context);
        response.HasAccess.Should().Be(hasAccess);
        response.CanWrite.Should().Be(canWrite);
    }

    private sealed class TestServerCallContext : ServerCallContext
    {
        protected override string MethodCore => "test";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "peer";
        protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
        protected override Metadata RequestHeadersCore { get; } = [];
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore { get; } = [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => new("peer", []);

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
            throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }
}
