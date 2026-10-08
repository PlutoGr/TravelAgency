namespace TravelAgency.Media.Infrastructure.Storage;

/// <summary>
/// Решает, можно ли не подписывать тело запроса в S3 (DisablePayloadSigning).
/// SigV4 без подписи тела (UNSIGNED-PAYLOAD) безопасен только поверх TLS,
/// поэтому AWS SDK запрещает его для http. Решаем по схеме Storage:ServiceUrl.
/// </summary>
public static class S3PayloadSigning
{
    /// <summary>
    /// true только для абсолютного https URL. Для http, пустого или неверного URL тело подписывается.
    /// </summary>
    public static bool CanDisablePayloadSigning(string? serviceUrl) =>
        Uri.TryCreate(serviceUrl, UriKind.Absolute, out var uri)
        && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}
