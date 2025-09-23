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
            var parts = hashedWithSalt.Split('.');
            if (parts.Length != 2) return false;
            var salt = Convert.FromBase64String(parts[0]);
            var stored = parts[1];
            var hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 10000, 256 / 8));
            return stored == hashed;
        }
    }
}
