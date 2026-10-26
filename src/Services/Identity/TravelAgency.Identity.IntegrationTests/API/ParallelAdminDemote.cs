using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Infrastructure.Services;

namespace TravelAgency.Identity.IntegrationTests.API;

/// <summary>
/// Два параллельных запроса на снятие роли дожидаются друг друга уже после проверки сессии,
/// но до записи в базу. Иначе один успевает отозвать сессию второго, и вместо
/// «хотя бы один администратор» приходит 401.
/// </summary>
internal static class ParallelAdminDemote
{
    private static readonly object Sync = new();
    private static int _arrived;
    private static TaskCompletionSource _gate = Create();

    public static void Reset()
    {
        lock (Sync)
        {
            _arrived = 0;
            if (!_gate.Task.IsCompleted)
                _gate.TrySetCanceled();
            _gate = Create();
        }
    }

    public static async Task ArriveAsync(CancellationToken cancellationToken)
    {
        Task wait;
        lock (Sync)
        {
            _arrived++;
            if (_arrived >= 2)
                _gate.TrySetResult();
            wait = _gate.Task;
        }

        await wait.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
    }

    private static TaskCompletionSource Create() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}

internal sealed class DemoteRaceLock(UserAdminLock inner, IHttpContextAccessor httpContext) : IUserAdminLock
{
    public const string HeaderName = "X-Test-Admin-Race";

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        if (httpContext.HttpContext?.Request.Headers.ContainsKey(HeaderName) == true)
            await ParallelAdminDemote.ArriveAsync(cancellationToken);

        return await inner.ExecuteAsync(action, cancellationToken);
    }
}
