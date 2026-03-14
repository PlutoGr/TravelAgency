using FluentAssertions;
using TravelAgency.Identity.Infrastructure.Services;

namespace TravelAgency.Identity.UnitTests.Infrastructure.Services;

public class PasswordHasherServiceTests
{
    private readonly PasswordHasherService _service = new();

    [Fact]
    public void Hash_ReturnsNonEmptyString()
    {
        var hash = _service.Hash("myPassword");

        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Hash_ReturnsBCryptFormattedHash()
    {
        var hash = _service.Hash("myPassword");

        hash.Should().StartWith("$2");
    }

    [Fact]
    public void Hash_CalledTwiceWithSamePassword_ReturnsDifferentHashes()
    {
        var hash1 = _service.Hash("samePassword");
        var hash2 = _service.Hash("samePassword");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _service.Hash("correctPassword");

        _service.Verify("correctPassword", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _service.Hash("correctPassword");

        _service.Verify("wrongPassword", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithEmptyPassword_ReturnsFalse()
    {
        var hash = _service.Hash("somePassword");

        _service.Verify("", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithCaseDifference_ReturnsFalse()
    {
        var hash = _service.Hash("Password");

        _service.Verify("password", hash).Should().BeFalse();
    }
}
