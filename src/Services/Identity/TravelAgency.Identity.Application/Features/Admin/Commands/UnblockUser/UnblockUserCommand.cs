using MediatR;
using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.Application.Features.Admin.Commands.UnblockUser;

public sealed record UnblockUserCommand(Guid UserId) : IRequest<AdminUserMutationResponse>;

public sealed class UnblockUserCommandHandler(UserAdminMutator mutator)
    : IRequestHandler<UnblockUserCommand, AdminUserMutationResponse>
{
    public Task<AdminUserMutationResponse> Handle(UnblockUserCommand command, CancellationToken cancellationToken) =>
        mutator.UnblockAsync(command.UserId, cancellationToken);
}
