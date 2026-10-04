using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.Exceptions;

namespace TravelAgency.Catalog.IntegrationTests.Helpers;

public sealed class FakeMediaFilesClient : IMediaFilesClient
{
    public string OwnerId { get; set; } = string.Empty;
    public int DefaultWidth { get; set; } = 1600;
    public Dictionary<Guid, RemoteMediaFile>? Files { get; set; }
    public bool FailGet { get; set; }
    public bool FailMark { get; set; }
    public List<Guid> Marked { get; } = [];

    public void Reset(string ownerId)
    {
        OwnerId = ownerId;
        DefaultWidth = 1600;
        Files = null;
        FailGet = false;
        FailMark = false;
        Marked.Clear();
    }

    public Task<IReadOnlyList<RemoteMediaFile>> GetMediaFilesAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (FailGet)
            throw new MediaUnavailableException();

        IReadOnlyList<RemoteMediaFile> files = Files is null
            ? ids.Select(id => new RemoteMediaFile(id, OwnerId, DefaultWidth, 900)).ToList()
            : ids.Where(Files.ContainsKey).Select(id => Files[id]).ToList();

        return Task.FromResult(files);
    }

    public Task MarkPublicAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (FailMark)
            throw new MediaUnavailableException();

        Marked.AddRange(ids);
        return Task.CompletedTask;
    }
}
