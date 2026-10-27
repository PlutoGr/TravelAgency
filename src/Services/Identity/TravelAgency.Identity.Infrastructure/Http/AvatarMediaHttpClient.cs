using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.Exceptions;

namespace TravelAgency.Identity.Infrastructure.Http;

public sealed class AvatarMediaHttpClient(HttpClient httpClient) : IAvatarMediaClient
{

    public async Task<AvatarUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        string? bearerToken,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        form.Add(streamContent, "file", string.IsNullOrWhiteSpace(fileName) ? "avatar.jpg" : fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, "media/upload?purpose=avatar")
        {
            Content = form
        };
        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            throw MapMediaValidation(body);

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new ForbiddenException("Нельзя изменить аватар другого пользователя.");

        if (!response.IsSuccessStatusCode)
            throw new BadRequestException("Не удалось загрузить аватар. Попробуйте ещё раз.");

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("id", out var idEl)
            || !Guid.TryParse(idEl.GetString(), out var id))
        {
            throw new BadRequestException("Не удалось загрузить аватар. Попробуйте ещё раз.");
        }

        return new AvatarUploadResult(id);
    }

    public async Task DeleteAsync(Guid mediaFileId, string? bearerToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"media/{mediaFileId:D}");
        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.NoContent)
            return;

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new ForbiddenException("Нельзя изменить аватар другого пользователя.");

        response.EnsureSuccessStatusCode();
    }

    private static AppValidationException MapMediaValidation(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("errors", out var errorsEl)
                && errorsEl.ValueKind == JsonValueKind.Object)
            {
                var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in errorsEl.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        map[prop.Name] = prop.Value.EnumerateArray()
                            .Select(v => v.GetString() ?? "Некорректное значение.")
                            .ToArray();
                    }
                }

                if (map.Count > 0)
                    return new AppValidationException(map);
            }

            if (doc.RootElement.TryGetProperty("detail", out var detail)
                && detail.GetString() is { Length: > 0 } text)
            {
                return new AppValidationException(new Dictionary<string, string[]>
                {
                    ["file"] = [text]
                });
            }
        }
        catch (JsonException)
        {
            // fall through
        }

        return new AppValidationException(new Dictionary<string, string[]>
        {
            ["file"] = ["Подходят JPG, PNG или WebP."]
        });
    }
}
