using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.Features.Favorites.Queries.GetMyFavorites;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Interfaces;

namespace TravelAgency.Booking.UnitTests.Application.Queries;

public class GetMyFavoritesQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IFavoriteRepository> _favoriteRepoMock = new();
    private readonly GetMyFavoritesQueryHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();

    public GetMyFavoritesQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(UserId);

        _handler = new GetMyFavoritesQueryHandler(
            _currentUserMock.Object,
            _favoriteRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnCurrentUserFavorites()
    {
        var favorite1 = Favorite.Create(UserId, Guid.NewGuid());
        var favorite2 = Favorite.Create(UserId, Guid.NewGuid());

        _favoriteRepoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Favorite> { favorite1, favorite2 }.AsReadOnly());

        var result = await _handler.Handle(new GetMyFavoritesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(f => f.UserId.Should().Be(UserId));
    }

    [Fact]
    public async Task Handle_WhenNoFavorites_ShouldReturnEmptyList()
    {
        _favoriteRepoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Favorite>().AsReadOnly());

        var result = await _handler.Handle(new GetMyFavoritesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldQueryRepositoryWithCurrentUserId()
    {
        _favoriteRepoMock.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Favorite>().AsReadOnly());

        await _handler.Handle(new GetMyFavoritesQuery(), CancellationToken.None);

        _favoriteRepoMock.Verify(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
