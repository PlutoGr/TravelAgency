using Grpc.Core;
using TravelAgency.Contracts.Grpc.Identity;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Features.Profile;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Identity.Infrastructure.GrpcServices;

namespace TravelAgency.Identity.UnitTests.Infrastructure;

public class IdentityGrpcServiceTests
{
    [Fact]
    public async Task GetUserSummary_KeepsTheExistingFieldSet()
    {
        var avatarId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var user = User.Create(
            "marina.private@example.com",
            "hash",
            "Марина",
            "Иванова",
            "+79995550001",
            UserRole.Manager);
        user.SetAvatar(avatarId);

        var repository = new Mock<IUserRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var response = await new IdentityGrpcService(repository.Object).GetUserSummary(
            new GetUserSummaryRequest { UserId = user.Id.ToString() },
            new TestServerCallContext());

        UserSummaryResponse.Descriptor.Fields.InDeclarationOrder()
            .Select(field => field.Name)
            .Should()
            .Equal("user_id", "email", "first_name", "last_name", "role", "avatar_url", "display_name");

        response.UserId.Should().Be(user.Id.ToString());
        response.Email.Should().Be("marina.private@example.com");
        response.FirstName.Should().Be("Марина");
        response.LastName.Should().Be("Иванова");
        response.Role.Should().Be("Manager");
        response.AvatarUrl.Should().Be(ProfileMapper.BuildAvatarUrl(avatarId));
        response.DisplayName.Should().Be("Марина Иванова");
        response.ToString().Should().NotContain("+79995550001");
        response.ToString().Should().NotContain("city");
        response.ToString().Should().NotContain("about");
        response.ToString().Should().NotContain("phone");
    }

    [Fact]
    public async Task GetUserPhone_ReturnsStoredPhone_EmptyStringWhenMissing_NotFoundWhenUnknown()
    {
        var withPhone = User.Create(
            "anna@example.com", "hash", "Анна", "Соколова", "+79991234567", UserRole.Client);
        var withoutPhone = User.Create(
            "empty@example.com", "hash", "Анна", "Соколова", null, UserRole.Client);
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(withPhone.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(withPhone);
        repository
            .Setup(repo => repo.GetByIdAsync(withoutPhone.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(withoutPhone);
        repository
            .Setup(repo => repo.GetByIdAsync(It.Is<Guid>(id => id != withPhone.Id && id != withoutPhone.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new IdentityGrpcService(repository.Object);

        var stored = await service.GetUserPhone(
            new GetUserPhoneRequest { UserId = withPhone.Id.ToString() },
            new TestServerCallContext());
        stored.UserId.Should().Be(withPhone.Id.ToString());
        stored.Phone.Should().Be("+79991234567");

        var empty = await service.GetUserPhone(
            new GetUserPhoneRequest { UserId = withoutPhone.Id.ToString() },
            new TestServerCallContext());
        empty.Phone.Should().BeEmpty();

        var missing = async () => await service.GetUserPhone(
            new GetUserPhoneRequest { UserId = Guid.NewGuid().ToString() },
            new TestServerCallContext());
        var notFound = await missing.Should().ThrowAsync<RpcException>();
        notFound.Which.StatusCode.Should().Be(StatusCode.NotFound);

        UserSummaryResponse.Descriptor.Fields.InDeclarationOrder()
            .Select(field => field.JsonName)
            .Should().NotContain("phone");
        typeof(ManagerPublicCardDto).GetProperties().Select(property => property.Name)
            .Should().BeEquivalentTo("Id", "DisplayName", "AvatarUrl", "Position", "TourismSince", "About", "Tags");
        typeof(ManagerPublicCardDto).GetProperty("Phone").Should().BeNull();
        typeof(ManagerPublicCardDto).GetProperty("Email").Should().BeNull();
    }

    private sealed class TestServerCallContext : ServerCallContext
    {
        protected override string MethodCore => string.Empty;
        protected override string HostCore => string.Empty;
        protected override string PeerCore => string.Empty;
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => [];
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore => [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore =>
            new(null, new Dictionary<string, List<AuthProperty>>());
        protected override ContextPropagationToken CreatePropagationTokenCore(
            ContextPropagationOptions? options) => throw new NotSupportedException();
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) =>
            Task.CompletedTask;
    }
}
