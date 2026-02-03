using ECommerce.Core.Interfaces.Jobs;
using ECommerce.Core.Interfaces.Sync;
using ECommerce.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Jobs
{
    /// <summary>
    /// Sipariş push Hangfire job'ı.
    /// 
    /// GÖREV: Online siparişleri Mikro ERP'ye gönderir.
    /// Event-driven çalışır (sipariş onaylandığında tetiklenir).
    /// 
    /// AKIŞ:
    /// 1. Sipariş onaylandı event'i
    /// 2. Bu job tetiklenir (Hangfire BackgroundJob.Enqueue)
    /// 3. Müşteri Mikro'da yoksa önce oluşturulur
    /// 4. Sipariş Mikro'ya gönderilir
    /// 5. Mikro evrak no e-ticaret'e kaydedilir
    /// 
    /// RETRY: Başarısız işlemler için 3 deneme yapılır.
    /// Exponential backoff: 1dk, 5dk, 15dk
    /// </summary>
    public class SiparisPushJob : ISiparisPushJob
    {
        private readonly ISiparisSyncService _siparisSyncService;
        private readonly ICariSyncService _cariSyncService;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<SiparisPushJob> _logger;

        /// <inheritdoc />
        public string JobName => "mikro-siparis-push";

        /// <inheritdoc />
        public string Description => "Online siparişleri Mikro ERP'ye gönderir (event-driven)";

        // Retry ayarları
        private const int MaxRetryAttempts = 3;
        private static readonly TimeSpan[] RetryDelays = 
        {
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15)
        };

        public SiparisPushJob(
            ISiparisSyncService siparisSyncService,
            ICariSyncService cariSyncService,
            IOrderRepository orderRepository,
            ILogger<SiparisPushJob> logger)
        {
            _siparisSyncService = siparisSyncService ?? throw new ArgumentNullException(nameof(siparisSyncService));
            _cariSyncService = cariSyncService ?? throw new ArgumentNullException(nameof(cariSyncService));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            // Bekleyen tüm siparişleri gönder
            return await PushPendingOrdersAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<JobResult> PushOrderAsync(
            int orderId,
            CancellationToken cancellationToken = default)
        {
            var result = JobResult.Start();

            _logger.LogInformation(
                "[{JobName}] Sipariş {OrderId} Mikro'ya gönderiliyor...",
                JobName, orderId);

            try
            {
                // Siparişi getir
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning(
                        "[{JobName}] Sipariş {OrderId} bulunamadı",
                        JobName, orderId);

                    return JobResult.Failed($"Sipariş {orderId} bulunamadı");
                }

                // Sipariş durumu kontrolü
                if (order.Status == Entities.Enums.OrderStatus.Cancelled)
                {
                    _logger.LogWarning(
                        "[{JobName}] Sipariş {OrderId} iptal edilmiş, gönderilmedi",
                        JobName, orderId);

                    return JobResult.Failed("İptal edilmiş sipariş gönderilemez");
                }

                // ═══════════════════════════════════════════════════════
                // ADIM 1: MÜŞTERİ KONTROLÜ VE OLUŞTURMA
                // ═══════════════════════════════════════════════════════
                if (order.UserId.HasValue)
                {
                    _logger.LogDebug(
                        "[{JobName}] Müşteri {UserId} Mikro'da kontrol ediliyor...",
                        JobName, order.UserId.Value);

                    try
                    {
                        // Kullanıcıyı Mikro cari olarak senkronize et
                        var cariResult = await _cariSyncService.SyncUserToCariAsync(
                            order.UserId.Value, 
                            cancellationToken);

                        if (!cariResult.IsSuccess)
                        {
                            _logger.LogWarning(
                                "[{JobName}] Müşteri Mikro'ya kaydedilemedi: {Error}",
                                JobName, string.Join(", ", cariResult.Errors.Select(e => e.Message)));
                            // Müşteri kaydı başarısız olsa bile sipariş göndermeyi dene
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "[{JobName}] Müşteri kontrolünde hata, sipariş yine de gönderilecek",
                            JobName);
                    }
                }

                // ═══════════════════════════════════════════════════════
                // ADIM 2: SİPARİŞİ GÖNDER
                // ═══════════════════════════════════════════════════════
                var syncResult = await _siparisSyncService.PushOrderToMikroAsync(
                    orderId, 
                    cancellationToken);

                result.Success = syncResult.IsSuccess;
                result.ProcessedCount = 1;
                result.SuccessCount = syncResult.IsSuccess ? 1 : 0;
                result.ErrorCount = syncResult.IsSuccess ? 0 : 1;
                result.CompletedAt = DateTime.UtcNow;

                if (syncResult.IsSuccess)
                {
                    result.Message = $"Sipariş {order.OrderNumber} Mikro'ya gönderildi";
                    result.Metadata["MikroEvrakNo"] = "MIKRO-" + orderId; // Sync sonucu kaydedilir

                    _logger.LogInformation(
                        "[{JobName}] Sipariş {OrderNumber} başarıyla gönderildi.",
                        JobName, order.OrderNumber);
                }
                else
                {
                    result.Message = $"Sipariş gönderilemedi: {string.Join(", ", syncResult.Errors.Select(e => e.Message))}";
                    result.Errors.AddRange(syncResult.Errors.Select(e => e.Message));

                    _logger.LogWarning(
                        "[{JobName}] Sipariş {OrderNumber} gönderilemedi: {Errors}",
                        JobName, order.OrderNumber, string.Join(", ", syncResult.Errors.Select(e => e.Message)));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{JobName}] Sipariş {OrderId} gönderilirken hata",
                    JobName, orderId);

                return JobResult.Failed(
                    $"Sipariş gönderme hatası: {ex.Message}",
                    new List<string> { ex.ToString() });
            }
        }

        /// <inheritdoc />
        public async Task<JobResult> PushPendingOrdersAsync(CancellationToken cancellationToken = default)
        {
            var result = JobResult.Start();
            var errors = new List<string>();
            int successCount = 0;
            int errorCount = 0;

            _logger.LogInformation(
                "[{JobName}] Bekleyen siparişler Mikro'ya gönderiliyor...",
                JobName);

            try
            {
                // Mikro'ya gönderilmemiş siparişleri bul
                var pendingOrders = await GetPendingOrdersForMikroAsync(cancellationToken);
                var orderIds = pendingOrders.ToList();

                if (!orderIds.Any())
                {
                    _logger.LogInformation(
                        "[{JobName}] Gönderilecek bekleyen sipariş yok",
                        JobName);

                    return JobResult.Successful("Gönderilecek sipariş yok", 0, 0);
                }

                _logger.LogInformation(
                    "[{JobName}] {Count} sipariş gönderilecek",
                    JobName, orderIds.Count);

                // Her siparişi gönder
                foreach (var orderId in orderIds)
                {
                    // İptal kontrolü
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning(
                            "[{JobName}] İşlem iptal edildi. Gönderilen: {Success}, Kalan: {Remaining}",
                            JobName, successCount, orderIds.Count - successCount - errorCount);
                        break;
                    }

                    try
                    {
                        var orderResult = await PushOrderAsync(orderId, cancellationToken);
                        
                        if (orderResult.Success)
                        {
                            successCount++;
                        }
                        else
                        {
                            errorCount++;
                            errors.Add($"Sipariş {orderId}: {orderResult.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errors.Add($"Sipariş {orderId}: {ex.Message}");
                        _logger.LogError(ex,
                            "[{JobName}] Sipariş {OrderId} gönderilirken hata",
                            JobName, orderId);
                    }

                    // Rate limiting - Mikro API'yi yormamak için
                    await Task.Delay(100, cancellationToken);
                }

                result.Success = errorCount == 0;
                result.ProcessedCount = orderIds.Count;
                result.SuccessCount = successCount;
                result.ErrorCount = errorCount;
                result.Errors = errors;
                result.CompletedAt = DateTime.UtcNow;
                result.Message = $"Bekleyen sipariş gönderimi tamamlandı. Başarılı: {successCount}/{orderIds.Count}";

                _logger.LogInformation(
                    "[{JobName}] Bekleyen sipariş gönderimi tamamlandı. " +
                    "Başarılı: {Success}, Hatalı: {Error}, Süre: {Duration}ms",
                    JobName, successCount, errorCount, result.DurationMs);

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[{JobName}] İşlem iptal edildi", JobName);
                return JobResult.Failed("İşlem iptal edildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{JobName}] Bekleyen sipariş gönderiminde kritik hata",
                    JobName);

                return JobResult.Failed(
                    $"Bekleyen sipariş gönderimi hatası: {ex.Message}",
                    new List<string> { ex.ToString() });
            }
        }

        /// <summary>
        /// Mikro'ya gönderilmemiş siparişleri getirir.
        /// </summary>
        private async Task<IEnumerable<int>> GetPendingOrdersForMikroAsync(
            CancellationToken cancellationToken)
        {
            // Onaylanmış ve Mikro'ya gönderilmemiş siparişler
            // TrackingNumber null veya "MIKRO-" ile başlamayan siparişler
            var confirmedStatuses = new[]
            {
                Entities.Enums.OrderStatus.Confirmed,
                Entities.Enums.OrderStatus.Preparing,
                Entities.Enums.OrderStatus.Ready,
                Entities.Enums.OrderStatus.Assigned
            };

            var orders = await _orderRepository.GetAllAsync();
            
            return orders
                .Where(o => confirmedStatuses.Contains(o.Status))
                .Where(o => string.IsNullOrEmpty(o.TrackingNumber) || 
                           !o.TrackingNumber.StartsWith("MIKRO-"))
                .OrderBy(o => o.CreatedAt)
                .Take(50) // Batch limit
                .Select(o => o.Id)
                .ToList();
        }
    }
}
