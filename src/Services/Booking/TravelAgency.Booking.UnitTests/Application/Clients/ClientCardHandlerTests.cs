using System.Text.Json;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Features.Bookings.Queries.GetBookingById;
using TravelAgency.Booking.Application.Features.Clients;
using BookingEntity = TravelAgency.Booking.Domain.Entities.Booking;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Enums;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Booking.Infrastructure.GrpcServices;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Booking.UnitTests.Application.Clients;

public class ClientCardHandlerTests
{
    private static readonly Guid ClientId = Guid.Parse("6b1c2a0e-7b4d-4e1a-9c2f-6a8b0d1e2f30");
    private static readonly Guid ManagerId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid AdminId = Guid.Parse("bbbbbbbb-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid OtherManagerId = Guid.Parse("cccccccc-bbbb-cccc-dddd-eeeeeeeeeeee");
    private const string Phone = "+79991234567";
    private const string Avatar = "/api/v1/media/files/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/a256";

    private readonly Mock<ICurrentUserService> _current = new();
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IClientNoteRepository> _notes = new();
    private readonly Mock<IIdentityGrpcClient> _identity = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public ClientCardHandlerTests()
    {
        _notes.Setup(repo => repo.ListByClientAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ClientNote>());
        _unitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task PhoneRule_AdminAlways_ManagerOnlyWhileAssigned_EmptyPhoneOmitted_HistoryDoesNotCount()
    {
        var openId = Guid.Parse("00000000-0000-0000-0000-0000000000b2");
        var closedId = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
        var sameMoment = new DateTime(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);
        var open = Sample(openId, sameMoment, ManagerId, BookingStatus.New);
        var closed = Sample(closedId, sameMoment, ManagerId, BookingStatus.Closed);
        var cancelled = Sample(Guid.NewGuid(), sameMoment.AddMinutes(-1), ManagerId, BookingStatus.Cancelled);
        StubBookings(open, closed, cancelled);
        StubClient();
        StubPhone(Phone);

        As(ManagerId, AppRoles.Manager);
        var assigned = await HandleCard();
        assigned.Phone.Should().Be(Phone);
        _identity.Verify(client => client.ReadUserPhoneAsync(ClientId, It.IsAny<CancellationToken>()), Times.Once);

        var removed = Sample(Guid.NewGuid(), sameMoment, null, BookingStatus.New);
        removed.AssignManager(ManagerId, AdminId, null, "снят");
        Set(removed, nameof(BookingEntity.AssignedManagerId), null);
        StubBookings(removed);
        _identity.Invocations.Clear();
        StubClient();
        As(ManagerId, AppRoles.Manager);
        var afterRemoval = await HandleCard();
        afterRemoval.Phone.Should().BeNull();
        _identity.Verify(client => client.ReadUserPhoneAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        var historyOnly = Sample(Guid.NewGuid(), sameMoment, null, BookingStatus.New);
        historyOnly.AssignManager(OtherManagerId, AdminId, null, null);
        Set(historyOnly, nameof(BookingEntity.AssignedManagerId), null);
        StubBookings(historyOnly);
        (await HandleCard()).Phone.Should().BeNull();

        StubBookings(Sample(Guid.NewGuid(), sameMoment, null, BookingStatus.New));
        (await HandleCard()).Phone.Should().BeNull();

        As(AdminId, AppRoles.Admin);
        StubPhone(Phone);
        (await HandleCard()).Phone.Should().Be(Phone);

        StubPhone(string.Empty);
        var empty = await HandleCard();
        empty.Phone.Should().BeNull();
        JsonSerializer.Serialize(empty, WebJson()).Should().NotContain("phone");
        JsonSerializer.Serialize(empty, WebJson()).Should().NotContain("\"\"");

        As(ManagerId, AppRoles.Manager);
        StubBookings(closed);
        StubPhone(Phone);
        (await HandleCard()).Phone.Should().Be(Phone);
        StubBookings(cancelled);
        (await HandleCard()).Phone.Should().Be(Phone);
    }

    [Fact]
    public async Task CardAssembly_NotFoundCases_DoNotCallPhone_AndFailuresDoNotInventAProfile()
    {
        As(AdminId, AppRoles.Admin);
        _bookings.Setup(repo => repo.GetByClientIdAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<BookingEntity>());

        var missingBookings = async () => await HandleCard();
        (await missingBookings.Should().ThrowAsync<NotFoundException>()).Which.Message.Should().Be(ClientCardMessages.NotFound);
        _identity.Verify(client => client.ReadCardProfileAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _identity.Verify(client => client.ReadUserPhoneAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        StubBookings(Sample(Guid.NewGuid(), DateTime.UtcNow, null, BookingStatus.New));
        _identity.Setup(client => client.ReadCardProfileAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityCardProfileRead(IdentityReadKind.NotFound, null));
        (await missingBookings.Should().ThrowAsync<NotFoundException>()).Which.Message.Should().Be(ClientCardMessages.NotFound);
        _identity.Verify(client => client.ReadUserPhoneAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        StubClient(role: AppRoles.Manager);
        (await missingBookings.Should().ThrowAsync<NotFoundException>()).Which.Message.Should().Be(ClientCardMessages.NotFound);
        _identity.Verify(client => client.ReadUserPhoneAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        StubClient();
        _identity.Setup(client => client.ReadUserPhoneAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPhoneRead(IdentityReadKind.NotFound, string.Empty));
        (await missingBookings.Should().ThrowAsync<NotFoundException>()).Which.Message.Should().Be(ClientCardMessages.NotFound);

        _identity.Setup(client => client.ReadCardProfileAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityCardProfileRead(IdentityReadKind.Unavailable, null));
        await missingBookings.Should().ThrowAsync<UpstreamUnavailableException>();

        StubClient();
        _identity.Setup(client => client.ReadUserPhoneAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPhoneRead(IdentityReadKind.Unavailable, Phone));
        await missingBookings.Should().ThrowAsync<UpstreamUnavailableException>();

        StubClient();
        StubPhone(Phone);
        var authorId = Guid.NewGuid();
        var note = ClientNote.Create(ClientId, authorId, "текст", new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc));
        _notes.Setup(repo => repo.ListByClientAsync(ClientId, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { note });
        _identity.Setup(client => client.ReadCardProfileAsync(authorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityCardProfileRead(IdentityReadKind.Unavailable, null));
        await missingBookings.Should().ThrowAsync<UpstreamUnavailableException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddNote_RejectsEmptyText_WithoutWriting(string? text)
    {
        var act = async () => await Handlers().Handle(new AddClientNoteCommand(ClientId.ToString(), text), CancellationToken.None);
        var error = await act.Should().ThrowAsync<AppValidationException>();
        error.Which.Errors["text"].Should().Equal(ClientCardMessages.EmptyNote);
        _notes.Verify(repo => repo.Stage(It.IsAny<ClientNote>()), Times.Never);
    }

    [Fact]
    public async Task AddNote_RejectsTextLongerThan500_AndKeepsExactly500_TrimmingOnlyEdges()
    {
        var tooLong = new string('я', 501);
        var act = async () => await Handlers().Handle(new AddClientNoteCommand(ClientId.ToString(), $"  {tooLong}  "), CancellationToken.None);
        var error = await act.Should().ThrowAsync<AppValidationException>();
        error.Which.Errors["text"].Should().Equal(ClientCardMessages.NoteTooLong);
        _notes.Verify(repo => repo.Stage(It.IsAny<ClientNote>()), Times.Never);

        StubBookings(Sample(Guid.NewGuid(), DateTime.UtcNow, null, BookingStatus.New));
        StubClient();
        StubAuthor("Марина Иванова");
        As(OtherManagerId, AppRoles.Manager);
        ClientNote? staged = null;
        _notes.Setup(repo => repo.Stage(It.IsAny<ClientNote>())).Callback<ClientNote>(note => staged = note);

        var exact = await Handlers().Handle(
            new AddClientNoteCommand(ClientId.ToString(), $"  {new string('я', 500)}  "),
            CancellationToken.None);
        exact.Text.Should().HaveLength(500);
        exact.AuthorName.Should().Be("Марина Иванова");
        staged.Should().NotBeNull();
        staged!.Text.Should().Be(new string('я', 500));

        var kept = await Handlers().Handle(
            new AddClientNoteCommand(ClientId.ToString(), "  просит  вылет  "),
            CancellationToken.None);
        kept.Text.Should().Be("просит  вылет");
        _identity.Verify(client => client.ReadUserPhoneAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddNote_AnyManagerOrAdmin_ReturnsOnlyTheNote_AndDoesNotWriteWhenAuthorLookupFails()
    {
        StubBookings(Sample(Guid.NewGuid(), DateTime.UtcNow, null, BookingStatus.New));
        StubClient();
        As(OtherManagerId, AppRoles.Manager);
        _identity.Setup(client => client.ReadCardProfileAsync(OtherManagerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityCardProfileRead(IdentityReadKind.Unavailable, null));

        var failed = async () => await Handlers().Handle(
            new AddClientNoteCommand(ClientId.ToString(), "просит вылет после 15 мая"),
            CancellationToken.None);
        await failed.Should().ThrowAsync<UpstreamUnavailableException>();
        _notes.Verify(repo => repo.Stage(It.IsAny<ClientNote>()), Times.Never);
        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        StubAuthor("Марина Иванова");
        var created = await Handlers().Handle(
            new AddClientNoteCommand(ClientId.ToString(), "просит вылет после 15 мая"),
            CancellationToken.None);
        created.Text.Should().Be("просит вылет после 15 мая");
        created.AuthorName.Should().Be("Марина Иванова");
        created.Id.Should().NotBe(Guid.Empty);
        JsonSerializer.Serialize(created, WebJson()).Should().NotContain("email");
        JsonSerializer.Serialize(created, WebJson()).Should().NotContain("phone");
        JsonSerializer.Serialize(created, WebJson()).Should().NotContain("avatarUrl");

        As(AdminId, AppRoles.Admin);
        StubAuthor("Админ Сергеев");
        var byAdmin = await Handlers().Handle(
            new AddClientNoteCommand(ClientId.ToString(), "просит вылет после 15 мая"),
            CancellationToken.None);
        byAdmin.AuthorName.Should().Be("Админ Сергеев");
    }

    [Fact]
    public async Task CardAndList_ShapeOrderAndAuthorName_FollowTheContract()
    {
        var olderId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var newerId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var moment = new DateTime(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);
        var older = Sample(olderId, moment, null, BookingStatus.New, Guid.Parse("10000000-0000-0000-0000-000000000001"));
        var newer = Sample(newerId, moment, ManagerId, BookingStatus.Cancelled, Guid.Parse("10000000-0000-0000-0000-000000000002"));
        StubBookings(older, newer);
        _identity.Setup(client => client.ReadCardProfileAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile("  Анна Соколова  ", AppRoles.Client, "anna@example.com", null));
        As(OtherManagerId, AppRoles.Manager);

        var noteOld = ClientNote.Create(ClientId, OtherManagerId, "старая", moment);
        var noteNew = ClientNote.Create(ClientId, ManagerId, "новая", moment);
        Set(noteOld, nameof(ClientNote.Id), olderId);
        Set(noteNew, nameof(ClientNote.Id), newerId);
        _notes.Setup(repo => repo.ListByClientAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { noteOld, noteNew });
        _identity.Setup(client => client.ReadCardProfileAsync(OtherManagerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile("Марина Иванова", AppRoles.Manager, "m@example.com", null));
        _identity.Setup(client => client.ReadCardProfileAsync(ManagerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityCardProfileRead(IdentityReadKind.NotFound, null));

        var card = await HandleCard();
        card.DisplayName.Should().Be("Анна Соколова");
        card.Email.Should().Be("anna@example.com");
        card.AvatarUrl.Should().BeNull();
        card.Phone.Should().BeNull();
        card.Bookings.Select(item => item.Id).Should().Equal(newerId, olderId);
        card.Bookings[0].Status.Should().Be(BookingStatus.Cancelled);
        card.Bookings[0].TourId.Should().Be(newer.TourId);
        card.Notes.Select(item => item.Id).Should().Equal(newerId, olderId);
        card.Notes[0].AuthorName.Should().Be(ClientCardMessages.StaffAuthor);
        card.Notes[1].AuthorName.Should().Be("Марина Иванова");
        card.Notes.Should().OnlyContain(item => item.Text.Length > 0);

        var json = JsonSerializer.Serialize(card, WebJson());
        json.Should().NotContain("avatarUrl");
        json.Should().NotContain("phone");
        json.Should().NotContain("price");
        json.Should().NotContain("comment");
        json.Should().NotContain("proposal");
        using (var document = JsonDocument.Parse(json))
        {
            document.RootElement.GetProperty("notes").ValueKind.Should().Be(JsonValueKind.Array);
            document.RootElement.GetProperty("bookings")[0].GetProperty("status").GetInt32().Should().Be(5);
            document.RootElement.GetProperty("bookings")[0].EnumerateObject().Select(property => property.Name)
                .Should().BeEquivalentTo("id", "tourId", "status", "createdAt");
        }

        _identity.Setup(client => client.ReadCardProfileAsync(ManagerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile("   ", AppRoles.Manager, "m@example.com", null));
        (await HandleCard()).Notes[0].AuthorName.Should().Be(ClientCardMessages.StaffAuthor);

        _identity.Setup(client => client.ReadCardProfileAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile("   ", AppRoles.Client, "anna@example.com", Avatar));
        As(ManagerId, AppRoles.Manager);
        StubPhone(Phone);
        var renamed = await HandleCard();
        renamed.DisplayName.Should().BeEmpty();
        renamed.DisplayName.Should().NotBe("Клиент").And.NotBe("Менеджер");
        renamed.AvatarUrl.Should().Be(Avatar);
        renamed.Phone.Should().Be(Phone);

        var managerAccount = Guid.Parse("dddddddd-bbbb-cccc-dddd-eeeeeeeeeeee");
        var noBookingClient = Guid.Parse("eeeeeeee-bbbb-cccc-dddd-eeeeeeeeeeee");
        var later = Sample(Guid.NewGuid(), moment.AddDays(1), null, BookingStatus.Closed);
        Set(later, nameof(BookingEntity.ClientId), Guid.Parse("ffffffff-bbbb-cccc-dddd-eeeeeeeeeeee"));
        _bookings.Setup(repo => repo.GetAllForManagersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { older, newer, later, SampleFor(managerAccount) });
        _identity.Setup(client => client.ReadCardProfileAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile("Анна Соколова", AppRoles.Client, "anna@example.com", null));
        _identity.Setup(client => client.ReadCardProfileAsync(later.ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile("Борис", AppRoles.Client, "boris@example.com", Avatar));
        _identity.Setup(client => client.ReadCardProfileAsync(managerAccount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile("Менеджер", AppRoles.Manager, "staff@example.com", null));
        _identity.Setup(client => client.ReadCardProfileAsync(noBookingClient, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile("Без заявки", AppRoles.Client, "none@example.com", null));
        _identity.Setup(client => client.ReadUserPhoneAsync(later.ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPhoneRead(IdentityReadKind.Found, Phone));

        As(AdminId, AppRoles.Admin);
        var list = await Handlers().Handle(new GetClientListQuery(), CancellationToken.None);
        list.Select(item => item.Id).Should().Equal(later.ClientId, ClientId);
        list[0].BookingCount.Should().Be(1);
        list[1].BookingCount.Should().Be(2);
        list[1].Phone.Should().Be(Phone);
        list.Should().NotContain(item => item.Id == managerAccount || item.Id == noBookingClient);
        var listJson = JsonSerializer.Serialize(list[1], WebJson());
        listJson.Should().NotContain("notes");
        listJson.Should().NotContain("bookings");
        listJson.Should().NotContain("avatarUrl");
        listJson.Should().Contain("bookingCount");

        _identity.Setup(client => client.ReadCardProfileAsync(later.ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityCardProfileRead(IdentityReadKind.Unavailable, null));
        var listFailure = async () => await Handlers().Handle(new GetClientListQuery(), CancellationToken.None);
        await listFailure.Should().ThrowAsync<UpstreamUnavailableException>();
    }

    [Fact]
    public async Task BookingDto_KeepsSwallowingIdentityFailure_AndDoesNotCarryTheCardPhone()
    {
        var booking = Sample(Guid.NewGuid(), DateTime.UtcNow, ManagerId, BookingStatus.New);
        _bookings.Setup(repo => repo.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        _identity.Setup(client => client.GetUserSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSummary?)null);
        _identity.Setup(client => client.ReadUserPhoneAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPhoneRead(IdentityReadKind.Found, Phone));
        As(ManagerId, AppRoles.Manager);

        var dto = await new GetBookingByIdQueryHandler(_current.Object, _bookings.Object, _identity.Object)
            .Handle(new GetBookingByIdQuery(booking.Id), CancellationToken.None);

        dto.ClientName.Should().BeNull();
        dto.ClientPhone.Should().BeNull();
        _identity.Verify(client => client.ReadCardProfileAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _identity.Verify(client => client.ReadUserPhoneAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        StubBookings(booking);
        _identity.Setup(client => client.ReadCardProfileAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityCardProfileRead(IdentityReadKind.Unavailable, null));
        var card = async () => await HandleCard();
        await card.Should().ThrowAsync<UpstreamUnavailableException>();

        typeof(BookingDto).GetProperties().Select(property => property.Name).Should().NotContain("Notes");
        typeof(BookingGrpcService).GetMethod("ValidateBookingAccess").Should().NotBeNull();

        var root = RepoRoot();
        var chatFiles = Directory.GetFiles(Path.Combine(root, "src/Services/Chat"), "*.cs", SearchOption.AllDirectories);
        chatFiles.Should().NotContain(file => File.ReadAllText(file).Contains("GetUserPhone", StringComparison.Ordinal));
        var messageDto = File.ReadAllText(Path.Combine(
            root, "src/Services/Chat/TravelAgency.Chat.Application/DTOs/ChatMessageDto.cs"));
        messageDto.Should().NotContain("Phone");
        messageDto.Should().NotContain("AvatarUrl");
        messageDto.Should().NotContain("Note");
    }

    [Fact]
    public void AddNote_PutPatchDelete_AreNotHandlers()
    {
        var commands = typeof(ClientCardHandlers).Assembly.GetTypes()
            .Where(type => type.Name.Contains("ClientNote", StringComparison.Ordinal) && type.Name.Contains("Command"));
        commands.Select(type => type.Name).Should().Equal("AddClientNoteCommand");
    }

    private ClientCardHandlers Handlers() =>
        new(_current.Object, _bookings.Object, _notes.Object, _identity.Object, _unitOfWork.Object);

    private Task<ClientCardDto> HandleCard() =>
        Handlers().Handle(new GetClientCardQuery(ClientId.ToString()), CancellationToken.None);

    private void As(Guid userId, string role)
    {
        _current.SetupGet(user => user.UserId).Returns(userId);
        _current.SetupGet(user => user.Role).Returns(role);
        _current.SetupGet(user => user.IsAuthenticated).Returns(true);
    }

    private void StubBookings(params BookingEntity[] bookings)
    {
        _bookings.Setup(repo => repo.GetByClientIdAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookings);
    }

    private void StubClient(string role = AppRoles.Client)
    {
        _identity.Setup(client => client.ReadCardProfileAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile("Анна Соколова", role, "anna@example.com", Avatar));
    }

    private void StubPhone(string phone)
    {
        _identity.Setup(client => client.ReadUserPhoneAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPhoneRead(IdentityReadKind.Found, phone));
    }

    private void StubAuthor(string name)
    {
        _identity.Setup(client => client.ReadCardProfileAsync(It.Is<Guid>(id => id != ClientId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile(name, AppRoles.Manager, "staff@example.com", null));
    }

    private static IdentityCardProfileRead Profile(string name, string role, string email, string? avatar) =>
        new(IdentityReadKind.Found, new IdentityCardProfile(email, name, role, avatar));

    private static BookingEntity Sample(
        Guid id,
        DateTime createdAt,
        Guid? managerId,
        BookingStatus status,
        Guid? tourId = null)
    {
        var booking = BookingEntity.Create(ClientId, tourId ?? Guid.NewGuid(), null, managerId);
        Set(booking, nameof(BookingEntity.Id), id);
        Set(booking, nameof(BookingEntity.CreatedAt), createdAt);
        Set(booking, nameof(BookingEntity.Status), status);
        return booking;
    }

    private static BookingEntity SampleFor(Guid clientId)
    {
        var booking = BookingEntity.Create(clientId, Guid.NewGuid(), null);
        Set(booking, nameof(BookingEntity.CreatedAt), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        return booking;
    }

    private static void Set(object target, string name, object? value) =>
        target.GetType().GetProperty(name)!.SetValue(target, value);

    private static JsonSerializerOptions WebJson() => new(JsonSerializerDefaults.Web);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "TravelAgency.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Корень репозитория не найден.");
    }
}
