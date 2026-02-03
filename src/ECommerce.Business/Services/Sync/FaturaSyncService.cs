using System.Diagnostics;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Interfaces.Sync;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using ECommerce.Infrastructure.Services.MicroServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ECommerce.Infrastructure.Config;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Fatura senkronizasyon servisi - E-ticaret siparişleri için Mikro'da fatura keser.
    /// 
    /// NEDEN: E-ticaret siparişlerinin muhasebeleştirilmesi için Mikro'da fatura kesilmesi gerekiyor.
    /// Fatura kesildiğinde:
    /// - Stok otomatik düşer (sth_cikis_depo_no'dan)
    /// - Cari hesaba borç yazılır
    /// - E-arşiv zorunluysa e-arşiv fatura oluşur
    /// 
    /// AKIŞ:
    /// 1. Sipariş tamamlandığında (ödeme alındı, teslim edildi)
    /// 2. CariSyncService ile müşteri kontrol/oluştur
    /// 3. FaturaKaydetV2 ile fatura kes
    /// 4. Fatura numarasını e-ticaret order'ına kaydet
    /// 
    /// ÖNEMLİ: Varsayılan olarak DEFAULT_DEPO_NO = 1 deposundan stok düşer.
    /// Bu ayar MikroSettings.DefaultDepoNo ile değiştirilebilir.
    /// </summary>
    public class FaturaSyncService : IFaturaSyncService
    {
        // ==================== BAĞIMLILIKLAR ====================
        
        private readonly MicroService _microService;
        private readonly IOrderRepository _orderRepository;
        private readonly IMikroSyncRepository _syncRepository;
        private readonly ICariSyncService _cariSyncService;
        private readonly MikroSettings _settings;
        private readonly ILogger<FaturaSyncService> _logger;

        // Sabitler
        private const string SYNC_TYPE = "Fatura";
        private const string DIRECTION_TO_ERP = "ToERP";
        private const int MAX_RETRY_ATTEMPTS = 3;

        // ==================== CONSTRUCTOR ====================

        public FaturaSyncService(
            MicroService microService, // Concrete tip çünkü SaveFaturaV2Async interface'de yok
            IOrderRepository orderRepository,
            IMikroSyncRepository syncRepository,
            ICariSyncService cariSyncService,
            IOptions<MikroSettings> settings,
            ILogger<FaturaSyncService> logger)
        {
            _microService = microService ?? throw new ArgumentNullException(nameof(microService));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _syncRepository = syncRepository ?? throw new ArgumentNullException(nameof(syncRepository));
            _cariSyncService = cariSyncService ?? throw new ArgumentNullException(nameof(cariSyncService));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ==================== ANA METODLAR ====================

        /// <inheritdoc />
        public async Task<SyncResult> CreateInvoiceForOrderAsync(
            int orderId, 
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[FaturaSyncService] Fatura kesiliyor. OrderId: {OrderId}",
                orderId);

            try
            {
                // 1. Siparişi bul
                var order = await _orderRepository.GetByIdAsync(orderId);

                if (order == null)
                {
                    _logger.LogWarning(
                        "[FaturaSyncService] Sipariş bulunamadı. OrderId: {OrderId}",
                        orderId);
                    return SyncResult.Fail(new SyncError(
                        "CreateInvoice", orderId.ToString(), "Sipariş bulunamadı"));
                }

                // 2. Zaten fatura kesilmiş mi kontrol et
                if (await IsInvoicedAsync(orderId, cancellationToken))
                {
                    _logger.LogWarning(
                        "[FaturaSyncService] Sipariş zaten faturalandı. OrderId: {OrderId}",
                        orderId);
                    return SyncResult.Ok(0); // Başarılı ama işlem yapmadık
                }

                // 3. Fatura kes
                var result = await CreateInvoiceWithRetryAsync(order, cancellationToken);

                stopwatch.Stop();

                if (result.IsSuccess)
                {
                    await _syncRepository.UpdateSyncSuccessAsync(
                        SYNC_TYPE, DIRECTION_TO_ERP, 1,
                        stopwatch.ElapsedMilliseconds, cancellationToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                await _syncRepository.UpdateSyncFailureAsync(
                    SYNC_TYPE, DIRECTION_TO_ERP, ex.Message, cancellationToken);

                _logger.LogError(ex,
                    "[FaturaSyncService] Fatura kesimi başarısız. OrderId: {OrderId}",
                    orderId);

                return SyncResult.Fail(new SyncError(
                    "CreateInvoice", orderId.ToString(), ex.Message));
            }
        }

        /// <inheritdoc />
        public async Task<SyncResult> CreateInvoicesForPendingOrdersAsync(
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<SyncError>();
            int successCount = 0;
            int failedCount = 0;

            _logger.LogInformation(
                "[FaturaSyncService] Bekleyen siparişler faturalaştırılıyor");

            try
            {
                // Faturası kesilmemiş, tamamlanmış siparişleri bul
                // NOT: Repository'de GetOrdersForInvoicing gibi bir metod eklenmeli
                // Şimdilik sync log'dan bekleyenleri al
                var pendingLogs = await _syncRepository.GetPendingLogsAsync(
                    "Invoice", MAX_RETRY_ATTEMPTS, cancellationToken);

                var logList = pendingLogs.ToList();

                _logger.LogInformation(
                    "[FaturaSyncService] {Count} bekleyen fatura bulundu",
                    logList.Count);

                foreach (var log in logList)
                {
                    if (!int.TryParse(log.InternalId, out int orderId))
                    {
                        failedCount++;
                        continue;
                    }

                    try
                    {
                        var result = await CreateInvoiceForOrderAsync(orderId, cancellationToken);

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
                        errors.Add(new SyncError("CreateInvoice", log.InternalId, ex.Message));
                    }
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "[FaturaSyncService] Bekleyen faturalar işlendi. " +
                    "Başarılı: {Success}, Hatalı: {Failed}, Süre: {Duration}ms",
                    successCount, failedCount, stopwatch.ElapsedMilliseconds);

                return SyncResult.Ok(successCount, errors);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "[FaturaSyncService] Bekleyen fatura kesimi başarısız!");

                return SyncResult.Fail(new SyncError(
                    "CreateInvoicesForPending", null, ex.Message));
            }
        }

        /// <inheritdoc />
        public async Task<SyncResult> CreateRefundInvoiceAsync(
            int orderId, 
            decimal refundAmount, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[FaturaSyncService] İade faturası kesiliyor. OrderId: {OrderId}, Tutar: {Tutar}",
                orderId, refundAmount);

            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);

                if (order == null)
                {
                    return SyncResult.Fail(new SyncError(
                        "CreateRefundInvoice", orderId.ToString(), "Sipariş bulunamadı"));
                }

                // Müşteri cari kodunu al
                var cariKod = await GetOrCreateCariAsync(order, cancellationToken);

                if (string.IsNullOrEmpty(cariKod))
                {
                    return SyncResult.Fail(new SyncError(
                        "CreateRefundInvoice", orderId.ToString(), "Cari kodu alınamadı"));
                }

                // İade faturası DTO oluştur
                var faturaRequest = new MikroFaturaKaydetRequestDto
                {
                    Evraklar = new List<MikroFaturaEvrakDto>
                    {
                        new MikroFaturaEvrakDto
                        {
                            ChaEvraknoSeri = _settings.DefaultEvrakSeri,
                            ChaTarihi = DateTime.Now.ToString("dd.MM.yyyy"),
                            ChaKod = cariKod,
                            ChaTip = 0, // Satış (ama iade olarak işaretlenecek)
                            ChaCinsi = 8, // Perakende Satış Faturası
                            ChaNormalIade = 1, // İADE
                            ChaDCins = 0,
                            ChaDKur = 1,
                            ChaAratoplam = refundAmount,
                            ChaAciklama = $"E-ticaret iade: {order.OrderNumber}",
                            ChaVade = 0,
                            ChaEvrakTip = 63,
                            Detay = new List<MikroFaturaSatirDto>
                            {
                                new MikroFaturaSatirDto
                                {
                                    SthStokKod = "IADE", // Genel iade kodu
                                    SthMiktar = 1,
                                    SthTutar = refundAmount,
                                    SthVergi = 0,
                                    SthEvraknoSeri = _settings.DefaultEvrakSeri,
                                    SthEvraktip = 4,
                                    SthTip = 1,
                                    SthNormalIade = 1, // İADE
                                    SthCikisDepoNo = _settings.DefaultDepoNo,
                                    SthGirisDepoNo = _settings.DefaultDepoNo,
                                    SthCariKodu = cariKod,
                                    SthTarih = DateTime.Now.ToString("dd.MM.yyyy"),
                                    SthAciklama = $"İade: {order.OrderNumber}"
                                }
                            }
                        }
                    }
                };

                var result = await _microService.SaveFaturaV2Async(faturaRequest, cancellationToken);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "[FaturaSyncService] İade faturası kesildi. OrderId: {OrderId}, Evrak: {Seri}-{Sira}",
                        orderId, result.Data?.EvrakSeri, result.Data?.EvrakSira);

                    return SyncResult.Ok(1);
                }
                else
                {
                    return SyncResult.Fail(new SyncError(
                        "CreateRefundInvoice", orderId.ToString(), result.Message ?? "İade faturası kesilemedi"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[FaturaSyncService] İade faturası kesimi başarısız. OrderId: {OrderId}",
                    orderId);

                return SyncResult.Fail(new SyncError(
                    "CreateRefundInvoice", orderId.ToString(), ex.Message));
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsInvoicedAsync(
            int orderId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Sync log'dan fatura kaydı kontrolü
                // GetLastLogAsync ile son başarılı fatura kaydını kontrol ediyoruz
                var lastLog = await _syncRepository.GetLastLogAsync(
                    "Invoice", 
                    internalId: orderId.ToString(), 
                    cancellationToken: cancellationToken);

                return lastLog != null && lastLog.Status == "Success";
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<(string? EvrakSeri, int? EvrakSira, string? EArsivNo)> GetInvoiceDetailsAsync(
            int orderId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Son başarılı fatura logunu getir
                var successLog = await _syncRepository.GetLastLogAsync(
                    "Invoice", 
                    internalId: orderId.ToString(), 
                    cancellationToken: cancellationToken);

                if (successLog != null && 
                    successLog.Status == "Success" && 
                    !string.IsNullOrEmpty(successLog.ExternalId))
                {
                    // ExternalId formatı: "SERI-SIRA" veya "SERI-SIRA|EARSIV_NO"
                    var parts = successLog.ExternalId.Split('|');
                    var evrakParts = parts[0].Split('-');

                    return (
                        evrakParts.Length > 0 ? evrakParts[0] : null,
                        evrakParts.Length > 1 && int.TryParse(evrakParts[1], out var sira) ? sira : null,
                        parts.Length > 1 ? parts[1] : null
                    );
                }

                return (null, null, null);
            }
            catch
            {
                return (null, null, null);
            }
        }

        // ==================== YARDIMCI METODLAR ====================

        /// <summary>
        /// Faturayı retry mekanizması ile keser.
        /// </summary>
        private async Task<SyncResult> CreateInvoiceWithRetryAsync(
            Order order,
            CancellationToken cancellationToken)
        {
            var syncLog = new MicroSyncLog
            {
                EntityType = "Invoice",
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

                    // 1. Müşteri cari kodunu al/oluştur
                    var cariKod = await GetOrCreateCariAsync(order, cancellationToken);

                    if (string.IsNullOrEmpty(cariKod))
                    {
                        throw new InvalidOperationException("Müşteri cari kaydı oluşturulamadı");
                    }

                    // 2. Sipariş kalemlerini hazırla
                    var kalemler = BuildInvoiceItems(order);

                    // 3. Fatura kes
                    var result = await _microService.CreateInvoiceFromOrderAsync(
                        cariKod,
                        order.OrderNumber ?? $"ORD-{order.Id}",
                        order.TotalPrice,
                        kalemler,
                        order.CustomerEmail,
                        order.CustomerName,
                        cancellationToken);

                    if (result.Success && result.Data != null)
                    {
                        // Başarılı - log kaydet
                        syncLog.Status = "Success";
                        syncLog.ExternalId = $"{result.Data.EvrakSeri}-{result.Data.EvrakSira}";
                        if (!string.IsNullOrEmpty(result.Data.EArsivNo))
                        {
                            syncLog.ExternalId += $"|{result.Data.EArsivNo}";
                        }
                        syncLog.Message = $"Fatura kesildi: {syncLog.ExternalId}";
                        await _syncRepository.CreateLogAsync(syncLog, cancellationToken);

                        _logger.LogInformation(
                            "[FaturaSyncService] Fatura başarıyla kesildi. " +
                            "OrderNo: {OrderNo}, Evrak: {Evrak}",
                            order.OrderNumber, syncLog.ExternalId);

                        return SyncResult.Ok(1);
                    }
                    else
                    {
                        throw new InvalidOperationException(result.Message ?? "Fatura kesilemedi");
                    }
                }
                catch (Exception ex)
                {
                    syncLog.LastError = ex.Message;

                    _logger.LogWarning(
                        "[FaturaSyncService] Fatura kesimi başarısız. " +
                        "OrderNo: {OrderNo}, Deneme: {Attempt}/{Max}, Hata: {Error}",
                        order.OrderNumber, attempt, MAX_RETRY_ATTEMPTS, ex.Message);

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
                "[FaturaSyncService] Fatura kesimi başarısız (max deneme). OrderNo: {OrderNo}",
                order.OrderNumber);

            return SyncResult.Fail(new SyncError(
                "CreateInvoice",
                order.OrderNumber,
                syncLog.LastError ?? "Max retry exceeded"));
        }

        /// <summary>
        /// Müşterinin cari kodunu alır veya yeni oluşturur.
        /// </summary>
        private async Task<string?> GetOrCreateCariAsync(
            Order order,
            CancellationToken cancellationToken)
        {
            try
            {
                // Önce mevcut cari kodunu kontrol et
                if (order.UserId.HasValue)
                {
                    var existingCode = await _cariSyncService.GetMikroCariKodAsync(
                        order.UserId.Value, cancellationToken);

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
                    return GenerateCariKod(order);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[FaturaSyncService] Cari oluşturma hatası. UserId: {UserId}",
                    order.UserId);
                return null;
            }
        }

        /// <summary>
        /// Sipariş kalemlerini fatura formatına dönüştürür.
        /// KDV oranı varsayılan %20 olarak hesaplanır.
        /// </summary>
        private List<(string StokKod, decimal Miktar, decimal BirimFiyat, decimal KdvTutari)> BuildInvoiceItems(
            Order order)
        {
            var items = new List<(string StokKod, decimal Miktar, decimal BirimFiyat, decimal KdvTutari)>();

            // Varsayılan KDV oranı (Türkiye'de genel oran)
            const decimal DEFAULT_KDV_ORANI = 0.20m;

            // NOT: Order.OrderItems navigation property'si yüklenmeli
            // Include ile yüklenmediyse boş gelir
            // Gerçek implementasyonda OrderItem'lar üzerinden dönülmeli

            if (order.OrderItems != null && order.OrderItems.Any())
            {
                foreach (var item in order.OrderItems)
                {
                    // Stok kodu: VariantSku > Product.SKU > PROD-{id}
                    var stokKod = item.VariantSku 
                        ?? item.Product?.SKU 
                        ?? $"PROD-{item.ProductId}";

                    // KDV tutarı hesaplama (Birim Fiyat * Miktar * KDV Oranı)
                    var toplamTutar = item.UnitPrice * item.Quantity;
                    var kdvTutari = Math.Round(toplamTutar * DEFAULT_KDV_ORANI, 2, MidpointRounding.AwayFromZero);

                    items.Add((
                        stokKod,
                        item.Quantity,
                        item.UnitPrice,
                        kdvTutari
                    ));
                }
            }
            else
            {
                // OrderItems yüklenmediyse genel kalem oluştur
                // Bu durumda stok düşürme tam doğru olmayacak!
                _logger.LogWarning(
                    "[FaturaSyncService] OrderItems yüklenemedi, genel kalem oluşturuluyor. OrderId: {OrderId}",
                    order.Id);

                items.Add((
                    $"SIPARIS-{order.Id}", // Genel stok kodu
                    1,
                    order.TotalPrice,
                    order.VatAmount
                ));
            }

            return items;
        }

        /// <summary>
        /// Müşteri için cari kod üretir.
        /// Format: ETCMUST + UserId veya timestamp
        /// </summary>
        private string GenerateCariKod(Order order)
        {
            if (order.UserId.HasValue)
                return $"ETCMUST{order.UserId.Value:D6}";

            return $"ETCMIS{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
    }
}
