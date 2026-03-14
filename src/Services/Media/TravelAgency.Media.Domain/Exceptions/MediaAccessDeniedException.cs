namespace TravelAgency.Media.Domain.Exceptions;

public sealed class MediaAccessDeniedException : Exception
{
    public MediaAccessDeniedException(Guid id)
        : base($"Access to media file '{id}' is denied.") { }
}
