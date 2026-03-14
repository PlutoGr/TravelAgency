using FluentAssertions;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Domain.Exceptions;

namespace TravelAgency.Identity.UnitTests.Domain;

public class UserEntityTests
{
    // ── Existing property-mapping tests (updated for enum Role) ──────────────

    [Fact]
    public void Create_WithValidParameters_SetsAllPropertiesCorrectly()
    {
        var before = DateTime.UtcNow;
        var user = User.Create("test@example.com", "hash123", "John", "Doe", "+1234567890");
        var after = DateTime.UtcNow;

        user.Id.Should().NotBe(Guid.Empty);
        user.Email.Should().Be("test@example.com");
        user.PasswordHash.Should().Be("hash123");
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Phone.Should().Be("+1234567890");
        user.Role.Should().Be(UserRole.Client);
        user.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        user.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullPhone_SetsPhoneToNull()
    {
        var user = User.Create("test@example.com", "hash", "John", "Doe", null);

        user.Phone.Should().BeNull();
    }

    [Fact]
    public void Create_WithExplicitRole_UsesProvidedRole()
    {
        var user = User.Create("admin@example.com", "hash", "Jane", "Admin", null, UserRole.Admin);

        user.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void Create_WithoutRole_DefaultsToClient()
    {
        var user = User.Create("user@example.com", "hash", "Jane", "Doe", null);

        user.Role.Should().Be(UserRole.Client);
    }

    [Fact]
    public void Create_EachCall_GeneratesUniqueId()
    {
        var user1 = User.Create("a@example.com", "hash", "A", "A", null);
        var user2 = User.Create("b@example.com", "hash", "B", "B", null);

        user1.Id.Should().NotBe(user2.Id);
    }

    [Fact]
    public void UpdateProfile_UpdatesNameAndPhone()
    {
        var user = User.Create("test@example.com", "hash", "John", "Doe", null);
        var before = DateTime.UtcNow;

        user.UpdateProfile("Jane", "Smith", "+9876543210");
        var after = DateTime.UtcNow;

        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
        user.Phone.Should().Be("+9876543210");
        user.UpdatedAt.Should().NotBeNull();
        user.UpdatedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void UpdateProfile_WithNullPhone_SetsPhoneToNull()
    {
        var user = User.Create("test@example.com", "hash", "John", "Doe", "+1234567890");

        user.UpdateProfile("John", "Doe", null);

        user.Phone.Should().BeNull();
    }

    [Fact]
    public void UpdateProfile_DoesNotChangeEmailOrRole()
    {
        var user = User.Create("test@example.com", "hash", "John", "Doe", null, UserRole.Manager);

        user.UpdateProfile("Jane", "Smith", null);

        user.Email.Should().Be("test@example.com");
        user.Role.Should().Be(UserRole.Manager);
    }

    [Fact]
    public void ChangePassword_UpdatesPasswordHashAndSetsUpdatedAt()
    {
        var user = User.Create("test@example.com", "oldHash", "John", "Doe", null);
        var before = DateTime.UtcNow;

        user.ChangePassword("newHash");
        var after = DateTime.UtcNow;

        user.PasswordHash.Should().Be("newHash");
        user.UpdatedAt.Should().NotBeNull();
        user.UpdatedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void ChangePassword_DoesNotChangeOtherProperties()
    {
        var user = User.Create("test@example.com", "oldHash", "John", "Doe", "+123");

        user.ChangePassword("newHash");

        user.Email.Should().Be("test@example.com");
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Phone.Should().Be("+123");
    }

    // ── FIX-005: UserRole enum ────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidParams_SetsRoleToClientEnum()
    {
        var user = User.Create("client@example.com", "hash", "Alice", "Smith", null);

        user.Role.Should().Be(UserRole.Client);
    }

    [Fact]
    public void Create_WithManagerRole_SetsRoleEnum()
    {
        var user = User.Create("manager@example.com", "hash", "Bob", "Jones", null, UserRole.Manager);

        user.Role.Should().Be(UserRole.Manager);
    }

    // ── FIX-006: Guard clauses ────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyEmail_ThrowsIdentityDomainException(string email)
    {
        var act = () => User.Create(email, "hash", "John", "Doe", null);

        act.Should().Throw<IdentityDomainException>()
            .WithMessage("*Email*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyFirstName_ThrowsIdentityDomainException(string firstName)
    {
        var act = () => User.Create("user@example.com", "hash", firstName, "Doe", null);

        act.Should().Throw<IdentityDomainException>()
            .WithMessage("*irst name*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyLastName_ThrowsIdentityDomainException(string lastName)
    {
        var act = () => User.Create("user@example.com", "hash", "John", lastName, null);

        act.Should().Throw<IdentityDomainException>()
            .WithMessage("*ast name*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyPasswordHash_ThrowsIdentityDomainException(string passwordHash)
    {
        var act = () => User.Create("user@example.com", passwordHash, "John", "Doe", null);

        act.Should().Throw<IdentityDomainException>()
            .WithMessage("*assword*");
    }

    // ── FIX-007: Email normalization ──────────────────────────────────────────

    [Fact]
    public void Create_NormalizesEmailToLowerCase()
    {
        var user = User.Create("User@EXAMPLE.COM", "hash", "John", "Doe", null);

        user.Email.Should().Be("user@example.com");
    }

    [Fact]
    public void Create_WithAlreadyLowerCaseEmail_StoresEmailUnchanged()
    {
        var user = User.Create("user@example.com", "hash", "John", "Doe", null);

        user.Email.Should().Be("user@example.com");
    }
}
