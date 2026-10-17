using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Media.Domain;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Enums;
using TravelAgency.Media.Infrastructure.Persistence;
using TravelAgency.Media.Infrastructure.Repositories;

namespace TravelAgency.Media.UnitTests.Infrastructure;

public class MediaFileRepositoryTests
{
    [Fact]
    public async Task MarkPublic_PublishesOnlyActiveTourImages()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<MediaDbContext>().UseSqlite(connection).Options;
        await using var db = new MediaDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var tourImage = File("cover.jpg", MediaPurposes.TourImage);
        var general = File("note.jpg", MediaPurposes.General);
        var deleted = File("gone.jpg", MediaPurposes.TourImage);
        deleted.MarkPublic();
        deleted.MarkAsDeleted();
        var alreadyPublic = File("old.jpg", MediaPurposes.TourImage);
        alreadyPublic.MarkPublic();

        db.MediaFiles.AddRange(tourImage, general, deleted, alreadyPublic);
        await db.SaveChangesAsync();

        var repository = new MediaFileRepository(db);
        await repository.MarkPublicAsync(
            [tourImage.Id, general.Id, deleted.Id, alreadyPublic.Id, Guid.NewGuid()]);

        await using var verify = new MediaDbContext(options);
        (await verify.MediaFiles.SingleAsync(file => file.Id == tourImage.Id)).IsPublic.Should().BeTrue();
        (await verify.MediaFiles.SingleAsync(file => file.Id == general.Id)).IsPublic.Should().BeFalse();
        var deletedRow = await verify.MediaFiles.SingleAsync(file => file.Id == deleted.Id);
        deletedRow.IsPublic.Should().BeTrue();
        deletedRow.Status.Should().Be(MediaFileStatus.Deleted);
        (await verify.MediaFiles.SingleAsync(file => file.Id == alreadyPublic.Id)).IsPublic.Should().BeTrue();
    }

    private static MediaFile File(string name, string purpose) =>
        MediaFile.Create(name, "image/jpeg", 10, name, "owner", purpose, width: 1600, height: 900);
}
