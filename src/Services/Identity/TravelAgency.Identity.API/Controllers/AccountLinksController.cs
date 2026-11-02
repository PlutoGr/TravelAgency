using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Features.Auth;
using TravelAgency.Identity.Application.Features.Auth.Account;
using TravelAgency.Shared.Contracts.Abstractions;

namespace TravelAgency.Identity.API.Controllers;

[ApiController]
[Route("identity")]
public class AccountLinksController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    [HttpPost("password-resets/request")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RequestReset([FromBody] PasswordResetRequestBody body, CancellationToken cancellationToken)
    {
        NoStore();
        var result = await mediator.Send(new RequestPasswordResetCommand(body.Email ?? ""), cancellationToken);
        return Ok(new { message = result.Message });
    }

    [HttpPost("password-resets/inspect")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> InspectReset([FromBody] TokenBody body, CancellationToken cancellationToken)
    {
        NoStore();
        var result = await mediator.Send(new InspectPasswordResetCommand(body.Token), cancellationToken);
        return Ok(Outcome(result));
    }

    [HttpPost("password-resets")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> CompleteReset([FromBody] CompletePasswordResetBody body, CancellationToken cancellationToken)
    {
        NoStore();
        var result = await mediator.Send(
            new CompletePasswordResetCommand(body.Token, body.Password, body.PasswordConfirm),
            cancellationToken);
        return Ok(Outcome(result));
    }

    [Authorize]
    [HttpPost("me/password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordBody body, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
            return Unauthorized();

        NoStore();
        AuthTokensDto tokens = await mediator.Send(
            new ChangePasswordCommand(body.CurrentPassword, body.NewPassword, body.PasswordConfirm),
            cancellationToken);
        return Ok(tokens);
    }

    [Authorize]
    [HttpPost("me/email")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailBody body, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
            return Unauthorized();

        NoStore();
        var result = await mediator.Send(new RequestEmailChangeCommand(body.Email, body.CurrentPassword), cancellationToken);
        return Ok(new { message = result.Message });
    }

    [HttpPost("email-confirmations")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailBody body, CancellationToken cancellationToken)
    {
        NoStore();
        var result = await mediator.Send(new ConfirmEmailChangeCommand(body.Token, body.RefreshToken), cancellationToken);
        return Ok(Outcome(result));
    }

    [HttpPost("email-cancellations")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> CancelEmail([FromBody] TokenBody body, CancellationToken cancellationToken)
    {
        NoStore();
        var result = await mediator.Send(new CancelEmailChangeCommand(body.Token), cancellationToken);
        return Ok(Outcome(result));
    }

    private void NoStore() => Response.Headers.CacheControl = "no-store";

    private static object Outcome(AccountOutcome result) =>
        result.Session is null
            ? new { outcome = result.Outcome }
            : new { outcome = result.Outcome, session = result.Session };
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PasswordResetRequestBody(string? Email);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TokenBody(string? Token);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CompletePasswordResetBody(string? Token, string? Password, string? PasswordConfirm);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ChangePasswordBody(string? CurrentPassword, string? NewPassword, string? PasswordConfirm);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ChangeEmailBody(string? Email, string? CurrentPassword);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfirmEmailBody(string? Token, string? RefreshToken);
