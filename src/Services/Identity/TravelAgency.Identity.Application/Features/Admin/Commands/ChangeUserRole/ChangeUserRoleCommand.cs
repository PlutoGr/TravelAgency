using FluentValidation;
using MediatR;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Identity.Application.Features.Admin.Commands.ChangeUserRole;

public sealed record ChangeUserRoleCommand(Guid UserId, string? Role) : IRequest<AdminUserMutationResponse>;

public sealed class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.Role)
            .Must(role => role is AppRoles.Client or AppRoles.Manager or AppRoles.Admin)
            .WithMessage(UserAdminText.UnknownRole);
    }
}

public sealed class ChangeUserRoleCommandHandler(UserAdminMutator mutator)
    : IRequestHandler<ChangeUserRoleCommand, AdminUserMutationResponse>
{
    public Task<AdminUserMutationResponse> Handle(ChangeUserRoleCommand command, CancellationToken cancellationToken) =>
        mutator.ChangeRoleAsync(command.UserId, command.Role, cancellationToken);
}
