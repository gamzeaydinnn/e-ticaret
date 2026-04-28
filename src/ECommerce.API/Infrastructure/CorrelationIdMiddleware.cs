using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Infrastructure
{
    /// <summary>
    /// Correlation ID middleware — her HTTP isteğine ve sync operasyonuna
    /// benzersiz bir izleme ID'si atar.
    /// 
    /// NEDEN: Dağıtık sistemde bir sync hatasını uçtan uca takip etmek için
    /// (API → HotPoll → DB → SignalR → Frontend) aynı correlation ID kullanılır.
    /// 
    /// AKIŞ:
    /// 1. İstek geldiğinde X-Correlation-ID header'ı var mı kontrol et
    /// 2. Yoksa yeni GUID oluştur
    /// 3. HttpContext.Items'a yaz (downstream servisler okuyabilir)
    /// 4. ILogger scope'una ekle (tüm loglar bu ID ile taglenir)
    /// 5. Response header'ına ekle (frontend/postman görebilir)
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        public const string HeaderName = "X-Correlation-ID";
        public const string ContextKey = "CorrelationId";

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
        {
            // Gelen header'dan oku veya yeni oluştur
            var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                ?? Guid.NewGuid().ToString("N")[..12]; // Kısa format: 12 karakter hex

            // HttpContext'e yaz — controller/service'ler erişebilir
            context.Items[ContextKey] = correlationId;

            // TraceIdentifier'ı da override et — GlobalExceptionMiddleware bunu kullanıyor
            context.TraceIdentifier = correlationId;

            // Response header'ına ekle — frontend hatayı raporlarken bu ID'yi gönderebilir
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            // Tüm logger çıktılarına CorrelationId ekle
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            }))
            {
                await _next(context);
            }
        }
    }

    /// <summary>
    /// Correlation ID'yi herhangi bir servis katmanından almak için yardımcı.
    /// Constructor injection ile IHttpContextAccessor üzerinden kullanılır.
    /// 
    /// KULLANIM:
    /// var correlationId = _correlationContext.CorrelationId;
    /// _logger.LogInformation("[HotPoll] İşlem başladı. CID: {CID}", correlationId);
    /// </summary>
    public interface ICorrelationContext
    {
        string CorrelationId { get; }
    }

    public class CorrelationContext : ICorrelationContext
    {
        private readonly IHttpContextAccessor _accessor;

        public CorrelationContext(IHttpContextAccessor accessor)
        {
            _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        }

        public string CorrelationId =>
            _accessor.HttpContext?.Items[CorrelationIdMiddleware.ContextKey]?.ToString()
            ?? Guid.NewGuid().ToString("N")[..12]; // Background service'lerde HttpContext yok
    }
}
