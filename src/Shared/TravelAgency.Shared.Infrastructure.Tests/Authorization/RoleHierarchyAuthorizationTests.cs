using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Shared.Contracts.Authorization;
using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Shared.Infrastructure.Tests.Authorization;

public class RoleHierarchyAuthorizationTests
{
    public static TheoryData<string, string, bool> PolicyCases { get; } = new()
    {
        { AppRoles.Admin, AuthPolicies.RequireManager, true },
        { AppRoles.Manager, AuthPolicies.RequireManager, true },
        { AppRoles.Client, AuthPolicies.RequireManager, false },
        { AppRoles.Admin, AuthPolicies.RequireManagerOrAdmin, true },
        { AppRoles.Manager, AuthPolicies.RequireManagerOrAdmin, true },
        { AppRoles.Client, AuthPolicies.RequireManagerOrAdmin, false },
        { AppRoles.Admin, AuthPolicies.RequireAdmin, true },
        { AppRoles.Manager, AuthPolicies.RequireAdmin, false },
        { AppRoles.Client, AuthPolicies.RequireAdmin, false },
        { AppRoles.Client, AuthPolicies.RequireAuthenticated, true },
        { AppRoles.Manager, AuthPolicies.RequireAuthenticated, true },
        { AppRoles.Admin, AuthPolicies.RequireAuthenticated, true },
    };

    [Theory]
    [MemberData(nameof(PolicyCases))]
    public async Task Policy_FollowsRoleHierarchy(string role, string policy, bool allowed)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationBuilder().AddRoleHierarchyPolicies();
        await using var provider = services.BuildServiceProvider();
        var auth = provider.GetRequiredService<IAuthorizationService>();

        var result = await auth.AuthorizeAsync(Principal(role), policy);

        result.Succeeded.Should().Be(allowed);
    }

    [Theory]
    [InlineData(AppRoles.Admin, AppRoles.Manager, true)]
    [InlineData(AppRoles.Manager, AppRoles.Manager, true)]
    [InlineData(AppRoles.Client, AppRoles.Manager, false)]
    [InlineData(AppRoles.Manager, AppRoles.Admin, false)]
    [InlineData(null, AppRoles.Manager, false)]
    public void IsAtLeast_ComparesRanks(string? role, string minimum, bool expected)
    {
        RoleHierarchy.IsAtLeast(role, minimum).Should().Be(expected);
    }

    private static ClaimsPrincipal Principal(string role) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], authenticationType: "Test"));
}
