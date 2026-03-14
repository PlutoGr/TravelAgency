using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Enums;

namespace TravelAgency.Media.UnitTests.Domain;

public class MediaFileTests
{
    [Fact]
    public void Create_SetsAllPropertiesCorrectly()
    {
        var originalFileName = "photo.jpg";
        var contentType = "image/jpeg";
        var sizeBytes = 1024L;
        var storageKey = "user1/abc/photo.jpg";
        var ownerId = "user-123";

        var file = MediaFile.Create(originalFileName, contentType, sizeBytes, storageKey, ownerId);

        file.Id.Should().NotBe(Guid.Empty);
        file.OriginalFileName.Should().Be(originalFileName);
        file.ContentType.Should().Be(contentType);
        file.SizeBytes.Should().Be(sizeBytes);
        file.StorageKey.Should().Be(storageKey);
        file.OwnerId.Should().Be(ownerId);
        file.Status.Should().Be(MediaFileStatus.Active);
        file.UploadedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        file.Thumbnails.Should().BeEmpty();
    }

    [Fact]
    public void Create_GeneratesUniqueId_ForEachCall()
    {
        var file1 = MediaFile.Create("a.jpg", "image/jpeg", 100, "key1", "user1");
        var file2 = MediaFile.Create("b.jpg", "image/jpeg", 100, "key2", "user1");

        file1.Id.Should().NotBe(file2.Id);
    }

    [Fact]
    public void AddThumbnail_AppendsThumbnailToList()
    {
        var file = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "key", "user1");

        file.AddThumbnail("key-thumb-200", 200, 150);

        file.Thumbnails.Should().HaveCount(1);
        file.Thumbnails[0].StorageKey.Should().Be("key-thumb-200");
        file.Thumbnails[0].Width.Should().Be(200);
        file.Thumbnails[0].Height.Should().Be(150);
    }

    [Fact]
    public void AddThumbnail_MultipleCalls_AddsAllThumbnails()
    {
        var file = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "key", "user1");

        file.AddThumbnail("key-thumb-200", 200, 0);
        file.AddThumbnail("key-thumb-800", 800, 0);

        file.Thumbnails.Should().HaveCount(2);
        file.Thumbnails.Should().Contain(t => t.Width == 200);
        file.Thumbnails.Should().Contain(t => t.Width == 800);
    }

    [Fact]
    public void MarkAsDeleted_ChangesStatusToDeleted()
    {
        var file = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "key", "user1");

        file.MarkAsDeleted();

        file.Status.Should().Be(MediaFileStatus.Deleted);
    }

    [Fact]
    public void MarkAsDeleted_Idempotent_WhenCalledTwice()
    {
        var file = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "key", "user1");

        file.MarkAsDeleted();
        file.MarkAsDeleted();

        file.Status.Should().Be(MediaFileStatus.Deleted);
    }

    [Fact]
    public void Thumbnails_IsReadOnly_CannotBeModifiedDirectly()
    {
        var file = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "key", "user1");

        file.Thumbnails.Should().BeAssignableTo<IReadOnlyList<MediaFileThumbnail>>();
    }

    [Fact]
    public void Create_NewFile_HasActiveStatus()
    {
        var file = MediaFile.Create("doc.pdf", "application/pdf", 2048, "key", "user1");

        file.Status.Should().Be(MediaFileStatus.Active);
    }
}
