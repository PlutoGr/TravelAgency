using FluentAssertions;
using Moq;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Profile.Commands.UpdateProfile;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.UnitTests.Application.Features;

public class UpdateProfileCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly UpdateProfileCommandHandler _handler;

    public UpdateProfileCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new UpdateProfileCommandHandler(
            _currentUserServiceMock.Object,
            _userRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ReturnsUpdatedUserProfileDto()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("u@example.com", "hash", "Old", "Name", "+111");

        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

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
        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
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

        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(
            new UpdateProfileCommand(new UpdateProfileRequest(null, null, null)),
            CancellationToken.None);

        result.FirstName.Should().Be("Existing");
        result.LastName.Should().Be("Name");
    }

    [Fact]
    public async Task Handle_WithValidRequest_CallsUnitOfWorkSaveChangesAsync()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("u@example.com", "hash", "J", "D", null);

        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _handler.Handle(
            new UpdateProfileCommand(new UpdateProfileRequest("J", "D", null)),
            CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // FIX-008: Phone is passed directly — null clears the field

    [Fact]
    public async Task Handle_WhenPhoneIsNull_ClearsUserPhone()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("u@example.com", "hash", "John", "Doe", "+111");

        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

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

        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(
            new UpdateProfileCommand(new UpdateProfileRequest(null, null, "+999")),
            CancellationToken.None);

        result.Phone.Should().Be("+999");
    }
}
