// ═══════════════════════════════════════════════════════════════════════════════════════════════
// POSNET DEPENDENCY INJECTION EXTENSIONS
// Yapı Kredi POSNET servislerini DI container'a kayıt eden extension metodlar
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// NEDEN BU YAPIYI SEÇTİK?
// 1. Extension method pattern - Clean startup configuration
// 2. Named HttpClient - HttpClientFactory ile lifecycle yönetimi
// 3. Scoped lifetime - Request bazlı instance, DbContext uyumu
// 4. Security servisleri - Rate limiting, fraud detection, audit logging
// 5. Conditional SSL - Test ortamında bypass, Production'da tam doğrulama
// ═══════════════════════════════════════════════════════════════════════════════════════════════

using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using ECommerce.Infrastructure.Config;
using ECommerce.Infrastructure.Services.Payment.Posnet.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services.Payment.Posnet
{
    /// <summary>
    /// POSNET servislerini DI container'a kayıt eden extension metodlar
    /// </summary>
    public static class PosnetServiceExtensions
    {
        /// <summary>
        /// POSNET ödeme servislerini kayıt eder
        /// Program.cs veya Startup.cs'de çağrılmalı
        /// </summary>
        /// <param name="services">IServiceCollection instance</param>
        /// <returns>IServiceCollection (fluent chaining için)</returns>
        public static IServiceCollection AddPosnetPaymentServices(this IServiceCollection services)
        {
            // XML Builder - Singleton (stateless, thread-safe)
            services.AddSingleton<IPosnetXmlBuilder, PosnetXmlBuilder>();
            
            // XML Parser - Scoped (logger injection için)
            services.AddScoped<IPosnetXmlParser, PosnetXmlParser>();

            // HTTP Client - Named HttpClient with custom configuration
            // SSL sertifika doğrulaması ortama göre yapılandırılır
            services.AddHttpClient<IPosnetHttpClient, PosnetHttpClient>("PosnetHttpClient", (serviceProvider, client) =>
            {
                var settings = serviceProvider.GetRequiredService<IOptions<PaymentSettings>>().Value;
                
                // Timeout yapılandırması (varsayılan 60 saniye)
                client.Timeout = TimeSpan.FromSeconds(settings.PosnetTimeoutSeconds > 0 
                    ? settings.PosnetTimeoutSeconds 
                    : 60);
                
                // Default request headers
                client.DefaultRequestHeaders.Add("Accept", "application/xml");
                client.DefaultRequestHeaders.Add("User-Agent", "YapiKrediPosnet-DotNet/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            {
                // ═══════════════════════════════════════════════════════════════════════
                // SSL SERTİFİKA DOĞRULAMA YAPILANDIRMASI
                // 
                // GÜVENLİK KRİTİK: 
                // - Test ortamında: Self-signed sertifikalar için SSL bypass aktif
                // - Production ortamında: Tam SSL doğrulama, bypass kesinlikle devre dışı
                //
                // NEDEN BU YAKLAŞIM?
                // 1. Test ortamlarında genellikle self-signed veya geçersiz sertifikalar kullanılır
                // 2. Production'da MAN-IN-THE-MIDDLE saldırılarına karşı SSL doğrulama şart
                // 3. Yapılandırma bazlı kontrol - environment variable ile de kontrol edilebilir
                // ═══════════════════════════════════════════════════════════════════════
                
                var settings = serviceProvider.GetRequiredService<IOptions<PaymentSettings>>().Value;
                var logger = serviceProvider.GetRequiredService<ILogger<PosnetHttpClient>>();
                
                var handler = new HttpClientHandler
                {
                    // Otomatik sıkıştırma desteği - performans için
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | 
                                             System.Net.DecompressionMethods.Deflate
                };

                // Ortama göre SSL sertifika doğrulama yapılandırması
                if (settings.PosnetIsTestEnvironment)
                {
                    // ═══════════════════════════════════════════════════════════════
                    // TEST ORTAMI: SSL sertifika doğrulama bypass
                    // DİKKAT: Bu ayar sadece test/sandbox ortamları için!
                    // Self-signed veya geçersiz sertifikalı test sunucuları için gerekli
                    // ═══════════════════════════════════════════════════════════════
                    handler.ServerCertificateCustomValidationCallback = CreateTestEnvironmentSslValidator(logger);
                    
                    logger.LogWarning(
                        "[POSNET-SSL] ⚠️ TEST ORTAMI: SSL sertifika doğrulama DEVRE DIŞI! " +
                        "Bu ayar sadece test ortamı için uygundur. Production'da kesinlikle kullanmayın!");
                }
                else
                {
                    // ═══════════════════════════════════════════════════════════════
                    // PRODUCTION ORTAMI: Tam SSL sertifika doğrulama
                    // Varsayılan davranış - hiçbir bypass yok
                    // MAN-IN-THE-MIDDLE saldırılarına karşı koruma sağlar
                    // ═══════════════════════════════════════════════════════════════
                    handler.ServerCertificateCustomValidationCallback = CreateProductionSslValidator(logger);
                    
                    logger.LogInformation(
                        "[POSNET-SSL] ✅ PRODUCTION ORTAMI: SSL sertifika doğrulama AKTİF. " +
                        "Tüm sertifikalar tam doğrulamadan geçecek.");
                }

                return handler;
            });

            // Ana POSNET service - Scoped (DbContext bağımlılığı için)
            services.AddScoped<IPosnetPaymentService, YapiKrediPosnetService>();
            
            // 3D Secure MAC Validator - Scoped (güvenlik servisi)
            services.AddScoped<IPosnetMacValidator, PosnetMacValidator>();
            
            // 3D Secure Callback Handler - Scoped (callback işleme servisi)
            services.AddScoped<IPosnet3DSecureCallbackHandler, Posnet3DSecureCallbackHandler>();
            
            // Transaction Log Service - Scoped (veritabanı loglama servisi)
            services.AddScoped<IPosnetTransactionLogService, PosnetTransactionLogService>();
            
            // ═══════════════════════════════════════════════════════════════
            // ADIM 8: GÜVENLİK & PRODUCTION SERVİSLERİ
            // ═══════════════════════════════════════════════════════════════
            
            // Security Service - Singleton (rate limiting, maskeleme, fraud detection)
            services.AddSingleton<IPosnetSecurityService, PosnetSecurityService>();
            
            // Audit Log Service - Scoped (detaylı işlem logları)
            services.AddScoped<IPosnetAuditLogService, PosnetAuditLogService>();
            
            // Mock Service - Singleton (test ortamı için)
            services.AddSingleton<IPosnetMockService>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<PaymentSettings>>().Value;
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PosnetMockService>>();
                // Mock sadece test ortamında aktif
                return new PosnetMockService(logger, settings.PosnetIsTestEnvironment);
            });
            
            // Health Check - Scoped
            services.AddHttpClient("PosnetHealthCheck", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            services.AddScoped<PosnetHealthCheck>();
            
            // IPaymentService olarak da kayıt (opsiyonel - PaymentManager'da kullanılacaksa)
            // Dikkat: Bu, IPaymentService'in varsayılan implementasyonunu override eder
            // services.AddScoped<IPaymentService, YapiKrediPosnetService>();

            return services;
        }

        /// <summary>
        /// POSNET servislerini basit modda kayıt eder (HttpClient yapılandırması olmadan)
        /// Test ortamları için uygundur
        /// </summary>
        public static IServiceCollection AddPosnetPaymentServicesSimple(this IServiceCollection services)
        {
            services.AddSingleton<IPosnetXmlBuilder, PosnetXmlBuilder>();
            services.AddScoped<IPosnetXmlParser, PosnetXmlParser>();
            services.AddHttpClient<IPosnetHttpClient, PosnetHttpClient>();
            services.AddScoped<IPosnetPaymentService, YapiKrediPosnetService>();

            return services;
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // SSL SERTİFİKA DOĞRULAMA METODLARI
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Test ortamı için SSL sertifika validator oluşturur
        /// Tüm sertifikaları kabul eder ama uyarı loglar
        /// 
        /// DİKKAT: Bu validator sadece test ortamı için kullanılmalıdır!
        /// Self-signed sertifikalar ve geçersiz sertifika zincirleri kabul edilir.
        /// </summary>
        private static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> 
            CreateTestEnvironmentSslValidator(ILogger logger)
        {
            return (request, cert, chain, errors) =>
            {
                // SSL hatası varsa logla ama yine de kabul et (test ortamı)
                if (errors != SslPolicyErrors.None)
                {
                    logger.LogWarning(
                        "[POSNET-SSL] Test ortamı SSL uyarısı - Host: {Host}, Hatalar: {Errors}, " +
                        "Sertifika: {Subject}, Geçerlilik: {ValidTo}",
                        request.RequestUri?.Host ?? "unknown",
                        errors.ToString(),
                        cert?.Subject ?? "N/A",
                        cert?.NotAfter.ToString("yyyy-MM-dd") ?? "N/A");
                }
                
                // Test ortamında her zaman kabul et
                return true;
            };
        }

        /// <summary>
        /// Production ortamı için SSL sertifika validator oluşturur
        /// Sadece geçerli sertifikaları kabul eder
        /// 
        /// GÜVENLİK: Bu validator MAN-IN-THE-MIDDLE saldırılarına karşı koruma sağlar.
        /// Geçersiz veya self-signed sertifikalar reddedilir.
        /// </summary>
        private static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> 
            CreateProductionSslValidator(ILogger logger)
        {
            return (request, cert, chain, errors) =>
            {
                // Hata yoksa kabul et
                if (errors == SslPolicyErrors.None)
                {
                    logger.LogDebug(
                        "[POSNET-SSL] Sertifika doğrulandı - Host: {Host}, Sertifika: {Subject}",
                        request.RequestUri?.Host ?? "unknown",
                        cert?.Subject ?? "N/A");
                    return true;
                }

                // ═══════════════════════════════════════════════════════════════
                // PRODUCTION'DA SSL HATALARI REDDEDİLİR
                // Olası hatalar:
                // - RemoteCertificateNameMismatch: Sertifika host adı eşleşmiyor
                // - RemoteCertificateChainErrors: Sertifika zinciri doğrulanamadı
                // - RemoteCertificateNotAvailable: Sertifika sunulmadı
                // ═══════════════════════════════════════════════════════════════
                
                logger.LogError(
                    "[POSNET-SSL] ❌ PRODUCTION SSL HATASI - Bağlantı reddedildi! " +
                    "Host: {Host}, Hatalar: {Errors}, Sertifika: {Subject}, " +
                    "Geçerlilik: {ValidFrom} - {ValidTo}. " +
                    "Bu bir güvenlik ihlali girişimi olabilir!",
                    request.RequestUri?.Host ?? "unknown",
                    errors.ToString(),
                    cert?.Subject ?? "N/A",
                    cert?.NotBefore.ToString("yyyy-MM-dd") ?? "N/A",
                    cert?.NotAfter.ToString("yyyy-MM-dd") ?? "N/A");

                // Sertifika zinciri hatalarını detaylı logla
                if (chain?.ChainStatus != null && chain.ChainStatus.Length > 0)
                {
                    foreach (var status in chain.ChainStatus)
                    {
                        logger.LogError(
                            "[POSNET-SSL] Sertifika zinciri hatası: {Status} - {StatusInfo}",
                            status.Status.ToString(),
                            status.StatusInformation);
                    }
                }

                // Production'da geçersiz sertifikayı REDDET
                return false;
            };
        }
    }
}
