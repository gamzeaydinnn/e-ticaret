using System.Diagnostics;
using ECommerce.Core.Interfaces.Sync;
using ECommerce.Entities.Concrete;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Mikro ERP senkronizasyon orkestratörü.
    /// 
    /// NEDEN: Tüm sync servislerini koordine eden merkezi nokta.
    /// Hangfire job'ları, manuel tetiklemeler ve API controller'ları
    /// bu orkestratör üzerinden çalışır.
    /// 
    /// SORUMLULUKLAR:
    /// - Senkronizasyon sırasını yönetir
    /// - Bağımlılıkları çözümler (önce cari, sonra sipariş gibi)
    /// - Toplu operasyonları koordine eder
    /// - Hata izleme ve raporlama
    /// 
    /// KULLANIM:
    /// - Günlük tam sync: await orchestrator.RunFullSyncAsync();
    /// - Saatlik delta sync: await orchestrator.RunDeltaSyncAsync();
    /// - Sipariş push: await orchestrator.PushOrderAsync(orderId);
    /// </summary>
    public class MikroSyncOrchestrator : IMikroSyncService
    {
        // ==================== BAĞIMLILIKLAR ====================

        private readonly IStokSyncService _stokSyncService;
        private readonly ISiparisSyncService _siparisSyncService;
        private readonly ICariSyncService _cariSyncService;
        private readonly IFiyatSyncService _fiyatSyncService;
        private readonly IMikroSyncRepository _syncRepository;
        private readonly ILogger<MikroSyncOrchestrator> _logger;

        // ==================== CONSTRUCTOR ====================

        public MikroSyncOrchestrator(
            IStokSyncService stokSyncService,
            ISiparisSyncService siparisSyncService,
            ICariSyncService cariSyncService,
            IFiyatSyncService fiyatSyncService,
            IMikroSyncRepository syncRepository,
            ILogger<MikroSyncOrchestrator> logger)
        {
            _stokSyncService = stokSyncService ?? throw new ArgumentNullException(nameof(stokSyncService));
            _siparisSyncService = siparisSyncService ?? throw new ArgumentNullException(nameof(siparisSyncService));
            _cariSyncService = cariSyncService ?? throw new ArgumentNullException(nameof(cariSyncService));
            _fiyatSyncService = fiyatSyncService ?? throw new ArgumentNullException(nameof(fiyatSyncService));
            _syncRepository = syncRepository ?? throw new ArgumentNullException(nameof(syncRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ==================== TAM SENKRONİZASYON ====================

        /// <inheritdoc />
        public async Task<SyncResult> RunFullSyncAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var allErrors = new List<SyncError>();
            int totalSuccess = 0;

            _logger.LogInformation(
                "[MikroSyncOrchestrator] ===== TAM SENKRONİZASYON BAŞLATILDI =====");

            try
            {
                // 1. STOK SENKRONİZASYONU (Mikro → E-ticaret)
                _logger.LogInformation(
                    "[MikroSyncOrchestrator] [1/4] Stok senkronizasyonu başlıyor...");

                var stokResult = await _stokSyncService.SyncAllFromMikroAsync(cancellationToken);
                
                if (stokResult.IsSuccess)
                    totalSuccess += stokResult.ProcessedCount;
                else
                    allErrors.AddRange(stokResult.Errors);

                _logger.LogInformation(
                    "[MikroSyncOrchestrator] [1/4] Stok: {Status}, İşlenen: {Count}",
                    stokResult.IsSuccess ? "✓" : "✗", stokResult.ProcessedCount);

                // 2. FİYAT SENKRONİZASYONU (Mikro → E-ticaret)
                _logger.LogInformation(
                    "[MikroSyncOrchestrator] [2/4] Fiyat senkronizasyonu başlıyor...");

                var fiyatResult = await _fiyatSyncService.SyncAllFromMikroAsync(cancellationToken);

                if (fiyatResult.IsSuccess)
                    totalSuccess += fiyatResult.ProcessedCount;
                else
                    allErrors.AddRange(fiyatResult.Errors);

                _logger.LogInformation(
                    "[MikroSyncOrchestrator] [2/4] Fiyat: {Status}, İşlenen: {Count}",
                    fiyatResult.IsSuccess ? "✓" : "✗", fiyatResult.ProcessedCount);

                // 3. CARİ SENKRONİZASYONU (E-ticaret → Mikro)
                _logger.LogInformation(
                    "[MikroSyncOrchestrator] [3/4] Cari senkronizasyonu başlıyor...");

                var cariResult = await _cariSyncService.SyncAllUsersToCariAsync(cancellationToken);

                if (cariResult.IsSuccess)
                    totalSuccess += cariResult.ProcessedCount;
                else
                    allErrors.AddRange(cariResult.Errors);

                _logger.LogInformation(
                    "[MikroSyncOrchestrator] [3/4] Cari: {Status}, İşlenen: {Count}",
                    cariResult.IsSuccess ? "✓" : "✗", cariResult.ProcessedCount);

                // 4. BEKLEYEN SİPARİŞLER (E-ticaret → Mikro)
                _logger.LogInformation(
                    "[MikroSyncOrchestrator] [4/4] Bekleyen siparişler gönderiliyor...");

                var siparisResult = await _siparisSyncService.PushPendingOrdersAsync(cancellationToken);

                if (siparisResult.IsSuccess)
                    totalSuccess += siparisResult.ProcessedCount;
                else
                    allErrors.AddRange(siparisResult.Errors);

                _logger.LogInformation(
                    "[MikroSyncOrchestrator] [4/4] Sipariş: {Status}, İşlenen: {Count}",
                    siparisResult.IsSuccess ? "✓" : "✗", siparisResult.ProcessedCount);

                stopwatch.Stop();

                // Özet log
                _logger.LogInformation(
                    "[MikroSyncOrchestrator] ===== TAM SENKRONİZASYON TAMAMLANDI =====\n" +
                    "Toplam başarılı: {Success}\n" +
                    "Toplam hata: {Errors}\n" +
                    "Süre: {Duration}ms",
                    totalSuccess, allErrors.Count, stopwatch.ElapsedMilliseconds);

                return allErrors.Count > 0
                    ? SyncResult.Ok(totalSuccess, allErrors)
                    : SyncResult.Ok(totalSuccess);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "[MikroSyncOrchestrator] TAM SENKRONİZASYON BAŞARISIZ! " +
                    "Süre: {Duration}ms",
                    stopwatch.ElapsedMilliseconds);

                return SyncResult.Fail(new SyncError(
                    "FullSync",
                    null,
                    ex.Message,
                    ex.StackTrace));
            }
        }

        // ==================== DELTA SENKRONİZASYON ====================

        /// <inheritdoc />
        public async Task<SyncResult> RunDeltaSyncAsync(
            DateTime? since = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var allErrors = new List<SyncError>();
            int totalSuccess = 0;

            _logger.LogInformation(
                "[MikroSyncOrchestrator] ===== DELTA SENKRONİZASYON BAŞLATILDI =====" +
                "\nBaşlangıç tarihi: {Since}",
                since ?? DateTime.UtcNow.AddHours(-1));

            try
            {
                // 1. STOK DELTa (Mikro → E-ticaret)
                var stokResult = await _stokSyncService.SyncDeltaFromMikroAsync(since, cancellationToken);
                
                if (stokResult.IsSuccess)
                    totalSuccess += stokResult.ProcessedCount;
                else
                    allErrors.AddRange(stokResult.Errors);

                // 2. FİYAT DELTA (Mikro → E-ticaret)
                var fiyatResult = await _fiyatSyncService.SyncDeltaFromMikroAsync(since, cancellationToken);

                if (fiyatResult.IsSuccess)
                    totalSuccess += fiyatResult.ProcessedCount;
                else
                    allErrors.AddRange(fiyatResult.Errors);

                // 3. BEKLEYEN SİPARİŞLER (retry)
                var siparisResult = await _siparisSyncService.PushPendingOrdersAsync(cancellationToken);

                if (siparisResult.IsSuccess)
                    totalSuccess += siparisResult.ProcessedCount;
                else
                    allErrors.AddRange(siparisResult.Errors);

                stopwatch.Stop();

                _logger.LogInformation(
                    "[MikroSyncOrchestrator] ===== DELTA SENKRONİZASYON TAMAMLANDI =====" +
                    "\nToplam başarılı: {Success}, Hata: {Errors}, Süre: {Duration}ms",
                    totalSuccess, allErrors.Count, stopwatch.ElapsedMilliseconds);

                return allErrors.Count > 0
                    ? SyncResult.Ok(totalSuccess, allErrors)
                    : SyncResult.Ok(totalSuccess);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "[MikroSyncOrchestrator] DELTA SENKRONİZASYON BAŞARISIZ!");

                return SyncResult.Fail(new SyncError("DeltaSync", null, ex.Message));
            }
        }

        // ==================== SYNC DURUMU ====================

        /// <inheritdoc />
        public async Task<SyncStatusReport> GetSyncStatusAsync(
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("[MikroSyncOrchestrator] Sync durumu sorgulanıyor...");

            try
            {
                var states = await _syncRepository.GetAllSyncStatesAsync(cancellationToken);
                var failedCount = await _syncRepository.GetFailedLogCountAsync(
                    DateTime.UtcNow.AddDays(-1), 
                    cancellationToken);

                var report = new SyncStatusReport
                {
                    GeneratedAt = DateTime.UtcNow,
                    SyncStates = states.ToList(),
                    Last24HourFailedCount = failedCount,
                    IsHealthy = failedCount < 10 && states.All(s => s.ConsecutiveFailures < 3)
                };

                _logger.LogDebug(
                    "[MikroSyncOrchestrator] Sync durumu: Sağlıklı={IsHealthy}, " +
                    "24 saat hata={Errors}",
                    report.IsHealthy, report.Last24HourFailedCount);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[MikroSyncOrchestrator] Sync durumu sorgulanamadı!");

                return new SyncStatusReport
                {
                    GeneratedAt = DateTime.UtcNow,
                    IsHealthy = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ==================== TEKİL OPERASYONLAR ====================

        /// <summary>
        /// Tek bir siparişi Mikro'ya gönderir.
        /// Sipariş onaylandığında çağrılır.
        /// 
        /// AKIŞ:
        /// 1. Önce müşteri cari kaydı kontrol edilir
        /// 2. Yoksa oluşturulur
        /// 3. Sipariş Mikro'ya aktarılır
        /// </summary>
        public async Task<SyncResult> PushOrderAsync(
            int orderId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[MikroSyncOrchestrator] Sipariş push başlatıldı. OrderId: {OrderId}",
                orderId);

            return await _siparisSyncService.PushOrderToMikroAsync(orderId, cancellationToken);
        }

        /// <summary>
        /// Tek bir kullanıcıyı Mikro cari olarak kaydeder.
        /// Yeni kayıt veya profil güncellemesinde çağrılır.
        /// </summary>
        public async Task<SyncResult> SyncUserAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[MikroSyncOrchestrator] Kullanıcı sync başlatıldı. UserId: {UserId}",
                userId);

            return await _cariSyncService.SyncUserToCariAsync(userId, cancellationToken);
        }

        /// <summary>
        /// Tek bir ürünün stoğunu Mikro'ya gönderir.
        /// Manuel stok düzeltmelerinde kullanılır.
        /// </summary>
        public async Task<SyncResult> PushStockAsync(
            int productId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[MikroSyncOrchestrator] Stok push başlatıldı. ProductId: {ProductId}",
                productId);

            return await _stokSyncService.PushStockToMikroAsync(productId, cancellationToken);
        }

        /// <summary>
        /// Tek bir ürünün fiyatını Mikro'ya gönderir.
        /// E-ticaret özel kampanyalarında kullanılır.
        /// </summary>
        public async Task<SyncResult> PushPriceAsync(
            int productId,
            decimal newPrice,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[MikroSyncOrchestrator] Fiyat push başlatıldı. " +
                "ProductId: {ProductId}, Fiyat: {Price}",
                productId, newPrice);

            return await _fiyatSyncService.PushPriceToMikroAsync(productId, newPrice, cancellationToken);
        }
    }
}
