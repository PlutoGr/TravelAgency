using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using TravelAgency.Identity.Infrastructure.Services;

namespace TravelAgency.Identity.UnitTests.Infrastructure.Services;

public class CurrentUserServiceTests
{
    private static CurrentUserService CreateService(IEnumerable<Claim>? claims = null)
    {
        var httpContextAccessor = new Mock<IHttpContextAccessor>();

        if (claims is null)
        {
            httpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        }
        else
        {
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
        }

        return new CurrentUserService(httpContextAccessor.Object);
    }

    [Fact]
    public void UserId_WithNameIdentifierClaim_ReturnsCorrectGuid()
    {
        var userId = Guid.NewGuid();
        var service = CreateService(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) });

        service.UserId.Should().Be(userId);
    }

    [Fact]
    public void UserId_WithSubClaim_ReturnsCorrectGuid()
    {
        var userId = Guid.NewGuid();
        var service = CreateService(new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()) });

        service.UserId.Should().Be(userId);
    }

    [Fact]
    public void UserId_WithoutClaim_ThrowsUnauthorizedAccessException()
    {
        var service = CreateService(Enumerable.Empty<Claim>());

        var act = () => service.UserId;

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void UserId_WithNoHttpContext_ThrowsUnauthorizedAccessException()
    {
        var service = CreateService(null);

        var act = () => service.UserId;

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void Email_WithEmailClaim_ReturnsCorrectEmail()
    {
        var service = CreateService(new[] { new Claim(JwtRegisteredClaimNames.Email, "user@example.com") });

        service.Email.Should().Be("user@example.com");
    }

    [Fact]
    public void Email_WithClaimTypesEmailClaim_ReturnsCorrectEmail()
    {
        var service = CreateService(new[] { new Claim(ClaimTypes.Email, "user@example.com") });

        service.Email.Should().Be("user@example.com");
    }

    [Fact]
    public void Email_WithoutEmailClaim_ReturnsEmptyString()
    {
        var service = CreateService(Enumerable.Empty<Claim>());

        service.Email.Should().BeEmpty();
    }

    [Fact]
    public void Role_WithRoleClaim_ReturnsCorrectRole()
    {
        var service = CreateService(new[] { new Claim(ClaimTypes.Role, "Admin") });

        service.Role.Should().Be("Admin");
    }

    [Fact]
    public void Role_WithLowercaseRoleClaim_ReturnsCorrectRole()
    {
        var service = CreateService(new[] { new Claim("role", "Manager") });

        service.Role.Should().Be("Manager");
    }

    [Fact]
    public void Role_WithoutRoleClaim_ReturnsEmptyString()
    {
        var service = CreateService(Enumerable.Empty<Claim>());

        service.Role.Should().BeEmpty();
    }

    [Fact]
    public void IsAuthenticated_WithAuthenticatedUser_ReturnsTrue()
    {
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "test") }, "Test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var service = new CurrentUserService(httpContextAccessor.Object);

        service.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WithNoHttpContext_ReturnsFalse()
    {
        var service = CreateService(null);

        service.IsAuthenticated.Should().BeFalse();
    }
}
