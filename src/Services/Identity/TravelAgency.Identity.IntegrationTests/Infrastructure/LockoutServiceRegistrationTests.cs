using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Infrastructure.Services;

namespace TravelAgency.Identity.IntegrationTests.Infrastructure;

/// <summary>
/// AUDIT-002: Verifies fallback to InMemoryLockoutService when Redis connection string is empty.
/// CustomWebApplicationFactory configures ConnectionStrings:Redis = string.Empty.
/// </summary>
public class LockoutServiceRegistrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LockoutServiceRegistrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void WhenRedisConnectionEmpty_ResolvesInMemoryLockoutService()
    {
        using var scope = _factory.Services.CreateScope();
        var lockoutService = scope.ServiceProvider.GetRequiredService<ILockoutService>();

        lockoutService.Should().BeOfType<InMemoryLockoutService>();
    }
}
