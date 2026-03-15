namespace TravelAgency.Identity.Application.Settings;

public class LockoutSettings
{
    public const string SectionName = "LockoutSettings";

    public int LockoutThreshold { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 15;
}
