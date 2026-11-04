using System.Text.Json;
using FluentAssertions;
using TravelAgency.Contracts.Grpc.Identity;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Features.Profile;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;

namespace TravelAgency.Identity.UnitTests.Application.Features;

public class NarrowProfileContractTests
{
    [Fact]
    public void Profile_AndCard_KeepOnlyTheNarrowFields()
    {
        var withPhoto = User.Create(
            "marina.private@example.com", "hash", "  Марина  ", "  Иванова  ", " +79991234567 ", UserRole.Manager);
        withPhoto.UpdateProfile("Марина", "Иванова", "+79991234567");
        var avatarId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        withPhoto.SetAvatar(avatarId);

        var withoutPhoto = User.Create("admin.private@example.com", "hash", "Марина", "Иванова", "", UserRole.Admin);
        withoutPhoto.UpdateProfile("Марина", "Иванова", "");

        withPhoto.DisplayName.Should().Be("Марина Иванова");
        withPhoto.Phone.Should().Be("+79991234567");
        withoutPhoto.Phone.Should().BeNull();
        typeof(User).GetProperty("City").Should().BeNull();
        typeof(User).GetProperty("LanguagesJson").Should().BeNull();
        typeof(User).GetProperty("Specialization").Should().BeNull();
        typeof(User).GetProperty("Passport").Should().BeNull();
        typeof(User).GetMethod("UpdateManagerProfile").Should().BeNull();
        typeof(User).GetMethod("GetLanguages").Should().BeNull();
        typeof(User).GetField("MaxCityLength").Should().BeNull();
        typeof(User).GetField("MaxSpecializationLength").Should().BeNull();
        User.MaxPositionLength.Should().Be(60);
        User.MaxAboutLength.Should().Be(1000);
        User.MinTourismSinceYear.Should().Be(1980);
        withPhoto.Position.Should().BeNull();
        withPhoto.TourismSince.Should().BeNull();
        withPhoto.About.Should().BeNull();
        withPhoto.TagsJson.Should().BeNull();

        var dto = ProfileMapper.ToDto(withPhoto);
        dto.Should().BeEquivalentTo(new UserProfileDto(
            withPhoto.Id,
            withPhoto.Email,
            "Марина",
            "Иванова",
            "+79991234567",
            "Manager",
            withPhoto.CreatedAt,
            ProfileMapper.BuildAvatarUrl(avatarId)));

        var card = ProfileMapper.ToPublicCard(withPhoto);
        card.Id.Should().Be(withPhoto.Id);
        card.DisplayName.Should().Be("Марина Иванова");
        card.AvatarUrl.Should().Be(ProfileMapper.BuildAvatarUrl(avatarId));
        card.Position.Should().BeNull();
        card.TourismSince.Should().BeNull();
        card.About.Should().BeNull();
        card.Tags.Should().BeEmpty();
        var emptyCard = JsonSerializer.Serialize(
            ProfileMapper.ToPublicCard(withoutPhoto),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        emptyCard.Should().NotContain("avatarUrl");
        using var emptyDoc = JsonDocument.Parse(emptyCard);
        emptyDoc.RootElement.GetProperty("position").ValueKind.Should().Be(JsonValueKind.Null);
        emptyDoc.RootElement.GetProperty("tourismSince").ValueKind.Should().Be(JsonValueKind.Null);
        emptyDoc.RootElement.GetProperty("about").ValueKind.Should().Be(JsonValueKind.Null);
        emptyDoc.RootElement.GetProperty("tags").GetArrayLength().Should().Be(0);

        UserSummaryResponse.Descriptor.Fields.InDeclarationOrder()
            .Select(field => field.Name)
            .Should().Equal("user_id", "email", "first_name", "last_name", "role", "avatar_url", "display_name");
        UserPhoneResponse.Descriptor.Fields.InDeclarationOrder()
            .Select(field => field.Name)
            .Should().Equal("user_id", "phone");
        IdentityGrpc.Descriptor.Methods.Select(method => method.Name)
            .Should().Equal("GetUserSummary", "GetUserPhone");
    }
}
