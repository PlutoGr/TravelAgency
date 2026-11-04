using System.Text;
using System.Text.Json;

namespace TravelAgency.Identity.Application.Features.Profile;

internal static class ManagerCardText
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }

    public static string NormalizeTag(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        var builder = new StringBuilder(trimmed.Length);
        var pendingSpace = false;
        foreach (var ch in trimmed)
        {
            if (char.IsWhiteSpace(ch))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }

    public static string? SerializeTags(IReadOnlyList<string> tags) =>
        tags.Count == 0 ? null : JsonSerializer.Serialize(tags, SerializerOptions);

    public static IReadOnlyList<string> ReadTags(string? tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson))
            return [];

        return JsonSerializer.Deserialize<string[]>(tagsJson, SerializerOptions) ?? [];
    }
}
