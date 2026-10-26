namespace TravelAgency.Identity.Domain.Entities;

/// <summary>
/// Единственная строка публичных настроек сайта. Секреты (SMTP, ключи) сюда не записываются.
/// </summary>
public sealed class SiteSettings
{
    public const int SingletonId = 1;
    public const string DefaultSiteName = "TravelAgency";
    public static readonly DateTimeOffset SeedUpdatedAt = new(2026, 10, 26, 0, 0, 0, TimeSpan.Zero);

    public int Id { get; set; } = SingletonId;
    public string SiteName { get; set; } = DefaultSiteName;
    public string? SupportPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? OfficeAddress { get; set; }
    public string? WorkingHours { get; set; }
    public string SocialLinksJson { get; set; } = "[]";
    public string? BannerText { get; set; }
    public bool BannerEnabled { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = SeedUpdatedAt;
    public Guid? UpdatedByUserId { get; set; }

    public static SiteSettings CreateDefault(DateTimeOffset now) => new()
    {
        Id = SingletonId,
        SiteName = DefaultSiteName,
        SocialLinksJson = "[]",
        BannerEnabled = false,
        UpdatedAt = now,
        UpdatedByUserId = null
    };
}
