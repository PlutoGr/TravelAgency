using FluentAssertions;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Exceptions;

namespace TravelAgency.Identity.UnitTests.Domain;

public class RefreshTokenEntityTests
{
    [Fact]
    public void Create_WithValidParameters_SetsAllPropertiesCorrectly()
    {
        var userId = Guid.NewGuid();
        var expires = DateTime.UtcNow.AddDays(7);
        var before = DateTime.UtcNow;

        var token = RefreshToken.Create(userId, "abc123token", expires);
        var after = DateTime.UtcNow;

        token.Id.Should().NotBe(Guid.Empty);
        token.UserId.Should().Be(userId);
        token.Token.Should().Be("abc123token");
        token.ExpiresAt.Should().Be(expires);
        token.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        token.RevokedAt.Should().BeNull();
        token.ReplacedByToken.Should().BeNull();
    }

    [Fact]
    public void Create_EachCall_GeneratesUniqueId()
    {
        var userId = Guid.NewGuid();
        var expires = DateTime.UtcNow.AddDays(7);

        var t1 = RefreshToken.Create(userId, "token1", expires);
        var t2 = RefreshToken.Create(userId, "token2", expires);

        t1.Id.Should().NotBe(t2.Id);
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsInFuture_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "t", DateTime.UtcNow.AddHours(1));

        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsInPast_ReturnsTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "t", DateTime.UtcNow.AddSeconds(-1));

        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsRevoked_WhenNotRevoked_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "t", DateTime.UtcNow.AddDays(7));

        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_AfterRevoke_ReturnsTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "t", DateTime.UtcNow.AddDays(7));
        token.Revoke();

        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenNotExpiredAndNotRevoked_ReturnsTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "t", DateTime.UtcNow.AddDays(7));

        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenExpired_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "t", DateTime.UtcNow.AddSeconds(-1));

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenRevoked_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "t", DateTime.UtcNow.AddDays(7));
        token.Revoke();

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_SetsRevokedAtToUtcNow()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "t", DateTime.UtcNow.AddDays(7));
        var before = DateTime.UtcNow;

        token.Revoke();
        var after = DateTime.UtcNow;

        token.RevokedAt.Should().NotBeNull();
        token.RevokedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Revoke_WithReplacedByToken_SetsReplacedByToken()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "t", DateTime.UtcNow.AddDays(7));

        token.Revoke("newToken123");

        token.ReplacedByToken.Should().Be("newToken123");
    }

    [Fact]
    public void Revoke_WithoutReplacedByToken_LeavesReplacedByTokenNull()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "t", DateTime.UtcNow.AddDays(7));

        token.Revoke();

        token.ReplacedByToken.Should().BeNull();
    }

    // ── FIX-006: Revoke guard clause ──────────────────────────────────────────

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ThrowsIdentityDomainException()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "t", DateTime.UtcNow.AddDays(7));
        token.Revoke();

        var act = () => token.Revoke();

        act.Should().Throw<IdentityDomainException>()
            .WithMessage("*already been revoked*");
    }

    [Fact]
    public void Revoke_WhenNotRevoked_SetsRevokedAt()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "t", DateTime.UtcNow.AddDays(7));
        var before = DateTime.UtcNow;

        token.Revoke();
        var after = DateTime.UtcNow;

        token.RevokedAt.Should().NotBeNull();
        token.RevokedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        token.IsRevoked.Should().BeTrue();
    }
}
