using System.Text.Json;
using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.Application.Features.SiteSettings;

internal static class SocialLinksCodec
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Write(IReadOnlyList<SocialLinkDto> links) =>
        JsonSerializer.Serialize(links, Options);

    public static IReadOnlyList<SocialLinkDto> Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var items = JsonSerializer.Deserialize<List<SocialLinkDto>>(json, Options);
            if (items is null)
                return [];

            return items
                .Where(item => !string.IsNullOrWhiteSpace(item.Label) && !string.IsNullOrWhiteSpace(item.Url))
                .Select(item => new SocialLinkDto(item.Label.Trim(), item.Url.Trim()))
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string Canonical(string? json) => Write(Read(json));
}
