namespace TravelAgency.Identity.Domain.Entities;

public sealed class UserAdminAudit
{
    public const string ActionRoleChanged = "RoleChanged";
    public const string ActionBlocked = "Blocked";
    public const string ActionUnblocked = "Unblocked";

    public Guid Id { get; private set; }
    public Guid TargetUserId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string Action { get; private set; } = null!;
    public string? OldRole { get; private set; }
    public string? NewRole { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private UserAdminAudit() { }

    public static UserAdminAudit RoleChanged(
        Guid targetUserId,
        Guid actorUserId,
        string oldRole,
        string newRole,
        DateTime createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TargetUserId = targetUserId,
            ActorUserId = actorUserId,
            Action = ActionRoleChanged,
            OldRole = oldRole,
            NewRole = newRole,
            CreatedAt = createdAt
        };

    public static UserAdminAudit Blocked(
        Guid targetUserId,
        Guid actorUserId,
        string reason,
        DateTime createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TargetUserId = targetUserId,
            ActorUserId = actorUserId,
            Action = ActionBlocked,
            Reason = reason,
            CreatedAt = createdAt
        };

    public static UserAdminAudit Unblocked(
        Guid targetUserId,
        Guid actorUserId,
        DateTime createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TargetUserId = targetUserId,
            ActorUserId = actorUserId,
            Action = ActionUnblocked,
            CreatedAt = createdAt
        };
}
