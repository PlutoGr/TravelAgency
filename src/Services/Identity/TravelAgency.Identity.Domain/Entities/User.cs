using System.Text.Json;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Domain.Exceptions;

namespace TravelAgency.Identity.Domain.Entities;

public class User
{
    private static readonly JsonSerializerOptions LanguagesJsonOptions = new(JsonSerializerDefaults.Web);

    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? Phone { get; private set; }
    public string? City { get; private set; }
    public string? About { get; private set; }
    public int? TourismSince { get; private set; }
    public string? LanguagesJson { get; private set; }
    public string? Specialization { get; private set; }
    public Guid? AvatarMediaFileId { get; private set; }
    public const int MaxBlockReasonLength = 500;
    public const int MaxCityLength = 100;
    public const int MaxAboutLength = 1000;
    public const int MaxSpecializationLength = 300;
    public const int MinTourismSinceYear = 1950;

    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? BlockedAt { get; private set; }
    public string? BlockReason { get; private set; }
    public Guid? BlockedByUserId { get; private set; }
    public long SessionVersion { get; private set; }

    public bool IsBlocked => BlockedAt.HasValue;

    public string DisplayName => $"{FirstName} {LastName}".Trim();

    private User() { }

    public static User Create(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        string? phone,
        UserRole role = UserRole.Client,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new IdentityDomainException("Email cannot be empty.");
        if (string.IsNullOrWhiteSpace(firstName))
            throw new IdentityDomainException("First name cannot be empty.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new IdentityDomainException("Last name cannot be empty.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new IdentityDomainException("Password hash cannot be empty.");

        return new User
        {
            Id = id is null || id == Guid.Empty ? Guid.NewGuid() : id.Value,
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string firstName, string lastName, string? phone, string? city)
    {
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        City = city;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateManagerProfile(string? about, int? tourismSince, IReadOnlyList<string>? languages, string? specialization)
    {
        About = about;
        TourismSince = tourismSince;
        LanguagesJson = languages is null ? null : JsonSerializer.Serialize(languages, LanguagesJsonOptions);
        Specialization = specialization;
        UpdatedAt = DateTime.UtcNow;
    }

    public IReadOnlyList<string>? GetLanguages()
    {
        if (string.IsNullOrWhiteSpace(LanguagesJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<string>>(LanguagesJson, LanguagesJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void SetAvatar(Guid mediaFileId)
    {
        if (mediaFileId == Guid.Empty)
            throw new IdentityDomainException("Avatar media file id is required.");

        AvatarMediaFileId = mediaFileId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearAvatar()
    {
        AvatarMediaFileId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkLoggedIn(DateTime utcNow)
    {
        LastLoginAt = utcNow;
    }

    public bool ChangeRole(UserRole newRole)
    {
        if (Role == newRole)
            return false;

        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public void RevokeSessions()
    {
        SessionVersion++;
    }

    public void Block(string reason, Guid actorId, DateTime utcNow)
    {
        var trimmed = reason.Trim();
        if (trimmed.Length == 0)
            throw new IdentityDomainException("Block reason is required.");
        if (trimmed.Length > MaxBlockReasonLength)
            throw new IdentityDomainException("Block reason is too long.");
        if (actorId == Guid.Empty)
            throw new IdentityDomainException("Actor is required.");

        BlockReason = trimmed;
        BlockedByUserId = actorId;
        BlockedAt = utcNow;
        UpdatedAt = utcNow;
        SessionVersion++;
    }

    public void Unblock(DateTime utcNow)
    {
        if (!IsBlocked)
            throw new IdentityDomainException("User is not blocked.");

        BlockedAt = null;
        BlockReason = null;
        BlockedByUserId = null;
        UpdatedAt = utcNow;
    }
}
