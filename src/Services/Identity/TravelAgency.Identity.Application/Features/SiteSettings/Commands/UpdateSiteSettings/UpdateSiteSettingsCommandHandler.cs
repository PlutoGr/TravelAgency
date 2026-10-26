using MediatR;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;
using SiteSettingsRow = TravelAgency.Identity.Domain.Entities.SiteSettings;
using TravelAgency.Shared.Contracts.Abstractions;

namespace TravelAgency.Identity.Application.Features.SiteSettings.Commands.UpdateSiteSettings;

public sealed class UpdateSiteSettingsCommandHandler(
    ICurrentUserService currentUserService,
    ISiteSettingsRepository repository,
    IUnitOfWork unitOfWork,
    ISiteSettingsPublicCache cache) : IRequestHandler<UpdateSiteSettingsCommand, SiteSettingsAdminDto>
{
    public async Task<SiteSettingsAdminDto> Handle(UpdateSiteSettingsCommand command, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId == Guid.Empty)
            throw new UnauthorizedException("User is not authenticated.");

        var next = Normalize(command.Request);
        var row = await repository.GetAsync(cancellationToken);
        var isNew = row is null;
        row ??= SiteSettingsRow.CreateDefault(DateTimeOffset.UtcNow);

        var changes = Diff(row, next);
        if (changes.Count == 0 && !isNew)
            return SiteSettingsMapper.ToAdmin(row, SiteSettingsText.SavedNotice);

        var now = DateTimeOffset.UtcNow;
        Apply(row, next, currentUserService.UserId, now);

        if (isNew)
            await repository.AddAsync(row, cancellationToken);

        if (changes.Count > 0)
        {
            var audit = changes
                .Select(change => new SiteSettingsAudit
                {
                    FieldName = change.Field,
                    OldValue = change.OldValue,
                    NewValue = change.NewValue,
                    ChangedByUserId = currentUserService.UserId,
                    ChangedAt = now
                })
                .ToList();
            await repository.AddAuditAsync(audit, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(cancellationToken);
        return SiteSettingsMapper.ToAdmin(row, SiteSettingsText.SavedNotice);
    }

    internal static NormalizedSiteSettings Normalize(UpdateSiteSettingsRequest request)
    {
        var links = (request.SocialLinks ?? [])
            .Select(link => new SocialLinkDto(link.Label!.Trim(), link.Url!.Trim()))
            .ToList();

        return new NormalizedSiteSettings(
            request.SiteName!.Trim(),
            EmptyToNull(request.SupportPhone),
            EmptyToNull(request.ContactEmail),
            EmptyToNull(request.OfficeAddress),
            EmptyToNull(request.WorkingHours),
            SocialLinksCodec.Write(links),
            EmptyToNull(request.BannerText),
            request.BannerEnabled);
    }

    private static List<FieldChange> Diff(SiteSettingsRow current, NormalizedSiteSettings next)
    {
        var changes = new List<FieldChange>();
        Add(changes, nameof(SiteSettingsRow.SiteName), current.SiteName, next.SiteName);
        Add(changes, nameof(SiteSettingsRow.SupportPhone), EmptyToNull(current.SupportPhone), next.SupportPhone);
        Add(changes, nameof(SiteSettingsRow.ContactEmail), EmptyToNull(current.ContactEmail), next.ContactEmail);
        Add(changes, nameof(SiteSettingsRow.OfficeAddress), EmptyToNull(current.OfficeAddress), next.OfficeAddress);
        Add(changes, nameof(SiteSettingsRow.WorkingHours), EmptyToNull(current.WorkingHours), next.WorkingHours);
        Add(changes, nameof(SiteSettingsRow.SocialLinksJson), SocialLinksCodec.Canonical(current.SocialLinksJson), next.SocialLinksJson);
        Add(changes, nameof(SiteSettingsRow.BannerText), EmptyToNull(current.BannerText), next.BannerText);
        Add(changes, nameof(SiteSettingsRow.BannerEnabled), BoolText(current.BannerEnabled), BoolText(next.BannerEnabled));
        return changes;
    }

    private static void Apply(SiteSettingsRow row, NormalizedSiteSettings next, Guid userId, DateTimeOffset now)
    {
        row.SiteName = next.SiteName;
        row.SupportPhone = next.SupportPhone;
        row.ContactEmail = next.ContactEmail;
        row.OfficeAddress = next.OfficeAddress;
        row.WorkingHours = next.WorkingHours;
        row.SocialLinksJson = next.SocialLinksJson;
        row.BannerText = next.BannerText;
        row.BannerEnabled = next.BannerEnabled;
        row.UpdatedAt = now;
        row.UpdatedByUserId = userId;
    }

    private static void Add(List<FieldChange> changes, string field, string? oldValue, string? newValue)
    {
        if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
            return;

        changes.Add(new FieldChange(field, oldValue, newValue));
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BoolText(bool value) => value ? "true" : "false";

    internal sealed record NormalizedSiteSettings(
        string SiteName,
        string? SupportPhone,
        string? ContactEmail,
        string? OfficeAddress,
        string? WorkingHours,
        string SocialLinksJson,
        string? BannerText,
        bool BannerEnabled);

    private sealed record FieldChange(string Field, string? OldValue, string? NewValue);
}
