using FluentAssertions;
using Moq;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Profile.Commands.UpdateProfile;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.UnitTests.Application.Features;

public class UpdateProfileCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly UpdateProfileCommandHandler _handler;

    public UpdateProfileCommandHandlerTests()
    {
        _handler = new UpdateProfileCommandHandler(
            _currentUserServiceMock.Object,
            _userRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ReturnsUpdatedUserProfileDto()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("u@example.com", "hash", "Old", "Name", "+111");

        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new UpdateProfileRequest("New", "Surname", "+999");
        var result = await _handler.Handle(new UpdateProfileCommand(request), CancellationToken.None);

        result.Should().BeOfType<UserProfileDto>();
        result.FirstName.Should().Be("New");
        result.LastName.Should().Be("Surname");
        result.Phone.Should().Be("+999");
        result.Email.Should().Be("u@example.com");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await _handler.Handle(
            new UpdateProfileCommand(new UpdateProfileRequest("J", "D", null)),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithNullFirstName_KeepsExistingFirstName()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("u@example.com", "hash", "Existing", "Name", null);

        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new UpdateProfileCommand(new UpdateProfileRequest(null, null, null)),
            CancellationToken.None);

        result.FirstName.Should().Be("Existing");
        result.LastName.Should().Be("Name");
    }

    [Fact]
    public async Task Handle_WithValidRequest_CallsUpdateAsync()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("u@example.com", "hash", "J", "D", null);

        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _handler.Handle(
            new UpdateProfileCommand(new UpdateProfileRequest("J", "D", null)),
            CancellationToken.None);

        _userRepoMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    // FIX-008: Phone is passed directly — null clears the field

    [Fact]
    public async Task Handle_WhenPhoneIsNull_ClearsUserPhone()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("u@example.com", "hash", "John", "Doe", "+111");

        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new UpdateProfileCommand(new UpdateProfileRequest(null, null, null)),
            CancellationToken.None);

        result.Phone.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenPhoneProvided_UpdatesPhone()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("u@example.com", "hash", "John", "Doe", "+111");

        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new UpdateProfileCommand(new UpdateProfileRequest(null, null, "+999")),
            CancellationToken.None);

        result.Phone.Should().Be("+999");
    }
}
