namespace TravelAgency.Chat.Domain.Exceptions;

public class ChatDomainException : Exception
{
    public ChatDomainException(string message) : base(message) { }

    public ChatDomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
