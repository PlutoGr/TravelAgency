namespace TravelAgency.Chat.Application.Exceptions;

public sealed class ForbiddenException(string message) : AppException(message, 403);
