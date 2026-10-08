using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TravelAgency.Media.Application.Services;
using TravelAgency.Media.Application.Settings;
using TravelAgency.Media.Infrastructure;

namespace TravelAgency.Media.UnitTests.Infrastructure;

public class UploadSettingsBindingTests
{
    [Fact]
    public void Bind_AppendsConfiguredWidthsOntoNonEmptyInitializer()
    {
        var configuration = ConfigurationWithWidths(200, 800);
        var sample = new WidthSample();

        configuration.GetSection("Upload").Bind(sample);

        sample.ThumbnailWidths.Should().Equal(200, 800, 200, 800);
    }

    [Fact]
    public void AddUploadSettings_KeepsEachConfiguredWidthOnce()
    {
        var configuration = ConfigurationWithWidths(200, 800, 200, 800);

        var settings = Bind(configuration);

        settings.ThumbnailWidths.Should().Equal(200, 800);
    }

    [Fact]
    public void AddUploadSettings_UsesDefaultWhenWidthsAreMissing()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var settings = Bind(configuration);

        settings.ThumbnailWidths.Should().Equal(ThumbnailWidthList.Default);
    }

    [Fact]
    public void DistinctPositive_DropsDuplicatesAndNonPositiveWidths()
    {
        ThumbnailWidthList.DistinctPositive([800, 0, 200, 800, -1]).Should().Equal(800, 200);
    }

    private static UploadSettings Bind(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddUploadSettings(configuration);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<UploadSettings>>().Value;
    }

    private static IConfiguration ConfigurationWithWidths(params int[] widths)
    {
        var values = new Dictionary<string, string?>();
        for (var index = 0; index < widths.Length; index++)
            values[$"Upload:ThumbnailWidths:{index}"] = widths[index].ToString();

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private sealed class WidthSample
    {
        public int[] ThumbnailWidths { get; set; } = [200, 800];
    }
}
