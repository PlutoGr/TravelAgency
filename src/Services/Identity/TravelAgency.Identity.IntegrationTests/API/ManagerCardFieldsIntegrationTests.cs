using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TravelAgency.Identity.Application.Features.Profile;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Identity.Infrastructure.Services;

namespace TravelAgency.Identity.IntegrationTests.API;

public class ManagerCardFieldsIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string SigningKey = "TestSigningKeyWithAtLeast32CharactersForHMAC";
    private const string BlockReason = "Секретная причина блокировки визитки";
    private const string ManagerOnlyPhrase = "доступно только менеджеру или администратору";

    private static readonly string[] CardKeys = ["id", "displayName", "position", "tourismSince", "about", "tags"];
    private static readonly string[] ProfileBannedKeys =
        ["position", "tourismSince", "about", "tags", "city", "languages", "specialization"];

    private readonly CustomWebApplicationFactory _factory;

    public ManagerCardFieldsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.Users.RemoveRange(db.Users);
        db.RefreshTokens.RemoveRange(db.RefreshTokens);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Tk1_SaveAndRead_NormalizesCard_AndKeepsTheAccount()
    {
        var avatarId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var manager = await SeedAsync(
            UserRole.Manager, "marina.card@example.com", "+79995550011", "Марина", "Иванова", avatarId);
        var admin = await SeedAsync(
            UserRole.Admin, "anna.card@example.com", "+79995550012", "Анна", "Соколова", null);
        var other = await SeedAsync(
            UserRole.Manager, "petr.card@example.com", "+79995550013", "Пётр", "Пустой", null);
        var managerBefore = await ReloadAsync(manager.Id);
        var adminBefore = await ReloadAsync(admin.Id);

        foreach (var user in new[] { manager, admin })
        {
            var started = DateTime.UtcNow;
            var saved = await PatchCardAsync(user.Id, user.Role.ToRoleString(), SampleBody());
            saved.StatusCode.Should().Be(HttpStatusCode.OK);
            HeaderValue(saved, "Cache-Control").Should().Contain("no-store");
            var savedJson = await saved.Content.ReadAsStringAsync();

            var author = await GetCardAsync(user.Id, user.Id, user.Role.ToRoleString());
            author.StatusCode.Should().Be(HttpStatusCode.OK);
            HeaderValue(author, "Cache-Control").Should().Contain("no-store");
            var authorJson = await author.Content.ReadAsStringAsync();
            authorJson.Should().Be(savedJson);

            foreach (var viewer in new[] { "Client", "Manager", "Admin" })
            {
                var response = await GetCardAsync(user.Id, Guid.NewGuid(), viewer);
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                (await response.Content.ReadAsStringAsync()).Should().Be(authorJson);
            }

            using var doc = JsonDocument.Parse(authorJson);
            var root = doc.RootElement;
            root.GetProperty("displayName").GetString().Should().Be($"{user.FirstName} {user.LastName}");
            root.GetProperty("position").GetString().Should().Be("Старший  менеджер");
            root.GetProperty("tourismSince").GetInt32().Should().Be(2013);
            root.GetProperty("about").GetString().Should().Be("Подбираю поездки\nпод даты.");
            root.GetProperty("tags").EnumerateArray().Select(item => item.GetString())
                .Should().Equal("Турция", "Горнолыжные туры");
            var names = root.EnumerateObject().Select(item => item.Name).ToArray();
            if (user.AvatarMediaFileId is null)
            {
                names.Should().BeEquivalentTo(CardKeys);
                root.TryGetProperty("avatarUrl", out _).Should().BeFalse();
            }
            else
            {
                names.Should().BeEquivalentTo(CardKeys.Append("avatarUrl"));
                root.GetProperty("avatarUrl").GetString().Should().Be(ProfileMapper.BuildAvatarUrl(avatarId));
            }

            authorJson.Should().NotContain(manager.Email);
            authorJson.Should().NotContain(admin.Email);
            authorJson.Should().NotContain(other.Email);
            authorJson.Should().NotContain(manager.Phone!);
            authorJson.Should().NotContain(admin.Phone!);
            authorJson.Should().NotContain(other.Phone!);
            authorJson.Should().NotContain("лет");

            var stored = await ReloadAsync(user.Id);
            AsUtc(stored.UpdatedAt!.Value).Should().BeOnOrAfter(started);
            stored.Email.Should().Be(user.Email);
            stored.Phone.Should().Be(user.Phone);
            stored.Role.Should().Be(user.Role);
            stored.PasswordHash.Should().Be(user.PasswordHash);
            stored.AvatarMediaFileId.Should().Be(user.AvatarMediaFileId);
            stored.FirstName.Should().Be(user.FirstName);
            stored.LastName.Should().Be(user.LastName);
            ReadTags(stored.TagsJson).Should().Equal("Турция", "Горнолыжные туры");
        }

        var empty = await GetCardAsync(other.Id, Guid.NewGuid(), "Client");
        await AssertEmptyCardAsync(empty);
        var emptyRow = await ReloadAsync(other.Id);
        emptyRow.Position.Should().BeNull();
        emptyRow.TourismSince.Should().BeNull();
        emptyRow.About.Should().BeNull();
        emptyRow.TagsJson.Should().BeNull();

        var renamed = await PatchProfileAsync(manager.Id, "Manager", """{"firstName":"Елена"}""");
        renamed.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterRename = await GetCardAsync(manager.Id, Guid.NewGuid(), "Client");
        using var renamedDoc = JsonDocument.Parse(await afterRename.Content.ReadAsStringAsync());
        renamedDoc.RootElement.GetProperty("displayName").GetString().Should().Be("Елена Иванова");
        renamedDoc.RootElement.GetProperty("position").GetString().Should().Be("Старший  менеджер");
        renamedDoc.RootElement.GetProperty("tourismSince").GetInt32().Should().Be(2013);
        renamedDoc.RootElement.GetProperty("about").GetString().Should().Be("Подбираю поездки\nпод даты.");
        renamedDoc.RootElement.GetProperty("tags").EnumerateArray().Select(item => item.GetString())
            .Should().Equal("Турция", "Горнолыжные туры");

        var managerAfter = await ReloadAsync(manager.Id);
        managerAfter.FirstName.Should().Be("Елена");
        managerAfter.Email.Should().Be(managerBefore.Email);
        managerAfter.Phone.Should().Be(managerBefore.Phone);
        managerAfter.PasswordHash.Should().Be(managerBefore.PasswordHash);
        managerAfter.AvatarMediaFileId.Should().Be(managerBefore.AvatarMediaFileId);
        (await ReloadAsync(admin.Id)).Position.Should().Be("Старший  менеджер");
        (await ReloadAsync(other.Id)).Position.Should().BeNull();
        adminBefore.Email.Should().Be(admin.Email);
    }

    [Fact]
    public async Task Tk2_EmptyValuesClear_AndMissingKeyDoesNotChangeTheRow()
    {
        var manager = await SeedAsync(
            UserRole.Manager, "empty.card@example.com", "+79995550021", "Марина", "Иванова", null);
        await AssertEmptyCardAsync(await GetCardAsync(manager.Id, Guid.NewGuid(), "Client"));

        foreach (var (position, about) in new (string?, string?)[]
        {
            (null, null),
            ("", ""),
            ("   ", "   "),
            (" \n ", "\t")
        })
        {
            var cleared = await PatchCardAsync(manager.Id, "Manager", CardJson(position, null, about, []));
            cleared.StatusCode.Should().Be(HttpStatusCode.OK);
            await AssertEmptyCardAsync(cleared);
            var stored = await ReloadAsync(manager.Id);
            stored.Position.Should().BeNull();
            stored.TourismSince.Should().BeNull();
            stored.About.Should().BeNull();
            stored.TagsJson.Should().BeNull();
        }

        var filled = CardJson("Было", 2001, "Текст", ["Один"]);
        (await PatchCardAsync(manager.Id, "Manager", filled)).StatusCode.Should().Be(HttpStatusCode.OK);
        var clear = await PatchCardAsync(manager.Id, "Manager", CardJson(null, null, null, []));
        clear.StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertEmptyCardAsync(clear);
        (await ReloadAsync(manager.Id)).TagsJson.Should().BeNull();

        (await PatchCardAsync(manager.Id, "Manager", filled)).StatusCode.Should().Be(HttpStatusCode.OK);
        var before = await ReloadAsync(manager.Id);
        foreach (var missing in new[] { "position", "tourismSince", "about", "tags" })
        {
            var response = await PatchCardAsync(manager.Id, "Manager", CardJsonWithout(missing));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            AssertSameRow(before, await ReloadAsync(manager.Id));
        }
    }

    [Fact]
    public async Task Tk3_Access_HidesPrivateData_AndBlockedStaffStayReadable()
    {
        var avatarId = Guid.Parse("cccccccc-dddd-eeee-ffff-000000000001");
        var manager = await SeedAsync(
            UserRole.Manager, "access.manager@example.com", "+79995550031", "Марина", "Иванова", avatarId);
        var client = await SeedAsync(
            UserRole.Client, "access.client@example.com", "+79995550032", "Клиент", "Скрытый", null);
        var blockedManager = await SeedAsync(
            UserRole.Manager, "blocked.manager.card@example.com", "+79995550033", "Борис", "Блок", null);
        var blockedAdmin = await SeedAsync(
            UserRole.Admin, "blocked.admin.card@example.com", "+79995550034", "Алла", "Блок", null);
        (await PatchCardAsync(manager.Id, "Manager", CardJson("Гид", 2011, "О себе", ["Море"]))).StatusCode
            .Should().Be(HttpStatusCode.OK);
        (await PatchCardAsync(blockedManager.Id, "Manager", CardJson("Гид", 2011, "О себе", ["Море"]))).StatusCode
            .Should().Be(HttpStatusCode.OK);
        (await PatchCardAsync(blockedAdmin.Id, "Admin", CardJson("Гид", 2011, "О себе", ["Море"]))).StatusCode
            .Should().Be(HttpStatusCode.OK);
        await BlockAsync(blockedManager.Id);
        await BlockAsync(blockedAdmin.Id);

        foreach (var path in new[] { CardPath(manager.Id), "/identity/me/manager-card" })
        {
            var guest = path.Contains("/me/", StringComparison.Ordinal)
                ? await PatchCardAsync(null, null, CardJson("Гид", 2011, "О себе", ["Море"]))
                : await _factory.CreateClient().GetAsync(path);
            guest.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var guestBody = await guest.Content.ReadAsStringAsync();
            guestBody.Should().NotContain("displayName");
            guestBody.Should().NotContain("position");
            guestBody.Should().NotContain("about");
            guestBody.Should().NotContain(manager.Email);
            guestBody.Should().NotContain(manager.Phone!);
        }

        var asClient = await GetCardAsync(manager.Id, client.Id, "Client");
        asClient.StatusCode.Should().Be(HttpStatusCode.OK);
        (await asClient.Content.ReadAsStringAsync()).Should().Contain("Гид");

        await AssertHiddenNotFoundAsync(await GetCardAsync(client.Id, Guid.NewGuid(), "Manager"), client);
        await AssertHiddenNotFoundAsync(await GetCardAsync(client.Id, client.Id, "Client"), client);

        var clientPatch = await PatchCardAsync(client.Id, "Client", CardJson("Чужая", 2012, "Нет", ["Нет"]));
        clientPatch.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await clientPatch.Content.ReadAsStringAsync()).Should().NotContain(ManagerOnlyPhrase);
        var clientRow = await ReloadAsync(client.Id);
        clientRow.Position.Should().BeNull();
        clientRow.TourismSince.Should().BeNull();
        clientRow.About.Should().BeNull();
        clientRow.TagsJson.Should().BeNull();

        var missingId = Guid.NewGuid();
        await AssertHiddenNotFoundAsync(await GetCardAsync(missingId, manager.Id, "Manager"), client);
        var missingPatch = await PatchCardAsync(missingId, "Manager", CardJson("Нет", 2012, "Нет", ["Нет"]));
        await AssertHiddenNotFoundAsync(missingPatch, client);
        (await FindAsync(missingId)).Should().BeNull();

        foreach (var blocked in new[] { blockedManager, blockedAdmin })
        {
            var response = await GetCardAsync(blocked.Id, client.Id, "Client");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("position").GetString().Should().Be("Гид");
            doc.RootElement.GetProperty("tourismSince").GetInt32().Should().Be(2011);
            doc.RootElement.GetProperty("about").GetString().Should().Be("О себе");
            doc.RootElement.GetProperty("tags").EnumerateArray().Select(item => item.GetString()).Should().Equal("Море");
            json.Should().NotContain(BlockReason);
            foreach (var key in new[] { "blockedAt", "blockReason", "blockedByUserId", "isBlocked" })
                doc.RootElement.TryGetProperty(key, out _).Should().BeFalse();
        }
    }

    [Fact]
    public async Task Tk4_Limits_RejectTheRow_AndBoundariesAreStored()
    {
        var year = DateTime.UtcNow.Year;
        var manager = await SeedAsync(
            UserRole.Manager, "limits.card@example.com", "+79995550041", "Марина", "Иванова", null);
        var initial = CardJson("Было", 2001, "Текст", ["Один"]);
        (await PatchCardAsync(manager.Id, "Manager", initial)).StatusCode.Should().Be(HttpStatusCode.OK);
        var before = await ReloadAsync(manager.Id);

        var aboutTooLong = new string('а', 500) + "\n" + new string('б', 500);
        var aboutExact = new string('а', 500) + "\n" + new string('б', 499);
        aboutTooLong.Length.Should().Be(1001);
        aboutExact.Length.Should().Be(1000);

        var cases = new (string Body, string Key, string Message)[]
        {
            (CardJson(new string('Я', 61), 2001, "Текст", ["Один"]), "position", "Должность не длиннее 60 символов."),
            (CardJson("Было", 1979, "Текст", ["Один"]), "tourismSince", "Укажите год с 1980 по текущий."),
            (CardJson("Было", year + 1, "Текст", ["Один"]), "tourismSince", "Укажите год с 1980 по текущий."),
            (CardJson("Было", 2001, aboutTooLong, ["Один"]), "about", "Текст «О себе» не длиннее 1000 символов."),
            (CardJson("Было", 2001, "Текст", [new string('т', 31)]), "tags", "Не длиннее 30 символов."),
            (CardJson("Было", 2001, "Текст", Enumerable.Range(1, 11).Select(i => "тег" + i).ToArray()), "tags", "Можно не больше 10."),
            (CardJson("Было", 2001, "Текст", ["  Турция ", "турция"]), "tags", "Такой тег уже есть."),
            (CardJson("Было", 2001, "Текст", ["Один", ""]), "tags", "Укажите тег."),
            (CardJson("Было", 2001, "Текст", ["   "]), "tags", "Укажите тег.")
        };

        foreach (var (body, key, message) in cases)
        {
            var response = await PatchCardAsync(manager.Id, "Manager", body);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("title").GetString().Should().Be("Validation Error");
            doc.RootElement.GetProperty("errors").GetProperty(key).EnumerateArray()
                .Select(item => item.GetString()).Should().Contain(message);
            AssertSameRow(before, await ReloadAsync(manager.Id));
        }

        var exactTag = " " + new string('т', 30) + " ";
        var boundary = CardJson(
            "  " + new string('Я', 60) + "  ",
            1980,
            "  " + aboutExact + "  ",
            Enumerable.Range(1, 9).Select(i => "тег" + i).Append(exactTag).ToArray());
        var accepted = await PatchCardAsync(manager.Id, "Manager", boundary);
        accepted.StatusCode.Should().Be(HttpStatusCode.OK);
        var current = CardJson(
            new string('Я', 60),
            year,
            aboutExact,
            Enumerable.Range(1, 9).Select(i => "тег" + i).Append(new string('т', 30)).ToArray());
        var currentResponse = await PatchCardAsync(manager.Id, "Manager", current);
        currentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var saved = JsonDocument.Parse(await currentResponse.Content.ReadAsStringAsync());
        saved.RootElement.GetProperty("position").GetString().Should().Be(new string('Я', 60));
        saved.RootElement.GetProperty("tourismSince").GetInt32().Should().Be(year);
        saved.RootElement.GetProperty("about").GetString().Should().Be(aboutExact);
        saved.RootElement.GetProperty("tags").GetArrayLength().Should().Be(10);
        saved.RootElement.GetProperty("tags")[9].GetString().Should().Be(new string('т', 30));

        var stored = await ReloadAsync(manager.Id);
        stored.Position.Should().Be(new string('Я', 60));
        stored.TourismSince.Should().Be(year);
        stored.About.Should().Be(aboutExact);
        ReadTags(stored.TagsJson).Should().HaveCount(10);
        ReadTags(stored.TagsJson)[9].Should().Be(new string('т', 30));
    }

    [Theory]
    [InlineData("Manager")]
    [InlineData("Admin")]
    public async Task Tk5_UnknownMemberOrWrongType_Returns400AndKeepsTheRow(string role)
    {
        var user = await SeedAsync(
            role == "Admin" ? UserRole.Admin : UserRole.Manager,
            $"extra.{role}@example.com",
            "+79995550051",
            "Марина",
            "Иванова",
            null);
        (await PatchCardAsync(user.Id, role, CardJson("Было", 2001, "Текст", ["Один"]))).StatusCode
            .Should().Be(HttpStatusCode.OK);
        var before = await ReloadAsync(user.Id);

        foreach (var (key, jsonValue) in new (string Key, string Value)[]
        {
            ("email", "\"hack@example.com\""),
            ("phone", "\"+79990000000\""),
            ("role", "\"Admin\""),
            ("firstName", "\"Новая\""),
            ("lastName", "\"Фамилия\""),
            ("city", "\"Сочи\""),
            ("languages", "[\"ru\"]"),
            ("specialization", "\"Море\""),
            ("passport", "\"4510\""),
            ("avatarUrl", "\"/api/v1/media/files/x/a256\""),
            ("nickname", "\"лишнее\"")
        })
        {
            var body = $$"""{"position":"Стало","tourismSince":2014,"about":"Другой","tags":["Иной"],"{{key}}":{{jsonValue}}}""";
            var response = await PatchCardAsync(user.Id, role, body);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().NotContain(ManagerOnlyPhrase);
            AssertSameRow(before, await ReloadAsync(user.Id));
        }

        foreach (var body in new[]
        {
            """{"position":1,"tourismSince":2014,"about":"a","tags":[]}""",
            """{"position":"a","tourismSince":"2014","about":"a","tags":[]}""",
            """{"position":"a","tourismSince":2014,"about":"a","tags":"Турция"}""",
            """{"position":"a","tourismSince":2014,"about":"a","tags":null}"""
        })
        {
            var response = await PatchCardAsync(user.Id, role, body);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().NotContain(ManagerOnlyPhrase);
            AssertSameRow(before, await ReloadAsync(user.Id));
        }
    }

    [Theory]
    [InlineData(UserRole.Client, "Client")]
    [InlineData(UserRole.Manager, "Manager")]
    [InlineData(UserRole.Admin, "Admin")]
    public async Task Tk6_ProfileRejectsCardKeys_AndDoesNotStoreThem(UserRole role, string roleName)
    {
        var user = await SeedAsync(role, $"profile.{roleName}@example.com", "+79995550061", "Test", "User", null);
        var profile = await SendAsync(HttpMethod.Get, "/identity/me", user.Id, roleName);
        profile.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await profile.Content.ReadAsStringAsync()))
        {
            foreach (var key in ProfileBannedKeys)
                doc.RootElement.TryGetProperty(key, out _).Should().BeFalse();
        }

        foreach (var (key, jsonValue) in new (string Key, string Value)[]
        {
            ("position", "\"Должность\""),
            ("tourismSince", "2010"),
            ("about", "\"текст\""),
            ("tags", "[\"Турция\"]"),
            ("city", "\"Сочи\""),
            ("languages", "[\"ru\"]"),
            ("specialization", "\"Море\"")
        })
        {
            var response = await PatchProfileAsync(
                user.Id,
                roleName,
                $$"""{"firstName":"Новое","{{key}}":{{jsonValue}}}""");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().NotContain(ManagerOnlyPhrase);
            var stored = await ReloadAsync(user.Id);
            stored.FirstName.Should().Be("Test");
            stored.LastName.Should().Be("User");
            stored.Phone.Should().Be(user.Phone);
            stored.Email.Should().Be(user.Email);
            stored.Role.Should().Be(role);
            stored.Position.Should().BeNull();
            stored.TourismSince.Should().BeNull();
            stored.About.Should().BeNull();
            stored.TagsJson.Should().BeNull();
        }

        var again = await SendAsync(HttpMethod.Get, "/identity/me", user.Id, roleName);
        using var after = JsonDocument.Parse(await again.Content.ReadAsStringAsync());
        after.RootElement.GetProperty("firstName").GetString().Should().Be("Test");
        foreach (var key in ProfileBannedKeys)
            after.RootElement.TryGetProperty(key, out _).Should().BeFalse();
    }

    [Fact]
    public async Task Tk8_CardBodiesAreNotLogged()
    {
        var avatarId = Guid.Parse("dddddddd-eeee-ffff-0000-111111111111");
        var user = await SeedAsync(
            UserRole.Manager, "card.log@example.com", "+79990000371", "Серафима", "Логина", avatarId);
        var body = CardJson("Хранитель маршрутов", 2016, "Собираю тихие поездки\nбез спешки", ["Алтай", "Поезда"]);
        _factory.Logs.Clear();
        _factory.Logs.Capture = true;
        try
        {
            (await PatchCardAsync(user.Id, "Manager", body)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await GetCardAsync(user.Id, Guid.NewGuid(), "Client")).StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            _factory.Logs.Capture = false;
        }

        var log = string.Join('\n', _factory.Logs.Lines);
        log.Should().Contain("/identity/me/manager-card");
        log.Should().Contain("/identity/users/");
        log.Should().NotContain("Серафима Логина");
        log.Should().NotContain("Хранитель маршрутов");
        log.Should().NotContain("Собираю тихие поездки");
        log.Should().NotContain("Алтай");
        log.Should().NotContain("Поезда");
        log.Should().NotContain(user.Email);
        log.Should().NotContain(user.Phone!);
        log.Should().NotContain(avatarId.ToString());
    }

    private async Task AssertEmptyCardAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        root.GetProperty("position").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("tourismSince").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("about").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("tags").GetArrayLength().Should().Be(0);
        root.TryGetProperty("avatarUrl", out _).Should().BeFalse();
        root.EnumerateObject().Select(item => item.Name).Should().BeEquivalentTo(CardKeys);
    }

    private async Task AssertHiddenNotFoundAsync(HttpResponseMessage response, User client)
    {
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("detail").GetString().Should().Be("User not found.");
        json.Should().NotContain(client.Email);
        json.Should().NotContain(client.Phone!);
    }

    private static void AssertSameRow(User before, User after)
    {
        after.Position.Should().Be(before.Position);
        after.TourismSince.Should().Be(before.TourismSince);
        after.About.Should().Be(before.About);
        after.TagsJson.Should().Be(before.TagsJson);
        after.FirstName.Should().Be(before.FirstName);
        after.LastName.Should().Be(before.LastName);
        after.Phone.Should().Be(before.Phone);
        after.Email.Should().Be(before.Email);
        after.Role.Should().Be(before.Role);
        after.PasswordHash.Should().Be(before.PasswordHash);
        after.AvatarMediaFileId.Should().Be(before.AvatarMediaFileId);
        after.UpdatedAt.Should().Be(before.UpdatedAt);
    }

    private static string SampleBody() =>
        CardJson("  Старший  менеджер  ", 2013, "  Подбираю поездки\nпод даты.  ", ["  Турция  ", "Горнолыжные   туры"]);

    private static string CardJson(string? position, int? year, string? about, string[] tags)
    {
        var payload = new Dictionary<string, object?>
        {
            ["position"] = position,
            ["tourismSince"] = year,
            ["about"] = about,
            ["tags"] = tags
        };
        return JsonSerializer.Serialize(payload);
    }

    private static string CardJsonWithout(string missing)
    {
        var payload = new Dictionary<string, object?>
        {
            ["position"] = "Новая",
            ["tourismSince"] = 2014,
            ["about"] = "Новый текст",
            ["tags"] = new[] { "Новый" }
        };
        payload.Remove(missing);
        return JsonSerializer.Serialize(payload);
    }

    private static string[] ReadTags(string? tagsJson)
    {
        tagsJson.Should().NotBeNullOrWhiteSpace();
        using var doc = JsonDocument.Parse(tagsJson!);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        return JsonSerializer.Deserialize<string[]>(tagsJson!, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }

    private async Task BlockAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var user = await db.Users.SingleAsync(item => item.Id == id);
        user.Block(BlockReason, Guid.NewGuid(), DateTime.UtcNow);
        await db.SaveChangesAsync();
    }

    private async Task<User?> FindAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await db.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
    }

    private async Task<User> ReloadAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await db.Users.AsNoTracking().SingleAsync(item => item.Id == id);
    }

    private async Task<User> SeedAsync(
        UserRole role,
        string email,
        string phone,
        string firstName,
        string lastName,
        Guid? avatarId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var user = User.Create(email, new PasswordHasherService().Hash("Password1"), firstName, lastName, phone, role);
        if (avatarId is Guid mediaId)
            user.SetAvatar(mediaId);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private Task<HttpResponseMessage> GetCardAsync(Guid targetId, Guid viewerId, string role) =>
        SendAsync(HttpMethod.Get, CardPath(targetId), viewerId, role);

    private Task<HttpResponseMessage> PatchCardAsync(Guid? userId, string? role, string json)
    {
        if (userId is null || role is null)
        {
            var guest = new HttpRequestMessage(HttpMethod.Patch, "/identity/me/manager-card")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return _factory.CreateClient().SendAsync(guest);
        }

        return SendAsync(HttpMethod.Patch, "/identity/me/manager-card", userId.Value, role, json);
    }

    private Task<HttpResponseMessage> PatchProfileAsync(Guid userId, string role, string json) =>
        SendAsync(HttpMethod.Patch, "/identity/me", userId, role, json);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, Guid userId, string role, string? json = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Jwt(userId, "viewer@example.com", role));
        if (json is not null)
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _factory.CreateClient().SendAsync(request);
    }

    private static string CardPath(Guid id) => $"/identity/users/{id}/manager-card";

    private static string HeaderValue(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var values))
            return string.Join(", ", values);
        if (response.Content.Headers.TryGetValues(name, out var contentValues))
            return string.Join(", ", contentValues);
        return string.Empty;
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static string Jwt(Guid userId, string email, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var token = new JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
