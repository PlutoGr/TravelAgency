using System.Globalization;
using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Application.Features.Tours.Manage;

public static class TourEtag
{
    public static string Format(long version) => $"\"{version.ToString(CultureInfo.InvariantCulture)}\"";

    public static void EnsureCurrent(Tour tour, string? ifMatch)
    {
        var expected = Parse(ifMatch);
        if (tour.Version != expected)
            throw new ConflictException("The tour was changed. Send the current If-Match value.");
    }

    public static long Parse(string? ifMatch)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
            throw new PreconditionRequiredException("If-Match header is required.");

        var value = ifMatch.Trim();
        if (value.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
            value = value[2..].Trim();

        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            value = value[1..^1];

        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var version) || version < 0)
            throw new PreconditionRequiredException("If-Match header is not a tour version.");

        return version;
    }
}
