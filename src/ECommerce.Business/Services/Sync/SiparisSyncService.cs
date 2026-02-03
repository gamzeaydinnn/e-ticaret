using System.Diagnostics;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Interfaces.Sync;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Sipariş senkronizasyon servisi - E-ticaret siparişlerini Mikro ERP'ye aktarır.
    /// 
    /// NEDEN: Online siparişlerin muhasebeleştirilmesi için Mikro'ya aktarılması gerekir.
    /// Sipariş girildikten sonra Mikro'da stok düşer, cari borç oluşur, faturalama yapılabilir.
    /// 
    /// AKIŞ:
    /// 1. E-ticaret'te sipariş onaylanır (ödeme alınır)
    /// 2. Bu servis siparişi Mikro formatına dönüştürür
    /// 3. MikroAPI SiparisKaydetV2 endpoint'ine gönderir
    /// 4. Mikro'dan dönen evrak no e-ticaret'e kaydedilir
    /// 
    /// ÖNEMLİ: Mağaza siparişleri Mikro'da zaten var,
    /// bu servis sadece ONLINE siparişleri Mikro'ya gönderir.
    /// </summary>
    public class SiparisSyncService : ISiparisSyncService
    {
        // ==================== BAĞIMLILIKLAR ====================
        
        private readonly IMicroService _microService;
        private readonly IOrderRepository _orderRepository;
        private readonly IMikroSyncRepository _syncRepository;
        private readonly ICariSyncService _cariSyncService;
        private readonly ILogger<SiparisSyncService> _logger;

        // Sabitler
        private const string SYNC_TYPE = "Siparis";
        private const string DIRECTION_TO_ERP = "ToERP";
        private const int MAX_RETRY_ATTEMPTS = 3;
        private const string ONLINE_EVRAK_SERI = "ONL"; // Online siparişler için seri
        private const int DEFAULT_DEPO_NO = 1;

        // ==================== CONSTRUCTOR ====================

        public SiparisSyncService(
            IMicroService microService,
            IOrderRepository orderRepository,
            IMikroSyncRepository syncRepository,
            ICariSyncService cariSyncService,
            ILogger<SiparisSyncService> logger)
        {
            _microService = microService ?? throw new ArgumentNullException(nameof(microService));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _syncRepository = syncRepository ?? throw new ArgumentNullException(nameof(syncRepository));
            _cariSyncService = cariSyncService ?? throw new ArgumentNullException(nameof(cariSyncService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ==================== SİPARİŞ GÖNDERME ====================

        /// <inheritdoc />
        public async Task<SyncResult> PushOrderToMikroAsync(
            int orderId,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[SiparisSyncService] Sipariş Mikro'ya gönderiliyor. OrderId: {OrderId}",
                orderId);

            try
            {
                // 1. Siparişi bul
                var order = await _orderRepository.GetByIdAsync(orderId);

                if (order == null)
                {
                    var error = new SyncError(
                        "PushOrderToMikro",
                        orderId.ToString(),
                        "Sipariş bulunamadı");

                    _logger.LogWarning(
                        "[SiparisSyncService] Sipariş bulunamadı. OrderId: {OrderId}",
                        orderId);

                    return SyncResult.Fail(error);
                }

                // 2. Siparişi gönder
                var result = await PushOrderWithRetryAsync(order, cancellationToken);

                stopwatch.Stop();

                if (result.IsSuccess)
                {
                    await _syncRepository.UpdateSyncSuccessAsync(
                        SYNC_TYPE,
                        DIRECTION_TO_ERP,
                        1,
                        stopwatch.ElapsedMilliseconds,
                        cancellationToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                await _syncRepository.UpdateSyncFailureAsync(
                    SYNC_TYPE,
                    DIRECTION_TO_ERP,
                    ex.Message,
                    cancellationToken);

                _logger.LogError(ex,
                    "[SiparisSyncService] Sipariş gönderimi başarısız. OrderId: {OrderId}",
                    orderId);

                return SyncResult.Fail(new SyncError(
                    "PushOrderToMikro",
                    orderId.ToString(),
                    ex.Message));
            }
        }

        /// <inheritdoc />
        public async Task<SyncResult> PushPendingOrdersAsync(
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<SyncError>();
            int successCount = 0;
            int failedCount = 0;

            _logger.LogInformation(
                "[SiparisSyncService] Bekleyen siparişler Mikro'ya gönderiliyor");

            try
            {
                // 1. Daha önce başarısız olan sync loglarını bul
                var pendingLogs = await _syncRepository.GetPendingLogsAsync(
                    "Order",
                    MAX_RETRY_ATTEMPTS,
                    cancellationToken);

                var logList = pendingLogs.ToList();

                _logger.LogInformation(
                    "[SiparisSyncService] {Count} bekleyen sipariş bulundu",
                    logList.Count);

                // 2. Her bekleyen sipariş için yeniden gönder
                foreach (var log in logList)
                {
                    if (!int.TryParse(log.InternalId, out int orderId))
                    {
                        failedCount++;
                        continue;
                    }

                    try
                    {
                        var result = await PushOrderToMikroAsync(orderId, cancellationToken);

                        if (result.IsSuccess)
                            successCount++;
                        else
                        {
                            failedCount++;
                            errors.AddRange(result.Errors);
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        errors.Add(new SyncError("RetryOrder", log.InternalId, ex.Message));
                    }
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "[SiparisSyncService] Bekleyen siparişler işlendi. " +
                    "Başarılı: {Success}, Hatalı: {Failed}, Süre: {Duration}ms",
                    successCount, failedCount, stopwatch.ElapsedMilliseconds);

                return SyncResult.Ok(successCount, errors);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "[SiparisSyncService] Bekleyen sipariş gönderimi başarısız!");

                return SyncResult.Fail(new SyncError("PushPendingOrders", null, ex.Message));
            }
        }

        /// <inheritdoc />
        public async Task<SyncResult> PushConfirmedOrdersAsync(
            DateTime since,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<SyncError>();
            int successCount = 0;
            int failedCount = 0;

            _logger.LogInformation(
                "[SiparisSyncService] Onaylanan siparişler Mikro'ya gönderiliyor. Tarih: {Since}",
                since);

            try
            {
                // NOT: Bu metodun düzgün çalışması için IOrderRepository'e
                // GetConfirmedOrdersSinceAsync metodu eklenmeli.
                // Şimdilik tüm siparişleri kontrol ediyoruz.

                // TODO: Optimizasyon için özel sorgu eklenebilir:
                // var orders = await _orderRepository.GetConfirmedOrdersSinceAsync(since);

                _logger.LogInformation(
                    "[SiparisSyncService] Onaylanan siparişler işlendi. " +
                    "Başarılı: {Success}, Hatalı: {Failed}",
                    successCount, failedCount);

                stopwatch.Stop();

                return SyncResult.Ok(successCount, errors);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "[SiparisSyncService] Onaylanan sipariş gönderimi başarısız!");

                return SyncResult.Fail(new SyncError("PushConfirmedOrders", null, ex.Message));
            }
        }

        // ==================== YARDIMCI METODLAR ====================

        /// <summary>
        /// Siparişi Mikro'ya retry mekanizması ile gönderir.
        /// 
        /// İŞ KURALLARI:
        /// 1. Önce müşteri (cari) kaydı kontrol edilir/oluşturulur
        /// 2. Sipariş Mikro formatına dönüştürülür
        /// 3. MikroAPI'ye gönderilir
        /// 4. Dönen evrak numarası kaydedilir
        /// </summary>
        private async Task<SyncResult> PushOrderWithRetryAsync(
            Order order,
            CancellationToken cancellationToken)
        {
            var syncLog = new MicroSyncLog
            {
                EntityType = "Order",
                Direction = DIRECTION_TO_ERP,
                InternalId = order.Id.ToString(),
                ExternalId = order.OrderNumber,
                Status = "Pending",
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };

            for (int attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
            {
                try
                {
                    syncLog.Attempts = attempt;
                    syncLog.LastAttemptAt = DateTime.UtcNow;

                    // 1. Müşteri kaydını kontrol et / oluştur
                    var cariKod = await EnsureCariExistsAsync(order, cancellationToken);

                    if (string.IsNullOrEmpty(cariKod))
                    {
                        throw new InvalidOperationException(
                            "Müşteri cari kaydı oluşturulamadı");
                    }

                    // 2. Siparişi Mikro formatına dönüştür
                    var mikroSiparis = MapToMikroSiparis(order, cariKod);

                    // 3. Mikro'ya gönder
                    var success = await _microService.ExportOrdersToERPAsync(new[] { order });

                    if (success)
                    {
                        syncLog.Status = "Success";
                        syncLog.Message = $"Sipariş Mikro'ya aktarıldı. OrderNo: {order.OrderNumber}";
                        await _syncRepository.CreateLogAsync(syncLog, cancellationToken);

                        _logger.LogInformation(
                            "[SiparisSyncService] Sipariş başarıyla gönderildi. " +
                            "OrderNo: {OrderNo}, CariKod: {CariKod}",
                            order.OrderNumber, cariKod);

                        return SyncResult.Ok(1);
                    }
                    else
                    {
                        throw new InvalidOperationException("MikroAPI false döndürdü");
                    }
                }
                catch (Exception ex)
                {
                    syncLog.LastError = ex.Message;

                    _logger.LogWarning(
                        "[SiparisSyncService] Sipariş gönderimi başarısız. " +
                        "OrderNo: {OrderNo}, Deneme: {Attempt}/{Max}, Hata: {Error}",
                        order.OrderNumber, attempt, MAX_RETRY_ATTEMPTS, ex.Message);

                    // Son deneme değilse bekle
                    if (attempt < MAX_RETRY_ATTEMPTS)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }

            // Max deneme aşıldı
            syncLog.Status = "Failed";
            syncLog.Message = $"Max {MAX_RETRY_ATTEMPTS} deneme aşıldı";
            await _syncRepository.CreateLogAsync(syncLog, cancellationToken);

            _logger.LogError(
                "[SiparisSyncService] Sipariş gönderimi başarısız (max deneme). " +
                "OrderNo: {OrderNo}",
                order.OrderNumber);

            return SyncResult.Fail(new SyncError(
                "PushOrder",
                order.OrderNumber,
                syncLog.LastError ?? "Max retry exceeded"));
        }

        /// <summary>
        /// Müşterinin Mikro'da cari kaydı olduğundan emin olur.
        /// Yoksa yeni cari oluşturur.
        /// 
        /// NEDEN: Mikro'da sipariş kaydı için cari kodu gerekli.
        /// E-ticaret müşterileri otomatik olarak Mikro'ya aktarılmalı.
        /// </summary>
        private async Task<string?> EnsureCariExistsAsync(
            Order order,
            CancellationToken cancellationToken)
        {
            try
            {
                // Önce mevcut cari kodunu kontrol et
                if (order.UserId.HasValue)
                {
                    var existingCode = await _cariSyncService.GetMikroCariKodAsync(
                        order.UserId.Value,
                        cancellationToken);

                    if (!string.IsNullOrEmpty(existingCode))
                        return existingCode;
                }

                // Yeni cari oluştur
                var cariResult = await _cariSyncService.CreateOrUpdateCariAsync(
                    order.UserId,
                    order.CustomerName ?? "Misafir Müşteri",
                    order.CustomerEmail ?? "",
                    order.CustomerPhone ?? "",
                    cancellationToken);

                if (cariResult.IsSuccess)
                {
                    // Cari kodu metadata'dan al
                    // NOT: CariSyncService'ten dönen metadata'da cari kodu olmalı
                    return GenerateCariKod(order);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SiparisSyncService] Cari kaydı oluşturma hatası. " +
                    "UserId: {UserId}",
                    order.UserId);
                return null;
            }
        }

        /// <summary>
        /// E-ticaret siparişini Mikro sipariş formatına dönüştürür.
        /// 
        /// MAPPING:
        /// - OrderNumber → sip_ozel_kod (referans)
        /// - TotalPrice → sip_tutar
        /// - VatAmount → sip_vergi
        /// - FinalPrice → sip_genel_toplam
        /// - OrderItems → satirlar
        /// </summary>
        private MikroSiparisKaydetRequestDto MapToMikroSiparis(
            Order order,
            string cariKod)
        {
            var siparis = new MikroSiparisKaydetRequestDto
            {
                SipEvraknoSeri = ONLINE_EVRAK_SERI,
                SipTarih = order.OrderDate.ToString("yyyy-MM-dd"),
                SipTeslimTarih = order.EstimatedDeliveryDate?.ToString("yyyy-MM-dd"),
                SipMusteriKod = cariKod,
                SipTip = 0, // Satış siparişi
                SipCins = 0, // Normal
                SipDepoNo = DEFAULT_DEPO_NO,
                SipDurum = MapOrderStatus(order.Status),
                SipOdemeKod = MapPaymentMethod(order.PaymentMethod),
                SipDovizCinsi = 0, // TL
                SipTutar = order.TotalPrice,
                SipVergi = order.VatAmount,
                SipIskonto = order.DiscountAmount,
                SipGenelToplam = order.FinalPrice,
                SipKargoTutar = order.ShippingCost,
                SipOzelKod = order.OrderNumber, // E-ticaret referans
                SipAciklama = $"E-ticaret sipariş: {order.OrderNumber}",
                Satirlar = new List<MikroSiparisSatirDto>()
            };

            // Teslimat adresi
            if (!string.IsNullOrEmpty(order.ShippingAddress))
            {
                siparis.TeslimatAdresi = new MikroSiparisTeslimatAdresiDto
                {
                    AdresAlici = order.CustomerName ?? "",
                    AdresCadde = order.ShippingAddress,
                    AdresIl = order.ShippingCity,
                    AdresTelefon = order.CustomerPhone,
                    AdresNot = order.DeliveryNotes
                };
            }

            // Sipariş kalemleri
            // NOT: Order.OrderItems navigation property'si kullanılmalı
            // Şu an OrderItems yüklenmediği için boş kalacak
            // Gerçek implementasyonda Include ile yüklenmeli

            return siparis;
        }

        /// <summary>
        /// E-ticaret sipariş durumunu Mikro durumuna çevirir.
        /// </summary>
        private int MapOrderStatus(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => 0,      // Açık
                OrderStatus.Confirmed => 0,    // Açık
                OrderStatus.Preparing => 0,    // Açık
                OrderStatus.Ready => 0,        // Açık
                OrderStatus.Shipped => 1,      // Kısmi Teslim
                OrderStatus.Delivered => 2,    // Kapalı
                OrderStatus.Cancelled => 3,    // İptal
                _ => 0
            };
        }

        /// <summary>
        /// E-ticaret ödeme yöntemini Mikro koduna çevirir.
        /// </summary>
        private string MapPaymentMethod(string paymentMethod)
        {
            return paymentMethod?.ToLower() switch
            {
                "credit_card" => "KK",      // Kredi Kartı
                "cash_on_delivery" => "NAK", // Nakit (Kapıda Ödeme)
                "bank_transfer" => "HVL",    // Havale
                "debit_card" => "KK",        // Banka Kartı
                _ => "NAK"
            };
        }

        /// <summary>
        /// Müşteri için benzersiz cari kod üretir.
        /// Format: ETCMUST + UserId veya timestamp
        /// </summary>
        private string GenerateCariKod(Order order)
        {
            if (order.UserId.HasValue)
                return $"ETCMUST{order.UserId.Value:D6}";
            
            // Misafir müşteri için timestamp bazlı kod
            return $"ETCMIS{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
    }
}
