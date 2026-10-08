namespace TravelAgency.Media.Infrastructure.Maintenance;

/// <summary>
/// Opt-in process arguments. A normal service start does not run the backfill.
/// </summary>
public static class MediaDimensionBackfillCommand
{
    public const string Name = "backfill-dimensions";
    public const string DryRunFlag = "--dry-run";

    public static bool IsRequested(IReadOnlyList<string> args) =>
        args.Any(arg => string.Equals(arg, Name, StringComparison.Ordinal));

    public static bool IsDryRun(IReadOnlyList<string> args) =>
        args.Any(arg => string.Equals(arg, DryRunFlag, StringComparison.Ordinal));

    /// <summary>
    /// Drops the command flags so the generic host does not treat them as configuration switches.
    /// </summary>
    public static string[] WithoutCommandArgs(string[] args) =>
        args.Where(arg =>
                !string.Equals(arg, Name, StringComparison.Ordinal)
                && !string.Equals(arg, DryRunFlag, StringComparison.Ordinal))
            .ToArray();
}
