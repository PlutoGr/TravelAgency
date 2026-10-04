using Amazon.S3.Model;

namespace TravelAgency.Media.Infrastructure.Storage;

/// <summary>
/// Reads an S3 object without buffering it, and disposes the SDK response with the stream.
/// </summary>
internal sealed class S3ResponseStream : Stream
{
    private readonly GetObjectResponse _response;
    private readonly Stream _inner;
    private bool _disposed;

    public S3ResponseStream(GetObjectResponse response)
    {
        _response = response;
        _inner = response.ResponseStream;
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count) =>
        _inner.Read(buffer, offset, count);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        _inner.ReadAsync(buffer, offset, count, cancellationToken);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        _inner.ReadAsync(buffer, cancellationToken);

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException();

    public override void SetLength(long value) =>
        throw new NotSupportedException();

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
        _inner.CopyToAsync(destination, bufferSize, cancellationToken);

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        _disposed = true;
        if (disposing)
            _response.Dispose();
        base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync()
    {
        if (_disposed)
            return ValueTask.CompletedTask;
        _disposed = true;
        _response.Dispose();
        return base.DisposeAsync();
    }
}
