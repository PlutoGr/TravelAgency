using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Infrastructure.Services;

namespace TravelAgency.Identity.UnitTests.Infrastructure;

/// <summary>
/// Covers FIX-004: UserId now throws the application-level <see cref="UnauthorizedException"/>
/// instead of <see cref="UnauthorizedAccessException"/> for both missing and malformed claims.
/// </summary>
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
    public void UserId_WhenNameIdentifierClaimIsValidGuid_ReturnsCorrectGuid()
    {
        var expectedId = Guid.NewGuid();
        var service = CreateService([new Claim(ClaimTypes.NameIdentifier, expectedId.ToString())]);

        service.UserId.Should().Be(expectedId);
    }

    [Fact]
    public void UserId_WhenSubClaimIsValidGuid_ReturnsCorrectGuid()
    {
        var expectedId = Guid.NewGuid();
        var service = CreateService([new Claim(JwtRegisteredClaimNames.Sub, expectedId.ToString())]);

        service.UserId.Should().Be(expectedId);
    }

    [Fact]
    public void UserId_WhenClaimValueIsMalformedGuid_ThrowsUnauthorizedException()
    {
        var service = CreateService([new Claim(ClaimTypes.NameIdentifier, "not-a-guid")]);

        var act = () => service.UserId;

        act.Should().Throw<UnauthorizedException>()
            .WithMessage("Invalid user identity claim in token.");
    }

    [Fact]
    public void UserId_WhenNoIdentifierClaimPresent_ThrowsUnauthorizedException()
    {
        var service = CreateService(Enumerable.Empty<Claim>());

        var act = () => service.UserId;

        act.Should().Throw<UnauthorizedException>()
            .WithMessage("User is not authenticated.");
    }

    [Fact]
    public void UserId_WhenHttpContextIsNull_ThrowsUnauthorizedException()
    {
        var service = CreateService(null);

        var act = () => service.UserId;

        act.Should().Throw<UnauthorizedException>()
            .WithMessage("User is not authenticated.");
    }
}
