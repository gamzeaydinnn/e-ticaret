using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
namespace ECommerce.Core.Helpers
{
    public static class HashingHelper
    {
        public static string HashPassword(string password)
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 10000, 256 / 8));
            return $"{Convert.ToBase64String(salt)}.{hashed}";
        }
public static bool VerifyPassword(string hashedWithSalt, string password)
        {
            try
            {
                var parts = hashedWithSalt.Split('.');
                if (parts.Length != 2) return false;

                var salt = Convert.FromBase64String(parts[0]);
                var storedBase64 = parts[1];

                // Recompute hash with the same parameters
                var computedBase64 = Convert.ToBase64String(
                    KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 10000, 256 / 8)
                );

                // Constant-time compare to avoid timing attacks
                var storedBytes = Convert.FromBase64String(storedBase64);
                var computedBytes = Convert.FromBase64String(computedBase64);
                return CryptographicOperations.FixedTimeEquals(storedBytes, computedBytes);
            }
            catch (FormatException)
            {
                // Invalid base64 input
                return false;
            }
        }
    }
}
