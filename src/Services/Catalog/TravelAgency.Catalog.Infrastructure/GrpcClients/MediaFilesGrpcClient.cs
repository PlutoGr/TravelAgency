using Grpc.Core;
using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Contracts.Grpc.Media;
using TravelAgency.Shared.Infrastructure.GrpcServices;

namespace TravelAgency.Catalog.Infrastructure.GrpcClients;

public sealed class MediaFilesGrpcClient(
    MediaService.MediaServiceClient client,
    IGrpcAuthCallOptionsFactory callOptionsFactory) : IMediaFilesClient
{
    public async Task<IReadOnlyList<RemoteMediaFile>> GetMediaFilesAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return [];

        var request = new GetMediaFilesRequest();
        foreach (var id in ids)
            request.Ids.Add(id.ToString());

        var response = await CallAsync(
            () => client.GetMediaFilesAsync(request, callOptionsFactory.Create(cancellationToken)).ResponseAsync);

        return response.Files
            .Select(file => new RemoteMediaFile(Guid.Parse(file.Id), file.OwnerId, file.Width, file.Height))
            .ToList();
    }

    public Task MarkPublicAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return Task.CompletedTask;

        var request = new MarkMediaFilesPublicRequest();
        foreach (var id in ids)
            request.Ids.Add(id.ToString());

        return CallAsync(
            () => client.MarkMediaFilesPublicAsync(request, callOptionsFactory.Create(cancellationToken)).ResponseAsync);
    }

    private static async Task<T> CallAsync<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (RpcException)
        {
            throw new MediaUnavailableException();
        }
    }
}
