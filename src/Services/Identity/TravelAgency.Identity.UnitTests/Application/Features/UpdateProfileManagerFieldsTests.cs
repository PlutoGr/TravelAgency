using FluentAssertions;
using Moq;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Profile.Commands.UpdateProfile;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;

namespace TravelAgency.Identity.UnitTests.Application.Features;

public class UpdateProfileManagerFieldsTests
{
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly UpdateProfileCommandHandler _handler;

    public UpdateProfileManagerFieldsTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdateProfileCommandHandler(_currentUser.Object, _users.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_ClientSendingAbout_Throws400WithField()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("c@example.com", "hash", "A", "B", null, UserRole.Client);
        _currentUser.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUser.Setup(s => s.UserId).Returns(userId);
        _users.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var act = async () => await _handler.Handle(
            new UpdateProfileCommand(new UpdateProfileRequest(FirstName: "A", About: "секрет")),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("about");
        user.About.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ManagerCanUpdateAboutAndLanguages()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("m@example.com", "hash", "A", "B", null, UserRole.Manager);
        _currentUser.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUser.Setup(s => s.UserId).Returns(userId);
        _users.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _handler.Handle(
            new UpdateProfileCommand(new UpdateProfileRequest(
                About: "Гид",
                TourismSince: 2012,
                Languages: ["ru", "en"],
                Specialization: "Азия")),
            CancellationToken.None);

        result.About.Should().Be("Гид");
        result.TourismSince.Should().Be(2012);
        result.Languages.Should().BeEquivalentTo(["ru", "en"]);
        result.Specialization.Should().Be("Азия");
    }

    [Fact]
    public async Task Handle_DoesNotChangeEmailOrRole()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("c@example.com", "hash", "A", "B", null, UserRole.Client);
        _currentUser.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUser.Setup(s => s.UserId).Returns(userId);
        _users.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _handler.Handle(
            new UpdateProfileCommand(new UpdateProfileRequest(FirstName: "New", City: "Москва")),
            CancellationToken.None);

        result.Email.Should().Be("c@example.com");
        result.Role.Should().Be("Client");
        result.City.Should().Be("Москва");
        result.FirstName.Should().Be("New");
    }
}
