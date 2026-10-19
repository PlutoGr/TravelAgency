namespace TravelAgency.Catalog.Application.Time;

/// <summary>
/// Пояс каталога для «сегодня» и запасной пояс города, которого нет в таблице.
/// </summary>
public sealed class CatalogOptions
{
    public const string SectionName = "Catalog";

    public const string DefaultTimeZoneId = "Europe/Moscow";

    public string DefaultTimeZone { get; set; } = DefaultTimeZoneId;

    public TimeZoneInfo ResolveZone()
    {
        var id = string.IsNullOrWhiteSpace(DefaultTimeZone) ? DefaultTimeZoneId : DefaultTimeZone.Trim();
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new InvalidOperationException($"Catalog:DefaultTimeZone \"{id}\" не найден.", exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new InvalidOperationException($"Catalog:DefaultTimeZone \"{id}\" не найден.", exception);
        }
    }
}
