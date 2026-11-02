using FluentAssertions;
using Serilog.Core;
using Serilog.Events;
using TravelAgency.Identity.API.Logging;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Features.Profile.Commands.UpdateProfile;

namespace TravelAgency.Identity.IntegrationTests.API;

public class PiiMaskingPolicyTests
{
    [Fact]
    public void UpdateProfile_MasksNameAndPhone_AndDoesNotDescribeRemovedFields()
    {
        var policy = new PiiMaskingDestructuringPolicy();
        var request = new UpdateProfileRequest("Марина", "Иванова", "+79991234567");

        policy.TryDestructure(request, new ScalarFactory(), out var requestValue).Should().BeTrue();
        var requestText = requestValue!.ToString();
        requestText.Should().Contain("Ма***");
        requestText.Should().Contain("Ив***");
        requestText.Should().NotContain("Марина");
        requestText.Should().NotContain("Иванова");
        requestText.Should().NotContain("+79991234567");
        requestText.Should().Contain("***");
        foreach (var name in new[] { "City", "About", "TourismSince", "Languages", "Specialization" })
            requestText.Should().NotContain(name);

        policy.TryDestructure(new UpdateProfileCommand(request), new ScalarFactory(), out var commandValue).Should().BeTrue();
        var commandText = commandValue!.ToString();
        commandText.Should().NotContain("Марина");
        commandText.Should().NotContain("+79991234567");
    }

    private sealed class ScalarFactory : ILogEventPropertyValueFactory
    {
        public LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects = false) =>
            new ScalarValue(value);
    }
}
