namespace TravelAgency.Catalog.Domain;

/// <summary>
/// Пределы публичной выдачи. Список карточек берёт не больше пяти превью,
/// пакет для избранного принимает не больше пятидесяти id.
/// </summary>
public static class PublicCatalogLimits
{
    public const int MaxListPreviews = 5;
    public const int MaxCardIds = 50;
}
