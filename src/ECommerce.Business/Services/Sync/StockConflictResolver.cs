using ECommerce.Core.Interfaces.Sync;
using ECommerce.Entities.Concrete;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Stok verileri için çakışma çözümleyici.
    /// 
    /// STRATEJİ: Last-Write-Wins (Son Yazan Kazanır)
    /// 
    /// NEDEN: Stok miktarı sürekli değişen bir değerdir.
    /// Mağazada satış olur, online satış olur, iade olur...
    /// En güncel değer doğru değerdir.
    /// 
    /// ÖZEL DURUMLAR:
    /// - Timestamp yoksa: Kaynak kazanır (yeni veri geldi)
    /// - Değerler aynıysa: Çakışma yok
    /// - Fark %50'den fazlaysa: Log'a uyarı yaz (olası hata)
    /// </summary>
    public class StockConflictResolver : IConflictResolver
    {
        private readonly ILogger<StockConflictResolver> _logger;
        
        // Uyarı eşiği: Bu orandan fazla fark varsa dikkat et
        private const decimal WarningThresholdPercent = 50;

        public string SupportedEntityType => "Stok";

        public StockConflictResolver(ILogger<StockConflictResolver> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<ConflictResolutionResult<T>> ResolveAsync<T>(ConflictContext<T> context) 
            where T : class
        {
            // Generic tip kontrolü - bu resolver sadece decimal/int stok değerleri için
            // Ancak T generic olduğu için StockConflictData wrapper kullanıyoruz
            
            _logger.LogDebug(
                "[StockConflictResolver] Çakışma kontrolü başlıyor: {Identifier}, Yön: {Direction}",
                context.Identifier, context.Direction);

            // Hedef değer yoksa çakışma yok - yeni kayıt
            if (context.TargetValue == null)
            {
                _logger.LogDebug(
                    "[StockConflictResolver] Hedef değer yok, yeni kayıt: {Identifier}",
                    context.Identifier);

                return Task.FromResult(ConflictResolutionResult<T>.NoConflict(context.SourceValue!));
            }

            // Kaynak değer yoksa hedef kazanır (silme işlemi değilse)
            if (context.SourceValue == null)
            {
                _logger.LogWarning(
                    "[StockConflictResolver] Kaynak değer null, hedef korunuyor: {Identifier}",
                    context.Identifier);

                return Task.FromResult(ConflictResolutionResult<T>.TargetWins(
                    context.TargetValue,
                    "Kaynak değer null, mevcut değer korunuyor"));
            }

            // Değerleri karşılaştır
            var conflicts = CompareValues(context);

            // Çakışma yoksa (değerler aynı)
            if (!conflicts.Any())
            {
                _logger.LogDebug(
                    "[StockConflictResolver] Değerler aynı, çakışma yok: {Identifier}",
                    context.Identifier);

                return Task.FromResult(new ConflictResolutionResult<T>
                {
                    HadConflict = false,
                    WinningValue = context.SourceValue,
                    Winner = ConflictWinner.Source,
                    Strategy = "NoChange",
                    Reason = "Değerler zaten eşit"
                });
            }

            // Last-Write-Wins stratejisi uygula
            var result = ApplyLastWriteWins(context, conflicts);

            // Büyük fark uyarısı
            CheckForLargeDifference(context, conflicts);

            return Task.FromResult(result);
        }

        /// <summary>
        /// Değerleri karşılaştırır ve farklı alanları döndürür.
        /// </summary>
        private List<FieldConflict> CompareValues<T>(ConflictContext<T> context) where T : class
        {
            var conflicts = new List<FieldConflict>();

            // StockConflictData olarak cast et
            if (context.SourceValue is StockConflictData sourceStock && 
                context.TargetValue is StockConflictData targetStock)
            {
                // Stok miktarı karşılaştırması
                if (sourceStock.Quantity != targetStock.Quantity)
                {
                    var diff = sourceStock.Quantity - targetStock.Quantity;
                    var percentDiff = targetStock.Quantity != 0 
                        ? Math.Abs(diff / targetStock.Quantity * 100) 
                        : 100;

                    conflicts.Add(new FieldConflict
                    {
                        FieldName = "StockQuantity",
                        SourceValue = sourceStock.Quantity.ToString("F2"),
                        TargetValue = targetStock.Quantity.ToString("F2"),
                        Difference = diff,
                        PercentDifference = percentDiff
                    });
                }

                // Rezerve miktar karşılaştırması
                if (sourceStock.ReservedQuantity != targetStock.ReservedQuantity)
                {
                    conflicts.Add(new FieldConflict
                    {
                        FieldName = "ReservedQuantity",
                        SourceValue = sourceStock.ReservedQuantity.ToString("F2"),
                        TargetValue = targetStock.ReservedQuantity.ToString("F2"),
                        Difference = sourceStock.ReservedQuantity - targetStock.ReservedQuantity
                    });
                }
            }
            else
            {
                // Generic karşılaştırma - string olarak
                var sourceStr = context.SourceValue?.ToString() ?? "";
                var targetStr = context.TargetValue?.ToString() ?? "";

                if (sourceStr != targetStr)
                {
                    conflicts.Add(new FieldConflict
                    {
                        FieldName = "Value",
                        SourceValue = sourceStr,
                        TargetValue = targetStr
                    });
                }
            }

            return conflicts;
        }

        /// <summary>
        /// Last-Write-Wins stratejisini uygular.
        /// </summary>
        private ConflictResolutionResult<T> ApplyLastWriteWins<T>(
            ConflictContext<T> context, 
            List<FieldConflict> conflicts) where T : class
        {
            // Her iki timestamp de var mı?
            if (context.SourceTimestamp.HasValue && context.TargetTimestamp.HasValue)
            {
                // Son güncellenen kazanır
                if (context.SourceTimestamp.Value >= context.TargetTimestamp.Value)
                {
                    _logger.LogInformation(
                        "[StockConflictResolver] Kaynak daha güncel ({SourceTime} >= {TargetTime}): {Identifier}",
                        context.SourceTimestamp.Value, context.TargetTimestamp.Value, context.Identifier);

                    return ConflictResolutionResult<T>.SourceWins(
                        context.SourceValue!,
                        $"Kaynak daha güncel: {context.SourceTimestamp.Value:HH:mm:ss} > {context.TargetTimestamp.Value:HH:mm:ss}",
                        conflicts);
                }
                else
                {
                    _logger.LogInformation(
                        "[StockConflictResolver] Hedef daha güncel ({TargetTime} > {SourceTime}): {Identifier}",
                        context.TargetTimestamp.Value, context.SourceTimestamp.Value, context.Identifier);

                    return ConflictResolutionResult<T>.TargetWins(
                        context.TargetValue!,
                        $"Hedef daha güncel: {context.TargetTimestamp.Value:HH:mm:ss} > {context.SourceTimestamp.Value:HH:mm:ss}",
                        conflicts);
                }
            }

            // Timestamp yoksa varsayılan olarak kaynak kazanır
            // (Mikro'dan gelen veri genellikle daha günceldir)
            _logger.LogInformation(
                "[StockConflictResolver] Timestamp yok, kaynak varsayılan kazanan: {Identifier}",
                context.Identifier);

            return ConflictResolutionResult<T>.SourceWins(
                context.SourceValue!,
                "Timestamp bilgisi yok, kaynak (Mikro) varsayılan olarak kazanır",
                conflicts);
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
                    "[StockConflictResolver] ⚠️ Büyük stok farkı tespit edildi: {Identifier}. " +
                    "Farklar: {Conflicts}",
                    context.Identifier,
                    string.Join(", ", largeConflicts.Select(c => 
                        $"{c.FieldName}: {c.SourceValue} vs {c.TargetValue} (%{c.PercentDifference:F1} fark)")));
            }
        }
    }

    /// <summary>
    /// Stok çakışma verisi wrapper'ı.
    /// </summary>
    public class StockConflictData
    {
        public string SKU { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
