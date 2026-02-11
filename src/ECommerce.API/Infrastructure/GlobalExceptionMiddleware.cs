using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ECommerce.Core.Exceptions;

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

                // Exception tipine göre status code ve mesaj belirle
                var (statusCode, userMessage) = MapException(ex);

                // Sadece 500 hataları ERROR seviyesinde logla, diğerleri WARNING
                if (statusCode >= 500)
                {
                    _logger.LogError(ex, "Unhandled exception for {Method} {Path} TraceId={TraceId}",
                        context.Request.Method, path, traceId);
                }
                else
                {
                    _logger.LogWarning(ex, "Handled exception ({StatusCode}) for {Method} {Path} TraceId={TraceId}",
                        statusCode, context.Request.Method, path, traceId);
                }

                // Varsa ILogService üzerinden de audit/log yaz
                try
                {
                    var logService = context.RequestServices.GetService<ECommerce.Core.Interfaces.ILogService>();
                    logService?.Error(ex, "Unhandled exception", new System.Collections.Generic.Dictionary<string, object?>
                    {
                        ["Path"] = path,
                        ["TraceId"] = traceId,
                        ["Method"] = context.Request.Method,
                        ["StatusCode"] = statusCode
                    });
                }
                catch
                {
                    // Logging sırasında hata olsa bile ana hatayı bastırma
                }

                if (!context.Response.HasStarted)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = statusCode;
                    context.Response.ContentType = "application/json";
                    var payload = new
                    {
                        status = statusCode,
                        message = userMessage,
                        traceId
                    };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
                }
            }
        }

        /// <summary>
        /// Exception tipine göre HTTP status code ve kullanıcıya gösterilecek mesajı belirler.
        /// Bilinmeyen exception'larda detay sızdırmaz, generic mesaj döner.
        /// </summary>
        private static (int StatusCode, string Message) MapException(Exception ex)
        {
            return ex switch
            {
                ValidationException ve =>
                    (StatusCodes.Status400BadRequest, ve.Message),

                NotFoundException nfe =>
                    (StatusCodes.Status404NotFound, nfe.Message),

                BusinessException be =>
                    (StatusCodes.Status422UnprocessableEntity, be.Message),

                UnauthorizedAccessException =>
                    (StatusCodes.Status401Unauthorized, "Bu işlem için yetkiniz bulunmamaktadır."),

                ArgumentException ae =>
                    (StatusCodes.Status400BadRequest, ae.Message),

                OperationCanceledException =>
                    (StatusCodes.Status400BadRequest, "İstek iptal edildi."),

                _ =>
                    (StatusCodes.Status500InternalServerError, "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin.")
            };
        }
    }
}
