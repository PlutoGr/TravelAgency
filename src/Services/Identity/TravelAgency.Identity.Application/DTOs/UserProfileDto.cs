namespace TravelAgency.Identity.Application.DTOs;

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string Role,
    DateTime CreatedAt,
    string? City = null,
    string? AvatarUrl = null,
    string? About = null,
    int? TourismSince = null,
    IReadOnlyList<string>? Languages = null,
    string? Specialization = null);
