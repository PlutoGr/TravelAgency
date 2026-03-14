namespace TravelAgency.Identity.Application.Exceptions;

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message, 401) { }
}
