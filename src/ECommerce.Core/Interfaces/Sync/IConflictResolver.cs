namespace ECommerce.Core.Interfaces.Sync
{
    /// <summary>
    /// Çakışma çözümleme stratejisi tanımı.
    /// 
    /// NEDEN: Mikro ve E-Ticaret arasında aynı veri farklı değerlerde olabilir.
    /// Bu interface, hangi değerin kazanacağına karar veren strateji sözleşmesidir.
    /// 
    /// KULLANIM:
    /// - Stok: Last-Write-Wins (son güncelleyen kazanır)
    /// - Fiyat: ERP-Wins (Mikro her zaman doğru kaynak)
    /// - Sipariş: E-Ticaret-Wins (online sipariş master)
    /// </summary>
    public interface IConflictResolver
    {
        /// <summary>
        /// İki değer arasındaki çakışmayı çözer.
        /// </summary>
        /// <typeparam name="T">Karşılaştırılacak değer tipi</typeparam>
        /// <param name="context">Çakışma bağlamı</param>
        /// <returns>Çözüm sonucu</returns>
        Task<ConflictResolutionResult<T>> ResolveAsync<T>(ConflictContext<T> context)
            where T : class;

        /// <summary>
        /// Bu resolver'ın desteklediği entity tipi.
        /// </summary>
        string SupportedEntityType { get; }
    }

    /// <summary>
    /// Çakışma bağlamı - karşılaştırılacak iki değer ve metadata.
    /// </summary>
    /// <typeparam name="T">Değer tipi</typeparam>
    public class ConflictContext<T> where T : class
    {
        /// <summary>
        /// Kaynak sistemdeki değer (ör: Mikro'dan gelen).
        /// </summary>
        public T? SourceValue { get; set; }

        /// <summary>
        /// Hedef sistemdeki mevcut değer (ör: E-Ticaret'teki).
        /// </summary>
        public T? TargetValue { get; set; }

        /// <summary>
        /// Kaynak verinin son güncelleme zamanı.
        /// </summary>
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Hedef verinin son güncelleme zamanı.
        /// </summary>
        public DateTime? TargetTimestamp { get; set; }

        /// <summary>
        /// Veri yönü: ToERP veya FromERP.
        /// </summary>
        public string Direction { get; set; } = "FromERP";

        /// <summary>
        /// Entity tipi: Stok, Fiyat, Siparis, Cari.
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Benzersiz tanımlayıcı (SKU, OrderId vb.).
        /// </summary>
        public string Identifier { get; set; } = string.Empty;

        /// <summary>
        /// Ek metadata (custom resolver'lar için).
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Çakışma çözüm sonucu.
    /// </summary>
    /// <typeparam name="T">Değer tipi</typeparam>
    public class ConflictResolutionResult<T> where T : class
    {
        /// <summary>
        /// Çakışma var mıydı?
        /// </summary>
        public bool HadConflict { get; set; }

        /// <summary>
        /// Kazanan değer (uygulanacak).
        /// </summary>
        public T? WinningValue { get; set; }

        /// <summary>
        /// Kazanan taraf: Source, Target, Merged.
        /// </summary>
        public ConflictWinner Winner { get; set; }

        /// <summary>
        /// Çözüm stratejisi adı.
        /// </summary>
        public string Strategy { get; set; } = string.Empty;

        /// <summary>
        /// Çözüm açıklaması (loglama için).
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Çakışma detayları (hangi alanlar farklıydı).
        /// </summary>
        public List<FieldConflict> FieldConflicts { get; set; } = new();

        /// <summary>
        /// Çakışma olmadan başarılı sonuç oluşturur.
        /// </summary>
        public static ConflictResolutionResult<T> NoConflict(T value)
        {
            return new ConflictResolutionResult<T>
            {
                HadConflict = false,
                WinningValue = value,
                Winner = ConflictWinner.Source,
                Strategy = "NoConflict",
                Reason = "Hedef sistemde kayıt yok, yeni kayıt oluşturulacak"
            };
        }

        /// <summary>
        /// Kaynak kazandı sonucu oluşturur.
        /// </summary>
        public static ConflictResolutionResult<T> SourceWins(T value, string reason, List<FieldConflict>? conflicts = null)
        {
            return new ConflictResolutionResult<T>
            {
                HadConflict = true,
                WinningValue = value,
                Winner = ConflictWinner.Source,
                Strategy = "SourceWins",
                Reason = reason,
                FieldConflicts = conflicts ?? new()
            };
        }

        /// <summary>
        /// Hedef kazandı sonucu oluşturur (güncelleme yapılmaz).
        /// </summary>
        public static ConflictResolutionResult<T> TargetWins(T value, string reason, List<FieldConflict>? conflicts = null)
        {
            return new ConflictResolutionResult<T>
            {
                HadConflict = true,
                WinningValue = value,
                Winner = ConflictWinner.Target,
                Strategy = "TargetWins",
                Reason = reason,
                FieldConflicts = conflicts ?? new()
            };
        }
    }

    /// <summary>
    /// Kazanan taraf enum'ı.
    /// </summary>
    public enum ConflictWinner
    {
        /// <summary>Kaynak sistem kazandı (Mikro veya E-Ticaret, yöne göre).</summary>
        Source,
        
        /// <summary>Hedef sistem kazandı (güncelleme yapılmaz).</summary>
        Target,
        
        /// <summary>Her iki tarafın değerleri birleştirildi.</summary>
        Merged,
        
        /// <summary>Manuel müdahale gerekiyor.</summary>
        RequiresManualReview
    }

    /// <summary>
    /// Tek bir alan için çakışma detayı.
    /// </summary>
    public class FieldConflict
    {
        /// <summary>Alan adı (ör: StockQuantity, Price).</summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>Kaynak değer (string representation).</summary>
        public string? SourceValue { get; set; }

        /// <summary>Hedef değer (string representation).</summary>
        public string? TargetValue { get; set; }

        /// <summary>Fark büyüklüğü (sayısal alanlar için).</summary>
        public decimal? Difference { get; set; }

        /// <summary>Yüzde fark (ör: fiyat %10 farklı).</summary>
        public decimal? PercentDifference { get; set; }
    }
}
