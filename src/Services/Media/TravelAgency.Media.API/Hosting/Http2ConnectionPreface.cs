namespace TravelAgency.Media.API.Hosting;

/// <summary>
/// The 24-byte client connection preface from RFC 7540 / RFC 9113.
/// A cleartext HTTP/2 client sends it before any other bytes (prior knowledge, h2c).
/// </summary>
internal static class Http2ConnectionPreface
{
    public static ReadOnlySpan<byte> Bytes => "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"u8;

    public static bool IsPrefix(ReadOnlySpan<byte> data)
    {
        var preface = Bytes;
        if (data.Length > preface.Length)
            data = data[..preface.Length];

        return preface.StartsWith(data);
    }

    public static bool IsComplete(ReadOnlySpan<byte> data) =>
        data.Length >= Bytes.Length && data[..Bytes.Length].SequenceEqual(Bytes);
}
