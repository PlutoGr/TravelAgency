using Microsoft.AspNetCore.Authorization;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Shared.Infrastructure.Extensions;

public static class RoleHierarchyAuthorizationExtensions
{
    public static AuthorizationBuilder AddRoleHierarchyPolicies(this AuthorizationBuilder builder)
    {
        builder.AddPolicy(AuthPolicies.RequireAuthenticated, policy =>
            policy.RequireRole(RoleHierarchy.RolesAtLeast(AppRoles.Client)));
        builder.AddPolicy(AuthPolicies.RequireManager, policy =>
            policy.RequireRole(RoleHierarchy.RolesAtLeast(AppRoles.Manager)));
        builder.AddPolicy(AuthPolicies.RequireAdmin, policy =>
            policy.RequireRole(RoleHierarchy.RolesAtLeast(AppRoles.Admin)));
        builder.AddPolicy(AuthPolicies.RequireManagerOrAdmin, policy =>
            policy.RequireRole(RoleHierarchy.RolesAtLeast(AppRoles.Manager)));

        return builder;
    }
}
