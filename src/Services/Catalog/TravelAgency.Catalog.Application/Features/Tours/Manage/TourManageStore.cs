using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Interfaces;
using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Catalog.Application.Features.Tours.Manage;

public sealed class TourManageStore(
    ITourRepository tours,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
{
    public ITourRepository Tours { get; } = tours;

    public ICurrentUserService CurrentUser { get; } = currentUser;

    public async Task<Tour> LoadAsync(Guid id, string? ifMatch, CancellationToken cancellationToken)
    {
        var tour = await Tours.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Tour), id);
        EnsureCanManage(tour);
        TourEtag.EnsureCurrent(tour, ifMatch);
        return tour;
    }

    public void EnsureCanManage(Tour tour)
    {
        if (IsAdmin)
            return;

        if (IsManager && tour.OwnerId == CurrentUser.UserId)
            return;

        throw new ForbiddenException("You can manage only your own tours.");
    }

    public Task SaveAsync(CancellationToken cancellationToken) =>
        unitOfWork.SaveChangesAsync(cancellationToken);

    private bool IsAdmin =>
        string.Equals(CurrentUser.Role, AppRoles.Admin, StringComparison.Ordinal);

    private bool IsManager =>
        string.Equals(CurrentUser.Role, AppRoles.Manager, StringComparison.Ordinal);
}
