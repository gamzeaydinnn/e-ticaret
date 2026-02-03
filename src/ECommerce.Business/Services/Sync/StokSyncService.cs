using System.Diagnostics;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Interfaces.Sync;
using ECommerce.Entities.Concrete;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Stok senkronizasyon servisi - Mikro ERP ile e-ticaret arası stok akışı.
    /// 
    /// NEDEN: Mağaza ve online satışlardan kaynaklanan stok değişiklikleri
    /// her iki sistemde de güncel tutulmalı. Bu servis çift yönlü akışı yönetir.
    /// 
    /// SORUMLULUKLAR:
    /// - Mikro'dan stok çekme (FromERP) - mağaza satışları, alışlar, sayımlar
    /// - Mikro'ya stok gönderme (ToERP) - online satışlar, rezervasyonlar
    /// 
    /// İLKELER:
    /// - Single Responsibility: Sadece stok senkronizasyonu
    /// - Delta sync: Sadece değişenler, performans için kritik
    /// - Retry: Max 3 deneme, exponential backoff
    /// </summary>
    public class StokSyncService : IStokSyncService
    {
        // ==================== BAĞIMLILIKLAR ====================
        
        private readonly IMicroService _microService;
        private readonly IProductRepository _productRepository;
        private readonly IMikroSyncRepository _syncRepository;
        private readonly ILogger<StokSyncService> _logger;
        
        // Sabitler
        private const string SYNC_TYPE = "Stok";
        private const string DIRECTION_FROM_ERP = "FromERP";
        private const string DIRECTION_TO_ERP = "ToERP";
        private const int MAX_RETRY_ATTEMPTS = 3;

        // ==================== CONSTRUCTOR ====================
        
        public StokSyncService(
            IMicroService microService,
            IProductRepository productRepository,
            IMikroSyncRepository syncRepository,
            ILogger<StokSyncService> logger)
        {
            _microService = microService ?? throw new ArgumentNullException(nameof(microService));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _syncRepository = syncRepository ?? throw new ArgumentNullException(nameof(syncRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ==================== MIKRO'DAN STOK ÇEKME ====================

        /// <inheritdoc />
        public async Task<SyncResult> SyncAllFromMikroAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<SyncError>();
            int processedCount = 0;
            int successCount = 0;
            int failedCount = 0;

            _logger.LogInformation(
                "[StokSyncService] Tam stok senkronizasyonu başlatıldı (FromERP)");

            try
            {
                // 1. Mikro'dan tüm stokları çek
                var stocks = await _microService.GetStocksAsync();
                var stockList = stocks.ToList();
                processedCount = stockList.Count;

                _logger.LogInformation(
                    "[StokSyncService] Mikro'dan {Count} stok kaydı alındı",
                    processedCount);

                // 2. Her stok için güncelleme yap
                foreach (var stock in stockList)
                {
                    if (string.IsNullOrWhiteSpace(stock.Sku))
                    {
                        _logger.LogWarning(
                            "[StokSyncService] SKU boş, atlanıyor: {@Stock}", stock);
                        continue;
                    }

                    try
                    {
                        var updateResult = await ProcessStockUpdateAsync(stock, cancellationToken);
                        
                        if (updateResult.IsSuccess)
                        {
                            successCount++;
                        }
                        else
                        {
                            failedCount++;
                            errors.AddRange(updateResult.Errors);
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        errors.Add(new SyncError(
                            "StokUpdate",
                            stock.Sku,
                            ex.Message,
                            ex.StackTrace));

                        _logger.LogError(ex,
                            "[StokSyncService] Stok güncelleme hatası. SKU: {Sku}",
                            stock.Sku);
                    }
                }

                stopwatch.Stop();

                // 3. Sync state güncelle
                await _syncRepository.UpdateSyncSuccessAsync(
                    SYNC_TYPE,
                    DIRECTION_FROM_ERP,
                    successCount,
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken);

                // 4. Sonuç log
                _logger.LogInformation(
                    "[StokSyncService] Tam stok senkronizasyonu tamamlandı. " +
                    "Toplam: {Total}, Başarılı: {Success}, Hatalı: {Failed}, Süre: {Duration}ms",
                    processedCount, successCount, failedCount, stopwatch.ElapsedMilliseconds);

                return SyncResult.Ok(successCount, errors);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                await _syncRepository.UpdateSyncFailureAsync(
                    SYNC_TYPE,
                    DIRECTION_FROM_ERP,
                    ex.Message,
                    cancellationToken);

                _logger.LogError(ex,
                    "[StokSyncService] Tam stok senkronizasyonu başarısız!");

                return SyncResult.Fail(new SyncError(
                    "SyncAllFromMikro",
                    null,
                    ex.Message,
                    ex.StackTrace));
            }
        }

        /// <inheritdoc />
        public async Task<SyncResult> SyncDeltaFromMikroAsync(
            DateTime? since = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<SyncError>();
            int processedCount = 0;
            int successCount = 0;
            int failedCount = 0;

            // Delta için başlangıç tarihi belirle
            var syncState = await _syncRepository.GetSyncStateAsync(SYNC_TYPE, DIRECTION_FROM_ERP, cancellationToken);
            var lastSyncTime = since ?? syncState?.LastSyncTime ?? DateTime.UtcNow.AddHours(-24);

            _logger.LogInformation(
                "[StokSyncService] Delta stok senkronizasyonu başlatıldı. " +
                "Başlangıç tarihi: {Since}",
                lastSyncTime);

            try
            {
                // 1. Mikro'dan değişen stokları çek
                // NOT: MicroService şu an tüm stokları döndürüyor,
                // gerçek implementasyonda tarihe göre filtreleme yapılmalı
                var stocks = await _microService.GetStocksAsync();
                
                // Filtreleme yapılamıyorsa tümünü işle
                // Gelecekte: stocks.Where(s => s.LastModified > lastSyncTime)
                var stockList = stocks.ToList();
                processedCount = stockList.Count;

                _logger.LogInformation(
                    "[StokSyncService] Mikro'dan {Count} stok kaydı alındı (delta)",
                    processedCount);

                // 2. Her stok için güncelleme
                foreach (var stock in stockList)
                {
                    if (string.IsNullOrWhiteSpace(stock.Sku))
                        continue;

                    try
                    {
                        var updateResult = await ProcessStockUpdateAsync(stock, cancellationToken);
                        
                        if (updateResult.IsSuccess)
                            successCount++;
                        else
                        {
                            failedCount++;
                            errors.AddRange(updateResult.Errors);
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        errors.Add(new SyncError("StokDeltaUpdate", stock.Sku, ex.Message));
                        
                        _logger.LogError(ex,
                            "[StokSyncService] Delta güncelleme hatası. SKU: {Sku}",
                            stock.Sku);
                    }
                }

                stopwatch.Stop();

                // 3. Sync state güncelle
                await _syncRepository.UpdateSyncSuccessAsync(
                    SYNC_TYPE,
                    DIRECTION_FROM_ERP,
                    successCount,
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken);

                _logger.LogInformation(
                    "[StokSyncService] Delta stok senkronizasyonu tamamlandı. " +
                    "Başarılı: {Success}, Hatalı: {Failed}, Süre: {Duration}ms",
                    successCount, failedCount, stopwatch.ElapsedMilliseconds);

                return SyncResult.Ok(successCount, errors);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                await _syncRepository.UpdateSyncFailureAsync(
                    SYNC_TYPE,
                    DIRECTION_FROM_ERP,
                    ex.Message,
                    cancellationToken);

                _logger.LogError(ex, "[StokSyncService] Delta stok senkronizasyonu başarısız!");

                return SyncResult.Fail(new SyncError("SyncDeltaFromMikro", null, ex.Message));
            }
        }

        // ==================== MIKRO'YA STOK GÖNDERME ====================

        /// <inheritdoc />
        public async Task<SyncResult> PushStockToMikroAsync(
            int productId,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[StokSyncService] Tekil stok gönderimi başlatıldı. ProductId: {ProductId}",
                productId);

            try
            {
                // 1. Ürünü bul
                var product = await _productRepository.GetByIdAsync(productId);
                
                if (product == null)
                {
                    var error = new SyncError(
                        "PushStockToMikro",
                        productId.ToString(),
                        "Ürün bulunamadı");

                    _logger.LogWarning(
                        "[StokSyncService] Ürün bulunamadı. ProductId: {ProductId}",
                        productId);

                    return SyncResult.Fail(error);
                }

                // 2. Mikro'ya gönder
                var result = await PushProductStockWithRetryAsync(product, cancellationToken);

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
                    "[StokSyncService] Stok gönderimi başarısız. ProductId: {ProductId}",
                    productId);

                return SyncResult.Fail(new SyncError(
                    "PushStockToMikro",
                    productId.ToString(),
                    ex.Message));
            }
        }

        /// <inheritdoc />
        public async Task<SyncResult> PushAllStocksToMikroAsync(
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<SyncError>();
            int successCount = 0;
            int failedCount = 0;

            _logger.LogInformation("[StokSyncService] Toplu stok gönderimi başlatıldı (ToERP)");

            try
            {
                // 1. Tüm ürünleri al
                var products = _productRepository.GetAll();
                var productList = products.ToList();

                _logger.LogInformation(
                    "[StokSyncService] {Count} ürün stoğu gönderilecek",
                    productList.Count);

                // 2. Her ürün için Mikro'ya gönder
                foreach (var product in productList)
                {
                    try
                    {
                        var result = await PushProductStockWithRetryAsync(product, cancellationToken);
                        
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
                        errors.Add(new SyncError(
                            "PushStock",
                            product.SKU,
                            ex.Message));

                        _logger.LogError(ex,
                            "[StokSyncService] Stok gönderim hatası. SKU: {Sku}",
                            product.SKU);
                    }
                }

                stopwatch.Stop();

                // 3. Sync state güncelle
                await _syncRepository.UpdateSyncSuccessAsync(
                    SYNC_TYPE,
                    DIRECTION_TO_ERP,
                    successCount,
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken);

                _logger.LogInformation(
                    "[StokSyncService] Toplu stok gönderimi tamamlandı. " +
                    "Başarılı: {Success}, Hatalı: {Failed}, Süre: {Duration}ms",
                    successCount, failedCount, stopwatch.ElapsedMilliseconds);

                return SyncResult.Ok(successCount, errors);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                await _syncRepository.UpdateSyncFailureAsync(
                    SYNC_TYPE,
                    DIRECTION_TO_ERP,
                    ex.Message,
                    cancellationToken);

                _logger.LogError(ex, "[StokSyncService] Toplu stok gönderimi başarısız!");

                return SyncResult.Fail(new SyncError("PushAllStocksToMikro", null, ex.Message));
            }
        }

        // ==================== YARDIMCI METODLAR ====================

        /// <summary>
        /// Tek bir stok kaydını e-ticaret veritabanında günceller.
        /// SKU eşlemesi yapılır, bulunamazsa log atılır ve atlanır.
        /// 
        /// NEDEN: Her stok güncellemesi bağımsız transaction'da
        /// işlenir ki bir hata diğerlerini etkilemesin.
        /// </summary>
        private async Task<SyncResult> ProcessStockUpdateAsync(
            MicroStockDto stock,
            CancellationToken cancellationToken)
        {
            // 1. SKU ile ürün bul
            var product = await _productRepository.GetBySkuAsync(stock.Sku);

            if (product == null)
            {
                // Ürün bulunamadı - log at ama hata döndürme
                // NEDEN: Mikro'da olup e-ticaret'te olmayan ürünler olabilir
                await _syncRepository.CreateLogAsync(new MicroSyncLog
                {
                    EntityType = "Stock",
                    Direction = DIRECTION_FROM_ERP,
                    ExternalId = stock.Sku,
                    Status = "Skipped",
                    Message = "Ürün e-ticaret'te bulunamadı",
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);

                _logger.LogDebug(
                    "[StokSyncService] Ürün bulunamadı, atlanıyor. SKU: {Sku}",
                    stock.Sku);

                // Atlanmış ama hata değil
                return SyncResult.Ok(0);
            }

            // 2. Stok miktarını güncelle
            // NEDEN: stock.Quantity ve stock.Stock iki farklı kaynak
            // olabilir, öncelik Quantity'de
            var newQuantity = stock.Quantity > 0 ? stock.Quantity : stock.Stock;
            var oldQuantity = product.StockQuantity;

            if (oldQuantity != newQuantity)
            {
                product.StockQuantity = newQuantity;
                await _productRepository.UpdateAsync(product);

                // Log başarılı güncelleme
                await _syncRepository.CreateLogAsync(new MicroSyncLog
                {
                    EntityType = "Stock",
                    Direction = DIRECTION_FROM_ERP,
                    ExternalId = stock.Sku,
                    InternalId = product.Id.ToString(),
                    Status = "Success",
                    Message = $"Stok güncellendi: {oldQuantity} → {newQuantity}",
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);

                _logger.LogDebug(
                    "[StokSyncService] Stok güncellendi. SKU: {Sku}, " +
                    "Önceki: {Old}, Yeni: {New}",
                    stock.Sku, oldQuantity, newQuantity);
            }

            return SyncResult.Ok(1);
        }

        /// <summary>
        /// Ürün stoğunu Mikro'ya retry mekanizması ile gönderir.
        /// 
        /// RETRY POLİTİKASI:
        /// - Max 3 deneme
        /// - Exponential backoff: 1s, 2s, 4s
        /// - Her hata loglanır
        /// </summary>
        private async Task<SyncResult> PushProductStockWithRetryAsync(
            Product product,
            CancellationToken cancellationToken)
        {
            var syncLog = new MicroSyncLog
            {
                EntityType = "Stock",
                Direction = DIRECTION_TO_ERP,
                InternalId = product.Id.ToString(),
                ExternalId = product.SKU,
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

                    // MicroService.UpsertStocksAsync kullan
                    var stockDto = new MicroStockDto
                    {
                        Sku = product.SKU,
                        Stock = product.StockQuantity,
                        Quantity = product.StockQuantity
                    };

                    var success = await _microService.UpsertStocksAsync(new[] { stockDto });

                    if (success)
                    {
                        syncLog.Status = "Success";
                        syncLog.Message = $"Stok gönderildi: {product.StockQuantity}";
                        await _syncRepository.CreateLogAsync(syncLog, cancellationToken);

                        _logger.LogDebug(
                            "[StokSyncService] Stok Mikro'ya gönderildi. " +
                            "SKU: {Sku}, Miktar: {Qty}",
                            product.SKU, product.StockQuantity);

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
                        "[StokSyncService] Stok gönderimi başarısız. " +
                        "SKU: {Sku}, Deneme: {Attempt}/{Max}, Hata: {Error}",
                        product.SKU, attempt, MAX_RETRY_ATTEMPTS, ex.Message);

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
                "[StokSyncService] Stok gönderimi başarısız (max deneme). SKU: {Sku}",
                product.SKU);

            return SyncResult.Fail(new SyncError(
                "PushStock",
                product.SKU,
                syncLog.LastError ?? "Max retry exceeded"));
        }
    }
}
