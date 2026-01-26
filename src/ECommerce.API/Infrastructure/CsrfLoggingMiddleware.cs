using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Infrastructure
{
    /// <summary>
    /// Catches Antiforgery validation failures and logs helpful, non-sensitive request context
    /// so we can monitor possible CSRF attacks or misconfigured clients.
    /// </summary>
    public class CsrfLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CsrfLoggingMiddleware> _logger;

        public CsrfLoggingMiddleware(RequestDelegate next, ILogger<CsrfLoggingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
        {
            // Only validate for unsafe HTTP methods
            var method = context.Request.Method.ToUpperInvariant();
            if (method == "GET" || method == "HEAD" || method == "OPTIONS")
            {
                await _next(context);
                return;
            }

            // Skip CSRF for ALL API routes - SPA uses JWT Authorization header
            var path = context.Request.Path.Value ?? "";
            if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
            // Skip CSRF for SignalR hubs (negotiate/postback uses auth headers or query token)
            if (path.StartsWith("/hubs", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            try
            {
                await antiforgery.ValidateRequestAsync(context);
                await _next(context);
            }
            catch (AntiforgeryValidationException ex)
            {
                // Log concise, non-sensitive context for diagnostics
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var requestPath = context.Request.Path;
                var ua = context.Request.Headers["User-Agent"].ToString();
                var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;

                _logger.LogWarning(ex, "CSRF validation failed for {Method} {Path}{Query} from {IP}. UserAgent={UserAgent}",
                    context.Request.Method, requestPath, query, ip, ua);

                // Return a generic 400 without exposing details
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing CSRF token" });
                return;
            }
        }
    }
}
