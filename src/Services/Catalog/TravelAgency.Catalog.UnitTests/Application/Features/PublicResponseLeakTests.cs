using System.Text.Json;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Mappings;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.UnitTests.Application.Features;

public class PublicResponseLeakTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string StoredAddress =
        "http://minio:9000/media/cover.jpg?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Signature=abc";

    private const string UnsplashAddress =
        "https://images.unsplash.com/photo-1514282401047-d79a71a590e8?w=800";

    [Fact]
    public void CatalogResponses_DoNotExposeStorageOrLegacyImageAddresses()
    {
        var coverId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var secondId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var withPhotos = TourWithStoredAddress(StoredAddress + " " + UnsplashAddress);
        withPhotos.ReplaceImages(
        [
            TourImage.Create(withPhotos.Id, coverId, 0, true, "Cover", 1600),
            TourImage.Create(withPhotos.Id, secondId, 1, false, "Second", 800)
        ]);

        var now = new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
        AssertClean(PublicCatalogMapper.ToPublicTour(withPhotos, now), coverId);
        AssertClean(PublicCatalogMapper.ToSummary(withPhotos, now), coverId);
        AssertClean(PublicCatalogMapper.ToCard(withPhotos, now), coverId);
        AssertClean(TourManageMapper.ToManageDto(withPhotos), expectedCover: null);

        var legacyOnly = TourWithStoredAddress(UnsplashAddress);
        var page = PublicCatalogMapper.ToPublicTour(legacyOnly, now);
        var summary = PublicCatalogMapper.ToSummary(legacyOnly, now);
        var card = PublicCatalogMapper.ToCard(legacyOnly, now);

        page.CoverMediaFileId.Should().BeNull();
        summary.CoverMediaFileId.Should().BeNull();
        card.CoverMediaFileId.Should().BeNull();
        page.Images.Should().BeEmpty();
        AssertNoAddress(JsonSerializer.Serialize(page, JsonOptions));
        AssertNoAddress(JsonSerializer.Serialize(summary, JsonOptions));
        AssertNoAddress(JsonSerializer.Serialize(card, JsonOptions));
    }

    private static Tour TourWithStoredAddress(string imageUrl) =>
        Tour.Create("Мальдивы", "Описание тура", TourType.Beach, "Мальдивы", 7, imageUrl);

    private static void AssertClean<T>(T dto, Guid? expectedCover)
    {
        var json = JsonSerializer.Serialize(dto, JsonOptions);
        AssertNoAddress(json);

        using var document = JsonDocument.Parse(json);
        AssertNoProperty(document.RootElement, "imageUrl");
        AssertNoProperty(document.RootElement, "url");
        AssertNoProperty(document.RootElement, "photos");

        if (expectedCover is Guid cover)
        {
            document.RootElement.GetProperty("coverMediaFileId").GetGuid().Should().Be(cover);
        }
    }

    private static void AssertNoAddress(string json)
    {
        json.Should().NotContainEquivalentOf("minio");
        json.Should().NotContain("X-Amz-");
        json.Should().NotContainEquivalentOf("unsplash");
        json.Should().NotContain("http://");
        json.Should().NotContain("https://");
    }

    private static void AssertNoProperty(JsonElement element, string name)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    property.Name.Should().NotBe(name);
                    AssertNoProperty(property.Value, name);
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    AssertNoProperty(item, name);
                break;
        }
    }
}
