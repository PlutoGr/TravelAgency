using FluentValidation;
using MediatR;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Domain.Entities;

namespace TravelAgency.Identity.Application.Features.Admin.Commands.BlockUser;

public sealed record BlockUserCommand(Guid UserId, string? Reason) : IRequest<AdminUserMutationResponse>;

public sealed class BlockUserCommandValidator : AbstractValidator<BlockUserCommand>
{
    public BlockUserCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage(UserAdminText.ReasonRequired)
            .MaximumLength(User.MaxBlockReasonLength)
            .WithMessage(UserAdminText.ReasonTooLong);
    }
}

public sealed class BlockUserCommandHandler(UserAdminMutator mutator)
    : IRequestHandler<BlockUserCommand, AdminUserMutationResponse>
{
    public Task<AdminUserMutationResponse> Handle(BlockUserCommand command, CancellationToken cancellationToken) =>
        mutator.BlockAsync(command.UserId, command.Reason, cancellationToken);
}
