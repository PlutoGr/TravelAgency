namespace TravelAgency.Media.Domain.Exceptions;

public sealed class MediaNotFoundException : Exception
{
    public MediaNotFoundException(Guid id)
        : base($"Media file '{id}' was not found.") { }
}
