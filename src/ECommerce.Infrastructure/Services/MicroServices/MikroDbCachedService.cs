using System.Collections.Concurrent;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Infrastructure.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services.MicroServices
{
    /// <summary>
    /// MikroDbService üzerine In-Memory cache katmanı + VPN bağlantı serileştirmesi.
    ///
    /// NEDEN: GetUnifiedProductsAsync her HTTP isteğinde çağrılıyor (7+ endpoint).
    /// Her çağrı VPN üzerinden SQL bağlantısı açıyor → connection pool tükeniyor.
    /// Bu servis sonuçları bellekte tutar, sadece cache süresi dolduğunda SQL'e gider.
    ///
    /// VPN KISITI: VPN tüneli eşzamanlı birden fazla SQL login'i kaldıramıyor.
    /// Error 10060 (login timeout) bunu kanıtlıyor. Tüm SQL operasyonları
    /// _sqlGate semaforu ile seri hale getirilir — aynı anda sadece 1 bağlantı.
    ///
    /// TASARIM KARARLARI:
    /// - Decorator Pattern: IMikroDbService interface'ini wrap eder, DI'da inner service yerine geçer.
    /// - _refreshLock: Cache stampede koruması (aynı anda 1 cache yenileme).
    /// - _sqlGate: VPN bağlantı serileştirmesi (aynı anda 1 SQL bağlantısı).
    /// - ConcurrentDictionary ile thread-safe SKU lookup desteği.
    /// - Cache süresi 2dk — HotPoll 10sn'de delta güncelleme yapıyor.
    /// - IServiceScopeFactory ile scoped inner service'e singleton'dan güvenli erişim.
    /// </summary>
    public class MikroDbCachedService : IMikroDbService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly MikroSettings _settings;
        private readonly ILogger<MikroDbCachedService> _logger;

        // ── Cache durumu (volatile: multi-thread görünürlük) ──
        private volatile List<MikroUnifiedProductDto>? _cachedProducts;
        private volatile ConcurrentDictionary<string, MikroUnifiedProductDto>? _cachedProductsBySku;
        private DateTime _cacheExpiry = DateTime.MinValue;

        // ── Stampede koruması: aynı anda sadece 1 cache yenileme SQL sorgusu ──
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        // ── VPN bağlantı serileştirmesi: aynı anda sadece 1 SQL bağlantısı ──
        // NEDEN: VPN tüneli eşzamanlı birden fazla SQL login'i kaldıramıyor (Error 10060).
        // Birleşik sorgu + HotPoll delta sorgusu aynı anda çalıştığında ikincisi timeout alıyor.
        // Bu semafor TÜM SQL operasyonlarını seri hale getirir.
        private static readonly SemaphoreSlim _sqlGate = new(1, 1);

        // ── Cache süresi: 2 dakika — HotPoll 10sn'de delta güncelleme yapıyor ──
        // NEDEN: Çok kısa tutarsak VPN'e sık bağlanır, çok uzun tutarsak veri bayatlar.
        // 2dk optimal: kullanıcı sayfayı yenilese bile cache'den döner.
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

        // ── SQL gate bekleme süresi: 60sn — VPN üzerinden sorgu tamamlanana kadar ──
        private static readonly TimeSpan SqlGateTimeout = TimeSpan.FromSeconds(60);

        public MikroDbCachedService(
            IServiceScopeFactory scopeFactory,
            IOptions<MikroSettings> settings,
            ILogger<MikroDbCachedService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_settings.SqlConnectionString);

        // ==================== BİRLEŞİK ÜRÜN SORGUSU (CACHED) ====================

        /// <summary>
        /// Birleşik ürün sorgusunu cache'den döner. Cache süresi dolmuşsa SQL'den yeniler.
        ///
        /// PERFORMANS ETKİSİ:
        /// - Cache hit: ~0ms (bellekten okuma, SQL gate almaz)
        /// - Cache miss: ~1-3s (SQL sorgusu, VPN üzerinden, SQL gate korumalı)
        /// - Stampede: 100 eşzamanlı istek → sadece 1 SQL sorgusu, 99'u bekler
        /// </summary>
        public async Task<List<MikroUnifiedProductDto>> GetUnifiedProductsAsync(
            int? fiyatListesiNo = null,
            int? depoNo = null,
            CancellationToken cancellationToken = default)
        {
            // Hızlı yol: cache geçerliyse doğrudan dön (lock almadan, SQL gate almadan)
            if (_cachedProducts != null && DateTime.UtcNow < _cacheExpiry)
            {
                return _cachedProducts;
            }

            // Yavaş yol: cache'i yenile (stampede korumalı)
            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                // Double-check: başka thread yenilemişse tekrar SQL'e gitme
                if (_cachedProducts != null && DateTime.UtcNow < _cacheExpiry)
                {
                    return _cachedProducts;
                }

                _logger.LogInformation(
                    "[MikroDbCachedService] Cache süresi doldu, SQL'den yenileniyor...");

                // VPN bağlantı serileştirmesi — başka SQL operasyonunun bitmesini bekle
                var acquired = await _sqlGate.WaitAsync(SqlGateTimeout, cancellationToken);
                if (!acquired)
                {
                    _logger.LogWarning(
                        "[MikroDbCachedService] SQL gate timeout (60sn). " +
                        "Başka SQL operasyonu hâlâ çalışıyor. Eski cache kullanılıyor.");
                    if (_cachedProducts != null) return _cachedProducts;
                    return new List<MikroUnifiedProductDto>();
                }

                try
                {
                    // NEDEN: Singleton servis scoped servise (MikroDbService) doğrudan bağlanamaz.
                    // Her cache miss'te yeni scope oluştur → yeni SqlConnection al → sorguyu çalıştır.
                    using var scope = _scopeFactory.CreateScope();
                    var innerService = scope.ServiceProvider.GetRequiredService<MikroDbService>();

                    var products = await innerService.GetUnifiedProductsAsync(
                        fiyatListesiNo, depoNo, cancellationToken);

                    if (products.Count > 0)
                    {
                        // SKU bazlı hızlı lookup dictionary'si oluştur
                        var skuMap = new ConcurrentDictionary<string, MikroUnifiedProductDto>(
                            StringComparer.OrdinalIgnoreCase);

                        foreach (var p in products)
                        {
                            if (!string.IsNullOrWhiteSpace(p.StokKod))
                                skuMap[p.StokKod] = p;
                        }

                        _cachedProductsBySku = skuMap;
                        _cachedProducts = products;
                        _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

                        _logger.LogInformation(
                            "[MikroDbCachedService] Cache yenilendi. Ürün: {Count}, " +
                            "Sonraki yenileme: {Expiry:HH:mm:ss}",
                            products.Count, _cacheExpiry.ToLocalTime());
                    }
                    else
                    {
                        // SQL boş döndü — eski cache varsa onu kullan (stale > empty)
                        if (_cachedProducts != null && _cachedProducts.Count > 0)
                        {
                            _logger.LogWarning(
                                "[MikroDbCachedService] SQL boş döndü ama eski cache mevcut " +
                                "({Count} ürün). Eski cache kullanılıyor (30sn uzatıldı).",
                                _cachedProducts.Count);
                            _cacheExpiry = DateTime.UtcNow.AddSeconds(30);
                            return _cachedProducts;
                        }

                        _logger.LogWarning(
                            "[MikroDbCachedService] SQL boş döndü ve eski cache yok. Boş liste dönüyor.");
                    }

                    return products;
                }
                finally
                {
                    _sqlGate.Release();
                }
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        // ==================== SKU BAZLI TEK ÜRÜN LOOKUP (CACHE'DEN) ====================

        /// <summary>
        /// Cache'den SKU ile tek ürün arar. Cache yoksa önce yeniler.
        ///
        /// NEDEN: ProductsController.GetProduct({id}) TÜM ürünleri çekip FirstOrDefault yapıyordu.
        /// Bu metod O(1) dictionary lookup ile aynı işi yapar — SQL'e bile gitmeden.
        /// </summary>
        public async Task<MikroUnifiedProductDto?> GetProductBySkuAsync(
            string sku,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return null;

            // Cache'i hazırla (yoksa veya süre dolduysa SQL'den çeker)
            await GetUnifiedProductsAsync(null, null, cancellationToken);

            // Dictionary lookup: O(1)
            if (_cachedProductsBySku != null &&
                _cachedProductsBySku.TryGetValue(sku.Trim(), out var product))
            {
                return product;
            }

            return null;
        }

        // ==================== CACHE'LENMEMİŞ METODLAR (SQL GATE KORUMALI) ====================
        // NEDEN: Bu metodlar seyrek çağrılır ve/veya parametrik olduğu için cache'lenmez.
        // Her biri kendi scope'unu oluşturur — singleton'dan scoped servise güvenli erişim.
        // _sqlGate ile VPN üzerinden eşzamanlı bağlantı engellenir.

        /// <inheritdoc/>
        public async Task<List<MikroFiyatSatirDto>> GetFiyatSatirlariAsync(
            int? fiyatListesiNo = null,
            CancellationToken cancellationToken = default)
        {
            await _sqlGate.WaitAsync(SqlGateTimeout, cancellationToken);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var inner = scope.ServiceProvider.GetRequiredService<MikroDbService>();
                return await inner.GetFiyatSatirlariAsync(fiyatListesiNo, cancellationToken);
            }
            finally { _sqlGate.Release(); }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, decimal>> GetStokMiktarlariAsync(
            int? depoNo = null,
            CancellationToken cancellationToken = default)
        {
            await _sqlGate.WaitAsync(SqlGateTimeout, cancellationToken);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var inner = scope.ServiceProvider.GetRequiredService<MikroDbService>();
                return await inner.GetStokMiktarlariAsync(depoNo, cancellationToken);
            }
            finally { _sqlGate.Release(); }
        }

        /// <inheritdoc/>
        public async Task<List<MikroUnifiedProductDto>> GetDeltaChangedProductsAsync(
            DateTime since,
            int? fiyatListesiNo = null,
            int? depoNo = null,
            CancellationToken cancellationToken = default)
        {
            // VPN bağlantı serileştirmesi — birleşik sorgu çalışıyorsa bekle
            var acquired = await _sqlGate.WaitAsync(SqlGateTimeout, cancellationToken);
            if (!acquired)
            {
                _logger.LogWarning(
                    "[MikroDbCachedService] Delta sorgusu SQL gate timeout — atlanıyor. " +
                    "Sonraki HotPoll cycle'da tekrar denenecek.");
                return new List<MikroUnifiedProductDto>();
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var inner = scope.ServiceProvider.GetRequiredService<MikroDbService>();
                return await inner.GetDeltaChangedProductsAsync(since, fiyatListesiNo, depoNo, cancellationToken);
            }
            finally { _sqlGate.Release(); }
        }

        /// <inheritdoc/>
        public async Task<int> GetWebProductCountAsync(CancellationToken cancellationToken = default)
        {
            await _sqlGate.WaitAsync(SqlGateTimeout, cancellationToken);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var inner = scope.ServiceProvider.GetRequiredService<MikroDbService>();
                return await inner.GetWebProductCountAsync(cancellationToken);
            }
            finally { _sqlGate.Release(); }
        }

        /// <inheritdoc/>
        public async Task<(int Deleted, int Inserted, int Updated)> PrepareWebPriceListAsync(
            int hedefListeNo = 11,
            int kaynakListeNo = 1,
            int hedefDepoNo = 0,
            CancellationToken cancellationToken = default)
        {
            await _sqlGate.WaitAsync(SqlGateTimeout, cancellationToken);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var inner = scope.ServiceProvider.GetRequiredService<MikroDbService>();
                return await inner.PrepareWebPriceListAsync(hedefListeNo, kaynakListeNo, hedefDepoNo, cancellationToken);
            }
            finally { _sqlGate.Release(); }
        }

        // ==================== CACHE YÖNETİMİ ====================

        /// <summary>
        /// Cache'i manuel olarak geçersiz kılar.
        /// HotPoll veya sync job'ları tarafından çağrılabilir.
        /// </summary>
        public void InvalidateCache()
        {
            _cachedProducts = null;
            _cachedProductsBySku = null;
            _cacheExpiry = DateTime.MinValue;
            _logger.LogInformation("[MikroDbCachedService] Cache manuel olarak geçersiz kılındı.");
        }
    }
}
