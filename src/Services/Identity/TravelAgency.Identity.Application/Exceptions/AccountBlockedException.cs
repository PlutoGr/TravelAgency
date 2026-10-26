namespace TravelAgency.Identity.Application.Exceptions;

public sealed class AccountBlockedException : AppException
{
    public const string ErrorCode = "AccountBlocked";

    public AccountBlockedException(string message) : base(message, 401) { }
}
