namespace TravelAgency.Identity.Application.Interfaces;

/// <summary>
/// Tracks failed login attempts and enforces account lockout to prevent brute-force attacks.
/// </summary>
public interface ILockoutService
{
    /// <summary>
    /// Returns true if the given email is currently locked out.
    /// </summary>
    bool IsLockedOut(string email);

    /// <summary>
    /// Records a failed login attempt for the given email. Call on invalid credentials.
    /// </summary>
    void RecordFailedAttempt(string email);

    /// <summary>
    /// Clears failed attempts for the given email. Call on successful login.
    /// </summary>
    void ResetFailedAttempts(string email);
}
