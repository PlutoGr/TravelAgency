using FluentAssertions;
using TravelAgency.Identity.Domain.Enums;

namespace TravelAgency.Identity.UnitTests.Domain;

public class UserRoleExtensionsTests
{
    [Theory]
    [InlineData(UserRole.Client, "Client")]
    [InlineData(UserRole.Manager, "Manager")]
    [InlineData(UserRole.Admin, "Admin")]
    public void ToRoleString_ReturnsCorrectString(UserRole role, string expected)
    {
        role.ToRoleString().Should().Be(expected);
    }

    [Fact]
    public void ToRoleString_WithInvalidRole_ThrowsArgumentOutOfRangeException()
    {
        var invalidRole = (UserRole)999;

        var act = () => invalidRole.ToRoleString();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("Client", UserRole.Client)]
    [InlineData("Manager", UserRole.Manager)]
    [InlineData("Admin", UserRole.Admin)]
    public void FromString_WithValidString_ReturnsCorrectRole(string input, UserRole expected)
    {
        UserRoleExtensions.FromString(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("client")]
    [InlineData("ADMIN")]
    [InlineData("unknown")]
    [InlineData("")]
    public void FromString_WithInvalidString_ThrowsArgumentException(string input)
    {
        var act = () => UserRoleExtensions.FromString(input);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToRoleString_RoundTrip_ProducesOriginalRole()
    {
        foreach (var role in Enum.GetValues<UserRole>())
        {
            var str = role.ToRoleString();
            var back = UserRoleExtensions.FromString(str);
            back.Should().Be(role);
        }
    }
}
