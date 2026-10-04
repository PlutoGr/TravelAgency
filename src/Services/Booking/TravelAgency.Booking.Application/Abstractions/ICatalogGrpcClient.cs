using TravelAgency.Booking.Application.DTOs;

namespace TravelAgency.Booking.Application.Abstractions;

public interface ICatalogGrpcClient
{
    Task<BookingTourSnapshotDto> GetTourSnapshotAsync(Guid tourId, CancellationToken ct = default);

    /// <summary>
    /// Снимок для уже существующей заявки. Снятый с публикации тур тоже возвращается.
    /// Если тур недоступен, клиент бросает <c>TourUnavailableException</c>.
    /// </summary>
    Task<BookingTourSnapshotDto> GetTourSnapshotForExistingBookingAsync(Guid tourId, CancellationToken ct = default);
}
