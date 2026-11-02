using TravelAgency.Identity.Application.Abstractions;

namespace TravelAgency.Identity.Infrastructure.Services;

/// <summary>
/// Заглушка до транспорта писем. Письмо не принимается, поэтому ссылка не сохраняется и на экран не попадает.
/// </summary>
public sealed class NoOpAccountMailSender : IAccountMailSender
{
    public Task<bool> SendAsync(AccountMail mail, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
