using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Features.Admin.Commands.BlockUser;
using TravelAgency.Identity.Application.Features.Admin.Commands.ChangeUserRole;
using TravelAgency.Identity.Application.Features.Admin.Commands.UnblockUser;
using TravelAgency.Identity.Application.Features.Admin.Queries.GetAdminUser;
using TravelAgency.Identity.Application.Features.Admin.Queries.GetAdminUsers;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Identity.API.Controllers;

[ApiController]
[Route("identity/admin/users")]
[Authorize(Policy = AuthPolicies.RequireAdmin)]
public class AdminUsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(AdminUserListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        NoStore();
        var result = await mediator.Send(new GetAdminUsersQuery(q, role, status, page), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminUserDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        NoStore();
        var result = await mediator.Send(new GetAdminUserQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/role")]
    [ProducesResponseType(typeof(AdminUserMutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeUserRoleRequest? request, CancellationToken cancellationToken)
    {
        NoStore();
        var result = await mediator.Send(new ChangeUserRoleCommand(id, request?.Role), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/block")]
    [ProducesResponseType(typeof(AdminUserMutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Block(Guid id, [FromBody] BlockUserRequest? request, CancellationToken cancellationToken)
    {
        NoStore();
        var result = await mediator.Send(new BlockUserCommand(id, request?.Reason), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/unblock")]
    [ProducesResponseType(typeof(AdminUserMutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Unblock(Guid id, CancellationToken cancellationToken)
    {
        NoStore();
        var result = await mediator.Send(new UnblockUserCommand(id), cancellationToken);
        return Ok(result);
    }

    private void NoStore() => Response.Headers.CacheControl = "no-store";
}
