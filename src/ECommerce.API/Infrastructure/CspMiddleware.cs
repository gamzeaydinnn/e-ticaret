using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ECommerce.API.Infrastructure
{
    /// <summary>
    /// Tüm HTTP güvenlik header'larını ekleyen middleware.
    /// CSP (Content-Security-Policy) + OWASP önerilen güvenlik header'ları.
    /// NEDEN tek middleware: Her response'a tutarlı şekilde eklenmesi gerektiği için
    /// ayrı middleware'ler yerine tek noktada yönetim tercih edildi.
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
            var headers = context.Response.Headers;

            // ═══════════════════════════════════════════════════════════
            // 1) CSP - Content Security Policy (per-request nonce ile)
            // Nonce tabanlı yaklaşım: inline script/style sadece doğru
            // nonce ile çalışır, XSS saldırılarını engeller
            // ═══════════════════════════════════════════════════════════
            var nonce = GenerateNonce();
            context.Items["CSPNonce"] = nonce;

            var csp = new StringBuilder();
            csp.Append("default-src 'self'; ");
            // eval() ve unsafe-inline engellenir, sadece nonce ile izin verilir
            csp.Append("script-src 'self' 'nonce-").Append(nonce).Append("'; ");
            csp.Append("style-src 'self' 'unsafe-inline'; "); // Bootstrap/Tailwind inline stilleri için gerekli
            csp.Append("img-src 'self' data: https:; "); // CDN görselleri ve data URI'ler için
            csp.Append("font-src 'self' https://fonts.gstatic.com; "); // Google Fonts desteği
            csp.Append("connect-src 'self' wss: https:; "); // SignalR WebSocket + API bağlantıları
            csp.Append("object-src 'none'; ");
            csp.Append("frame-ancestors 'none'; "); // Clickjacking koruması (X-Frame-Options yedeği)
            csp.Append("base-uri 'self'; ");
            csp.Append("form-action 'self';"); // Form gönderimini kısıtla

            headers["Content-Security-Policy"] = csp.ToString();

            // ═══════════════════════════════════════════════════════════
            // 2) X-Content-Type-Options
            // MIME type sniffing saldırısını engeller
            // Tarayıcı Content-Type header'ına güvenmek zorunda kalır
            // ═══════════════════════════════════════════════════════════
            headers["X-Content-Type-Options"] = "nosniff";

            // ═══════════════════════════════════════════════════════════
            // 3) X-Frame-Options
            // Clickjacking saldırısını engeller (eski tarayıcı desteği)
            // CSP frame-ancestors ile aynı görevi yapar ama IE11 için gerekli
            // ═══════════════════════════════════════════════════════════
            headers["X-Frame-Options"] = "DENY";

            // ═══════════════════════════════════════════════════════════
            // 4) Referrer-Policy
            // Kullanıcının hangi sayfadan geldiği bilgisini kısıtlar
            // strict-origin-when-cross-origin: Aynı site içinde tam URL,
            // farklı sitelere sadece origin (domain) gönderilir
            // ═══════════════════════════════════════════════════════════
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // ═══════════════════════════════════════════════════════════
            // 5) Permissions-Policy (eski adı: Feature-Policy)
            // Tarayıcı özelliklerine erişimi kısıtlar
            // E-ticaret sitesi için kamera, mikrofon, geolocation gereksiz
            // ═══════════════════════════════════════════════════════════
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(self), payment=(self)";

            // ═══════════════════════════════════════════════════════════
            // 6) X-XSS-Protection
            // Eski tarayıcılarda XSS filtresi (modern tarayıcılarda CSP yeterli)
            // 1; mode=block: XSS algılanırsa sayfayı tamamen engelle
            // ═══════════════════════════════════════════════════════════
            headers["X-XSS-Protection"] = "1; mode=block";

            // ═══════════════════════════════════════════════════════════
            // 7) Cache-Control (hassas API endpoint'leri için)
            // API yanıtlarının proxy/CDN'de cache'lenmesini engeller
            // Kişisel veri içeren response'lar paylaşılan cache'te kalmamalı
            // ═══════════════════════════════════════════════════════════
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
            if (path.StartsWith("/api/"))
            {
                headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                headers["Pragma"] = "no-cache";
            }

            await _next(context);
        }

        /// <summary>
        /// Kriptografik olarak güvenli, tahmin edilemez nonce üretir.
        /// Her request için benzersiz olması CSP nonce güvenliğinin temelidir.
        /// </summary>
        private static string GenerateNonce()
        {
            var bytes = new byte[16];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
