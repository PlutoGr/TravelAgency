using System.Text.Json.Serialization;

namespace TravelAgency.Identity.Application.DTOs;

/// <summary>
/// Публичная визитка менеджера или администратора. Контактов и служебных полей нет.
/// Пустой адрес фото в JSON не пишется. Должность, год, текст и теги есть всегда:
/// пустые строки и год — null, теги — пустой массив.
/// </summary>
public sealed record ManagerPublicCardDto(
    Guid Id,
    string DisplayName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? AvatarUrl,
    string? Position,
    int? TourismSince,
    string? About,
    IReadOnlyList<string> Tags);
