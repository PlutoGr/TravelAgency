namespace TravelAgency.Media.Application.Services;

/// <summary>
/// Validates file content matches declared MIME type using magic bytes (file signatures).
/// </summary>
public static class FileContentValidator
{
    // Magic byte signatures for allowed types
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Gif87aSignature = [0x47, 0x49, 0x46, 0x38, 0x37, 0x61]; // GIF87a
    private static readonly byte[] Gif89aSignature = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61]; // GIF89a
    private static readonly byte[] WebpRiff = [0x52, 0x49, 0x46, 0x46]; // RIFF
    private static readonly byte[] WebpWebp = [0x57, 0x45, 0x42, 0x50]; // WEBP at offset 8
    private static readonly byte[] PdfSignature = [0x25, 0x50, 0x44, 0x46, 0x2D]; // %PDF-

    private const int MaxBytesToRead = 16;

    /// <summary>
    /// Validates that the stream content matches the declared content type.
    /// Reads magic bytes from the stream and resets position if the stream is seekable.
    /// </summary>
    /// <returns>True if content matches the declared type; false otherwise.</returns>
    public static async Task<bool> ValidateAsync(Stream stream, string contentType, CancellationToken ct = default)
    {
        if (!stream.CanSeek)
            return false; // Cannot validate without consuming; reject for security

        var buffer = new byte[MaxBytesToRead];
        var read = await stream.ReadAsync(buffer.AsMemory(0, MaxBytesToRead), ct);

        stream.Position = 0;

        if (read == 0)
            return false;

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => MatchesSignature(buffer, read, JpegSignature),
            "image/png" => MatchesSignature(buffer, read, PngSignature),
            "image/gif" => MatchesSignature(buffer, read, Gif87aSignature) || MatchesSignature(buffer, read, Gif89aSignature),
            "image/webp" => MatchesSignature(buffer, read, WebpRiff) && read >= 12 && MatchesSignature(buffer, read, WebpWebp, 8),
            "application/pdf" => MatchesSignature(buffer, read, PdfSignature),
            _ => true // Unknown types: no magic-byte validation (caller should restrict allowed types)
        };
    }

    private static bool MatchesSignature(byte[] buffer, int bytesRead, byte[] signature, int offset = 0)
    {
        if (bytesRead < offset + signature.Length)
            return false;

        for (var i = 0; i < signature.Length; i++)
        {
            if (buffer[offset + i] != signature[i])
                return false;
        }
        return true;
    }
}
