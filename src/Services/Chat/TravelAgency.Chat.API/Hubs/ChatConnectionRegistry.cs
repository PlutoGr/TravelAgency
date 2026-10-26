using System.Collections.Concurrent;

namespace TravelAgency.Chat.API.Hubs;

public sealed class ChatConnectionRegistry
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, Action>> _aborts = new();

    public void Register(Guid userId, string connectionId, Action abort)
    {
        var connections = _aborts.GetOrAdd(userId, _ => new ConcurrentDictionary<string, Action>());
        connections[connectionId] = abort;
    }

    public void Unregister(Guid userId, string connectionId)
    {
        if (!_aborts.TryGetValue(userId, out var connections))
            return;

        connections.TryRemove(connectionId, out _);
        if (connections.IsEmpty)
            _aborts.TryRemove(userId, out _);
    }

    public void Abort(Guid userId)
    {
        if (!_aborts.TryGetValue(userId, out var connections))
            return;

        foreach (var abort in connections.Values.ToArray())
        {
            try
            {
                abort();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}
