using FluentAssertions;
using Moq;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Profile.Queries.GetProfile;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.UnitTests.Application.Features;

public class GetProfileQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly GetProfileQueryHandler _handler;

    public GetProfileQueryHandlerTests()
    {
        _handler = new GetProfileQueryHandler(
            _currentUserServiceMock.Object,
            _userRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsUserProfileDto()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("user@example.com", "hash", "John", "Doe", "+123");

        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new GetProfileQuery(), CancellationToken.None);

        result.Should().BeOfType<UserProfileDto>();
        result.Email.Should().Be("user@example.com");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Phone.Should().Be("+123");
        result.Role.Should().Be("Client");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await _handler.Handle(new GetProfileQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ReturnsDtoWithCorrectId()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("u@example.com", "hash", "J", "D", null);

        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new GetProfileQuery(), CancellationToken.None);

        result.Id.Should().Be(user.Id);
        result.CreatedAt.Should().BeCloseTo(user.CreatedAt, TimeSpan.FromSeconds(1));
    }
}
