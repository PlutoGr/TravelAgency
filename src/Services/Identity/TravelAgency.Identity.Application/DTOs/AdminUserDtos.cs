namespace TravelAgency.Identity.Application.DTOs;

public sealed record AdminUserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Role,
    string Status,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    DateTime? BlockedAt,
    string? BlockReason);

public sealed record AdminUserListDto(
    IReadOnlyList<AdminUserDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record UserAdminAuditItemDto(
    Guid Id,
    string Action,
    string? OldRole,
    string? NewRole,
    string? Reason,
    Guid ActorUserId,
    string ActorName,
    DateTime CreatedAt);

public sealed record AdminUserDetailsDto(
    AdminUserDto User,
    IReadOnlyList<UserAdminAuditItemDto> Audit);

public sealed record AdminUserMutationResponse(AdminUserDto User, string Message);

public sealed record ChangeUserRoleRequest(string? Role);

public sealed record BlockUserRequest(string? Reason);
