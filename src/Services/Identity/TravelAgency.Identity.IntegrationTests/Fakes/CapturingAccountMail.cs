using TravelAgency.Identity.Application.Abstractions;

namespace TravelAgency.Identity.IntegrationTests.Fakes;

public sealed class CapturingAccountMail : IAccountMailSender
{
    public List<AccountMail> Calls { get; } = [];

    public bool Accept { get; set; } = true;

    public int Io { get; private set; }

    public Task<bool> SendAsync(AccountMail mail, CancellationToken cancellationToken = default)
    {
        Calls.Add(mail);
        if (!mail.Deliver)
            return Task.FromResult(false);

        Io++;
        return Task.FromResult(Accept);
    }

    public void Reset()
    {
        Calls.Clear();
        Io = 0;
        Accept = true;
    }
}
