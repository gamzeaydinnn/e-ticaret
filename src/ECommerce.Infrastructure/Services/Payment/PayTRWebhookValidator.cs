using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using ECommerce.Infrastructure.Config;

namespace ECommerce.Infrastructure.Services.Payment
{
    public static class PayTRWebhookValidator
    {
        // Minimal validator: compute HMAC-SHA256 over request body using PayTRSecretKey
        // and compare to header 'X-Paytr-Signature' (common convention).
        public static bool Validate(HttpRequest request, PaymentSettings settings)
        {
            try
            {
                var secret = settings.PayTRSecretKey ?? string.Empty;
                if (string.IsNullOrWhiteSpace(secret)) return false;

                var sigHeader = request.Headers["X-Paytr-Signature"].ToString();
                if (string.IsNullOrWhiteSpace(sigHeader)) sigHeader = request.Headers["PayTR-Signature"].ToString();
                if (string.IsNullOrWhiteSpace(sigHeader)) return false;

                request.Body.Position = 0;
                using var sr = new System.IO.StreamReader(request.Body, leaveOpen: true);
                var body = sr.ReadToEndAsync().GetAwaiter().GetResult();
                request.Body.Position = 0;

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
                var expected = Convert.ToBase64String(hash);

                return string.Equals(expected, sigHeader, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }
    }
}
