using System.Text.Json.Serialization;

namespace TravelAgency.Identity.Application.DTOs;

/// <summary>
/// PATCH body for the current user's profile. Unknown members (email, role, city, …) are rejected.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateProfileRequest(
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null);
