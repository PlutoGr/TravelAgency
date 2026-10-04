namespace TravelAgency.Catalog.Application.Exceptions;

public sealed class TourImageRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
