using TravelAgency.Media.Infrastructure.Maintenance;

namespace TravelAgency.Media.UnitTests.Infrastructure;

public class MediaDimensionBackfillCommandTests
{
    [Fact]
    public void EmptyArgs_AreNotABackfillRequest()
    {
        var args = Array.Empty<string>();

        MediaDimensionBackfillCommand.IsRequested(args).Should().BeFalse();
        MediaDimensionBackfillCommand.IsDryRun(args).Should().BeFalse();
        MediaDimensionBackfillCommand.WithoutCommandArgs(args).Should().BeEmpty();
    }

    [Fact]
    public void CommandArgs_AreRecognizedAndRemovedFromHostArgs()
    {
        var args = new[] { "backfill-dimensions", "--dry-run", "--environment", "Production" };

        MediaDimensionBackfillCommand.IsRequested(args).Should().BeTrue();
        MediaDimensionBackfillCommand.IsDryRun(args).Should().BeTrue();
        MediaDimensionBackfillCommand.WithoutCommandArgs(args).Should().Equal("--environment", "Production");
    }
}
