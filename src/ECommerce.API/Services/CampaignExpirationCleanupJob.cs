using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ECommerce.Business.Services.Interfaces;

namespace ECommerce.API.Services
{
    /// <summary>
    /// Kampanya süresi dolan ürünlerin SpecialPrice değerlerini otomatik temizleyen background job.
    ///
    /// Bu servis şu işlemleri yapar:
    /// 1. Uygulama başladığında tüm ürünlerin kampanya durumunu kontrol eder
    /// 2. Düzenli aralıklarla (varsayılan: 1 saat) süresi dolan kampanyaları temizler
    /// 3. Aktif kampanyası olmayan ürünlerin SpecialPrice değerini null yapar
    ///
    /// PROBLEM ÇÖZÜMÜ:
    /// - Kampanya bittiğinde SpecialPrice veritabanında dolu kalıyordu
    /// - Bu job sayesinde süresi dolan kampanyalar otomatik temizlenir
    /// - Ana sayfada artık sadece aktif kampanyalı ürünler indirimli görünecek
    /// </summary>
    public class CampaignExpirationCleanupJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CampaignExpirationCleanupJob> _logger;

        // Kontrol aralığı: Her 1 saatte bir kampanya durumunu kontrol et
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

        // Startup'ta hemen ilk kontrolü yap (5 saniye bekleme)
        private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(5);

        public CampaignExpirationCleanupJob(
            IServiceScopeFactory scopeFactory,
            ILogger<CampaignExpirationCleanupJob> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "[CampaignExpirationCleanupJob] Kampanya temizleme servisi başlatıldı. " +
                "Kontrol aralığı: {Interval} saat",
                CheckInterval.TotalHours);

            // İlk kontrolü yapmadan önce kısa bir bekleme (DI container hazır olsun)
            await Task.Delay(InitialDelay, stoppingToken);

            // İlk çalıştırmada hemen kontrol et (uygulama yeniden başladığında eski kampanyaları temizle)
            await RecalculateCampaignPricesAsync(stoppingToken);

            // Sonra düzenli aralıklarla kontrol et
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(CheckInterval, stoppingToken);
                await RecalculateCampaignPricesAsync(stoppingToken);
            }
        }

        /// <summary>
        /// Tüm ürünlerin kampanya durumunu yeniden hesaplar.
        /// Aktif kampanyası olmayan ürünlerin SpecialPrice'ı null yapılır.
        /// </summary>
        private async Task RecalculateCampaignPricesAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("[CampaignExpirationCleanupJob] Kampanya fiyatları yeniden hesaplanıyor...");

                // Scoped service'leri almak için yeni scope oluştur
                using var scope = _scopeFactory.CreateScope();
                var campaignApplicationService = scope.ServiceProvider
                    .GetRequiredService<ICampaignApplicationService>();

                // Tüm ürünlerin SpecialPrice değerlerini kampanya durumuna göre güncelle
                var updatedCount = await campaignApplicationService.RecalculateAllCampaignsAsync();

                if (updatedCount > 0)
                {
                    _logger.LogInformation(
                        "[CampaignExpirationCleanupJob] Kampanya fiyatları güncellendi. " +
                        "Güncellenen ürün sayısı: {UpdatedCount}",
                        updatedCount);
                }
                else
                {
                    _logger.LogDebug(
                        "[CampaignExpirationCleanupJob] Kampanya kontrolü tamamlandı. " +
                        "Değişiklik yok.");
                }
            }
            catch (Exception ex)
            {
                // Hata olsa bile servis çalışmaya devam etsin
                _logger.LogError(
                    ex,
                    "[CampaignExpirationCleanupJob] Kampanya fiyatları hesaplanırken hata oluştu");
            }
        }
    }
}
