using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Features.Favorites.Commands.RemoveFavorite;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Interfaces;

namespace TravelAgency.Booking.UnitTests.Application.Commands;

public class RemoveFavoriteCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IFavoriteRepository> _favoriteRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly RemoveFavoriteCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TourId = Guid.NewGuid();

    public RemoveFavoriteCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(UserId);

        _handler = new RemoveFavoriteCommandHandler(
            _currentUserMock.Object,
            _favoriteRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingFavorite_ShouldRemoveFavorite()
    {
        var favorite = Favorite.Create(UserId, TourId);

        _favoriteRepoMock.Setup(r => r.GetAsync(UserId, TourId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(favorite);

        await _handler.Handle(new RemoveFavoriteCommand(TourId), CancellationToken.None);

        _favoriteRepoMock.Verify(r => r.Remove(favorite), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FavoriteNotFound_ShouldThrowNotFoundException()
    {
        _favoriteRepoMock.Setup(r => r.GetAsync(UserId, TourId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Favorite?)null);

        var act = async () => await _handler.Handle(new RemoveFavoriteCommand(TourId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not found*");
    }
}
