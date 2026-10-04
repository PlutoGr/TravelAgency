using MediatR;
using TravelAgency.Catalog.Application.DTOs;

namespace TravelAgency.Catalog.Application.Features.Tours.Manage;

public record CreateTourDraftCommand(CreateTourDraftRequest? Request) : IRequest<TourManageDto>;

public record UpdateTourBasicsCommand(Guid Id, string? IfMatch, UpdateTourBasicsRequest Request) : IRequest<TourManageDto>;

public record UpdateTourDescriptionCommand(Guid Id, string? IfMatch, UpdateTourDescriptionRequest Request) : IRequest<TourManageDto>;

public record UpdateTourProgramCommand(Guid Id, string? IfMatch, UpdateTourProgramRequest Request) : IRequest<TourManageDto>;

public record UpdateTourConditionsCommand(Guid Id, string? IfMatch, UpdateTourConditionsRequest Request) : IRequest<TourManageDto>;

public record UpdateTourManagePricesCommand(Guid Id, string? IfMatch, UpdateTourManagePricesRequest Request) : IRequest<TourManageDto>;

public record UpdateTourImagesCommand(Guid Id, string? IfMatch, UpdateTourImagesRequest Request) : IRequest<TourManageDto>;

public record PublishTourCommand(Guid Id, string? IfMatch) : IRequest<TourManageDto>;

public record UnpublishTourCommand(Guid Id, string? IfMatch) : IRequest<TourManageDto>;

public record DeleteManagedTourCommand(Guid Id, string? IfMatch) : IRequest<Unit>;

public record ListManagedToursQuery : IRequest<IReadOnlyList<TourManageDto>>;
