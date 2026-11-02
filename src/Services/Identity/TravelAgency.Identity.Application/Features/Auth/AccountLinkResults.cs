namespace TravelAgency.Identity.Application.Features.Auth;

public sealed record AccountMessage(string Message);

public sealed record AccountOutcome(string Outcome, string? Session = null);

public static class AccountSessionKind
{
    public const string Current = "current";
    public const string Other = "other";
    public const string None = "none";

    public static string From(Guid? signedInUserId, Guid accountId)
    {
        if (signedInUserId is null || signedInUserId == Guid.Empty)
            return None;
        return signedInUserId == accountId ? Current : Other;
    }
}
