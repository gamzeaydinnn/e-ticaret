using System.Diagnostics;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Interfaces.Sync;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Infrastructure.Config;
using ECommerce.Infrastructure.Services.MicroServices;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// ECommerce → Mikro yönünde anlık veri push servisi.
    /// 
    /// NEDEN: Sipariş sonrası stok düşüşü, admin fiyat değişikliği, ürün güncelleme
    /// gibi olaylar Mikro'ya anında bildirilmeli. 15dk polling ile Mikro tarafında
    /// mağaza stoğu ile online stok arasında tutarsızlık oluşuyor.
    /// 
    /// STRATEJİ:
    /// - Hangfire BackgroundJob.Enqueue ile anında kuyruğa alır (fire-and-retry)
    /// - Her push işlemi bağımsız olarak retry edilir (3 deneme, exponential backoff)
    /// - Başarısız push'lar MicroSyncLog tablosuna yazılır
    /// - Rate limiting: Mikro API'yi boğmamak için ardışık push'lar arası 100ms bekleme
    /// 
    /// GÜVENLİK:
    /// - Mikro API auth token her istekte yenilenir (günlük MD5 hash)
    /// - Timeout: Her istek 30sn'de kesilir (Mikro API timeout'unu aşmamak için)
    /// </summary>
    public class MikroOutboundSyncService : IMikroOutboundSyncService
    {
        private readonly IMicroService _microService;
        private readonly IProductRepository _productRepository;
        private readonly IMikroSyncRepository _syncRepository;
        private readonly ISyncLogger _syncLogger;
        private readonly ECommerceDbContext _context;
        private readonly MikroSettings _settings;
        private readonly ILogger<MikroOutboundSyncService> _logger;

        // Mikro API'yi boğmamak için push'lar arası minimum bekleme
        private static readonly TimeSpan PushThrottleDelay = TimeSpan.FromMilliseconds(100);

        // Tek seferde push edilecek max kayıt (batch limit)
        private const int MaxBatchSize = 50;

        public MikroOutboundSyncService(
            IMicroService microService,
            IProductRepository productRepository,
            IMikroSyncRepository syncRepository,
            ISyncLogger syncLogger,
            ECommerceDbContext context,
            IOptions<MikroSettings> settings,
            ILogger<MikroOutboundSyncService> logger)
        {
            _microService = microService ?? throw new ArgumentNullException(nameof(microService));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _syncRepository = syncRepository ?? throw new ArgumentNullException(nameof(syncRepository));
            _syncLogger = syncLogger ?? throw new ArgumentNullException(nameof(syncLogger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<OutboundPushResult> PushStockChangeAsync(
            int productId,
            int newQuantity,
            string changeReason,
            CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();

            // SKU'yu bul — Mikro'ya stok kodu ile göndermemiz gerekiyor
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("[OutboundSync] Ürün bulunamadı. ProductId: {ProductId}", productId);
                return OutboundPushResult.Fail($"Ürün bulunamadı: {productId}", 1);
            }

            if (string.IsNullOrWhiteSpace(product.SKU))
            {
                _logger.LogWarning(
                    "[OutboundSync] Ürünün SKU'su yok, Mikro'ya push yapılamaz. ProductId: {ProductId}",
                    productId);
                return OutboundPushResult.Fail($"SKU eksik: {productId}", 1);
            }

            var log = await _syncLogger.StartOperationAsync(
                "Stok", "ToERP", product.SKU, productId.ToString(),
                $"Stok push: {product.SKU} → {newQuantity} ({changeReason})",
                cancellationToken);

            try
            {
                // MikroProductCache'deki eşleşmeyi kontrol et
                var cache = await _context.Set<MikroProductCache>()
                    .FirstOrDefaultAsync(c => c.StokKod == product.SKU, cancellationToken);

                if (cache == null)
                {
                    _logger.LogWarning(
                        "[OutboundSync] SKU({Sku}) MikroProductCache'de bulunamadı — push atlandı.",
                        product.SKU);
                    await _syncLogger.FailOperationAsync(log.Id, "Cache'de karşılık yok", cancellationToken);
                    return OutboundPushResult.Fail($"Cache'de bulunamadı: {product.SKU}", 1);
                }

                // Mikro'ya stok güncelleme gönder (SaveStokV2Async)
                var result = await _microService.UpsertStocksAsync(
                    new[]
                    {
                        new MicroStockDto
                        {
                            Sku = product.SKU,
                            Quantity = newQuantity
                        }
                    });

                sw.Stop();

                if (result)
                {
                    // Cache'i güncelle — Mikro ile senkron tutmak için
                    cache.DepoMiktari = newQuantity;
                    cache.SatilabilirMiktar = newQuantity;
                    cache.GuncellemeTarihi = DateTime.UtcNow;
                    cache.SyncStatus = (int)MikroSyncStatus.Synced;
                    await _context.SaveChangesAsync(cancellationToken);

                    await _syncLogger.CompleteOperationAsync(
                        log.Id,
                        $"Stok push başarılı: {product.SKU} = {newQuantity}",
                        cancellationToken);

                    await _syncRepository.UpdateSyncSuccessAsync(
                        "StockPush", "ToERP", 1, sw.ElapsedMilliseconds, cancellationToken);

                    _logger.LogInformation(
                        "[OutboundSync] Stok push başarılı. SKU: {Sku}, Qty: {Qty}, Sebep: {Reason}, Süre: {Ms}ms",
                        product.SKU, newQuantity, changeReason, sw.ElapsedMilliseconds);

                    return OutboundPushResult.Ok(1, sw.ElapsedMilliseconds);
                }

                await _syncLogger.FailOperationAsync(log.Id, "Mikro API false döndürdü", cancellationToken);
                await _syncRepository.UpdateSyncFailureAsync(
                    "StockPush", "ToERP", "Mikro API false döndürdü", cancellationToken);

                return OutboundPushResult.Fail("Mikro API başarısız yanıt döndü", 1, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[OutboundSync] Stok push hatası. SKU: {Sku}, ProductId: {ProductId}",
                    product.SKU, productId);

                await _syncLogger.FailOperationAsync(log.Id, ex.Message, cancellationToken);
                await _syncRepository.UpdateSyncFailureAsync(
                    "StockPush", "ToERP", ex.Message, cancellationToken);

                return OutboundPushResult.Fail(ex.Message, 1, sw.ElapsedMilliseconds);
            }
        }

        /// <inheritdoc />
        public async Task<OutboundPushResult> PushPriceChangeAsync(
            int productId,
            decimal newPrice,
            CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();

            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                return OutboundPushResult.Fail($"Ürün bulunamadı: {productId}", 1);

            if (string.IsNullOrWhiteSpace(product.SKU))
                return OutboundPushResult.Fail($"SKU eksik: {productId}", 1);

            var log = await _syncLogger.StartOperationAsync(
                "Fiyat", "ToERP", product.SKU, productId.ToString(),
                $"Fiyat push: {product.SKU} → {newPrice:N2}₺",
                cancellationToken);

            try
            {
                var result = await _microService.UpsertPricesAsync(
                    new[]
                    {
                        new MicroPriceDto
                        {
                            Sku = product.SKU,
                            Price = newPrice
                        }
                    });

                sw.Stop();

                if (result)
                {
                    // Cache güncelle
                    var cache = await _context.Set<MikroProductCache>()
                        .FirstOrDefaultAsync(c => c.StokKod == product.SKU, cancellationToken);
                    if (cache != null)
                    {
                        cache.SatisFiyati = newPrice;
                        cache.GuncellemeTarihi = DateTime.UtcNow;
                        cache.SyncStatus = (int)MikroSyncStatus.Synced;
                        await _context.SaveChangesAsync(cancellationToken);
                    }

                    await _syncLogger.CompleteOperationAsync(
                        log.Id,
                        $"Fiyat push başarılı: {product.SKU} = {newPrice:N2}₺",
                        cancellationToken);

                    await _syncRepository.UpdateSyncSuccessAsync(
                        "PricePush", "ToERP", 1, sw.ElapsedMilliseconds, cancellationToken);

                    _logger.LogInformation(
                        "[OutboundSync] Fiyat push başarılı. SKU: {Sku}, Fiyat: {Price:N2}₺, Süre: {Ms}ms",
                        product.SKU, newPrice, sw.ElapsedMilliseconds);

                    return OutboundPushResult.Ok(1, sw.ElapsedMilliseconds);
                }

                await _syncLogger.FailOperationAsync(log.Id, "Mikro API false döndürdü", cancellationToken);
                return OutboundPushResult.Fail("Mikro API başarısız yanıt döndü", 1, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[OutboundSync] Fiyat push hatası. SKU: {Sku}, ProductId: {ProductId}",
                    product.SKU, productId);
                await _syncLogger.FailOperationAsync(log.Id, ex.Message, cancellationToken);
                return OutboundPushResult.Fail(ex.Message, 1, sw.ElapsedMilliseconds);
            }
        }

        /// <inheritdoc />
        public async Task<OutboundPushResult> PushProductInfoChangeAsync(
            int productId,
            CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();

            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                return OutboundPushResult.Fail($"Ürün bulunamadı: {productId}", 1);

            if (string.IsNullOrWhiteSpace(product.SKU))
                return OutboundPushResult.Fail($"SKU eksik: {productId}", 1);

            var log = await _syncLogger.StartOperationAsync(
                "UrunBilgi", "ToERP", product.SKU, productId.ToString(),
                $"Ürün bilgi push: {product.SKU} ({product.Name})",
                cancellationToken);

            try
            {
                var result = await _microService.UpsertProductsAsync(
                    new[]
                    {
                        new MicroProductDto
                        {
                            Sku = product.SKU,
                            Name = product.Name,
                            Price = product.Price,
                            Stock = product.StockQuantity
                        }
                    });

                sw.Stop();

                if (result)
                {
                    await _syncLogger.CompleteOperationAsync(log.Id,
                        $"Ürün bilgi push başarılı: {product.SKU}", cancellationToken);

                    _logger.LogInformation(
                        "[OutboundSync] Ürün bilgi push başarılı. SKU: {Sku}, Süre: {Ms}ms",
                        product.SKU, sw.ElapsedMilliseconds);

                    return OutboundPushResult.Ok(1, sw.ElapsedMilliseconds);
                }

                await _syncLogger.FailOperationAsync(log.Id, "Mikro API false döndürdü", cancellationToken);
                return OutboundPushResult.Fail("Mikro API başarısız yanıt döndü", 1, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[OutboundSync] Ürün bilgi push hatası. SKU: {Sku}", product.SKU);
                await _syncLogger.FailOperationAsync(log.Id, ex.Message, cancellationToken);
                return OutboundPushResult.Fail(ex.Message, 1, sw.ElapsedMilliseconds);
            }
        }

        /// <inheritdoc />
        public async Task<OutboundPushResult> PushBulkStockChangesAsync(
            IDictionary<int, int> productStockMap,
            string changeReason,
            CancellationToken cancellationToken = default)
        {
            if (productStockMap == null || productStockMap.Count == 0)
                return OutboundPushResult.Ok(0);

            var sw = Stopwatch.StartNew();
            int pushed = 0, failed = 0;
            var errors = new List<OutboundPushError>();

            // Batch'ler halinde işle — Mikro API'yi boğmamak için
            foreach (var batch in productStockMap.Chunk(MaxBatchSize))
            {
                foreach (var (productId, newQty) in batch)
                {
                    var result = await PushStockChangeAsync(productId, newQty, changeReason, cancellationToken);

                    if (result.Success)
                        pushed++;
                    else
                    {
                        failed++;
                        errors.Add(new OutboundPushError
                        {
                            ProductId = productId,
                            Operation = "BulkStockPush",
                            ErrorMessage = result.ErrorMessage ?? "Bilinmeyen hata"
                        });
                    }

                    // Rate limiting — ardışık push'lar arası minimum bekleme
                    await Task.Delay(PushThrottleDelay, cancellationToken);
                }
            }

            sw.Stop();

            _logger.LogInformation(
                "[OutboundSync] Toplu stok push tamamlandı. " +
                "Başarılı: {Pushed}, Başarısız: {Failed}, Süre: {Ms}ms",
                pushed, failed, sw.ElapsedMilliseconds);

            return OutboundPushResult.Partial(pushed, failed, sw.ElapsedMilliseconds, errors);
        }

        /// <inheritdoc />
        public async Task<OutboundPushResult> RetryFailedPushesAsync(
            CancellationToken cancellationToken = default)
        {
            // Başarısız sync log'larını çek (son 24 saat, max 3 retry)
            var failedLogs = await _context.Set<MicroSyncLog>()
                .Where(l => l.Status == "Failed"
                         && l.Attempts < 3
                         && l.Direction == "ToERP"
                         && l.CreatedAt >= DateTime.UtcNow.AddHours(-24))
                .OrderBy(l => l.CreatedAt)
                .Take(MaxBatchSize)
                .ToListAsync(cancellationToken);

            if (failedLogs.Count == 0)
                return OutboundPushResult.Ok(0);

            _logger.LogInformation(
                "[OutboundSync] {Count} başarısız push yeniden deneniyor...",
                failedLogs.Count);

            int retried = 0, succeeded = 0;
            foreach (var log in failedLogs)
            {
                retried++;

                // Log'dan productId ve operation tipini çıkar
                if (!int.TryParse(log.InternalId, out var productId))
                    continue;

                OutboundPushResult result;
                switch (log.EntityType)
                {
                    case "Stok":
                        var product = await _productRepository.GetByIdAsync(productId);
                        if (product == null) continue;
                        result = await PushStockChangeAsync(
                            productId, product.StockQuantity, "Retry", cancellationToken);
                        break;
                    case "Fiyat":
                        var priceProduct = await _productRepository.GetByIdAsync(productId);
                        if (priceProduct == null) continue;
                        result = await PushPriceChangeAsync(
                            productId, priceProduct.Price, cancellationToken);
                        break;
                    case "UrunBilgi":
                        result = await PushProductInfoChangeAsync(productId, cancellationToken);
                        break;
                    default:
                        continue;
                }

                if (result.Success)
                    succeeded++;

                // Retry sayısını güncelle
                log.Attempts++;
                log.Status = result.Success ? "Success" : "Failed";
                log.LastAttemptAt = DateTime.UtcNow;
                log.LastError = result.Success ? null : result.ErrorMessage;

                await Task.Delay(PushThrottleDelay, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[OutboundSync] Retry tamamlandı. Denenen: {Retried}, Başarılı: {Succeeded}",
                retried, succeeded);

            return OutboundPushResult.Partial(succeeded, retried - succeeded, 0);
        }
    }

    /// <summary>
    /// Hangfire üzerinden anında tetiklenen outbound push job'ı.
    /// 
    /// KULLANIM: InventoryManager veya ProductManager bir değişiklik yaptığında
    /// BackgroundJob.Enqueue ile kuyruğa alınır. Bu sayede ana iş akışı bloklanmaz.
    /// </summary>
    public static class MikroOutboundPushTrigger
    {
        /// <summary>
        /// Stok değişikliğini Hangfire kuyruğuna ekler.
        /// Ana iş akışını bloklamaz — arka planda çalışır.
        /// </summary>
        public static void EnqueueStockPush(int productId, int newQuantity, string reason)
        {
            BackgroundJob.Enqueue<IMikroOutboundSyncService>(
                svc => svc.PushStockChangeAsync(productId, newQuantity, reason, CancellationToken.None));
        }

        /// <summary>
        /// Fiyat değişikliğini Hangfire kuyruğuna ekler.
        /// </summary>
        public static void EnqueuePricePush(int productId, decimal newPrice)
        {
            BackgroundJob.Enqueue<IMikroOutboundSyncService>(
                svc => svc.PushPriceChangeAsync(productId, newPrice, CancellationToken.None));
        }

        /// <summary>
        /// Ürün bilgi değişikliğini Hangfire kuyruğuna ekler.
        /// </summary>
        public static void EnqueueProductInfoPush(int productId)
        {
            BackgroundJob.Enqueue<IMikroOutboundSyncService>(
                svc => svc.PushProductInfoChangeAsync(productId, CancellationToken.None));
        }
    }
}
