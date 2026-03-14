using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Catalog.Application.Features.Tours.Commands.UpdateTour;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.UnitTests.Application.Features;

public class UpdateTourCommandHandlerTests
{
    private readonly Mock<ITourRepository> _tourRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly UpdateTourCommandHandler _handler;

    public UpdateTourCommandHandlerTests()
    {
        _handler = new UpdateTourCommandHandler(_tourRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingTour_UpdatesTourAndReturnsTourDto()
    {
        var tour = Tour.Create("Original Title", "Original Description", TourType.Beach, "Greece", 7, null, null);
        var updateRequest = new UpdateTourRequest(
            "Updated Title",
            "Updated Description",
            TourType.Mountain,
            "Italy",
            10,
            null,
            null);
        var command = new UpdateTourCommand(tour.Id, updateRequest);

        _tourRepositoryMock
            .Setup(r => r.GetByIdAsync(tour.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tour);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Title.Should().Be("Updated Title");
        result.Country.Should().Be("Italy");
        result.DurationDays.Should().Be(10);

        _tourRepositoryMock.Verify(r => r.Update(tour), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistingTour_ThrowsNotFoundException()
    {
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateTourRequest("Title", "Description", TourType.Beach, "Greece", 7, null, null);
        var command = new UpdateTourCommand(nonExistentId, updateRequest);

        _tourRepositoryMock
            .Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tour?)null);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();

        _tourRepositoryMock.Verify(r => r.Update(It.IsAny<Tour>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
