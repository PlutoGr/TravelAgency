namespace TravelAgency.Identity.Domain.Entities;

/// <summary>
/// Одна строка журнала: какое поле настроек изменилось, с какого значения и кем.
/// </summary>
public sealed class SiteSettingsAudit
{
    public long Id { get; set; }
    public string FieldName { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
