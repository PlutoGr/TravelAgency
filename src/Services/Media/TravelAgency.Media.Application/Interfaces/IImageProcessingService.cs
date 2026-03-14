namespace TravelAgency.Media.Application.Interfaces;

public interface IImageProcessingService
{
    bool IsImage(string contentType);
    Task<Stream> ResizeAsync(Stream source, int width, CancellationToken ct = default);
}
