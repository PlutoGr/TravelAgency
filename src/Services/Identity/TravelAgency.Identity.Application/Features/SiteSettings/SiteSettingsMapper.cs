using TravelAgency.Identity.Application.DTOs;
using SiteSettingsRow = TravelAgency.Identity.Domain.Entities.SiteSettings;

namespace TravelAgency.Identity.Application.Features.SiteSettings;

internal static class SiteSettingsMapper
{
    public static SiteSettingsPublicDto ToPublic(SiteSettingsRow? row)
    {
        if (row is null || string.IsNullOrWhiteSpace(row.SiteName))
        {
            return new SiteSettingsPublicDto(
                SiteSettingsRow.DefaultSiteName,
                EmptyToNull(row?.SupportPhone),
                EmptyToNull(row?.ContactEmail),
                EmptyToNull(row?.OfficeAddress),
                EmptyToNull(row?.WorkingHours),
                row is null ? [] : SocialLinksCodec.Read(row.SocialLinksJson),
                EmptyToNull(row?.BannerText),
                row?.BannerEnabled ?? false);
        }

        return new SiteSettingsPublicDto(
            row.SiteName.Trim(),
            EmptyToNull(row.SupportPhone),
            EmptyToNull(row.ContactEmail),
            EmptyToNull(row.OfficeAddress),
            EmptyToNull(row.WorkingHours),
            SocialLinksCodec.Read(row.SocialLinksJson),
            EmptyToNull(row.BannerText),
            row.BannerEnabled);
    }

    public static SiteSettingsAdminDto ToAdmin(SiteSettingsRow row, string? message = null)
    {
        var publicDto = ToPublic(row);
        return new SiteSettingsAdminDto(
            publicDto.SiteName,
            publicDto.SupportPhone,
            publicDto.ContactEmail,
            publicDto.OfficeAddress,
            publicDto.WorkingHours,
            publicDto.SocialLinks,
            publicDto.BannerText,
            publicDto.BannerEnabled,
            row.UpdatedAt,
            row.UpdatedByUserId,
            message);
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
