using System;
namespace ECommerce.Business.Services.Interfaces
{
    public interface ILoginRateLimitService
    {
        /// <summary>
        /// Returns true if the given email is currently blocked. Also outputs remaining block time.
        /// </summary>
        bool IsBlocked(string email, out TimeSpan remaining);

        /// <summary>
        /// Increment failed attempt counter for email and return current attempt count after increment.
        /// If threshold is reached, the email will be blocked for configured period.
        /// </summary>
        int IncrementFailedAttempt(string email);

        /// <summary>
        /// Reset attempts and any existing block for the email (call on successful login).
        /// </summary>
        void ResetAttempts(string email);

        /// <summary>
        /// Get current attempts count (0 if none).
        /// </summary>
        int GetAttempts(string email);
    }
}
