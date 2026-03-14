using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Enums;

namespace TravelAgency.Booking.UnitTests.Domain;

public class OutboxMessageEntityTests
{
    [Fact]
    public void Create_ShouldCreateWithPendingStatus()
    {
        var before = DateTime.UtcNow;
        var message = OutboxMessage.Create("TestEvent", "{\"key\":\"value\"}");
        var after = DateTime.UtcNow;

        message.Id.Should().NotBe(Guid.Empty);
        message.EventType.Should().Be("TestEvent");
        message.Payload.Should().Be("{\"key\":\"value\"}");
        message.Status.Should().Be(OutboxMessageStatus.Pending);
        message.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        message.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void MarkProcessed_ShouldSetStatusToProcessed()
    {
        var message = OutboxMessage.Create("TestEvent", "{}");
        var before = DateTime.UtcNow;

        message.MarkProcessed();
        var after = DateTime.UtcNow;

        message.Status.Should().Be(OutboxMessageStatus.Processed);
        message.ProcessedAt.Should().NotBeNull();
        message.ProcessedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void MarkFailed_ShouldSetStatusToFailed()
    {
        var message = OutboxMessage.Create("TestEvent", "{}");
        var before = DateTime.UtcNow;

        message.MarkFailed();
        var after = DateTime.UtcNow;

        message.Status.Should().Be(OutboxMessageStatus.Failed);
        message.ProcessedAt.Should().NotBeNull();
        message.ProcessedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
