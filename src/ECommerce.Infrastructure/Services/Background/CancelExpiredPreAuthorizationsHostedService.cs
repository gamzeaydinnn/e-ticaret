// ═══════════════════════════════════════════════════════════════════════════════════════════════
// CANCEL EXPIRED PRE-AUTHORIZATIONS BACKGROUND SERVICE
// MADDE 20: Süresi dolan provizyonları otomatik iptal eden zamanlanmış görev
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// NEDEN BU YAPIYI SEÇTİK?
// 1. .NET BackgroundService — Bağımlılık olmadan built-in scheduler
// 2. Her gece 02:00 TR saatinde çalışır (düşük yük saati)
// 3. WeightBasedPaymentService.CancelExpiredPreAuthorizationsAsync() çağırır
// 4. Log tabanlı izleme — alerting için log.error kullanılıyor
// ═══════════════════════════════════════════════════════════════════════════════════════════════

using System;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services.Background
{
    /// <summary>
    /// Süresi dolan Pre-Authorization provizyonlarını otomatik iptal eden BackgroundService.
    ///
    /// ── MADDE 20: Zamanlanmış iş (Scheduled Job) ──────────────────────────────────
    /// POSNET provizyon süresi 7 gündür (168 saat). Bu süreyi geçen ve
    /// finalize edilmemiş provizyonlar banka tarafından otomatik düşürülür,
    /// ancak sistemdeki Order durumu PreAuthorized olarak kalır.
    ///
    /// Bu servis:
    /// - Her gece saat 02:00 (TR saati) çalışır
    /// - Süresi dolan provizyonları POSNET'te iptal eder (Reverse)
    /// - Order durumunu günceller (WeightAdjustmentStatus = Failed)
    /// - Detaylı log bırakır
    ///
    /// PROGRAM.CS'E KAYIT:
    /// builder.Services.AddHostedService&lt;CancelExpiredPreAuthorizationsHostedService&gt;();
    /// </summary>
    public class CancelExpiredPreAuthorizationsHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CancelExpiredPreAuthorizationsHostedService> _logger;

        // Her 24 saatte bir çalış
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

        // Her gece kaçta çalışsın (TR saati): 02:00
        private static readonly TimeSpan RunAtTimeOfDay = TimeSpan.FromHours(2);

        public CancelExpiredPreAuthorizationsHostedService(
            IServiceProvider serviceProvider,
            ILogger<CancelExpiredPreAuthorizationsHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "[CANCEL-EXPIRED-PREAUTH] BackgroundService başlatıldı. " +
                "Her gece 02:00 TR saatinde çalışacak.");

            // İlk çalıştırmada bir sonraki 02:00'a kadar bekle
            var delay = CalculateDelayToNextRun();
            _logger.LogInformation(
                "[CANCEL-EXPIRED-PREAUTH] İlk çalışma için {Delay} bekleniyor...",
                delay);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(delay, stoppingToken);

                    if (stoppingToken.IsCancellationRequested)
                        break;

                    await RunCancellationJobAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Uygulama kapanıyor, normal davranış
                    _logger.LogInformation("[CANCEL-EXPIRED-PREAUTH] Servis durduruldu.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[CANCEL-EXPIRED-PREAUTH] Provizyon iptal işlemi sırasında beklenmeyen hata oluştu.");
                }

                // Bir sonraki 02:00'a kadar bekle
                delay = CalculateDelayToNextRun();
                _logger.LogInformation(
                    "[CANCEL-EXPIRED-PREAUTH] Sonraki çalışma: {NextRun}",
                    DateTime.UtcNow.Add(delay).ToString("yyyy-MM-dd HH:mm:ss UTC"));
            }
        }

        /// <summary>
        /// Süresi dolan provizyonları iptal etme işini çalıştırır.
        /// </summary>
        private async Task RunCancellationJobAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "[CANCEL-EXPIRED-PREAUTH] Provizyon iptal işi başlatıldı. Zaman: {Now}",
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));

            try
            {
                // Scoped servisler için yeni scope oluştur
                using var scope = _serviceProvider.CreateScope();
                var paymentService = scope.ServiceProvider.GetRequiredService<IWeightBasedPaymentService>();

                var cancelledCount = await paymentService.CancelExpiredPreAuthorizationsAsync(stoppingToken);

                if (cancelledCount > 0)
                {
                    _logger.LogWarning(
                        "[CANCEL-EXPIRED-PREAUTH] ⚠️ {Count} adet süresi dolan provizyon iptal edildi. " +
                        "Bu siparişlerin tekrar ödeme alınması gerekebilir.",
                        cancelledCount);
                }
                else
                {
                    _logger.LogInformation(
                        "[CANCEL-EXPIRED-PREAUTH] ✅ Süresi dolan provizyon bulunamadı. İşlem tamamlandı.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[CANCEL-EXPIRED-PREAUTH] Provizyon iptal servis çağrısı başarısız oldu.");
                throw; // Üst seviyede yakalanacak
            }
        }

        /// <summary>
        /// Bir sonraki 02:00 TR saatine kadar olan bekleme süresini hesaplar.
        /// </summary>
        private static TimeSpan CalculateDelayToNextRun()
        {
            TimeZoneInfo turkeyTz;
            try { turkeyTz = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
            catch { turkeyTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }

            var nowTr = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTz);
            var nextRun = nowTr.Date.Add(RunAtTimeOfDay);

            // Eğer bugün 02:00'ı geçtiyse, yarın 02:00'a ayarla
            if (nowTr >= nextRun)
            {
                nextRun = nextRun.AddDays(1);
            }

            var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRun, turkeyTz);
            var delay = nextRunUtc - DateTime.UtcNow;

            // Negatif delay koruması
            return delay < TimeSpan.Zero ? TimeSpan.FromMinutes(1) : delay;
        }
    }
}
