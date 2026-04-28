using System.Diagnostics;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces.Sync;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Infrastructure.Config;
using ECommerce.Infrastructure.Services.MicroServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Mikro ERP'den anlık delta değişiklik tespiti yapan arka plan servisi.
    /// 
    /// MİMARİ ROL: IHostedService olarak uygulama ömrü boyunca çalışır.
    /// Her 10 saniyede bir yalnızca SON DEĞİŞEN ürünleri Mikro DB'den çeker,
    /// MikroProductCache + Product tablolarını günceller ve
    /// IStockNotificationService üzerinden SignalR ile frontend'e bildirir.
    /// 
    /// NEDEN IHostedService (Hangfire değil):
    /// - 10sn aralık Hangfire için çok sık (overhead + dashboard gürültüsü)
    /// - Dedicated thread ile timer: daha verimli, daha az lock contention
    /// - CancellationToken ile graceful shutdown desteği
    /// 
    /// PERFORMANS ETKİSİ:
    /// - Olağan durumda 0-5 kayıt döner (yalnızca son 15sn'de değişenler)
    /// - Yoğun dönemde (toplu fiyat güncellemesi) 50-200 kayıt dönebilir
    /// - Mikro DB'de indeksli alanlara sorgu yapıldığı için &lt;500ms sürer
    /// </summary>
    public class MikroHotPollBackgroundService : BackgroundService, IMikroHotPollService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MikroHotPollBackgroundService> _logger;
        private readonly MikroSettings _settings;

        // Durum takibi — volatile çünkü timer thread'inden okunuyor
        private volatile HotPollStatus _currentStatus = new();
        private DateTime? _lastSuccessfulPollTime;
        private int _consecutiveFailureCount;

        // Polling aralığı — config'den okunabilir, varsayılan 10sn
        private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(10);

        // Ardışık hata sonrası cooldown — DB'yi boğmamak için geri çekilme
        private static readonly TimeSpan ErrorCooldownInterval = TimeSpan.FromSeconds(30);

        // İlk başlatma gecikmesi — uygulama tamamen ayağa kalkana kadar bekle
        private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(15);

        // Maksimum delta penceresi — çok geriye gitmemek için (5 dakika)
        private static readonly TimeSpan MaxDeltaWindow = TimeSpan.FromMinutes(5);

        public MikroHotPollBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<MikroSettings> settings,
            ILogger<MikroHotPollBackgroundService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ==================== IMikroHotPollService Implementation ====================

        public DateTime? LastSuccessfulPollTime => _lastSuccessfulPollTime;
        public int ConsecutiveFailureCount => _consecutiveFailureCount;

        public HotPollStatus GetStatus() => _currentStatus;

        public async Task<HotPollResult> PollDeltaChangesAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            return await ExecutePollCycleAsync(scope.ServiceProvider, cancellationToken);
        }

        // ==================== BackgroundService Lifecycle ====================

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // SQL bağlantı yoksa hiç başlama
            if (string.IsNullOrWhiteSpace(_settings.SqlConnectionString))
            {
                _logger.LogWarning(
                    "[HotPoll] SqlConnectionString yapılandırılmamış — HotPoll devre dışı. " +
                    "MikroSettings:SqlConnectionString ayarını doldurun.");
                return;
            }

            _logger.LogInformation(
                "[HotPoll] Mikro HotPoll servisi başlatılıyor. " +
                "Aralık: {Interval}sn, İlk gecikme: {Delay}sn",
                DefaultPollInterval.TotalSeconds, InitialDelay.TotalSeconds);

            // İlk gecikme — uygulama tamamen başlasın, DB bağlantıları hazır olsun
            await Task.Delay(InitialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var result = await ExecutePollCycleAsync(scope.ServiceProvider, stoppingToken);

                    if (result.Success)
                    {
                        _consecutiveFailureCount = 0;
                        _lastSuccessfulPollTime = DateTime.UtcNow;
                    }
                    else
                    {
                        _consecutiveFailureCount++;

                        if (_consecutiveFailureCount >= 3)
                        {
                            _logger.LogError(
                                "[HotPoll] 3+ ardışık hata! ConsecutiveFailures: {Count}, Son hata: {Error}",
                                _consecutiveFailureCount, result.ErrorMessage);
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Graceful shutdown — normal davranış
                    break;
                }
                catch (Exception ex)
                {
                    _consecutiveFailureCount++;
                    _logger.LogError(ex, "[HotPoll] Beklenmeyen hata oluştu.");
                }

                // Hata durumunda cooldown uygula — DB'yi boğmamak için
                var delay = _consecutiveFailureCount >= 3
                    ? ErrorCooldownInterval
                    : DefaultPollInterval;

                await Task.Delay(delay, stoppingToken);
            }

            _logger.LogInformation("[HotPoll] Mikro HotPoll servisi durduruldu.");
        }

        // ==================== Ana Polling Mantığı ====================

        /// <summary>
        /// Tek bir polling döngüsü: Delta çek → Cache güncelle → Product güncelle → SignalR bildir.
        /// 
        /// NEDEN Scoped: ECommerceDbContext per-request lifecycle — her cycle kendi scope'unu oluşturur.
        /// </summary>
        private async Task<HotPollResult> ExecutePollCycleAsync(
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            var dbService = serviceProvider.GetRequiredService<IMikroDbService>();
            var context = serviceProvider.GetRequiredService<ECommerceDbContext>();
            var notificationService = serviceProvider.GetService<IStockNotificationService>();
            var syncRepository = serviceProvider.GetRequiredService<IMikroSyncRepository>();
            var conflictCoordinator = serviceProvider.GetService<SyncConflictCoordinator>();
            var metricsService = serviceProvider.GetService<ISyncMetricsService>();

            // Delta penceresi: Son başarılı poll'dan itibaren, ama max 5dk geriye git
            var since = _lastSuccessfulPollTime ?? DateTime.UtcNow.AddMinutes(-2);
            var minSince = DateTime.UtcNow.Subtract(MaxDeltaWindow);
            if (since < minSince) since = minSince;

            // Mikro DB'den değişen ürünleri çek
            var changedProducts = await dbService.GetDeltaChangedProductsAsync(
                since,
                fiyatListesiNo: 1,
                depoNo: 0,
                cancellationToken);

            if (changedProducts.Count == 0)
            {
                // Değişiklik yok — status güncelle ve çık
                sw.Stop();
                UpdateStatus(true, 0, sw.ElapsedMilliseconds);
                return HotPollResult.Ok(0, 0, 0, sw.ElapsedMilliseconds);
            }

            _logger.LogInformation(
                "[HotPoll] {Count} ürün değişikliği tespit edildi. Since: {Since:HH:mm:ss}",
                changedProducts.Count, since);

            // Değişiklikleri cache ve product tablosuna uygula
            var (stockUpdated, priceUpdated, infoUpdated, changes) =
                await ApplyChangesToDatabaseAsync(context, changedProducts, conflictCoordinator, cancellationToken);

            // SignalR bildirimi gönder
            if (notificationService != null && changes.Count > 0)
            {
                await notificationService.NotifyBulkStockUpdateAsync(changes, "MikroHotPoll", cancellationToken);
            }

            // Sync state güncelle
            await syncRepository.UpdateSyncSuccessAsync(
                "HotPoll", "FromERP", changedProducts.Count, sw.ElapsedMilliseconds, cancellationToken);

            // Alert değerlendirmesi — eşik kontrolü ve admin bildirimi
            if (metricsService != null)
            {
                try { await metricsService.EvaluateAlertsAsync(cancellationToken); }
                catch (Exception ex) { _logger.LogWarning(ex, "[HotPoll] Alert değerlendirmesi başarısız — senkronizasyonu engellemez."); }
            }

            sw.Stop();
            UpdateStatus(true, changedProducts.Count, sw.ElapsedMilliseconds);

            _logger.LogInformation(
                "[HotPoll] Cycle tamamlandı. Stok: {Stock}, Fiyat: {Price}, Bilgi: {Info}, Süre: {Ms}ms",
                stockUpdated, priceUpdated, infoUpdated, sw.ElapsedMilliseconds);

            return HotPollResult.Ok(stockUpdated, priceUpdated, infoUpdated, sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Mikro'dan gelen delta verileri MikroProductCache + Product tablolarına yazar.
        /// 
        /// NEDEN TEK TRANSACTION DEĞİL: Cache ve Product tabloları farklı scope'larda
        /// güncellenebilir. Partial failure kabul edilir — sonraki cycle düzeltir.
        /// Stok tutarlılığı zaten conflict resolver ile sağlanıyor.
        /// </summary>
        private async Task<(int stockUpdated, int priceUpdated, int infoUpdated, List<ProductChangeEvent> changes)>
            ApplyChangesToDatabaseAsync(
                ECommerceDbContext context,
                List<MikroUnifiedProductDto> changedProducts,
                SyncConflictCoordinator? conflictCoordinator,
                CancellationToken cancellationToken)
        {
            int stockUpdated = 0, priceUpdated = 0, infoUpdated = 0;
            var changes = new List<ProductChangeEvent>();

            // Mevcut cache kayıtlarını al — tek sorguda tüm ilgili SKU'ları çek
            var skuList = changedProducts.Select(p => p.StokKod).ToList();
            var existingCache = await context.Set<MikroProductCache>()
                .Where(c => skuList.Contains(c.StokKod))
                .ToDictionaryAsync(c => c.StokKod, StringComparer.OrdinalIgnoreCase, cancellationToken);

            // SKU → Product eşlemesi — cache'deki LocalProductId üzerinden
            var localProductIds = existingCache.Values
                .Where(c => c.LocalProductId.HasValue)
                .Select(c => c.LocalProductId!.Value)
                .ToList();
            var existingProducts = await context.Set<Product>()
                .Where(p => localProductIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            foreach (var mikro in changedProducts)
            {
                var changeEvent = new ProductChangeEvent { StokKod = mikro.StokKod };
                var changeType = ProductChangeType.None;

                // ── Cache güncelleme ──
                if (existingCache.TryGetValue(mikro.StokKod, out var cache))
                {
                    // Stok miktarı değişti mi?
                    if (cache.DepoMiktari != mikro.StokMiktar)
                    {
                        changeEvent.OldStockQuantity = cache.DepoMiktari;
                        changeEvent.NewStockQuantity = mikro.StokMiktar;
                        cache.DepoMiktari = mikro.StokMiktar;
                        cache.SatilabilirMiktar = mikro.StokMiktar;
                        changeType |= ProductChangeType.Stock;
                        stockUpdated++;
                    }

                    // Fiyat değişti mi?
                    if (cache.SatisFiyati != mikro.Fiyat)
                    {
                        changeEvent.OldPrice = cache.SatisFiyati;
                        changeEvent.NewPrice = mikro.Fiyat;
                        cache.SatisFiyati = mikro.Fiyat;
                        changeType |= ProductChangeType.Price;
                        priceUpdated++;
                    }

                    // İsim, barkod, birim, KDV veya GrupKod değişti mi?
                    if (!string.Equals(cache.StokAd, mikro.StokAd, StringComparison.Ordinal) ||
                        !string.Equals(cache.Barkod, mikro.Barkod, StringComparison.Ordinal) ||
                        !string.Equals(cache.Birim, mikro.Birim, StringComparison.Ordinal) ||
                        !string.Equals(cache.GrupKod, mikro.GrupKod, StringComparison.Ordinal) ||
                        cache.KdvOrani != mikro.KdvOrani)
                    {
                        changeEvent.OldName = cache.StokAd;
                        changeEvent.NewName = mikro.StokAd;
                        cache.StokAd = mikro.StokAd;
                        cache.Barkod = mikro.Barkod;
                        cache.Birim = mikro.Birim;
                        cache.GrupKod = mikro.GrupKod;
                        cache.KdvOrani = mikro.KdvOrani;
                        changeType |= ProductChangeType.Info;
                        infoUpdated++;
                    }

                    if (changeType != ProductChangeType.None)
                    {
                        cache.GuncellemeTarihi = DateTime.UtcNow;
                        cache.SonHareketTarihi = mikro.SonHareketTarihi;
                        cache.SyncStatus = (int)MikroSyncStatus.PendingUpdate;
                    }

                    // ── Product tablosu güncelleme ──
                    if (cache.LocalProductId.HasValue &&
                        existingProducts.TryGetValue(cache.LocalProductId.Value, out var product))
                    {
                        changeEvent.LocalProductId = product.Id;

                        if (changeType.HasFlag(ProductChangeType.Stock))
                        {
                            // SyncConflictCoordinator varsa çatışma çöz, yoksa Mikro kazanır
                            if (conflictCoordinator != null)
                            {
                                var stockResult = conflictCoordinator.ResolveStockConflict(
                                    mikro.StokKod,
                                    mikroValue: mikro.StokMiktar,
                                    ecommerceValue: product.StockQuantity,
                                    mikroLastUpdate: mikro.SonHareketTarihi,
                                    ecommerceLastUpdate: cache.GuncellemeTarihi);
                                product.StockQuantity = (int)stockResult.ResolvedValue;
                            }
                            else
                            {
                                product.StockQuantity = (int)mikro.StokMiktar;
                            }
                        }

                        if (changeType.HasFlag(ProductChangeType.Price))
                        {
                            // Fiyatta ERP-Wins: Mikro her zaman master — conflict resolver varsa log tutar
                            if (conflictCoordinator != null)
                            {
                                var priceResult = conflictCoordinator.ResolvePriceConflict(
                                    mikro.StokKod,
                                    mikroPrice: mikro.Fiyat,
                                    ecommercePrice: product.Price,
                                    isAdminOverride: false);
                                product.Price = priceResult.ResolvedPrice;
                            }
                            else
                            {
                                product.Price = mikro.Fiyat;
                            }
                        }

                        if (changeType.HasFlag(ProductChangeType.Info))
                        {
                            // İsim güncellemesi + slug regenerasyonu
                            if (!string.IsNullOrWhiteSpace(mikro.StokAd))
                            {
                                product.Name = mikro.StokAd;
                                product.Slug = GenerateSlug(mikro.StokAd);
                            }

                            // Birim → WeightUnit eşlemesi (ağırlık bazlı satış kontrolü)
                            if (!string.IsNullOrWhiteSpace(mikro.Birim))
                            {
                                var newUnit = MapBirimToWeightUnit(mikro.Birim);
                                if (product.WeightUnit != newUnit)
                                {
                                    product.WeightUnit = newUnit;
                                    product.IsWeightBased = IsWeightBasedUnit(newUnit);
                                }
                            }

                            product.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                if (changeType != ProductChangeType.None)
                {
                    changeEvent.ChangeType = changeType;
                    changes.Add(changeEvent);
                }
            }

            // Tüm değişiklikleri tek SaveChanges ile yaz — batch performansı
            if (changes.Count > 0)
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            return (stockUpdated, priceUpdated, infoUpdated, changes);
        }

        private void UpdateStatus(bool success, int changedCount, long durationMs, string? error = null)
        {
            _currentStatus = new HotPollStatus
            {
                IsRunning = true,
                LastPollTime = DateTime.UtcNow,
                LastSuccessTime = success ? DateTime.UtcNow : _currentStatus.LastSuccessTime,
                ConsecutiveFailures = success ? 0 : _consecutiveFailureCount,
                LastPollDurationMs = durationMs,
                LastPollChangedCount = changedCount,
                LastError = error
            };
        }

        // ==================== Birim / Slug Yardımcıları ====================

        // Birim eşleme tablosu — ProductInfoSyncService + MikroStokMapper ile tutarlı
        private static readonly Dictionary<string, Entities.Enums.WeightUnit> _birimMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["ADET"] = Entities.Enums.WeightUnit.Piece,
                ["AD"] = Entities.Enums.WeightUnit.Piece,
                ["KG"] = Entities.Enums.WeightUnit.Kilogram,
                ["KILOGRAM"] = Entities.Enums.WeightUnit.Kilogram,
                ["KİLOGRAM"] = Entities.Enums.WeightUnit.Kilogram,
                ["GR"] = Entities.Enums.WeightUnit.Gram,
                ["GRAM"] = Entities.Enums.WeightUnit.Gram,
                ["LT"] = Entities.Enums.WeightUnit.Liter,
                ["LİTRE"] = Entities.Enums.WeightUnit.Liter,
                ["LITRE"] = Entities.Enums.WeightUnit.Liter,
                ["ML"] = Entities.Enums.WeightUnit.Milliliter,
                ["MİLİLİTRE"] = Entities.Enums.WeightUnit.Milliliter,
                ["PAKET"] = Entities.Enums.WeightUnit.Piece,
                ["KUTU"] = Entities.Enums.WeightUnit.Piece,
                ["ŞİŞE"] = Entities.Enums.WeightUnit.Piece,
                ["SISE"] = Entities.Enums.WeightUnit.Piece,
                ["DEMET"] = Entities.Enums.WeightUnit.Piece
            };

        private static Entities.Enums.WeightUnit MapBirimToWeightUnit(string birim)
            => _birimMap.TryGetValue(birim.Trim(), out var unit) ? unit : Entities.Enums.WeightUnit.Piece;

        private static bool IsWeightBasedUnit(Entities.Enums.WeightUnit unit)
            => unit is Entities.Enums.WeightUnit.Kilogram
                   or Entities.Enums.WeightUnit.Gram
                   or Entities.Enums.WeightUnit.Liter
                   or Entities.Enums.WeightUnit.Milliliter;

        /// <summary>
        /// URL-dostu slug oluşturur — Türkçe karakter desteği ile.
        /// </summary>
        private static string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            var slug = name.Trim().ToLowerInvariant()
                .Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
                .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c")
                .Replace("İ", "i").Replace("Ğ", "g").Replace("Ü", "u")
                .Replace("Ş", "s").Replace("Ö", "o").Replace("Ç", "c");

            var chars = new char[slug.Length];
            for (int i = 0; i < slug.Length; i++)
                chars[i] = char.IsLetterOrDigit(slug[i]) ? slug[i] : '-';

            slug = new string(chars);
            while (slug.Contains("--")) slug = slug.Replace("--", "-");
            return slug.Trim('-');
        }
    }
}
