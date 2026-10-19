namespace TravelAgency.Catalog.Application.Time;

/// <summary>
/// Город вылета и IANA-пояс. Список фиксированный: расширение справочника вне этой задачи.
/// Сопоставление: без регистра, без пробелов по краям, «ё» равно «е».
/// </summary>
public static class DepartureCityTimeZones
{
    /// <summary>
    /// Верхний регистр и «ё» слева, нижний регистр справа. «Ё» и «ё» обе становятся «е».
    /// Длины равны, чтобы translate в миграции совпадал с Normalize.
    /// </summary>
    public const string FoldSource =
        "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯABCDEFGHIJKLMNOPQRSTUVWXYZё";

    public const string FoldTarget =
        "абвгдеежзийклмнопрстуфхцчшщъыьэюяabcdefghijklmnopqrstuvwxyz\u0435";

    public static IReadOnlyList<(string City, string ZoneId)> Entries { get; } =
    [
        ("Москва", "Europe/Moscow"),
        ("Санкт-Петербург", "Europe/Moscow"),
        ("Казань", "Europe/Moscow"),
        ("Нижний Новгород", "Europe/Moscow"),
        ("Ярославль", "Europe/Moscow"),
        ("Краснодар", "Europe/Moscow"),
        ("Сочи", "Europe/Moscow"),
        ("Ростов-на-Дону", "Europe/Moscow"),
        ("Минеральные Воды", "Europe/Moscow"),
        ("Калининград", "Europe/Kaliningrad"),
        ("Самара", "Europe/Samara"),
        ("Волгоград", "Europe/Volgograd"),
        ("Екатеринбург", "Asia/Yekaterinburg"),
        ("Пермь", "Asia/Yekaterinburg"),
        ("Уфа", "Asia/Yekaterinburg"),
        ("Челябинск", "Asia/Yekaterinburg"),
        ("Тюмень", "Asia/Yekaterinburg"),
        ("Омск", "Asia/Omsk"),
        ("Новосибирск", "Asia/Novosibirsk"),
        ("Красноярск", "Asia/Krasnoyarsk"),
        ("Иркутск", "Asia/Irkutsk"),
        ("Хабаровск", "Asia/Vladivostok"),
        ("Владивосток", "Asia/Vladivostok")
    ];

    private static readonly Dictionary<string, string> Zones = Entries.ToDictionary(
        entry => Normalize(entry.City),
        entry => entry.ZoneId,
        StringComparer.Ordinal);

    public static string Normalize(string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return string.Empty;

        var trimmed = city.Trim();
        var chars = trimmed.ToCharArray();
        for (var index = 0; index < chars.Length; index++)
        {
            var folded = FoldSource.IndexOf(chars[index]);
            if (folded >= 0)
                chars[index] = FoldTarget[folded];
        }

        return new string(chars);
    }

    public static string? FindZoneId(string? departureCity)
    {
        var key = Normalize(departureCity);
        if (key.Length == 0)
            return null;

        return Zones.TryGetValue(key, out var zoneId) ? zoneId : null;
    }
}
