namespace TravelAgency.Catalog.Application.Exceptions;

public sealed class PreconditionRequiredException(string message) : Exception(message);
