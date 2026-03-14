using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Catalog.Application.Features.Tours.Commands.CreateTour;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.UnitTests.Application.Features;

public class CreateTourCommandHandlerTests
{
    private readonly Mock<ITourRepository> _tourRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly CreateTourCommandHandler _handler;

    public CreateTourCommandHandlerTests()
    {
        _handler = new CreateTourCommandHandler(_tourRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_CreatesTourAndReturnsTourDto()
    {
        var request = new CreateTourRequest(
            "Santorini Explorer",
            "Beautiful Greek island tour",
            TourType.Beach,
            "Greece",
            7,
            null,
            null);
        var command = new CreateTourCommand(request);

        _tourRepositoryMock
            .Setup(r => r.ExistsByTitleAsync(request.Title, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _tourRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<TravelAgency.Catalog.Domain.Entities.Tour>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Title.Should().Be("Santorini Explorer");

        _tourRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TravelAgency.Catalog.Domain.Entities.Tour>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateTitle_ThrowsConflictException()
    {
        var request = new CreateTourRequest(
            "Existing Tour",
            "Description",
            TourType.Beach,
            "Greece",
            7,
            null,
            null);
        var command = new CreateTourCommand(request);

        _tourRepositoryMock
            .Setup(r => r.ExistsByTitleAsync(request.Title, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();

        _tourRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TravelAgency.Catalog.Domain.Entities.Tour>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
