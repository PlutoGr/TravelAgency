using System.Text.Json.Serialization;

namespace TravelAgency.Identity.Application.DTOs;

public sealed record SocialLinkDto(string Label, string Url);

public sealed record SocialLinkRequest(string? Label, string? Url);

public sealed record UpdateSiteSettingsRequest(
    string? SiteName,
    string? SupportPhone,
    string? ContactEmail,
    string? OfficeAddress,
    string? WorkingHours,
    IReadOnlyList<SocialLinkRequest>? SocialLinks,
    string? BannerText,
    bool BannerEnabled);

public sealed record SiteSettingsPublicDto(
    string SiteName,
    string? SupportPhone,
    string? ContactEmail,
    string? OfficeAddress,
    string? WorkingHours,
    IReadOnlyList<SocialLinkDto> SocialLinks,
    string? BannerText,
    bool BannerEnabled);

public sealed record SiteSettingsAdminDto(
    string SiteName,
    string? SupportPhone,
    string? ContactEmail,
    string? OfficeAddress,
    string? WorkingHours,
    IReadOnlyList<SocialLinkDto> SocialLinks,
    string? BannerText,
    bool BannerEnabled,
    DateTimeOffset UpdatedAt,
    Guid? UpdatedByUserId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Message);

public sealed record SiteSettingsAuditItemDto(
    string FieldName,
    string? OldValue,
    string? NewValue,
    Guid ChangedByUserId,
    DateTimeOffset ChangedAt);

public static class SiteSettingsText
{
    public const string SavedNotice =
        "Настройки сохранены. Изменения на сайте появятся в течение минуты.";
}
