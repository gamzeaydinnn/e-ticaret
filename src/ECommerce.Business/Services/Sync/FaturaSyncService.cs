using System.Diagnostics;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Helpers;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Interfaces.Sync;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using ECommerce.Infrastructure.Services.MicroServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ECommerce.Infrastructure.Config;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Fatura senkronizasyon servisi - E-ticaret siparişleri için Mikro'da fatura keser.
    /// İade faturası gerçek SKU satırları ile Mikro stok artışını sağlar (Faz 4/5).
    /// </summary>
    public class FaturaSyncService : IFaturaSyncService
    {
        private readonly MicroService _microService;
        private readonly IOrderRepository _orderRepository;
        private readonly IMikroSyncRepository _syncRepository;
        private readonly ICariSyncService _cariSyncService;
        private readonly ECommerceDbContext _db;
        private readonly MikroSettings _settings;
        private readonly ILogger<FaturaSyncService> _logger;

        private const string SYNC_TYPE = "Fatura";
        private const string DIRECTION_TO_ERP = "ToERP";
        private const int MAX_RETRY_ATTEMPTS = 3;

        public FaturaSyncService(
            MicroService microService,
            IOrderRepository orderRepository,
            IMikroSyncRepository syncRepository,
            ICariSyncService cariSyncService,
            ECommerceDbContext db,
            IOptions<MikroSettings> settings,
            ILogger<FaturaSyncService> logger)
        {
            _microService = microService ?? throw new ArgumentNullException(nameof(microService));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _syncRepository = syncRepository ?? throw new ArgumentNullException(nameof(syncRepository));
            _cariSyncService = cariSyncService ?? throw new ArgumentNullException(nameof(cariSyncService));
            _db = db ?? throw new ArgumentNullException(nameof(db));
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
                var alreadyDone = await _db.Set<MicroSyncLog>().AsNoTracking()
                    .AnyAsync(l =>
                        l.EntityType == LocalInventoryPolicy.SyncEntityRefundInvoice &&
                        l.InternalId == orderId.ToString() &&
                        l.Status == "Success" &&
                        l.Direction == DIRECTION_TO_ERP,
                        cancellationToken);

                if (alreadyDone)
                {
                    // Fatura daha önce gitti; stok artışı kalmış olabilir
                    var orderExisting = await _orderRepository.GetByIdAsync(orderId);
                    if (orderExisting != null)
                    {
                        var itemsExisting = await LoadOrderItemsWithSkuAsync(orderExisting, cancellationToken);
                        await EnsureRefundStockIncreaseAsync(orderExisting, itemsExisting, cancellationToken);
                    }

                    _logger.LogInformation(
                        "[FaturaSyncService] İade faturası zaten vardı. OrderId={OrderId}",
                        orderId);
                    return SyncResult.Ok(0);
                }

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

                // İade faturası — gerçek sipariş kalemleri (SKU) ile stok artışı Mikro'da yansır
                var orderItems = await LoadOrderItemsWithSkuAsync(order, cancellationToken);
                var detay = BuildRefundInvoiceLines(order, orderItems, cariKod, refundAmount);
                var usedGenericIade = detay.Any(d =>
                    string.Equals(d.SthStokKod, "IADE", StringComparison.OrdinalIgnoreCase));

                var faturaRequest = new MikroFaturaKaydetRequestDto
                {
                    Evraklar = new List<MikroFaturaEvrakDto>
                    {
                        new MikroFaturaEvrakDto
                        {
                            ChaEvraknoSeri = _settings.DefaultEvrakSeri,
                            ChaTarihi = DateTime.Now.ToString("dd.MM.yyyy"),
                            ChaKod = cariKod,
                            ChaTip = 0,
                            ChaCinsi = 8,
                            ChaNormalIade = 1, // İADE
                            ChaDCins = 0,
                            ChaDKur = 1,
                            ChaAratoplam = refundAmount,
                            ChaAciklama = $"E-ticaret iade: {order.OrderNumber}",
                            ChaVade = 0,
                            ChaEvrakTip = 63,
                            Detay = detay
                        }
                    }
                };

                var result = await _microService.SaveFaturaV2Async(faturaRequest, cancellationToken);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "[FaturaSyncService] İade faturası kesildi. OrderId: {OrderId}, Evrak: {Seri}-{Sira}, Lines={Lines}",
                        orderId, result.Data?.EvrakSeri, result.Data?.EvrakSira, detay.Count);

                    await _syncRepository.CreateLogAsync(new MicroSyncLog
                    {
                        EntityType = LocalInventoryPolicy.SyncEntityRefundInvoice,
                        Direction = DIRECTION_TO_ERP,
                        InternalId = orderId.ToString(),
                        ExternalId = $"{result.Data?.EvrakSeri}-{result.Data?.EvrakSira}",
                        Status = "Success",
                        Message = $"İade faturası: {order.OrderNumber}, tutar={refundAmount}",
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);

                    // Generic "IADE" satırı gerçek ürün stoğunu artırmaz → Dahili ile tamamla
                    if (usedGenericIade)
                    {
                        await EnsureRefundStockIncreaseAsync(order, orderItems, cancellationToken);
                    }

                    await AlignCacheAfterRefundAsync(orderItems, cancellationToken);

                    return SyncResult.Ok(1);
                }

                // Fatura başarısız → eldeki stok yine de artsın (satıştaki Dahili çıkışın tersi)
                _logger.LogWarning(
                    "[FaturaSyncService] İade faturası başarısız, Dahili stok artışı deneniyor. OrderId={OrderId}, Msg={Msg}",
                    orderId, result.Message);

                var increased = await EnsureRefundStockIncreaseAsync(order, orderItems, cancellationToken);
                if (increased)
                {
                    await AlignCacheAfterRefundAsync(orderItems, cancellationToken);
                    await _syncRepository.CreateLogAsync(new MicroSyncLog
                    {
                        EntityType = LocalInventoryPolicy.SyncEntityRefundInvoice,
                        Direction = DIRECTION_TO_ERP,
                        InternalId = orderId.ToString(),
                        ExternalId = order.OrderNumber,
                        Status = "Partial",
                        Message = $"Fatura başarısız; Dahili stok artışı OK. FaturaMsg={result.Message}",
                        CreatedAt = DateTime.UtcNow,
                        LastError = result.Message
                    }, cancellationToken);

                    return SyncResult.Ok(1);
                }

                return SyncResult.Fail(new SyncError(
                    "CreateRefundInvoice", orderId.ToString(), result.Message ?? "İade faturası kesilemedi"));
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
            syncLog.Message = $"{MAX_RETRY_ATTEMPTS} deneme sonrası senkronizasyon durduruldu";
            await _syncRepository.CreateLogAsync(syncLog, cancellationToken);

            _logger.LogError(
                "[FaturaSyncService] Fatura kesimi başarısız (max deneme). OrderNo: {OrderNo}",
                order.OrderNumber);

            return SyncResult.Fail(new SyncError(
                "CreateInvoice",
                order.OrderNumber,
                syncLog.LastError ?? "Maksimum deneme sayısına ulaşıldı"));
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

        private async Task<List<(OrderItem Item, Product? Product)>> LoadOrderItemsWithSkuAsync(
            Order order,
            CancellationToken cancellationToken)
        {
            var items = await _db.OrderItems
                .AsNoTracking()
                .Where(oi => oi.OrderId == order.Id)
                .ToListAsync(cancellationToken);

            var productIds = items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _db.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            return items
                .Select(i =>
                {
                    products.TryGetValue(i.ProductId, out var product);
                    return (i, product);
                })
                .ToList();
        }

        private List<MikroFaturaSatirDto> BuildRefundInvoiceLines(
            Order order,
            List<(OrderItem Item, Product? Product)> orderItems,
            string cariKod,
            decimal refundAmount)
        {
            var depoNo = _settings.DefaultDepoNo > 0 ? _settings.DefaultDepoNo : 1;
            var today = DateTime.Now.ToString("dd.MM.yyyy");
            var lines = new List<MikroFaturaSatirDto>();

            foreach (var (item, product) in orderItems)
            {
                var sku = MikroStockCacheAligner.ResolveSku(item, product);
                if (string.IsNullOrWhiteSpace(sku) || item.Quantity <= 0)
                {
                    continue;
                }

                var lineAmount = item.UnitPrice * item.Quantity;
                lines.Add(new MikroFaturaSatirDto
                {
                    SthStokKod = sku,
                    SthMiktar = item.Quantity,
                    SthTutar = lineAmount,
                    SthVergi = 0,
                    SthEvraknoSeri = _settings.DefaultEvrakSeri,
                    SthEvraktip = 4,
                    SthTip = 1,
                    SthNormalIade = 1,
                    SthCikisDepoNo = depoNo,
                    SthGirisDepoNo = depoNo,
                    SthCariKodu = cariKod,
                    SthTarih = today,
                    SthAciklama = $"İade: {order.OrderNumber} / Item {item.Id}"
                });
            }

            if (lines.Count == 0)
            {
                _logger.LogWarning(
                    "[FaturaSyncService] İade satırı SKU bulunamadı, genel IADE kalemi kullanılıyor. OrderId={OrderId}",
                    order.Id);

                lines.Add(new MikroFaturaSatirDto
                {
                    SthStokKod = "IADE",
                    SthMiktar = 1,
                    SthTutar = refundAmount,
                    SthVergi = 0,
                    SthEvraknoSeri = _settings.DefaultEvrakSeri,
                    SthEvraktip = 4,
                    SthTip = 1,
                    SthNormalIade = 1,
                    SthCikisDepoNo = depoNo,
                    SthGirisDepoNo = depoNo,
                    SthCariKodu = cariKod,
                    SthTarih = today,
                    SthAciklama = $"İade: {order.OrderNumber}"
                });
            }

            return lines;
        }

        private async Task AlignCacheAfterRefundAsync(
            List<(OrderItem Item, Product? Product)> orderItems,
            CancellationToken cancellationToken)
        {
            var deltas = new List<(string Sku, decimal QuantityDelta)>();
            foreach (var (item, product) in orderItems)
            {
                var sku = MikroStockCacheAligner.ResolveSku(item, product);
                if (string.IsNullOrWhiteSpace(sku) || item.Quantity <= 0)
                {
                    continue;
                }

                deltas.Add((sku, item.Quantity));
            }

            if (deltas.Count > 0)
            {
                await MikroStockCacheAligner.ApplyDeltaAsync(_db, deltas, _logger, cancellationToken);
            }
        }

        /// <summary>
        /// İade miktarını Mikro'da DahiliStokHareket ile artırır (eldeki stok).
        /// Fatura başarısız / generic IADE durumunda kullanılır. Idempotent.
        /// </summary>
        private async Task<bool> EnsureRefundStockIncreaseAsync(
            Order order,
            List<(OrderItem Item, Product? Product)> orderItems,
            CancellationToken cancellationToken)
        {
            if (!LocalInventoryPolicy.PushDahiliStockIncreaseOnOnlineRefund)
            {
                return false;
            }

            var alreadyIncreased = await _db.Set<MicroSyncLog>().AsNoTracking()
                .AnyAsync(l =>
                    l.EntityType == LocalInventoryPolicy.SyncEntityOrderStockIncrease &&
                    l.InternalId == order.Id.ToString() &&
                    l.Status == "Success" &&
                    l.Direction == DIRECTION_TO_ERP,
                    cancellationToken);

            if (alreadyIncreased)
            {
                return false;
            }

            var stockDtos = new List<MicroStockDto>();
            foreach (var group in orderItems
                         .Where(x => x.Item.Quantity > 0)
                         .GroupBy(x => MikroStockCacheAligner.ResolveSku(x.Item, x.Product)))
            {
                if (string.IsNullOrWhiteSpace(group.Key))
                {
                    continue;
                }

                var qty = group.Sum(x => x.Item.Quantity);
                stockDtos.Add(new MicroStockDto
                {
                    Sku = group.Key!,
                    Quantity = qty,
                    Stock = qty,
                    IsStockIncrease = true
                });
            }

            if (stockDtos.Count == 0)
            {
                _logger.LogWarning(
                    "[FaturaSyncService] Dahili stok artışı atlandı — SKU yok. OrderId={OrderId}",
                    order.Id);
                return false;
            }

            var ok = await _microService.UpsertStocksAsync(stockDtos);
            await _syncRepository.CreateLogAsync(new MicroSyncLog
            {
                EntityType = LocalInventoryPolicy.SyncEntityOrderStockIncrease,
                Direction = DIRECTION_TO_ERP,
                InternalId = order.Id.ToString(),
                ExternalId = order.OrderNumber,
                Status = ok ? "Success" : "Failed",
                Message = ok
                    ? $"Dahili stok girişi: {string.Join(", ", stockDtos.Select(s => $"{s.Sku}x{s.Quantity}"))}"
                    : "DahiliStokHareketKaydetV2 artış başarısız",
                CreatedAt = DateTime.UtcNow,
                LastAttemptAt = DateTime.UtcNow,
                Attempts = 1,
                LastError = ok ? null : "UpsertStocksAsync false"
            }, cancellationToken);

            if (!ok)
            {
                _logger.LogError(
                    "[FaturaSyncService] Mikro eldeki stok artışı BAŞARISIZ. OrderId={OrderId}",
                    order.Id);
                return false;
            }

            _logger.LogInformation(
                "[FaturaSyncService] Mikro eldeki stok artırıldı (Dahili). OrderId={OrderId}, Lines={Lines}",
                order.Id, stockDtos.Count);

            return true;
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
