using MediatR;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Booking.Application.Features.Clients;

public sealed record GetClientCardQuery(string ClientId) : IRequest<ClientCardDto>;

public sealed record GetClientListQuery : IRequest<IReadOnlyList<ClientListItemDto>>;

public sealed record AddClientNoteCommand(string ClientId, string? Text) : IRequest<ClientNoteDto>;

public static class ClientCardMessages
{
    public const string NotFound = "Client card was not found.";
    public const string EmptyNote = "Напишите текст заметки.";
    public const string NoteTooLong = "Заметка не длиннее 500 символов.";
    public const string StaffAuthor = "Сотрудник";

    public static string NormalizeNote(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw Validation("text", EmptyNote);

        var trimmed = text.Trim();
        if (trimmed.Length > ClientNote.MaxTextLength)
            throw Validation("text", NoteTooLong);

        return trimmed;
    }

    private static AppValidationException Validation(string key, string message) =>
        new(new Dictionary<string, string[]> { [key] = [message] });
}

public sealed class ClientCardHandlers(
    ICurrentUserService currentUser,
    IBookingRepository bookingRepository,
    IClientNoteRepository noteRepository,
    IIdentityGrpcClient identityGrpcClient,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetClientCardQuery, ClientCardDto>,
        IRequestHandler<GetClientListQuery, IReadOnlyList<ClientListItemDto>>,
        IRequestHandler<AddClientNoteCommand, ClientNoteDto>
{
    public async Task<ClientCardDto> Handle(GetClientCardQuery query, CancellationToken cancellationToken)
    {
        var clientId = ParseClientId(query.ClientId);
        var bookings = await RequireBookingsAsync(clientId, cancellationToken);
        var profile = await RequireClientAsync(clientId, cancellationToken);
        var phone = await PhoneForViewerAsync(clientId, bookings, cancellationToken);
        var notes = await noteRepository.ListByClientAsync(clientId, cancellationToken);
        var authors = new Dictionary<Guid, string>();

        var noteDtos = new List<ClientNoteDto>(notes.Count);
        foreach (var note in notes.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id))
        {
            if (!authors.TryGetValue(note.AuthorId, out var authorName))
            {
                authorName = await AuthorNameAsync(note.AuthorId, cancellationToken);
                authors[note.AuthorId] = authorName;
            }

            noteDtos.Add(new ClientNoteDto(note.Id, note.Text, authorName, note.CreatedAt));
        }

        return new ClientCardDto(
            clientId,
            DisplayName(profile),
            profile.Email,
            profile.AvatarUrl,
            phone,
            bookings
                .OrderByDescending(b => b.CreatedAt)
                .ThenByDescending(b => b.Id)
                .Select(b => new ClientCardBookingDto(b.Id, b.TourId, b.Status, b.CreatedAt))
                .ToList(),
            noteDtos);
    }

    public async Task<IReadOnlyList<ClientListItemDto>> Handle(
        GetClientListQuery query,
        CancellationToken cancellationToken)
    {
        var bookings = await bookingRepository.GetAllForManagersAsync(cancellationToken);
        var rows = new List<(DateTime Latest, ClientListItemDto Item)>();

        foreach (var group in bookings.GroupBy(b => b.ClientId))
        {
            var clientBookings = group.ToList();
            var read = await identityGrpcClient.ReadCardProfileAsync(group.Key, cancellationToken);
            if (read.Kind == IdentityReadKind.Unavailable)
                throw new UpstreamUnavailableException();
            if (read.Kind != IdentityReadKind.Found || read.Profile is null || read.Profile.Role != AppRoles.Client)
                continue;

            var phone = await PhoneForViewerAsync(group.Key, clientBookings, cancellationToken);
            var latest = clientBookings.Max(b => b.CreatedAt);
            rows.Add((latest, new ClientListItemDto(
                group.Key,
                DisplayName(read.Profile),
                read.Profile.Email,
                read.Profile.AvatarUrl,
                phone,
                clientBookings.Count)));
        }

        return rows
            .OrderByDescending(row => row.Latest)
            .ThenByDescending(row => row.Item.Id)
            .Select(row => row.Item)
            .ToList();
    }

    public async Task<ClientNoteDto> Handle(AddClientNoteCommand command, CancellationToken cancellationToken)
    {
        var text = ClientCardMessages.NormalizeNote(command.Text);
        var clientId = ParseClientId(command.ClientId);
        await RequireBookingsAsync(clientId, cancellationToken);
        await RequireClientAsync(clientId, cancellationToken);
        var authorName = await AuthorNameAsync(currentUser.UserId, cancellationToken);

        var note = ClientNote.Create(clientId, currentUser.UserId, text);
        noteRepository.Stage(note);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ClientNoteDto(note.Id, note.Text, authorName, note.CreatedAt);
    }

    private async Task<string?> PhoneForViewerAsync(
        Guid clientId,
        IReadOnlyList<Domain.Entities.Booking> bookings,
        CancellationToken cancellationToken)
    {
        if (!AllowsPhone(currentUser.Role, currentUser.UserId, bookings))
            return null;

        var read = await identityGrpcClient.ReadUserPhoneAsync(clientId, cancellationToken);
        if (read.Kind == IdentityReadKind.NotFound)
            throw new NotFoundException(ClientCardMessages.NotFound);
        if (read.Kind == IdentityReadKind.Unavailable)
            throw new UpstreamUnavailableException();

        return string.IsNullOrEmpty(read.Phone) ? null : read.Phone;
    }

    internal static bool AllowsPhone(
        string role,
        Guid viewerId,
        IReadOnlyList<Domain.Entities.Booking> bookings)
    {
        if (role == AppRoles.Admin)
            return true;
        if (role != AppRoles.Manager)
            return false;

        return bookings.Any(booking => booking.AssignedManagerId == viewerId);
    }

    private async Task<IdentityCardProfile> RequireClientAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var read = await identityGrpcClient.ReadCardProfileAsync(clientId, cancellationToken);
        if (read.Kind == IdentityReadKind.Unavailable)
            throw new UpstreamUnavailableException();
        if (read.Kind != IdentityReadKind.Found || read.Profile is null || read.Profile.Role != AppRoles.Client)
            throw new NotFoundException(ClientCardMessages.NotFound);

        return read.Profile;
    }

    private async Task<IReadOnlyList<Domain.Entities.Booking>> RequireBookingsAsync(
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var bookings = await bookingRepository.GetByClientIdAsync(clientId, cancellationToken);
        if (bookings.Count == 0)
            throw new NotFoundException(ClientCardMessages.NotFound);

        return bookings;
    }

    private async Task<string> AuthorNameAsync(Guid authorId, CancellationToken cancellationToken)
    {
        var read = await identityGrpcClient.ReadCardProfileAsync(authorId, cancellationToken);
        if (read.Kind == IdentityReadKind.Unavailable)
            throw new UpstreamUnavailableException();
        if (read.Kind != IdentityReadKind.Found || read.Profile is null)
            return ClientCardMessages.StaffAuthor;

        var name = read.Profile.DisplayName.Trim();
        return name.Length == 0 ? ClientCardMessages.StaffAuthor : name;
    }

    private static Guid ParseClientId(string clientId)
    {
        if (!Guid.TryParse(clientId, out var id) || id == Guid.Empty)
            throw new NotFoundException(ClientCardMessages.NotFound);

        return id;
    }

    private static string DisplayName(IdentityCardProfile profile) => profile.DisplayName.Trim();
}
