using TravelAgency.Chat.Application.Abstractions;
using TravelAgency.Chat.Application.Exceptions;
using TravelAgency.Chat.Application.Features.Messages.Queries.GetMessages;
using TravelAgency.Chat.Domain.Entities;
using TravelAgency.Chat.Domain.Enums;
using TravelAgency.Chat.Domain.Interfaces;

namespace TravelAgency.Chat.UnitTests.Application.Queries;

public class GetMessagesQueryHandlerTests
{
    private readonly IBookingAccessService _bookingAccessService = Substitute.For<IBookingAccessService>();
    private readonly IChatMessageRepository _messageRepository = Substitute.For<IChatMessageRepository>();
    private readonly GetMessagesQueryHandler _handler;

    private static readonly Guid BookingId = Guid.NewGuid();

    public GetMessagesQueryHandlerTests()
    {
        _handler = new GetMessagesQueryHandler(_bookingAccessService, _messageRepository);
    }

    [Fact]
    public async Task Handle_WhenUserHasAccess_ReturnsMessages()
    {
        _bookingAccessService
            .CanAccessBookingAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var message = ChatMessage.Create(
            BookingId,
            Guid.NewGuid().ToString(),
            "Test User",
            SenderRole.Client,
            "Hello");

        _messageRepository
            .GetByBookingIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new[] { message }.ToList().AsReadOnly());

        var result = await _handler.Handle(new GetMessagesQuery(BookingId), CancellationToken.None);

        result.Should().NotBeNull().And.HaveCount(1);
        result[0].BookingId.Should().Be(BookingId);
        result[0].Text.Should().Be("Hello");
        result[0].SenderRole.Should().Be(nameof(SenderRole.Client));
    }

    [Fact]
    public async Task Handle_WhenUserHasNoAccess_ThrowsForbiddenException()
    {
        _bookingAccessService
            .CanAccessBookingAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var act = async () => await _handler.Handle(new GetMessagesQuery(BookingId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*access*");
    }

    [Fact]
    public async Task Handle_WhenNoMessages_ReturnsEmptyList()
    {
        _bookingAccessService
            .CanAccessBookingAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _messageRepository
            .GetByBookingIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ChatMessage>().ToList().AsReadOnly());

        var result = await _handler.Handle(new GetMessagesQuery(BookingId), CancellationToken.None);

        result.Should().NotBeNull().And.BeEmpty();
    }
}
