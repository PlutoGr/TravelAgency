namespace TravelAgency.Identity.Application.Settings;

public class JwtSettings
{
    public string Issuer { get; set; } = "TravelAgency.Identity";
    public string Audience { get; set; } = "TravelAgency";
    public string SigningKey { get; set; } = "";
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public bool ValidateLifetime { get; set; } = true;
}
