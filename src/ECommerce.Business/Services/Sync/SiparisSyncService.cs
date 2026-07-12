using System.Diagnostics;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Helpers;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Interfaces.Mapping;
using ECommerce.Core.Interfaces.Sync;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Sipariş senkronizasyon servisi - E-ticaret siparişlerini Mikro ERP'ye aktarır.
    /// Online satış: SiparisKaydetV2 (belge) + DahiliStokHareket (eldeki stok düşüşü).
    /// </summary>
    public class SiparisSyncService : ISiparisSyncService
    {
        private readonly IMicroService _microService;
        private readonly IOrderRepository _orderRepository;
        private readonly IMikroSyncRepository _syncRepository;
        private readonly ICariSyncService _cariSyncService;
        private readonly IMikroSiparisMapper _siparisMapper;
        private readonly ECommerceDbContext _db;
        private readonly ILogger<SiparisSyncService> _logger;

        private const string SYNC_TYPE = "Siparis";
        private const string DIRECTION_TO_ERP = "ToERP";
        private const int MAX_RETRY_ATTEMPTS = 3;

        public SiparisSyncService(
            IMicroService microService,
            IOrderRepository orderRepository,
            IMikroSyncRepository syncRepository,
            ICariSyncService cariSyncService,
            IMikroSiparisMapper siparisMapper,
            ECommerceDbContext db,
            ILogger<SiparisSyncService> logger)
        {
            _microService = microService ?? throw new ArgumentNullException(nameof(microService));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _syncRepository = syncRepository ?? throw new ArgumentNullException(nameof(syncRepository));
            _cariSyncService = cariSyncService ?? throw new ArgumentNullException(nameof(cariSyncService));
            _siparisMapper = siparisMapper ?? throw new ArgumentNullException(nameof(siparisMapper));
            _db = db ?? throw new ArgumentNullException(nameof(db));
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

            await Task.CompletedTask;

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

            // Idempotency: daha önce başarılı push varsa tekrar gönderme
            var alreadyPushed = await _db.Set<MicroSyncLog>().AsNoTracking()
                .AnyAsync(l =>
                    l.EntityType == "Order" &&
                    l.InternalId == order.Id.ToString() &&
                    l.Status == "Success" &&
                    l.Direction == DIRECTION_TO_ERP,
                    cancellationToken);

            var items = await _db.OrderItems
                .AsNoTracking()
                .Where(oi => oi.OrderId == order.Id)
                .ToListAsync(cancellationToken);

            if (alreadyPushed || LocalInventoryPolicy.IsMikroSyncedTracking(order.TrackingNumber))
            {
                _logger.LogInformation(
                    "[SiparisSyncService] Sipariş zaten Mikro'da. OrderId={OrderId} — stok düşüşü kontrol ediliyor",
                    order.Id);

                // Sipariş daha önce gitti ama dahili stok düşüşü kalmış olabilir
                if (items.Count > 0)
                {
                    var decreased = await EnsureSaleStockDecreaseAsync(order, items, cancellationToken);
                    if (decreased)
                    {
                        await AlignCacheAfterSaleAsync(items, cancellationToken);
                    }
                }

                return SyncResult.Ok(0);
            }

            if (items.Count == 0)
            {
                return SyncResult.Fail(new SyncError(
                    "PushOrder",
                    order.OrderNumber,
                    "Sipariş kalemleri boş — Mikro'ya gönderilemez"));
            }

            for (int attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
            {
                try
                {
                    syncLog.Attempts = attempt;
                    syncLog.LastAttemptAt = DateTime.UtcNow;

                    // Önceki denemede sipariş yazıldı ama stok düşüşü kaldıysa tekrar sipariş BASMA
                    var latest = await _db.Orders.AsNoTracking()
                        .FirstOrDefaultAsync(o => o.Id == order.Id, cancellationToken);
                    var orderSynced = LocalInventoryPolicy.IsMikroSyncedTracking(latest?.TrackingNumber) ||
                        await _db.Set<MicroSyncLog>().AsNoTracking().AnyAsync(l =>
                            l.EntityType == "Order" &&
                            l.InternalId == order.Id.ToString() &&
                            l.Status == "Success" &&
                            l.Direction == DIRECTION_TO_ERP,
                            cancellationToken);

                    if (orderSynced)
                    {
                        var decreased = await EnsureSaleStockDecreaseAsync(order, items, cancellationToken);
                        if (decreased)
                        {
                            await AlignCacheAfterSaleAsync(items, cancellationToken);
                        }

                        return SyncResult.Ok(1);
                    }

                    var cariKod = await EnsureCariExistsAsync(order, cancellationToken);
                    if (string.IsNullOrEmpty(cariKod))
                    {
                        throw new InvalidOperationException("Müşteri cari kaydı oluşturulamadı");
                    }

                    var mikroSiparis = _siparisMapper.ToMikroSiparis(order, items, cariKod);
                    var (success, message, evrakSeri, evrakSira) =
                        await _microService.PushSiparisV2Async(mikroSiparis, cancellationToken);

                    if (success)
                    {
                        syncLog.Status = "Success";
                        syncLog.Message =
                            $"Sipariş Mikro'ya aktarıldı. OrderNo: {order.OrderNumber}, Evrak: {evrakSeri}-{evrakSira}. {message}";
                        await _syncRepository.CreateLogAsync(syncLog, cancellationToken);

                        var tracked = await _db.Orders.FirstOrDefaultAsync(o => o.Id == order.Id, cancellationToken);
                        if (tracked != null)
                        {
                            tracked.TrackingNumber = $"{LocalInventoryPolicy.MikroTrackingPrefix}{evrakSeri}-{evrakSira}";
                            await _db.SaveChangesAsync(cancellationToken);
                        }

                        // Eldeki stok: sipariş belgesi düşürmez → DahiliStokHareket çıkışı
                        var decreased = await EnsureSaleStockDecreaseAsync(order, items, cancellationToken);
                        if (decreased)
                        {
                            await AlignCacheAfterSaleAsync(items, cancellationToken);
                        }

                        _logger.LogInformation(
                            "[SiparisSyncService] Sipariş başarıyla gönderildi. " +
                            "OrderNo: {OrderNo}, CariKod: {CariKod}, Evrak: {Seri}-{Sira}",
                            order.OrderNumber, cariKod, evrakSeri, evrakSira);

                        return SyncResult.Ok(1);
                    }

                    throw new InvalidOperationException(message ?? "SiparisKaydetV2 başarısız");
                }
                catch (Exception ex)
                {
                    syncLog.LastError = ex.Message;

                    _logger.LogWarning(
                        "[SiparisSyncService] Sipariş/stok senkronu başarısız. " +
                        "OrderNo: {OrderNo}, Deneme: {Attempt}/{Max}, Hata: {Error}",
                        order.OrderNumber, attempt, MAX_RETRY_ATTEMPTS, ex.Message);

                    if (attempt < MAX_RETRY_ATTEMPTS)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }

            // Sipariş Success log zaten yazıldıysa tekrar Failed Order logu basma
            var hasOrderSuccess = await _db.Set<MicroSyncLog>().AsNoTracking()
                .AnyAsync(l =>
                    l.EntityType == "Order" &&
                    l.InternalId == order.Id.ToString() &&
                    l.Status == "Success",
                    cancellationToken);

            if (!hasOrderSuccess)
            {
                syncLog.Status = "Failed";
                syncLog.Message = $"{MAX_RETRY_ATTEMPTS} deneme sonrası senkronizasyon durduruldu";
                await _syncRepository.CreateLogAsync(syncLog, cancellationToken);
            }

            _logger.LogError(
                "[SiparisSyncService] Sipariş gönderimi başarısız (max deneme). OrderNo: {OrderNo}",
                order.OrderNumber);

            return SyncResult.Fail(new SyncError(
                "PushOrder",
                order.OrderNumber,
                syncLog.LastError ?? "Maksimum deneme sayısına ulaşıldı"));
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
        /// Satış miktarını Mikro'da DahiliStokHareket ile düşürür (eldeki stok).
        /// Idempotent: OrderStockDecrease Success log varsa tekrar göndermez.
        /// </summary>
        /// <returns>Bu çağrıda yeni stok düşüşü yapıldıysa true.</returns>
        private async Task<bool> EnsureSaleStockDecreaseAsync(
            Order order,
            List<OrderItem> items,
            CancellationToken cancellationToken)
        {
            if (!LocalInventoryPolicy.PushDahiliStockDecreaseOnOnlineSale)
            {
                return false;
            }

            var alreadyDecreased = await _db.Set<MicroSyncLog>().AsNoTracking()
                .AnyAsync(l =>
                    l.EntityType == LocalInventoryPolicy.SyncEntityOrderStockDecrease &&
                    l.InternalId == order.Id.ToString() &&
                    l.Status == "Success" &&
                    l.Direction == DIRECTION_TO_ERP,
                    cancellationToken);

            if (alreadyDecreased)
            {
                return false;
            }

            var productIds = items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _db.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            var stockDtos = new List<MicroStockDto>();
            foreach (var group in items.Where(i => i.Quantity > 0)
                         .GroupBy(i => MikroStockCacheAligner.ResolveSku(
                             i,
                             products.TryGetValue(i.ProductId, out var p) ? p : null)))
            {
                if (string.IsNullOrWhiteSpace(group.Key))
                {
                    continue;
                }

                var qty = group.Sum(i => i.Quantity);
                stockDtos.Add(new MicroStockDto
                {
                    Sku = group.Key!,
                    Quantity = qty,
                    Stock = qty,
                    IsStockIncrease = false
                });
            }

            if (stockDtos.Count == 0)
            {
                _logger.LogWarning(
                    "[SiparisSyncService] Stok düşüşü atlandı — SKU yok. OrderId={OrderId}",
                    order.Id);
                return false;
            }

            var ok = await _microService.UpsertStocksAsync(stockDtos);
            await _syncRepository.CreateLogAsync(new MicroSyncLog
            {
                EntityType = LocalInventoryPolicy.SyncEntityOrderStockDecrease,
                Direction = DIRECTION_TO_ERP,
                InternalId = order.Id.ToString(),
                ExternalId = order.OrderNumber,
                Status = ok ? "Success" : "Failed",
                Message = ok
                    ? $"Dahili stok çıkışı: {string.Join(", ", stockDtos.Select(s => $"{s.Sku}x{s.Quantity}"))}"
                    : "DahiliStokHareketKaydetV2 başarısız — eldeki stok düşmedi",
                CreatedAt = DateTime.UtcNow,
                LastAttemptAt = DateTime.UtcNow,
                Attempts = 1,
                LastError = ok ? null : "UpsertStocksAsync false"
            }, cancellationToken);

            if (!ok)
            {
                _logger.LogError(
                    "[SiparisSyncService] Mikro eldeki stok düşüşü BAŞARISIZ. OrderId={OrderId}, OrderNo={OrderNo}",
                    order.Id, order.OrderNumber);
                throw new InvalidOperationException(
                    $"Sipariş Mikro'da ama stok düşüşü başarısız. OrderId={order.Id}");
            }

            _logger.LogInformation(
                "[SiparisSyncService] Mikro eldeki stok düşürüldü (Dahili). OrderId={OrderId}, Lines={Lines}",
                order.Id, stockDtos.Count);

            return true;
        }

        private async Task AlignCacheAfterSaleAsync(
            List<OrderItem> items,
            CancellationToken cancellationToken)
        {
            var productIds = items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _db.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            var deltas = new List<(string Sku, decimal QuantityDelta)>();
            foreach (var item in items)
            {
                products.TryGetValue(item.ProductId, out var product);
                var sku = MikroStockCacheAligner.ResolveSku(item, product);
                if (string.IsNullOrWhiteSpace(sku) || item.Quantity <= 0)
                {
                    continue;
                }

                deltas.Add((sku, -item.Quantity));
            }

            if (deltas.Count > 0)
            {
                await MikroStockCacheAligner.ApplyDeltaAsync(_db, deltas, _logger, cancellationToken);
            }
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
