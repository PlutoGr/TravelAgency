namespace TravelAgency.Identity.Application.Features.Admin;

public static class UserSearch
{
    public static string LikePattern(string term, bool foldCase = true)
    {
        var escaped = term
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        if (foldCase)
            escaped = escaped.ToLowerInvariant();
        return "%" + escaped + "%";
    }
}
