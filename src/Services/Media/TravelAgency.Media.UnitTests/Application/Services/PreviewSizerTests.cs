using TravelAgency.Media.Application.Services;

namespace TravelAgency.Media.UnitTests.Application.Services;

public class PreviewSizerTests
{
    [Fact]
    public void FitWithin_ScalesDown_AndKeepsAspectRatio()
    {
        var (width, height) = PreviewSizer.FitWithin(1600, 900, 200);

        width.Should().Be(200);
        height.Should().Be(113);
    }

    [Fact]
    public void FitWithin_ExactMaxWidth_KeepsOriginal()
    {
        var (width, height) = PreviewSizer.FitWithin(800, 600, 800);

        width.Should().Be(800);
        height.Should().Be(600);
    }

    [Fact]
    public void FitWithin_SmallerThanMax_DoesNotUpscale()
    {
        var (width, height) = PreviewSizer.FitWithin(100, 40, 1600);

        width.Should().Be(100);
        height.Should().Be(40);
    }

    [Theory]
    [InlineData(0, 10, 200)]
    [InlineData(10, 0, 200)]
    [InlineData(10, 10, 0)]
    [InlineData(-1, 10, 200)]
    public void FitWithin_NonPositiveDimensions_Throws(int width, int height, int maxWidth)
    {
        var act = () => PreviewSizer.FitWithin(width, height, maxWidth);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
