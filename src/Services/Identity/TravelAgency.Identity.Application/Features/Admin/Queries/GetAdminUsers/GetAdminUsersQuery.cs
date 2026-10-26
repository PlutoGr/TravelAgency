using FluentValidation;
using MediatR;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Identity.Application.Features.Admin.Queries.GetAdminUsers;

public sealed record GetAdminUsersQuery(string? Query, string? Role, string? Status, int Page)
    : IRequest<AdminUserListDto>;

public sealed class GetAdminUsersQueryValidator : AbstractValidator<GetAdminUsersQuery>
{
    public GetAdminUsersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage(UserAdminText.InvalidPage);

        RuleFor(x => x.Query)
            .MaximumLength(UserAdminText.MaxQueryLength)
            .WithMessage(UserAdminText.QueryTooLong);

        RuleFor(x => x.Role)
            .Must(role => string.IsNullOrWhiteSpace(role)
                || role is AppRoles.Client or AppRoles.Manager or AppRoles.Admin)
            .WithMessage(UserAdminText.UnknownRole);

        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrWhiteSpace(status)
                || status.Equals(UserAdminText.ActiveStatus, StringComparison.OrdinalIgnoreCase)
                || status.Equals(UserAdminText.BlockedStatus, StringComparison.OrdinalIgnoreCase))
            .WithMessage(UserAdminText.UnknownStatus);
    }
}

public sealed class GetAdminUsersQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetAdminUsersQuery, AdminUserListDto>
{
    public async Task<AdminUserListDto> Handle(GetAdminUsersQuery query, CancellationToken cancellationToken)
    {
        UserRole? role = string.IsNullOrWhiteSpace(query.Role)
            ? null
            : UserRoleExtensions.FromString(query.Role);
        bool? blocked = string.IsNullOrWhiteSpace(query.Status)
            ? null
            : query.Status.Equals(UserAdminText.BlockedStatus, StringComparison.OrdinalIgnoreCase);

        var term = string.IsNullOrWhiteSpace(query.Query) ? null : query.Query.Trim();
        var (items, total) = await userRepository.SearchAsync(
            term,
            role,
            blocked,
            query.Page,
            UserAdminText.PageSize,
            cancellationToken);

        return new AdminUserListDto(
            items.Select(AdminUserMapper.ToDto).ToList(),
            total,
            query.Page,
            UserAdminText.PageSize);
    }
}
