using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Identity.Application.Features.Admin;

public sealed class UserAdminMutator(
    ICurrentUserService currentUser,
    IUserRepository userRepository,
    IUserAdminAuditRepository auditRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUserAdminLock adminLock,
    IUnitOfWork unitOfWork,
    ISessionRevocationStore sessionRevocationStore,
    IOptions<JwtSettings> jwtSettings,
    ILogger<UserAdminMutator> logger)
{
    private const int SessionPublishAttempts = 3;
    private static readonly TimeSpan SessionPublishRetryBaseDelay = TimeSpan.FromMilliseconds(50);

    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public Task<AdminUserMutationResponse> ChangeRoleAsync(Guid targetId, string? role, CancellationToken cancellationToken)
    {
        EnsureSignedIn();
        if (string.IsNullOrWhiteSpace(role) || !TryParseRole(role, out var newRole))
            throw new BadRequestException(UserAdminText.UnknownRole);

        return ExecuteAsync(targetId, ct => ChangeRoleCore(targetId, newRole, ct), cancellationToken);
    }

    public Task<AdminUserMutationResponse> BlockAsync(Guid targetId, string? reason, CancellationToken cancellationToken)
    {
        EnsureSignedIn();
        var trimmed = reason?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            throw new BadRequestException(UserAdminText.ReasonRequired);
        if (trimmed.Length > User.MaxBlockReasonLength)
            throw new BadRequestException(UserAdminText.ReasonTooLong);

        return ExecuteAsync(targetId, ct => BlockCore(targetId, trimmed, ct), cancellationToken);
    }

    public Task<AdminUserMutationResponse> UnblockAsync(Guid targetId, CancellationToken cancellationToken)
    {
        EnsureSignedIn();
        return ExecuteAsync(targetId, ct => UnblockCore(targetId, ct), cancellationToken);
    }

    private async Task<AdminUserMutationResponse> ExecuteAsync(
        Guid targetId,
        Func<CancellationToken, Task<MutationOutcome>> action,
        CancellationToken cancellationToken)
    {
        var outcome = await adminLock.ExecuteAsync(action, cancellationToken);
        if (outcome.Publish)
            await PublishAsync(targetId, outcome.Version, outcome.Blocked, cancellationToken);

        return new AdminUserMutationResponse(outcome.User, outcome.Message);
    }

    private async Task<MutationOutcome> ChangeRoleCore(Guid targetId, UserRole newRole, CancellationToken cancellationToken)
    {
        var target = await RequireTargetAsync(targetId, cancellationToken);
        if (target.Role == newRole)
            return MutationOutcome.Quiet(AdminUserMapper.ToDto(target), UserAdminText.RoleUnchanged);

        if (currentUser.UserId == target.Id && target.Role.IsDowngradeTo(newRole))
            throw new BadRequestException(UserAdminText.SelfDowngrade);

        if (RemovesActiveAdmin(target, newRole, blocking: false)
            && await userRepository.CountActiveAdminsAsync(cancellationToken) <= 1)
            throw new BadRequestException(UserAdminText.LastAdmin);

        await EnsureActorStillAdminAsync(cancellationToken);

        var oldRole = target.Role;
        var downgrade = oldRole.IsDowngradeTo(newRole);
        var now = DateTime.UtcNow;
        target.ChangeRole(newRole);
        if (downgrade)
            target.RevokeSessions();

        auditRepository.Stage(UserAdminAudit.RoleChanged(
            target.Id,
            currentUser.UserId,
            oldRole.ToRoleString(),
            newRole.ToRoleString(),
            now));

        if (downgrade)
            await refreshTokenRepository.RevokeAllByUserIdAsync(target.Id, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new MutationOutcome(
            AdminUserMapper.ToDto(target),
            UserAdminText.RoleChanged,
            Publish: downgrade,
            target.SessionVersion,
            target.IsBlocked);
    }

    private async Task<MutationOutcome> BlockCore(Guid targetId, string reason, CancellationToken cancellationToken)
    {
        var target = await RequireTargetAsync(targetId, cancellationToken);
        if (currentUser.UserId == target.Id)
            throw new BadRequestException(UserAdminText.SelfBlock);
        if (target.IsBlocked)
            throw new ConflictException(UserAdminText.AlreadyBlocked);

        if (RemovesActiveAdmin(target, newRole: null, blocking: true)
            && await userRepository.CountActiveAdminsAsync(cancellationToken) <= 1)
            throw new BadRequestException(UserAdminText.LastAdmin);

        await EnsureActorStillAdminAsync(cancellationToken);

        var now = DateTime.UtcNow;
        target.Block(reason, currentUser.UserId, now);
        auditRepository.Stage(UserAdminAudit.Blocked(target.Id, currentUser.UserId, target.BlockReason!, now));
        await refreshTokenRepository.RevokeAllByUserIdAsync(target.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new MutationOutcome(
            AdminUserMapper.ToDto(target),
            UserAdminText.UserBlocked,
            Publish: true,
            target.SessionVersion,
            Blocked: true);
    }

    private async Task<MutationOutcome> UnblockCore(Guid targetId, CancellationToken cancellationToken)
    {
        var target = await RequireTargetAsync(targetId, cancellationToken);
        if (!target.IsBlocked)
            throw new ConflictException(UserAdminText.NotBlocked);

        await EnsureActorStillAdminAsync(cancellationToken);

        var now = DateTime.UtcNow;
        target.Unblock(now);
        auditRepository.Stage(UserAdminAudit.Unblocked(target.Id, currentUser.UserId, now));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new MutationOutcome(
            AdminUserMapper.ToDto(target),
            UserAdminText.UserUnblocked,
            Publish: true,
            target.SessionVersion,
            Blocked: false);
    }

    private async Task<User> RequireTargetAsync(Guid targetId, CancellationToken cancellationToken)
    {
        var target = await userRepository.GetByIdAsync(targetId, cancellationToken);
        if (target is null)
            throw new NotFoundException(UserAdminText.UserNotFound);
        return target;
    }

    private async Task EnsureActorStillAdminAsync(CancellationToken cancellationToken)
    {
        var actor = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken);
        if (actor is null || actor.IsBlocked || actor.Role != UserRole.Admin)
            throw new ForbiddenException(UserAdminText.ActorNotAdmin);
    }

    private void EnsureSignedIn()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
            throw new UnauthorizedException("User is not authenticated.");
    }

    private async Task PublishAsync(Guid userId, long version, bool blocked, CancellationToken cancellationToken)
    {
        var ttl = TimeSpan.FromMinutes(Math.Max(1, _jwtSettings.AccessTokenExpirationMinutes) + 2);
        for (var attempt = 1; attempt <= SessionPublishAttempts; attempt++)
        {
            try
            {
                await sessionRevocationStore.SetAsync(userId, version, blocked, ttl, cancellationToken);
                await sessionRevocationStore.PublishAsync(userId, cancellationToken);
                return;
            }
            catch (Exception exception)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogWarning(exception, "Не удалось записать отзыв сессии {UserId}", userId);
                    return;
                }

                if (attempt == SessionPublishAttempts)
                {
                    logger.LogError(
                        exception,
                        "Не удалось записать отзыв сессии {UserId} после {Attempts} попыток",
                        userId,
                        attempt);
                    return;
                }

                await Task.Delay(SessionPublishRetryBaseDelay * attempt, CancellationToken.None);
            }
        }
    }

    private static bool RemovesActiveAdmin(User target, UserRole? newRole, bool blocking)
    {
        if (target.Role != UserRole.Admin || target.IsBlocked)
            return false;

        return blocking || (newRole is not null && newRole != UserRole.Admin);
    }

    private static bool TryParseRole(string role, out UserRole parsed)
    {
        try
        {
            parsed = UserRoleExtensions.FromString(role.Trim());
            return true;
        }
        catch (ArgumentException)
        {
            parsed = default;
            return false;
        }
    }

    private sealed record MutationOutcome(
        AdminUserDto User,
        string Message,
        bool Publish,
        long Version,
        bool Blocked)
    {
        public static MutationOutcome Quiet(AdminUserDto user, string message) =>
            new(user, message, false, 0, false);
    }
}
