using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using ECommerce.Infrastructure.Config;

namespace ECommerce.Infrastructure.Services.Payment
{
    public static class IyzicoWebhookValidator
    {
        // Minimal HMAC-SHA256 over token (form) using secret key.
        // Iyzipay has different callback variants; this implements a conservative check
        // that the request includes a signature header and that it matches HMAC(token).
        public static bool Validate(HttpRequest request, PaymentSettings settings)
        {
            try
            {
                var secret = settings.IyzicoSecretKey ?? string.Empty;
                if (string.IsNullOrWhiteSpace(secret)) return false;

                // Prefer common header names if provider sets them
                var sigHeader = request.Headers["Iyzico-Signature"].ToString();
                if (string.IsNullOrWhiteSpace(sigHeader)) sigHeader = request.Headers["Authorization"].ToString();
                if (string.IsNullOrWhiteSpace(sigHeader)) sigHeader = request.Headers["X-Iyzipay-Signature"].ToString();

                if (string.IsNullOrWhiteSpace(sigHeader)) return false;

                // read token from form if available, otherwise body
                string token = string.Empty;
                if (request.HasFormContentType && request.Form.ContainsKey("token"))
                {
                    token = request.Form["token"].ToString();
                }
                else
                {
                    request.Body.Position = 0;
                    using var sr = new System.IO.StreamReader(request.Body, leaveOpen: true);
                    token = sr.ReadToEndAsync().GetAwaiter().GetResult();
                    request.Body.Position = 0;
                }

                if (string.IsNullOrWhiteSpace(token)) return false;

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(token));
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
