namespace TravelAgency.Shared.Contracts.Authorization;

public interface ISessionRevocationStore
{
    Task SetAsync(Guid userId, long version, bool blocked, TimeSpan ttl, CancellationToken cancellationToken = default);

    Task<SessionRevocationEntry?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task PublishAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface ISessionRevocationListener
{
    Task ListenAsync(Action<Guid> onRevoked, CancellationToken cancellationToken);
}
