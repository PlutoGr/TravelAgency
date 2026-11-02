using System.Text.Json.Serialization;

namespace TravelAgency.Identity.Application.DTOs;

/// <summary>
/// Краткая карточка менеджера или администратора. Контактов и служебных полей нет.
/// Пустой адрес фото в JSON не пишется.
/// </summary>
public sealed record ManagerPublicCardDto(
    Guid Id,
    string DisplayName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? AvatarUrl = null);
