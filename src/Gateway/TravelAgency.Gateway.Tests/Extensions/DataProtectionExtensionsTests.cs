using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using TravelAgency.Gateway.Extensions;

namespace TravelAgency.Gateway.Tests.Extensions;

public sealed class DataProtectionExtensionsTests : IDisposable
{
    private readonly string _keysDir = Path.Combine(Path.GetTempPath(), "ta-dp-keys-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_keysDir))
            Directory.Delete(_keysDir, recursive: true);
    }

    private static ServiceProvider BuildProvider(string? keysPath)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [DataProtectionExtensions.KeysPathKey] = keysPath })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGatewayDataProtection(config);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddGatewayDataProtection_SetsApplicationName()
    {
        using var provider = BuildProvider(keysPath: null);

        var options = provider.GetRequiredService<IOptions<DataProtectionOptions>>().Value;

        Assert.Equal(DataProtectionExtensions.ApplicationName, options.ApplicationDiscriminator);
    }

    [Fact]
    public void AddGatewayDataProtection_WithKeysPath_WritesKeyToDirectory()
    {
        using var provider = BuildProvider(_keysDir);

        provider.GetRequiredService<IDataProtectionProvider>().CreateProtector("test").Protect("hello");

        Assert.NotEmpty(Directory.GetFiles(_keysDir, "key-*.xml"));
    }

    [Fact]
    public void AddGatewayDataProtection_WithKeysPath_DataSurvivesNewInstance()
    {
        // Как пересоздание контейнера: новый процесс с тем же volume расшифровывает старые данные.
        string protectedValue;
        using (var first = BuildProvider(_keysDir))
        {
            protectedValue = first.GetRequiredService<IDataProtectionProvider>().CreateProtector("cookie").Protect("payload");
        }

        using var second = BuildProvider(_keysDir);
        var restored = second.GetRequiredService<IDataProtectionProvider>().CreateProtector("cookie").Unprotect(protectedValue);

        Assert.Equal("payload", restored);
    }
}
