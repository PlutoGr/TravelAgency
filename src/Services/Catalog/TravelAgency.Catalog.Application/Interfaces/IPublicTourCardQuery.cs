using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Application.Interfaces;

public interface IPublicTourCardQuery
{
    /// <summary>
    /// Туры для пакета карточек. Черновики и неизвестные id не возвращаются.
    /// </summary>
    Task<IReadOnlyList<Tour>> GetPublishedOrUnpublishedAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);
}
