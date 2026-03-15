using TravelAgency.Identity.Application.Interfaces;

namespace TravelAgency.Identity.Infrastructure.Services;

public sealed class PasswordHasherService : IPasswordHasher
{
    private const int DefaultWorkFactor = 11;

    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: DefaultWorkFactor);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
