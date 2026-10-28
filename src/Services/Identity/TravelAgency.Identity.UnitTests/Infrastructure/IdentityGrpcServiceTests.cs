using Grpc.Core;
using TravelAgency.Contracts.Grpc.Identity;
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
        user.UpdateManagerProfile("  Первая строка\nвторая  ", 2018, ["ru", "en"], "Море-104");
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
        response.ToString().Should().NotContain("Первая строка");
        response.ToString().Should().NotContain("2018");
        response.ToString().Should().NotContain("Море-104");
        response.ToString().Should().NotContain("ru");
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
