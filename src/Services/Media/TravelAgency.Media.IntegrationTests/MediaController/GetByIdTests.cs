using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Media.IntegrationTests.MediaController;

public class GetByIdTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string OwnerId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    private const string OtherClientId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
    private const string StaffId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GetByIdTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<MediaFile> SeedMediaFileAsync(string ownerId = OwnerId, bool isPublic = false)
    {
        _factory.EnsureDatabaseCreated();
        var file = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "storage/key/photo.jpg", ownerId);
        if (isPublic)
            file.MarkPublic();

        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMediaFileRepository>();
        await repo.AddAsync(file);
        await repo.SaveChangesAsync();

        return file;
    }

    [Fact]
    public async Task GetById_ExistingMedia_Returns200WithContent()
    {
        var mediaFile = await SeedMediaFileAsync();
        var token = JwtTokenHelper.GenerateToken();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/media/{mediaFile.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("image/jpeg");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();
        AssertPrivateNoStore(response);
    }

    [Fact]
    public async Task GetById_PublicFile_OtherClient_Returns200()
    {
        var mediaFile = await SeedMediaFileAsync(isPublic: true);

        var response = await SendAsync(mediaFile.Id, AppRoles.Client, OtherClientId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("image/jpeg");
        (await response.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
        CacheControl(response).Should().NotContain("private");
    }

    [Fact]
    public async Task GetById_Owner_PrivateFile_Returns200()
    {
        var mediaFile = await SeedMediaFileAsync();

        var response = await SendAsync(mediaFile.Id, AppRoles.Client, OwnerId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
        AssertPrivateNoStore(response);
    }

    [Fact]
    public async Task GetById_Manager_OtherPrivateFile_Returns200()
    {
        var mediaFile = await SeedMediaFileAsync();

        var response = await SendAsync(mediaFile.Id, AppRoles.Manager, StaffId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
        AssertPrivateNoStore(response);
    }

    [Fact]
    public async Task GetById_Admin_OtherPrivateFile_Returns200()
    {
        var mediaFile = await SeedMediaFileAsync();

        var response = await SendAsync(mediaFile.Id, AppRoles.Admin, StaffId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
        AssertPrivateNoStore(response);
    }

    [Fact]
    public async Task GetById_OtherClient_PrivateFile_Returns404_SameBodyAsMissingId()
    {
        var mediaFile = await SeedMediaFileAsync();
        var missingId = Guid.NewGuid();

        var denied = await SendAsync(mediaFile.Id, AppRoles.Client, OtherClientId);
        var missing = await SendAsync(missingId, AppRoles.Client, OtherClientId);

        denied.StatusCode.Should().Be(HttpStatusCode.NotFound);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        denied.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        missing.Content.Headers.ContentType?.MediaType.Should().Be(denied.Content.Headers.ContentType?.MediaType);
        CacheControl(denied).Should().Be(CacheControl(missing));

        var deniedBody = await denied.Content.ReadAsStringAsync();
        var missingBody = await missing.Content.ReadAsStringAsync();
        deniedBody.Should().NotContain("403");
        deniedBody.Should().NotContain("Forbidden");
        CanonicalProblem(deniedBody, mediaFile.Id).Should().Be(CanonicalProblem(missingBody, missingId));
        CanonicalProblem(deniedBody, mediaFile.Id).Should().Contain("status=404");
        CanonicalProblem(deniedBody, mediaFile.Id).Should().Contain("title=Not Found");
        CanonicalProblem(deniedBody, mediaFile.Id).Should().Contain("detail=Media file '{id}' was not found.");
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var unknownId = Guid.NewGuid();
        var token = JwtTokenHelper.GenerateToken();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/media/{unknownId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_NoAuth_Returns401()
    {
        var mediaFile = await SeedMediaFileAsync();

        var response = await _client.GetAsync($"/media/{mediaFile.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpResponseMessage> SendAsync(Guid id, string role, string userId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/media/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTokenHelper.GenerateToken(userId, role));
        return await _client.SendAsync(request);
    }

    private static void AssertPrivateNoStore(HttpResponseMessage response)
    {
        var cache = CacheControl(response);
        cache.Should().Contain("private");
        cache.Should().Contain("no-store");
        cache.Should().NotContain("public");
    }

    private static string CacheControl(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Cache-Control", out var values))
            return string.Join(", ", values);
        if (response.Content.Headers.TryGetValues("Cache-Control", out var contentValues))
            return string.Join(", ", contentValues);
        return string.Empty;
    }

    private static string CanonicalProblem(string json, Guid id)
    {
        using var doc = JsonDocument.Parse(json);
        var pairs = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.NameEquals("traceId"))
                continue;

            var raw = prop.Value.ValueKind == JsonValueKind.String
                ? prop.Value.GetString() ?? ""
                : prop.Value.GetRawText();
            pairs[prop.Name] = raw.Replace(id.ToString(), "{id}", StringComparison.OrdinalIgnoreCase);
        }

        return string.Join("|", pairs.Select(pair => pair.Key + "=" + pair.Value));
    }
}
