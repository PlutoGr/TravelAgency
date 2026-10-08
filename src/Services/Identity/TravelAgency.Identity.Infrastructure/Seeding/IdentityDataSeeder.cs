using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Shared.Contracts.Seeding;
using TravelAgency.Shared.Infrastructure.Seeding;

namespace TravelAgency.Identity.Infrastructure.Seeding;

/// <summary>
/// Тестовые аккаунты. Уже существующий email сохраняет свой id.
/// Фиксированный id получает только новая строка.
/// Пароль берётся из конфигурации <see cref="DemoSeedGate.TestUserPasswordKey"/> и в код не записывается.
/// Пустой пароль — одно предупреждение, новые пользователи не создаются, процесс не падает.
/// </summary>
public sealed class IdentityDataSeeder(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    IConfiguration configuration,
    ILogger<IdentityDataSeeder> logger) : IHostedService
{
    private static readonly (Guid Id, string Email, string FirstName, string LastName, UserRole Role)[] TestUsers =
    [
        (DemoSeedIds.ClientId, "client@test.com", "Клиент", "Тестов", UserRole.Client),
        (DemoSeedIds.ManagerId, "manager@test.com", "Менеджер", "Тестов", UserRole.Manager),
        (DemoSeedIds.AdminId, "admin@test.com", "Админ", "Тестов", UserRole.Admin),
        (DemoSeedIds.Manager2Id, "manager2@test.com", "Менеджер", "Второй", UserRole.Manager),
    ];

    public static bool ShouldSeed(IHostEnvironment environment, IConfiguration configuration) =>
        DemoSeedGate.ShouldSeed(environment, configuration, includeDemoCatalogFlag: false);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!ShouldSeed(environment, configuration))
            return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await SeedAsync(db, passwordHasher, configuration, logger, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Identity seed failed (non-fatal)");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public static async Task SeedAsync(
        IdentityDbContext db,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var passwordHash = HashConfiguredPassword(passwordHasher, configuration);
        var skippedNewUser = false;

        foreach (var (id, email, firstName, lastName, role) in TestUsers)
        {
            var normalized = email.ToLowerInvariant();
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
            if (existing is not null)
                continue;

            if (passwordHash is null)
            {
                skippedNewUser = true;
                continue;
            }

            db.Users.Add(User.Create(normalized, passwordHash, firstName, lastName, null, role, id));
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded user {Email} with role {Role}", normalized, role);
        }

        if (skippedNewUser)
        {
            logger.LogWarning(
                "Skipping new seed users because {Setting} is empty",
                DemoSeedGate.TestUserPasswordKey);
        }
    }

    private static string? HashConfiguredPassword(IPasswordHasher passwordHasher, IConfiguration configuration)
    {
        var password = configuration[DemoSeedGate.TestUserPasswordKey];
        if (string.IsNullOrWhiteSpace(password))
            return null;

        return passwordHasher.Hash(password);
    }
}
