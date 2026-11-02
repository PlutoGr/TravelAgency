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
        typeof(User).GetProperty("About").Should().BeNull();
        typeof(User).GetProperty("TourismSince").Should().BeNull();
        typeof(User).GetProperty("LanguagesJson").Should().BeNull();
        typeof(User).GetProperty("Specialization").Should().BeNull();
        typeof(User).GetProperty("Passport").Should().BeNull();
        typeof(User).GetMethod("UpdateManagerProfile").Should().BeNull();
        typeof(User).GetMethod("GetLanguages").Should().BeNull();
        typeof(User).GetField("MaxCityLength").Should().BeNull();
        typeof(User).GetField("MaxAboutLength").Should().BeNull();
        typeof(User).GetField("MaxSpecializationLength").Should().BeNull();
        typeof(User).GetField("MinTourismSinceYear").Should().BeNull();

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
        JsonSerializer.Serialize(ProfileMapper.ToPublicCard(withoutPhoto))
            .Should().NotContain("avatarUrl");

        UserSummaryResponse.Descriptor.Fields.InDeclarationOrder()
            .Select(field => field.Name)
            .Should().Equal("user_id", "email", "first_name", "last_name", "role", "avatar_url", "display_name");
        IdentityGrpc.Descriptor.Methods.Select(method => method.Name)
            .Should().Equal("GetUserSummary");
    }
}
