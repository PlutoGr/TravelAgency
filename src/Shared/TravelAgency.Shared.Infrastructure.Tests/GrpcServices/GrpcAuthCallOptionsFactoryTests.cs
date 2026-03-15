using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TravelAgency.Shared.Infrastructure.GrpcServices;

namespace TravelAgency.Shared.Infrastructure.Tests.GrpcServices;

/// <summary>
/// Tests for AUDIT-004: GrpcAuthCallOptionsFactory fail-fast when token is missing.
/// </summary>
public class GrpcAuthCallOptionsFactoryTests
{
    [Fact]
    public void Create_WhenTokenConfigured_ReturnsCallOptionsWithAuthHeader()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GrpcSettings:InternalServiceToken"] = "test-internal-token"
            })
            .Build();

        var factory = new GrpcAuthCallOptionsFactory(configuration);
        var options = factory.Create(CancellationToken.None);

        options.Headers.Should().NotBeNull();
        var authEntry = options.Headers!.FirstOrDefault(h =>
            string.Equals(h.Key, "x-internal-auth", StringComparison.OrdinalIgnoreCase));
        authEntry.Should().NotBeNull();
        authEntry!.Value.Should().Be("test-internal-token");
    }

    [Fact]
    public void Create_WhenTokenIsNull_ThrowsInvalidOperationException()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GrpcSettings:InternalServiceToken"] = null
            })
            .Build();

        var factory = new GrpcAuthCallOptionsFactory(configuration);

        var act = () => factory.Create(CancellationToken.None);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*GrpcSettings:InternalServiceToken*required*");
    }

    [Fact]
    public void Create_WhenTokenIsEmpty_ThrowsInvalidOperationException()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GrpcSettings:InternalServiceToken"] = ""
            })
            .Build();

        var factory = new GrpcAuthCallOptionsFactory(configuration);

        var act = () => factory.Create(CancellationToken.None);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*GrpcSettings:InternalServiceToken*required*");
    }

    [Fact]
    public void Create_WhenTokenIsWhitespace_ThrowsInvalidOperationException()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GrpcSettings:InternalServiceToken"] = "   "
            })
            .Build();

        var factory = new GrpcAuthCallOptionsFactory(configuration);

        var act = () => factory.Create(CancellationToken.None);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*GrpcSettings:InternalServiceToken*required*");
    }
}
