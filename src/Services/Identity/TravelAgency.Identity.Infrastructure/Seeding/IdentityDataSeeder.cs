using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.Infrastructure.Seeding;

/// <summary>
/// Seeds test users when the database is empty. Only runs in Development or when explicitly enabled.
/// Test accounts (password: Test123!):
/// - client@test.com (Client)
/// - manager@test.com (Manager)
/// - admin@test.com (Admin)
/// </summary>
public sealed class IdentityDataSeeder(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    IConfiguration configuration,
    ILogger<IdentityDataSeeder> logger) : IHostedService
{
    private const string TestPassword = "Test123!";

    private static readonly (string Email, string FirstName, string LastName, UserRole Role)[] TestUsers =
    [
        ("client@test.com", "Клиент", "Тестов", UserRole.Client),
        ("manager@test.com", "Менеджер", "Тестов", UserRole.Manager),
        ("admin@test.com", "Админ", "Тестов", UserRole.Admin),
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() &&
            !string.Equals(configuration["ASPNETCORE_SEED_DATA"], "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var anyExists = await userRepository.ExistsAsync(TestUsers[0].Email, cancellationToken);
            if (anyExists)
            {
                logger.LogDebug("Seed users already exist, skipping Identity seed");
                return;
            }

            var passwordHash = passwordHasher.Hash(TestPassword);
            foreach (var (email, firstName, lastName, role) in TestUsers)
            {
                if (await userRepository.ExistsAsync(email, cancellationToken))
                    continue;

                var user = User.Create(email, passwordHash, firstName, lastName, null, role);
                await userRepository.AddAsync(user, cancellationToken);
                logger.LogInformation("Seeded user {Email} with role {Role}", email, role);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Identity seed failed (non-fatal)");
        }

        await Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
