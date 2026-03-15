using Microsoft.Extensions.Options;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Identity.Infrastructure.Services;

namespace TravelAgency.Identity.UnitTests.Infrastructure.Services;

/// <summary>
/// AUDIT-002: InMemoryLockoutService records failed attempts, returns lockout status when
/// threshold exceeded, and clears lockout via Reset. Used as fallback when Redis not configured.
/// </summary>
public class InMemoryLockoutServiceTests
{
    private static InMemoryLockoutService CreateSut(int threshold = 5, int durationMinutes = 15)
    {
        var settings = new LockoutSettings
        {
            LockoutThreshold = threshold,
            LockoutDurationMinutes = durationMinutes
        };
        return new InMemoryLockoutService(Options.Create(settings));
    }

    [Fact]
    public void IsLockedOut_WhenNoAttempts_ReturnsFalse()
    {
        var sut = CreateSut();

        sut.IsLockedOut("user@example.com").Should().BeFalse();
    }

    [Fact]
    public void RecordFailedAttempt_WhenUnderThreshold_DoesNotLockOut()
    {
        var sut = CreateSut(threshold: 5);

        for (var i = 0; i < 4; i++)
            sut.RecordFailedAttempt("user@example.com");

        sut.IsLockedOut("user@example.com").Should().BeFalse();
    }

    [Fact]
    public void RecordFailedAttempt_WhenThresholdReached_LocksOut()
    {
        var sut = CreateSut(threshold: 5);

        for (var i = 0; i < 5; i++)
            sut.RecordFailedAttempt("user@example.com");

        sut.IsLockedOut("user@example.com").Should().BeTrue();
    }

    [Fact]
    public void IsLockedOut_WhenLockedOut_ReturnsTrue()
    {
        var sut = CreateSut(threshold: 3);

        sut.RecordFailedAttempt("a@test.com");
        sut.RecordFailedAttempt("a@test.com");
        sut.RecordFailedAttempt("a@test.com");

        sut.IsLockedOut("a@test.com").Should().BeTrue();
    }

    [Fact]
    public void ResetFailedAttempts_ClearsLockout()
    {
        var sut = CreateSut(threshold: 3);

        sut.RecordFailedAttempt("user@example.com");
        sut.RecordFailedAttempt("user@example.com");
        sut.RecordFailedAttempt("user@example.com");
        sut.IsLockedOut("user@example.com").Should().BeTrue();

        sut.ResetFailedAttempts("user@example.com");

        sut.IsLockedOut("user@example.com").Should().BeFalse();
    }

    [Fact]
    public void ResetFailedAttempts_ThenRecordAgain_CountsFromZero()
    {
        var sut = CreateSut(threshold: 3);

        sut.RecordFailedAttempt("user@example.com");
        sut.RecordFailedAttempt("user@example.com");
        sut.ResetFailedAttempts("user@example.com");

        sut.RecordFailedAttempt("user@example.com");
        sut.IsLockedOut("user@example.com").Should().BeFalse();

        sut.RecordFailedAttempt("user@example.com");
        sut.RecordFailedAttempt("user@example.com");
        sut.IsLockedOut("user@example.com").Should().BeTrue();
    }

    [Fact]
    public void NormalizesEmail_CaseInsensitive()
    {
        var sut = CreateSut(threshold: 2);

        sut.RecordFailedAttempt("User@Example.COM");
        sut.RecordFailedAttempt("user@example.com");

        sut.IsLockedOut("USER@EXAMPLE.COM").Should().BeTrue();
    }

    [Fact]
    public void DifferentEmails_HaveIndependentLockoutState()
    {
        var sut = CreateSut(threshold: 2);

        sut.RecordFailedAttempt("a@test.com");
        sut.RecordFailedAttempt("a@test.com");
        sut.RecordFailedAttempt("b@test.com");

        sut.IsLockedOut("a@test.com").Should().BeTrue();
        sut.IsLockedOut("b@test.com").Should().BeFalse();
    }
}
