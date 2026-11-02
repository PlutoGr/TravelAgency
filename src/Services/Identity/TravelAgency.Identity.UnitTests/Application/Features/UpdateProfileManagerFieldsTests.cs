using FluentAssertions;
using Moq;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
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

    [Theory]
    [InlineData(UserRole.Client)]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Admin)]
    public async Task Handle_TrimsNameAndPhone_ForEveryRole(UserRole role)
    {
        var userId = Guid.NewGuid();
        var user = User.Create("person@example.com", "hash", "Старое", "Имя", "+79990000000", role);
        _currentUser.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUser.Setup(s => s.UserId).Returns(userId);
        _users.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _handler.Handle(
            new UpdateProfileCommand(new UpdateProfileRequest(" Марина ", " Иванова ", " +79991234567 ")),
            CancellationToken.None);

        result.FirstName.Should().Be("Марина");
        result.LastName.Should().Be("Иванова");
        result.Phone.Should().Be("+79991234567");
        result.Email.Should().Be("person@example.com");
        result.Role.Should().Be(role.ToRoleString());
        user.DisplayName.Should().Be("Марина Иванова");
    }
}
