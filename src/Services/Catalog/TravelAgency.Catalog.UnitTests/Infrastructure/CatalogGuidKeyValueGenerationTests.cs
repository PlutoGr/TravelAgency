using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TravelAgency.Catalog.Infrastructure.Persistence;

namespace TravelAgency.Catalog.UnitTests.Infrastructure;

public class CatalogGuidKeyValueGenerationTests
{
    /// <summary>
    /// Guid-ключи, которые генерирует база. У Catalog таких нет: Tour, Direction,
    /// TourOffer, TourImage, TourDay, TourInclusion и TourComponent получают Id в домене
    /// через Guid.NewGuid(). ValueGeneratedOnAdd на таком ключе заставляет EF делать UPDATE.
    /// </summary>
    private static readonly HashSet<string> DatabaseGeneratedGuidKeys = [];

    [Fact]
    public void GuidPrimaryKeys_AreValueGeneratedNever_UnlessTheDatabaseAssignsThem()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql("Host=localhost;Database=catalog_model;Username=user")
            .Options;

        using var db = new CatalogDbContext(options);

        var missing = new List<string>();
        foreach (var entityType in db.Model.GetEntityTypes())
        {
            if (IsOwnedWithoutOwnGuidKey(entityType))
                continue;

            var key = entityType.FindPrimaryKey();
            if (key is null)
                continue;

            foreach (var property in key.Properties.Where(p => p.ClrType == typeof(Guid)))
            {
                var name = $"{EntityName(entityType)}.{property.Name}";
                if (DatabaseGeneratedGuidKeys.Contains(name))
                    continue;

                if (property.ValueGenerated != ValueGenerated.Never)
                    missing.Add($"{name} ({property.ValueGenerated})");
            }
        }

        missing.Should().BeEmpty(
            "у этих Guid-ключей нет ValueGenerated.Never: {0}",
            string.Join(", ", missing));
    }

    private static bool IsOwnedWithoutOwnGuidKey(IReadOnlyEntityType entityType)
    {
        if (!entityType.IsOwned())
            return false;

        var key = entityType.FindPrimaryKey();
        var ownsGuidKey = key?.Properties.Any(p => p.ClrType == typeof(Guid) && !p.IsForeignKey()) == true;
        return !ownsGuidKey;
    }

    private static string EntityName(IReadOnlyEntityType entityType) =>
        entityType.ClrType?.Name ?? entityType.Name;
}
