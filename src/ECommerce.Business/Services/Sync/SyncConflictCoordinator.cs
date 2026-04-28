using ECommerce.Core.Interfaces.Sync;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Mikro ↔ ECommerce senkronizasyonunda çakışma çözümleme koordinatörü.
    /// 
    /// NEDEN: StockConflictResolver ve PriceConflictResolver farklı stratejiler kullanır.
    /// Bu koordinatör entity tipine göre doğru resolver'ı seçer ve sonucu döner.
    /// HotPoll ve OutboundSync servisleri bu koordinatörü kullanarak tutarlı 
    /// çakışma yönetimi sağlar.
    /// 
    /// STRATEJİ MATRİSİ:
    /// ┌──────────┬─────────────────────────────────────────────────────────┐
    /// │ Senaryo  │ Kural                                                   │
    /// ├──────────┼─────────────────────────────────────────────────────────┤
    /// │ Stok     │ Mikro değişti + EC değişmedi → Mikro kazanır           │
    /// │ Stok     │ EC sipariş düştü → EC'nin delta'sı geçerli, push       │
    /// │ Stok     │ Her ikisi değişti → min(Mikro, EC) muhafazakar         │
    /// │ Fiyat    │ Mikro değişti → Mikro master (ERP-Wins)                │
    /// │ Fiyat    │ Admin değişti → Admin push, sonraki çekimde senkron    │
    /// │ Ürün adı │ Mikro değişti → Mikro master                          │
    /// │ Ürün adı │ Admin değişti → Admin push eder                       │
    /// └──────────┴─────────────────────────────────────────────────────────┘
    /// </summary>
    public class SyncConflictCoordinator
    {
        private readonly ILogger<SyncConflictCoordinator> _logger;
        private readonly ISyncLogger _syncLogger;

        public SyncConflictCoordinator(
            ILogger<SyncConflictCoordinator> logger,
            ISyncLogger syncLogger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _syncLogger = syncLogger ?? throw new ArgumentNullException(nameof(syncLogger));
        }

        /// <summary>
        /// Stok çakışmasını çözer.
        /// 
        /// KURAL: Her iki taraf da değiştiyse muhafazakar yaklaşım (min) kullanılır.
        /// Böylece aşırı satış riski ortadan kalkar. Müşteri ürün göremez ama
        /// "olmayan ürünü sattık" durumu oluşmaz.
        /// </summary>
        public StockConflictResult ResolveStockConflict(
            string stokKod,
            decimal mikroValue,
            decimal ecommerceValue,
            DateTime? mikroLastUpdate,
            DateTime? ecommerceLastUpdate)
        {
            // Değerler aynıysa çakışma yok
            if (mikroValue == ecommerceValue)
            {
                return new StockConflictResult
                {
                    HasConflict = false,
                    ResolvedValue = mikroValue,
                    Strategy = "NoConflict",
                    Reason = "Stok miktarları zaten eşit"
                };
            }

            // Tek taraf değiştiyse o tarafın değeri geçerli
            if (ecommerceLastUpdate == null || mikroLastUpdate > ecommerceLastUpdate)
            {
                // Mikro daha güncel — Mikro kazanır
                _logger.LogDebug(
                    "[ConflictCoordinator] Stok: Mikro kazanır. SKU: {Sku}, Mikro: {M}, EC: {E}",
                    stokKod, mikroValue, ecommerceValue);

                return new StockConflictResult
                {
                    HasConflict = true,
                    ResolvedValue = mikroValue,
                    Strategy = "MikroWins",
                    Reason = "Mikro'daki güncelleme daha yeni"
                };
            }

            if (mikroLastUpdate == null || ecommerceLastUpdate > mikroLastUpdate)
            {
                // ECommerce daha güncel — ECommerce kazanır
                _logger.LogDebug(
                    "[ConflictCoordinator] Stok: EC kazanır. SKU: {Sku}, Mikro: {M}, EC: {E}",
                    stokKod, mikroValue, ecommerceValue);

                return new StockConflictResult
                {
                    HasConflict = true,
                    ResolvedValue = ecommerceValue,
                    Strategy = "ECommerceWins",
                    Reason = "E-Ticaret'teki güncelleme daha yeni"
                };
            }

            // Her iki taraf da aynı zaman aralığında değişti — muhafazakar yaklaşım
            var conservativeValue = Math.Min(mikroValue, ecommerceValue);

            _logger.LogWarning(
                "[ConflictCoordinator] Stok çakışması! SKU: {Sku}, Mikro: {M}, EC: {E} → min={Min}",
                stokKod, mikroValue, ecommerceValue, conservativeValue);

            // Çakışmayı logla — admin panelde görülebilir
            _ = _syncLogger.StartOperationAsync(
                "StokConflict", "Conflict",
                stokKod, null,
                $"Stok çakışması: Mikro={mikroValue}, EC={ecommerceValue}, Çözüm=min({conservativeValue})");

            return new StockConflictResult
            {
                HasConflict = true,
                ResolvedValue = conservativeValue,
                Strategy = "Conservative_Min",
                Reason = $"Her iki taraf da değişti — aşırı satış engeli: min(Mikro:{mikroValue}, EC:{ecommerceValue})"
            };
        }

        /// <summary>
        /// Fiyat çakışmasını çözer.
        /// 
        /// KURAL: Mikro her zaman master (ERP-Wins).
        /// ÖZEL DURUM: Admin panelden bilinçli fiyat değişikliği yapıldıysa
        /// admin değeri korunur ve Mikro'ya push edilir.
        /// </summary>
        public PriceConflictResult ResolvePriceConflict(
            string stokKod,
            decimal mikroPrice,
            decimal ecommercePrice,
            bool isAdminOverride = false)
        {
            // Fiyatlar aynıysa çakışma yok
            if (mikroPrice == ecommercePrice)
            {
                return new PriceConflictResult
                {
                    HasConflict = false,
                    ResolvedPrice = mikroPrice,
                    Strategy = "NoConflict",
                    ShouldPushToMikro = false
                };
            }

            // Admin bilinçli override yaptıysa — admin kazanır ve Mikro'ya push gerekir
            if (isAdminOverride)
            {
                _logger.LogInformation(
                    "[ConflictCoordinator] Fiyat: Admin override. SKU: {Sku}, Admin: {A}, Mikro: {M}",
                    stokKod, ecommercePrice, mikroPrice);

                return new PriceConflictResult
                {
                    HasConflict = true,
                    ResolvedPrice = ecommercePrice,
                    Strategy = "AdminOverride",
                    ShouldPushToMikro = true,
                    Reason = "Admin fiyat değişikliği — Mikro'ya push gerekiyor"
                };
            }

            // Normal akış — ERP-Wins
            _logger.LogDebug(
                "[ConflictCoordinator] Fiyat: ERP-Wins. SKU: {Sku}, Mikro: {M} kazandı, EC: {E}",
                stokKod, mikroPrice, ecommercePrice);

            return new PriceConflictResult
            {
                HasConflict = true,
                ResolvedPrice = mikroPrice,
                Strategy = "ERP_Wins",
                ShouldPushToMikro = false,
                Reason = "Mikro ERP fiyatı master kabul edildi"
            };
        }

        /// <summary>
        /// Ürün bilgi çakışmasını çözer (ad, birim, barkod).
        /// 
        /// KURAL: Mikro master. Admin değişikliği push edilir.
        /// </summary>
        public InfoConflictResult ResolveInfoConflict(
            string stokKod,
            string? mikroName,
            string? ecommerceName,
            bool isAdminOverride = false)
        {
            if (string.Equals(mikroName, ecommerceName, StringComparison.Ordinal))
            {
                return new InfoConflictResult
                {
                    HasConflict = false,
                    ResolvedName = mikroName ?? ecommerceName ?? string.Empty,
                    Strategy = "NoConflict",
                    ShouldPushToMikro = false
                };
            }

            if (isAdminOverride)
            {
                return new InfoConflictResult
                {
                    HasConflict = true,
                    ResolvedName = ecommerceName ?? mikroName ?? string.Empty,
                    Strategy = "AdminOverride",
                    ShouldPushToMikro = true,
                    Reason = "Admin ürün adı değişikliği — Mikro'ya push gerekiyor"
                };
            }

            return new InfoConflictResult
            {
                HasConflict = true,
                ResolvedName = mikroName ?? ecommerceName ?? string.Empty,
                Strategy = "ERP_Wins",
                ShouldPushToMikro = false,
                Reason = "Mikro ERP ürün adı master kabul edildi"
            };
        }
    }

    // ==================== Sonuç Sınıfları ====================

    public class StockConflictResult
    {
        public bool HasConflict { get; set; }
        public decimal ResolvedValue { get; set; }
        public string Strategy { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    public class PriceConflictResult
    {
        public bool HasConflict { get; set; }
        public decimal ResolvedPrice { get; set; }
        public string Strategy { get; set; } = string.Empty;
        public string? Reason { get; set; }

        /// <summary>
        /// Admin override durumunda true — OutboundSync Mikro'ya push etmeli.
        /// </summary>
        public bool ShouldPushToMikro { get; set; }
    }

    public class InfoConflictResult
    {
        public bool HasConflict { get; set; }
        public string ResolvedName { get; set; } = string.Empty;
        public string Strategy { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public bool ShouldPushToMikro { get; set; }
    }
}
