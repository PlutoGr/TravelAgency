using System.Text.Json;
using FluentAssertions;
using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.UnitTests.Application.DTOs;

public class AuthTokensDtoTests
{
    [Fact]
    public void Serialize_IncludesRefreshExpiresAt()
    {
        var refreshExpiresAt = new DateTime(2026, 12, 1, 15, 30, 0, DateTimeKind.Utc);
        var dto = new AuthTokensDto(
            "access",
            "refresh",
            new DateTime(2026, 10, 17, 10, 0, 0, DateTimeKind.Utc),
            refreshExpiresAt);

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("refreshExpiresAt", out var prop).Should().BeTrue(
            "поле RefreshExpiresAt обязательно в ответе login, register и refresh");
        prop.GetDateTime().Should().Be(refreshExpiresAt);
    }
}
