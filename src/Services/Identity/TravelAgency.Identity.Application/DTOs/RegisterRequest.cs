namespace TravelAgency.Identity.Application.DTOs;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone);
