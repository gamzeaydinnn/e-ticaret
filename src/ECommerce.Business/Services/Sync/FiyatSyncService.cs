using System.Diagnostics;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Interfaces.Sync;
using ECommerce.Entities.Concrete;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Fiyat senkronizasyon servisi - Mikro ERP ile e-ticaret arası fiyat akışı.
    /// 
    /// NEDEN: Merkezi fiyat yönetimi genelde ERP'de yapılır (Mikro).
    /// Bu servis Mikro'daki fiyat değişikliklerini e-ticaret'e yansıtır.
    /// 
    /// AKIŞ (ÖNERİLEN - FromERP):
    /// 1. Mikro'da fiyat değişir (manuel veya toplu)
    /// 2. Bu servis değişiklikleri periyodik olarak çeker
    /// 3. E-ticaret ürün fiyatları güncellenir
    /// 
    /// TERSİ (ToERP - NADİR):
    /// Sadece e-ticaret'e özel kampanyalarda kullanılır.
    /// Dikkatli olunmalı, ERP ana kaynak olmalı.
    /// </summary>
    public class FiyatSyncService : IFiyatSyncService
    {
        // ==================== BAĞIMLILIKLAR ====================

        private readonly IMicroService _microService;
        private readonly IProductRepository _productRepository;
        private readonly IMikroSyncRepository _syncRepository;
        private readonly ILogger<FiyatSyncService> _logger;

        // Sabitler
        private const string SYNC_TYPE = "Fiyat";
        private const string DIRECTION_FROM_ERP = "FromERP";
        private const string DIRECTION_TO_ERP = "ToERP";
        private const int MAX_RETRY_ATTEMPTS = 3;
        private const int DEFAULT_FIYAT_NO = 1; // Perakende fiyat listesi

        // ==================== CONSTRUCTOR ====================

        public FiyatSyncService(
            IMicroService microService,
            IProductRepository productRepository,
            IMikroSyncRepository syncRepository,
            ILogger<FiyatSyncService> logger)
        {
            _microService = microService ?? throw new ArgumentNullException(nameof(microService));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _syncRepository = syncRepository ?? throw new ArgumentNullException(nameof(syncRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ==================== MIKRO'DAN FİYAT ÇEKME ====================

        /// <inheritdoc />
        public async Task<SyncResult> SyncAllFromMikroAsync(
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<SyncError>();
            int processedCount = 0;
            int successCount = 0;
            int failedCount = 0;

            _logger.LogInformation(
                "[FiyatSyncService] Tam fiyat senkronizasyonu başlatıldı (FromERP)");

            try
            {
                // 1. Mikro'dan tüm fiyatları çek
                var prices = await _microService.GetPricesAsync();
                var priceList = prices.ToList();
                processedCount = priceList.Count;

                _logger.LogInformation(
                    "[FiyatSyncService] Mikro'dan {Count} fiyat kaydı alındı",
                    processedCount);

                // 2. Her fiyat için güncelleme yap
                foreach (var price in priceList)
                {
                    if (string.IsNullOrWhiteSpace(price.Sku))
                    {
                        _logger.LogWarning(
                            "[FiyatSyncService] SKU boş, atlanıyor: {@Price}", price);
                        continue;
                    }

                    try
                    {
                        var updateResult = await ProcessPriceUpdateAsync(price, cancellationToken);

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
                        errors.Add(new SyncError(
                            "FiyatUpdate",
                            price.Sku,
                            ex.Message,
                            ex.StackTrace));

                        _logger.LogError(ex,
                            "[FiyatSyncService] Fiyat güncelleme hatası. SKU: {Sku}",
                            price.Sku);
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
                    "[FiyatSyncService] Tam fiyat senkronizasyonu tamamlandı. " +
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
                    "[FiyatSyncService] Tam fiyat senkronizasyonu başarısız!");

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
            int successCount = 0;
            int failedCount = 0;

            // Delta için başlangıç tarihi belirle
            var syncState = await _syncRepository.GetSyncStateAsync(
                SYNC_TYPE, 
                DIRECTION_FROM_ERP, 
                cancellationToken);
            var lastSyncTime = since ?? syncState?.LastSyncTime ?? DateTime.UtcNow.AddHours(-24);

            _logger.LogInformation(
                "[FiyatSyncService] Delta fiyat senkronizasyonu başlatıldı. Tarih: {Since}",
                lastSyncTime);

            try
            {
                // 1. Mikro'dan değişen fiyatları çek
                var prices = await _microService.GetPricesAsync();
                var priceList = prices.ToList();

                _logger.LogInformation(
                    "[FiyatSyncService] Mikro'dan {Count} fiyat kaydı alındı (delta)",
                    priceList.Count);

                // 2. Her fiyat için güncelleme
                foreach (var price in priceList)
                {
                    if (string.IsNullOrWhiteSpace(price.Sku))
                        continue;

                    try
                    {
                        var updateResult = await ProcessPriceUpdateAsync(price, cancellationToken);

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
                        errors.Add(new SyncError("FiyatDeltaUpdate", price.Sku, ex.Message));
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
                    "[FiyatSyncService] Delta fiyat senkronizasyonu tamamlandı. " +
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

                _logger.LogError(ex,
                    "[FiyatSyncService] Delta fiyat senkronizasyonu başarısız!");

                return SyncResult.Fail(new SyncError("SyncDeltaFromMikro", null, ex.Message));
            }
        }

        // ==================== MIKRO'YA FİYAT GÖNDERME ====================

        /// <inheritdoc />
        public async Task<SyncResult> PushPriceToMikroAsync(
            int productId,
            decimal newPrice,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[FiyatSyncService] Tekil fiyat gönderimi başlatıldı. " +
                "ProductId: {ProductId}, YeniFiyat: {Price}",
                productId, newPrice);

            try
            {
                // 1. Ürünü bul
                var product = await _productRepository.GetByIdAsync(productId);

                if (product == null)
                {
                    var error = new SyncError(
                        "PushPriceToMikro",
                        productId.ToString(),
                        "Ürün bulunamadı");

                    _logger.LogWarning(
                        "[FiyatSyncService] Ürün bulunamadı. ProductId: {ProductId}",
                        productId);

                    return SyncResult.Fail(error);
                }

                // 2. Fiyat güncelleme DTO'su oluştur
                var priceDto = new MicroPriceDto
                {
                    Sku = product.SKU,
                    Price = newPrice
                };

                // 3. Mikro'ya gönder (retry ile)
                var result = await PushPriceWithRetryAsync(product, priceDto, cancellationToken);

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
                    "[FiyatSyncService] Fiyat gönderimi başarısız. ProductId: {ProductId}",
                    productId);

                return SyncResult.Fail(new SyncError(
                    "PushPriceToMikro",
                    productId.ToString(),
                    ex.Message));
            }
        }

        /// <inheritdoc />
        public async Task<SyncResult> PushCampaignPricesToMikroAsync(
            IEnumerable<(int ProductId, decimal CampaignPrice, DateTime? StartDate, DateTime? EndDate)> campaignPrices,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<SyncError>();
            int successCount = 0;
            int failedCount = 0;

            var pricesList = campaignPrices.ToList();

            _logger.LogInformation(
                "[FiyatSyncService] Kampanya fiyatları Mikro'ya gönderiliyor. Adet: {Count}",
                pricesList.Count);

            try
            {
                foreach (var (productId, campaignPrice, startDate, endDate) in pricesList)
                {
                    try
                    {
                        var result = await PushPriceToMikroAsync(productId, campaignPrice, cancellationToken);

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
                            "CampaignPrice",
                            productId.ToString(),
                            ex.Message));
                    }
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "[FiyatSyncService] Kampanya fiyatları gönderildi. " +
                    "Başarılı: {Success}, Hatalı: {Failed}, Süre: {Duration}ms",
                    successCount, failedCount, stopwatch.ElapsedMilliseconds);

                return SyncResult.Ok(successCount, errors);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "[FiyatSyncService] Kampanya fiyatları gönderimi başarısız!");

                return SyncResult.Fail(new SyncError(
                    "PushCampaignPrices",
                    null,
                    ex.Message));
            }
        }

        // ==================== YARDIMCI METODLAR ====================

        /// <summary>
        /// Tek bir fiyat kaydını e-ticaret veritabanında günceller.
        /// SKU eşlemesi yapılır, bulunamazsa atlanır.
        /// 
        /// NEDEN: Her fiyat güncellemesi bağımsız işlenir,
        /// bir hata diğerlerini etkilemez.
        /// </summary>
        private async Task<SyncResult> ProcessPriceUpdateAsync(
            MicroPriceDto price,
            CancellationToken cancellationToken)
        {
            // 1. SKU ile ürün bul
            var product = await _productRepository.GetBySkuAsync(price.Sku);

            if (product == null)
            {
                // Ürün bulunamadı - log at ama hata döndürme
                await _syncRepository.CreateLogAsync(new MicroSyncLog
                {
                    EntityType = "Price",
                    Direction = DIRECTION_FROM_ERP,
                    ExternalId = price.Sku,
                    Status = "Skipped",
                    Message = "Ürün e-ticaret'te bulunamadı",
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);

                _logger.LogDebug(
                    "[FiyatSyncService] Ürün bulunamadı, atlanıyor. SKU: {Sku}",
                    price.Sku);

                return SyncResult.Ok(0);
            }

            // 2. Fiyat değişti mi kontrol et
            var oldPrice = product.Price;
            var newPrice = price.Price;

            if (oldPrice != newPrice)
            {
                product.Price = newPrice;
                await _productRepository.UpdateAsync(product);

                // Log başarılı güncelleme
                await _syncRepository.CreateLogAsync(new MicroSyncLog
                {
                    EntityType = "Price",
                    Direction = DIRECTION_FROM_ERP,
                    ExternalId = price.Sku,
                    InternalId = product.Id.ToString(),
                    Status = "Success",
                    Message = $"Fiyat güncellendi: {oldPrice:C} → {newPrice:C}",
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);

                _logger.LogDebug(
                    "[FiyatSyncService] Fiyat güncellendi. SKU: {Sku}, " +
                    "Önceki: {Old:C}, Yeni: {New:C}",
                    price.Sku, oldPrice, newPrice);
            }

            return SyncResult.Ok(1);
        }

        /// <summary>
        /// Ürün fiyatını Mikro'ya retry mekanizması ile gönderir.
        /// </summary>
        private async Task<SyncResult> PushPriceWithRetryAsync(
            Product product,
            MicroPriceDto priceDto,
            CancellationToken cancellationToken)
        {
            var syncLog = new MicroSyncLog
            {
                EntityType = "Price",
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

                    // MicroService.UpsertPricesAsync kullan
                    var success = await _microService.UpsertPricesAsync(new[] { priceDto });

                    if (success)
                    {
                        syncLog.Status = "Success";
                        syncLog.Message = $"Fiyat gönderildi: {priceDto.Price:C}";
                        await _syncRepository.CreateLogAsync(syncLog, cancellationToken);

                        _logger.LogDebug(
                            "[FiyatSyncService] Fiyat Mikro'ya gönderildi. " +
                            "SKU: {Sku}, Fiyat: {Price:C}",
                            product.SKU, priceDto.Price);

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
                        "[FiyatSyncService] Fiyat gönderimi başarısız. " +
                        "SKU: {Sku}, Deneme: {Attempt}/{Max}, Hata: {Error}",
                        product.SKU, attempt, MAX_RETRY_ATTEMPTS, ex.Message);

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
                "[FiyatSyncService] Fiyat gönderimi başarısız (max deneme). SKU: {Sku}",
                product.SKU);

            return SyncResult.Fail(new SyncError(
                "PushPrice",
                product.SKU,
                syncLog.LastError ?? "Max retry exceeded"));
        }
    }
}
