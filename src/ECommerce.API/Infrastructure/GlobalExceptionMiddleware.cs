using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Infrastructure
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var traceId = context.TraceIdentifier;
                var path = context.Request.Path.ToString();

                // Önce standart logger'a yaz
                _logger.LogError(ex, "Unhandled exception for {Method} {Path} TraceId={TraceId}",
                    context.Request.Method, path, traceId);

                // Varsa ILogService üzerinden de audit/log yaz
                try
                {
                    var logService = context.RequestServices.GetService<ECommerce.Core.Interfaces.ILogService>();
                    logService?.Error(ex, "Unhandled exception", new System.Collections.Generic.Dictionary<string, object?>
                    {
                        ["Path"] = path,
                        ["TraceId"] = traceId,
                        ["Method"] = context.Request.Method
                    });
                }
                catch
                {
                    // Logging sırasında hata olsa bile ana hatayı bastırma
                }

                if (!context.Response.HasStarted)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";
                    var payload = new
                    {
                        status = context.Response.StatusCode,
                        message = "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin.",
                        traceId
                    };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
                }
            }
        }
    }
}
