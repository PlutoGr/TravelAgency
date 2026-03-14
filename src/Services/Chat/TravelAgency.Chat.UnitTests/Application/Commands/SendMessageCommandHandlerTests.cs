using TravelAgency.Chat.Application.Abstractions;
using TravelAgency.Chat.Application.Exceptions;
using TravelAgency.Chat.Application.Features.Messages.Commands.SendMessage;
using TravelAgency.Chat.Domain.Entities;
using TravelAgency.Chat.Domain.Enums;
using TravelAgency.Chat.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Chat.UnitTests.Application.Commands;

public class SendMessageCommandHandlerTests
{
    private readonly IBookingAccessService _bookingAccessService = Substitute.For<IBookingAccessService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IChatMessageRepository _messageRepository = Substitute.For<IChatMessageRepository>();
    private readonly SendMessageCommandHandler _handler;

    private static readonly Guid BookingId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private const string DisplayName = "Test User";

    public SendMessageCommandHandlerTests()
    {
        _handler = new SendMessageCommandHandler(_bookingAccessService, _currentUserService, _messageRepository);
    }

    [Fact]
    public async Task Handle_WhenUserHasAccess_CreatesAndReturnsMessage()
    {
        _bookingAccessService
            .CanAccessBookingAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _currentUserService.UserId.Returns(UserId);
        _currentUserService.Role.Returns(AppRoles.Client);
        _currentUserService.DisplayName.Returns(DisplayName);

        var createdMessage = ChatMessage.Create(
            BookingId,
            UserId.ToString(),
            DisplayName,
            SenderRole.Client,
            "Hello");

        _messageRepository
            .AddAsync(Arg.Any<ChatMessage>(), Arg.Any<CancellationToken>())
            .Returns(createdMessage);

        var command = new SendMessageCommand(BookingId, "Hello");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.BookingId.Should().Be(BookingId);
        result.Text.Should().Be("Hello");
        result.SenderId.Should().Be(UserId.ToString());
        result.SenderName.Should().Be(DisplayName);
        result.SenderRole.Should().Be(nameof(SenderRole.Client));
    }

    [Fact]
    public async Task Handle_WhenUserHasNoAccess_ThrowsForbiddenException()
    {
        _bookingAccessService
            .CanAccessBookingAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new SendMessageCommand(BookingId, "Hello");
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*access*");
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsMessage()
    {
        _bookingAccessService
            .CanAccessBookingAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _currentUserService.UserId.Returns(UserId);
        _currentUserService.Role.Returns(AppRoles.Manager);
        _currentUserService.DisplayName.Returns(DisplayName);

        ChatMessage? capturedMessage = null;
        _messageRepository
            .AddAsync(Arg.Any<ChatMessage>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var msg = call.Arg<ChatMessage>();
                capturedMessage = msg;
                return msg;
            });

        var command = new SendMessageCommand(BookingId, "Test message", new[] { "attachment1" }.ToList().AsReadOnly());
        var result = await _handler.Handle(command, CancellationToken.None);

        await _messageRepository.Received(1).AddAsync(Arg.Any<ChatMessage>(), Arg.Any<CancellationToken>());
        capturedMessage.Should().NotBeNull();
        capturedMessage!.BookingId.Should().Be(BookingId);
        capturedMessage.Text.Should().Be("Test message");
        capturedMessage.SenderRole.Should().Be(SenderRole.Manager);
        capturedMessage.Attachments.Should().NotBeNull().And.Contain("attachment1");
    }
}
