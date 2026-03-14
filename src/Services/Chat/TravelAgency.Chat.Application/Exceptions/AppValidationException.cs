namespace TravelAgency.Chat.Application.Exceptions;

public sealed class AppValidationException(Dictionary<string, string[]> errors)
    : AppException("One or more validation errors occurred.", 400)
{
    public Dictionary<string, string[]> Errors { get; } = errors;
}
