using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Domain.Exceptions;

namespace TravelAgency.Identity.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? Phone { get; private set; }
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private User() { }

    public static User Create(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        string? phone,
        UserRole role = UserRole.Client)
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
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string firstName, string lastName, string? phone)
    {
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }
}
