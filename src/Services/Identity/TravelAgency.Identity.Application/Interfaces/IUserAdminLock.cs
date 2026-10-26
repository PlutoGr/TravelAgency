namespace TravelAgency.Identity.Application.Interfaces;

public interface IUserAdminLock
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
}
