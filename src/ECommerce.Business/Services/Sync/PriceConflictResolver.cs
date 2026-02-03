using ECommerce.Core.Interfaces.Sync;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Fiyat verileri için çakışma çözümleyici.
    /// 
    /// STRATEJİ: ERP-Wins (Mikro Her Zaman Kazanır)
    /// 
    /// NEDEN: Fiyatlar Mikro ERP'de merkezi olarak yönetilir.
    /// Muhasebe, maliyet hesapları, kampanyalar hep orada.
    /// E-Ticaret sadece gösterim katmanıdır.
    /// 
    /// ÖZEL DURUMLAR:
    /// - Kampanya fiyatı varsa: E-Ticaret'teki kampanya korunur
    /// - Fark %20'den fazlaysa: Log'a uyarı yaz (olası hata)
    /// - KDV dahil/hariç farkı: Dönüşüm hatası olabilir
    /// </summary>
    public class PriceConflictResolver : IConflictResolver
    {
        private readonly ILogger<PriceConflictResolver> _logger;
        
        // Uyarı eşiği: Bu orandan fazla fark varsa dikkat et
        private const decimal WarningThresholdPercent = 20;
        
        // Kampanya korunma süresi: Bu kadar süre içinde kampanya varsa ERP geçersiz kılınır
        private const int CampaignProtectionHours = 48;

        public string SupportedEntityType => "Fiyat";

        public PriceConflictResolver(ILogger<PriceConflictResolver> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<ConflictResolutionResult<T>> ResolveAsync<T>(ConflictContext<T> context) 
            where T : class
        {
            _logger.LogDebug(
                "[PriceConflictResolver] Fiyat çakışma kontrolü: {Identifier}, Yön: {Direction}",
                context.Identifier, context.Direction);

            // Hedef değer yoksa çakışma yok - yeni kayıt
            if (context.TargetValue == null)
            {
                _logger.LogDebug(
                    "[PriceConflictResolver] Hedef fiyat yok, yeni kayıt: {Identifier}",
                    context.Identifier);

                return Task.FromResult(ConflictResolutionResult<T>.NoConflict(context.SourceValue!));
            }

            // Kaynak değer yoksa hedef korunur
            if (context.SourceValue == null)
            {
                _logger.LogWarning(
                    "[PriceConflictResolver] Kaynak fiyat null, hedef korunuyor: {Identifier}",
                    context.Identifier);

                return Task.FromResult(ConflictResolutionResult<T>.TargetWins(
                    context.TargetValue,
                    "Mikro'dan fiyat gelmedi, mevcut fiyat korunuyor"));
            }

            // Fiyat verilerini karşılaştır
            var conflicts = CompareValues(context);

            // Çakışma yoksa
            if (!conflicts.Any())
            {
                return Task.FromResult(new ConflictResolutionResult<T>
                {
                    HadConflict = false,
                    WinningValue = context.SourceValue,
                    Winner = ConflictWinner.Source,
                    Strategy = "NoChange",
                    Reason = "Fiyatlar zaten eşit"
                });
            }

            // Kampanya kontrolü
            if (HasActiveCampaign(context))
            {
                _logger.LogInformation(
                    "[PriceConflictResolver] Aktif kampanya var, E-Ticaret fiyatı korunuyor: {Identifier}",
                    context.Identifier);

                return Task.FromResult(ConflictResolutionResult<T>.TargetWins(
                    context.TargetValue,
                    "Aktif kampanya fiyatı korunuyor",
                    conflicts));
            }

            // ERP-Wins stratejisi: Mikro her zaman kazanır
            var result = ConflictResolutionResult<T>.SourceWins(
                context.SourceValue,
                "Mikro ERP fiyatı uygulandı (ERP-Wins stratejisi)",
                conflicts);

            // Büyük fark uyarısı
            CheckForLargeDifference(context, conflicts);

            _logger.LogInformation(
                "[PriceConflictResolver] ERP-Wins: Mikro fiyatı uygulanacak: {Identifier}",
                context.Identifier);

            return Task.FromResult(result);
        }

        /// <summary>
        /// Fiyat değerlerini karşılaştırır.
        /// </summary>
        private List<FieldConflict> CompareValues<T>(ConflictContext<T> context) where T : class
        {
            var conflicts = new List<FieldConflict>();

            if (context.SourceValue is PriceConflictData sourcePrice && 
                context.TargetValue is PriceConflictData targetPrice)
            {
                // Liste fiyatı karşılaştırması
                if (sourcePrice.ListPrice != targetPrice.ListPrice)
                {
                    var diff = sourcePrice.ListPrice - targetPrice.ListPrice;
                    var percentDiff = targetPrice.ListPrice != 0 
                        ? Math.Abs(diff / targetPrice.ListPrice * 100) 
                        : 100;

                    conflicts.Add(new FieldConflict
                    {
                        FieldName = "ListPrice",
                        SourceValue = sourcePrice.ListPrice.ToString("C2"),
                        TargetValue = targetPrice.ListPrice.ToString("C2"),
                        Difference = diff,
                        PercentDifference = percentDiff
                    });
                }

                // Satış fiyatı karşılaştırması (KDV dahil)
                if (sourcePrice.SalePrice != targetPrice.SalePrice)
                {
                    var diff = sourcePrice.SalePrice - targetPrice.SalePrice;
                    var percentDiff = targetPrice.SalePrice != 0 
                        ? Math.Abs(diff / targetPrice.SalePrice * 100) 
                        : 100;

                    conflicts.Add(new FieldConflict
                    {
                        FieldName = "SalePrice",
                        SourceValue = sourcePrice.SalePrice.ToString("C2"),
                        TargetValue = targetPrice.SalePrice.ToString("C2"),
                        Difference = diff,
                        PercentDifference = percentDiff
                    });
                }

                // KDV oranı karşılaştırması
                if (sourcePrice.VatRate != targetPrice.VatRate)
                {
                    conflicts.Add(new FieldConflict
                    {
                        FieldName = "VatRate",
                        SourceValue = $"%{sourcePrice.VatRate}",
                        TargetValue = $"%{targetPrice.VatRate}",
                        Difference = sourcePrice.VatRate - targetPrice.VatRate
                    });
                }
            }
            else
            {
                // Generic karşılaştırma
                var sourceStr = context.SourceValue?.ToString() ?? "";
                var targetStr = context.TargetValue?.ToString() ?? "";

                if (sourceStr != targetStr)
                {
                    conflicts.Add(new FieldConflict
                    {
                        FieldName = "Price",
                        SourceValue = sourceStr,
                        TargetValue = targetStr
                    });
                }
            }

            return conflicts;
        }

        /// <summary>
        /// Aktif kampanya var mı kontrol eder.
        /// </summary>
        private bool HasActiveCampaign<T>(ConflictContext<T> context) where T : class
        {
            // Metadata'dan kampanya bilgisi kontrol et
            if (context.Metadata.TryGetValue("HasActiveCampaign", out var hasCampaign))
            {
                return hasCampaign is bool b && b;
            }

            if (context.Metadata.TryGetValue("CampaignEndDate", out var endDateObj))
            {
                if (endDateObj is DateTime endDate && endDate > DateTime.UtcNow)
                {
                    return true;
                }
            }

            // PriceConflictData üzerinden kontrol
            if (context.TargetValue is PriceConflictData targetPrice)
            {
                return targetPrice.HasActiveCampaign;
            }

            return false;
        }

        /// <summary>
        /// Büyük fark durumunda uyarı loglar.
        /// </summary>
        private void CheckForLargeDifference<T>(
            ConflictContext<T> context, 
            List<FieldConflict> conflicts) where T : class
        {
            var largeConflicts = conflicts
                .Where(c => c.PercentDifference.HasValue && c.PercentDifference > WarningThresholdPercent)
                .ToList();

            if (largeConflicts.Any())
            {
                _logger.LogWarning(
                    "[PriceConflictResolver] ⚠️ Büyük fiyat farkı tespit edildi: {Identifier}. " +
                    "Bu bir veri hatası olabilir! Farklar: {Conflicts}",
                    context.Identifier,
                    string.Join(", ", largeConflicts.Select(c => 
                        $"{c.FieldName}: {c.SourceValue} vs {c.TargetValue} (%{c.PercentDifference:F1} fark)")));
            }
        }
    }

    /// <summary>
    /// Fiyat çakışma verisi wrapper'ı.
    /// </summary>
    public class PriceConflictData
    {
        public string SKU { get; set; } = string.Empty;
        public decimal ListPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal VatRate { get; set; }
        public bool HasActiveCampaign { get; set; }
        public DateTime? CampaignEndDate { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
