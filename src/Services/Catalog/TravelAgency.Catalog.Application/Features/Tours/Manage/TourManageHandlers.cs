using MediatR;
using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Catalog.Application.Mappings;
using TravelAgency.Catalog.Domain;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Domain.Exceptions;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Catalog.Application.Features.Tours.Manage;

public sealed class CreateTourDraftCommandHandler(TourManageStore store)
    : IRequestHandler<CreateTourDraftCommand, TourManageDto>
{
    public async Task<TourManageDto> Handle(CreateTourDraftCommand command, CancellationToken cancellationToken)
    {
        if (store.CurrentUser.UserId == Guid.Empty
            || (store.CurrentUser.Role != AppRoles.Manager && store.CurrentUser.Role != AppRoles.Admin))
            throw new ForbiddenException("Only a manager or an admin can create a tour draft.");

        var tour = Tour.CreateDraft(store.CurrentUser.UserId);
        var request = command.Request;
        if (request is not null && HasBasics(request))
        {
            tour.SetBasics(
                request.Title,
                request.ShortDescription,
                request.DepartureCity,
                request.Country,
                ParseTourType(request.TourType, tour.TourType),
                request.DurationDays ?? tour.DurationDays,
                request.DirectionId);
        }

        if (!string.IsNullOrWhiteSpace(request?.Description))
            tour.SetDescription(request.Description);

        await store.Tours.AddAsync(tour, cancellationToken);
        await store.SaveAsync(cancellationToken);
        return TourManageMapper.ToManageDto(tour);
    }

    private static bool HasBasics(CreateTourDraftRequest request) =>
        request.Title is not null
        || request.ShortDescription is not null
        || request.DepartureCity is not null
        || request.Country is not null
        || request.TourType is not null
        || request.DurationDays is not null
        || request.DirectionId is not null;

    internal static TourType ParseTourType(string? value, TourType fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : Enum.Parse<TourType>(value, ignoreCase: true);
}

public sealed class UpdateTourBasicsCommandHandler(TourManageStore store)
    : IRequestHandler<UpdateTourBasicsCommand, TourManageDto>
{
    public async Task<TourManageDto> Handle(UpdateTourBasicsCommand command, CancellationToken cancellationToken)
    {
        var tour = await store.LoadAsync(command.Id, command.IfMatch, cancellationToken);
        var request = command.Request;
        tour.SetBasics(
            request.Title,
            request.ShortDescription,
            request.DepartureCity,
            request.Country,
            CreateTourDraftCommandHandler.ParseTourType(request.TourType, tour.TourType),
            request.DurationDays ?? tour.DurationDays,
            request.DirectionId ?? tour.DirectionId);
        await store.SaveAsync(cancellationToken);
        return TourManageMapper.ToManageDto(tour);
    }
}

public sealed class UpdateTourDescriptionCommandHandler(TourManageStore store)
    : IRequestHandler<UpdateTourDescriptionCommand, TourManageDto>
{
    public async Task<TourManageDto> Handle(UpdateTourDescriptionCommand command, CancellationToken cancellationToken)
    {
        var tour = await store.LoadAsync(command.Id, command.IfMatch, cancellationToken);
        tour.SetDescription(command.Request.Description);
        await store.SaveAsync(cancellationToken);
        return TourManageMapper.ToManageDto(tour);
    }
}

public sealed class UpdateTourProgramCommandHandler(TourManageStore store)
    : IRequestHandler<UpdateTourProgramCommand, TourManageDto>
{
    public async Task<TourManageDto> Handle(UpdateTourProgramCommand command, CancellationToken cancellationToken)
    {
        var tour = await store.LoadAsync(command.Id, command.IfMatch, cancellationToken);
        var days = (command.Request.Days ?? [])
            .Select(day => TourDay.Create(tour.Id, day.DayNumber, day.Title ?? string.Empty, day.Description ?? string.Empty))
            .ToList();
        tour.ReplaceDays(days);
        await store.SaveAsync(cancellationToken);
        return TourManageMapper.ToManageDto(tour);
    }
}

public sealed class UpdateTourConditionsCommandHandler(TourManageStore store)
    : IRequestHandler<UpdateTourConditionsCommand, TourManageDto>
{
    public async Task<TourManageDto> Handle(UpdateTourConditionsCommand command, CancellationToken cancellationToken)
    {
        var tour = await store.LoadAsync(command.Id, command.IfMatch, cancellationToken);
        var request = command.Request;
        var inclusions = (request.Inclusions ?? [])
            .Select(item => TourInclusion.Create(
                tour.Id,
                item.Text ?? string.Empty,
                Enum.Parse<TourInclusionKind>(item.Kind!, ignoreCase: true),
                item.SortOrder))
            .ToList();
        MealPlan? meal = string.IsNullOrWhiteSpace(request.MealPlan)
            ? null
            : Enum.Parse<MealPlan>(request.MealPlan, ignoreCase: true);
        tour.ReplaceConditions(inclusions, meal, request.AccommodationText);
        await store.SaveAsync(cancellationToken);
        return TourManageMapper.ToManageDto(tour);
    }
}

public sealed class UpdateTourManagePricesCommandHandler(TourManageStore store)
    : IRequestHandler<UpdateTourManagePricesCommand, TourManageDto>
{
    public async Task<TourManageDto> Handle(UpdateTourManagePricesCommand command, CancellationToken cancellationToken)
    {
        var tour = await store.LoadAsync(command.Id, command.IfMatch, cancellationToken);
        var offers = (command.Request.Offers ?? [])
            .Select(offer => TourOffer.Create(
                tour.Id,
                offer.ValidFrom,
                offer.ValidTo,
                offer.PricePerPerson,
                offer.Currency ?? string.Empty,
                offer.AvailableSeats))
            .ToList();
        tour.ReplaceOffers(offers);
        await store.SaveAsync(cancellationToken);
        return TourManageMapper.ToManageDto(tour);
    }
}

public sealed class UpdateTourImagesCommandHandler(TourManageStore store, IMediaFilesClient media)
    : IRequestHandler<UpdateTourImagesCommand, TourManageDto>
{
    public async Task<TourManageDto> Handle(UpdateTourImagesCommand command, CancellationToken cancellationToken)
    {
        var tour = await store.LoadAsync(command.Id, command.IfMatch, cancellationToken);
        var inputs = command.Request.Images ?? [];
        var files = await TourImageRules.LoadOwnedAsync(
            media,
            tour.OwnerId,
            inputs.Select(image => image.MediaFileId).ToList(),
            cancellationToken);

        var cover = inputs.SingleOrDefault(image => image.IsCover);
        int? coverWidth = null;
        if (cover is not null)
        {
            coverWidth = files[cover.MediaFileId].Width;
            TourImageRules.EnsureCoverWidth(coverWidth.Value);
        }

        var images = inputs.Select(image =>
        {
            var width = files[image.MediaFileId].Width;
            return TourImage.Create(
                tour.Id,
                image.MediaFileId,
                image.SortOrder,
                image.IsCover,
                image.Alt,
                width > 0 ? width : null);
        }).ToList();

        tour.ReplaceImages(images, coverWidth);
        await store.SaveAsync(cancellationToken);
        return TourManageMapper.ToManageDto(tour);
    }
}

public sealed class PublishTourCommandHandler(TourManageStore store, IMediaFilesClient media)
    : IRequestHandler<PublishTourCommand, TourManageDto>
{
    public async Task<TourManageDto> Handle(PublishTourCommand command, CancellationToken cancellationToken)
    {
        var tour = await store.LoadAsync(command.Id, command.IfMatch, cancellationToken);
        var now = DateTime.UtcNow;
        var mediaIds = tour.Images.Select(image => image.MediaFileId).ToList();
        var files = await TourImageRules.LoadOwnedAsync(media, tour.OwnerId, mediaIds, cancellationToken);

        var cover = tour.Images.SingleOrDefault(image => image.IsCover);
        int? coverWidth = null;
        if (cover is not null && files.TryGetValue(cover.MediaFileId, out var coverFile))
        {
            // Ширина с Media, не из сохранённого WidthPx: 0 — размер неизвестен, уже 1280 — мало.
            coverWidth = coverFile.Width;
            TourImageRules.EnsureCoverWidth(coverWidth.Value);
        }

        var missing = tour.GetMissingPublishRequirements(now, coverWidth);
        if (missing.Count > 0)
            throw new TourNotPublishableException(missing);

        await media.MarkPublicAsync(mediaIds, cancellationToken);
        tour.Publish(now, coverWidth);
        await store.SaveAsync(cancellationToken);
        return TourManageMapper.ToManageDto(tour);
    }
}

public sealed class UnpublishTourCommandHandler(TourManageStore store)
    : IRequestHandler<UnpublishTourCommand, TourManageDto>
{
    public async Task<TourManageDto> Handle(UnpublishTourCommand command, CancellationToken cancellationToken)
    {
        var tour = await store.LoadAsync(command.Id, command.IfMatch, cancellationToken);
        if (tour.Status != TourStatus.Published && tour.Status != TourStatus.Unpublished)
            throw new ConflictException("Only a published tour can be unpublished.");

        tour.Unpublish();
        await store.SaveAsync(cancellationToken);
        return TourManageMapper.ToManageDto(tour);
    }
}

public sealed class DeleteManagedTourCommandHandler(TourManageStore store)
    : IRequestHandler<DeleteManagedTourCommand, Unit>
{
    public async Task<Unit> Handle(DeleteManagedTourCommand command, CancellationToken cancellationToken)
    {
        var tour = await store.LoadAsync(command.Id, command.IfMatch, cancellationToken);
        try
        {
            tour.Delete();
        }
        catch (CatalogDomainException)
        {
            throw new ConflictException("Only a draft tour can be deleted.");
        }

        store.Tours.Remove(tour);
        await store.SaveAsync(cancellationToken);
        return Unit.Value;
    }
}

public sealed class ListManagedToursQueryHandler(TourManageStore store)
    : IRequestHandler<ListManagedToursQuery, IReadOnlyList<TourManageDto>>
{
    public async Task<IReadOnlyList<TourManageDto>> Handle(ListManagedToursQuery request, CancellationToken cancellationToken)
    {
        if (store.CurrentUser.Role != AppRoles.Manager && store.CurrentUser.Role != AppRoles.Admin)
            throw new ForbiddenException("Only a manager or an admin can list managed tours.");

        Guid? ownerFilter = store.CurrentUser.Role == AppRoles.Admin ? null : store.CurrentUser.UserId;
        var tours = await store.Tours.ListForManageAsync(ownerFilter, cancellationToken);
        return tours.Select(TourManageMapper.ToManageDto).ToList();
    }
}
