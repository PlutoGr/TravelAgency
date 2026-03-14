using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Features.Favorites.Commands.AddFavorite;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Interfaces;

namespace TravelAgency.Booking.UnitTests.Application.Commands;

public class AddFavoriteCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IFavoriteRepository> _favoriteRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly AddFavoriteCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TourId = Guid.NewGuid();

    public AddFavoriteCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(UserId);

        _handler = new AddFavoriteCommandHandler(
            _currentUserMock.Object,
            _favoriteRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_NewFavorite_ShouldStageFavorite()
    {
        _favoriteRepoMock.Setup(r => r.GetAsync(UserId, TourId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Favorite?)null);

        var result = await _handler.Handle(new AddFavoriteCommand(TourId), CancellationToken.None);

        result.Should().NotBeNull();
        result.UserId.Should().Be(UserId);
        result.TourId.Should().Be(TourId);

        _favoriteRepoMock.Verify(r => r.Stage(It.IsAny<Favorite>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateFavorite_ShouldThrowConflictException()
    {
        var existingFavorite = Favorite.Create(UserId, TourId);

        _favoriteRepoMock.Setup(r => r.GetAsync(UserId, TourId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFavorite);

        var act = async () => await _handler.Handle(new AddFavoriteCommand(TourId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already in favorites*");
    }
}
