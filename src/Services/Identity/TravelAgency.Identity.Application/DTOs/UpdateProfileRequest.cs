namespace TravelAgency.Identity.Application.DTOs;

public sealed record UpdateProfileRequest(
    string? FirstName,
    string? LastName,
    string? Phone);
