using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ECommerce.API.Infrastructure
{
    /// <summary>
    /// Adds a Content-Security-Policy header with a per-request nonce exposed at HttpContext.Items["CSPNonce"].
    /// Conservative default: allow scripts/styles from 'self' and those using the generated nonce; block eval.
    /// </summary>
    public class CspMiddleware
    {
        private readonly RequestDelegate _next;

        public CspMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Generate a cryptographic nonce per request
            var nonce = GenerateNonce();
            context.Items["CSPNonce"] = nonce;

            // Build a conservative CSP. Adjust according to your asset pipeline (CDN, inline styles, etc.).
            var csp = new StringBuilder();
            // default-src: only self
            csp.Append("default-src 'self'; ");
            // block eval and unsafe-inline except for nonce-based inline
            csp.Append("script-src 'self' 'nonce-").Append(nonce).Append("'; ");
            csp.Append("style-src 'self' 'nonce-").Append(nonce).Append("'; ");
            // restrict other common sources
            csp.Append("img-src 'self' data:; ");
            csp.Append("font-src 'self'; ");
            csp.Append("object-src 'none'; frame-ancestors 'none'; base-uri 'self';");

            context.Response.Headers["Content-Security-Policy"] = csp.ToString();

            await _next(context);
        }

        private static string GenerateNonce()
        {
            var bytes = new byte[16];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
