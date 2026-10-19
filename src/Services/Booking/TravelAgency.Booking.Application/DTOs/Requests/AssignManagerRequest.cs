namespace TravelAgency.Booking.Application.DTOs.Requests;

public record AssignManagerRequest(Guid ManagerId, Guid? ExpectedManagerId, string? Comment);
