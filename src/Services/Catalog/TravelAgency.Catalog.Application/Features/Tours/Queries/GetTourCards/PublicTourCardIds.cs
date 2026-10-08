using TravelAgency.Catalog.Domain;

namespace TravelAgency.Catalog.Application.Features.Tours.Queries.GetTourCards;

public static class PublicTourCardIds
{
    public static bool TryParse(IReadOnlyList<string>? raw, out IReadOnlyList<Guid> ids, out string? error)
    {
        ids = [];
        error = null;
        if (raw is null || raw.Count == 0)
            return true;

        var parsed = new List<Guid>();
        var seen = new HashSet<Guid>();
        foreach (var part in raw)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;

            foreach (var token in part.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!Guid.TryParse(token, out var id))
                {
                    error = "ids must be guids.";
                    ids = [];
                    return false;
                }

                if (seen.Add(id))
                    parsed.Add(id);
            }
        }

        if (parsed.Count > PublicCatalogLimits.MaxCardIds)
        {
            error = "ids accepts at most 50 values.";
            ids = [];
            return false;
        }

        ids = parsed;
        return true;
    }
}
