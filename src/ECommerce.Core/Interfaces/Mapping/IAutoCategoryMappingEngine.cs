using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces.Mapping
{
    /// <summary>
    /// Mikro grup kodlarını mevcut e-ticaret kategorileriyle otomatik eşleştiren motor.
    /// 
    /// NEDEN: 6000+ üründe manuel eşleme pratik değil. Bu motor:
    /// 1. Türkçe metin normalizasyonu ile fuzzy match yapar
    /// 2. Eşik altı kalanlara yeni kategori oluşturur (config ile kontrol)
    /// 3. Tüm eşlenemeyenleri "Diğer" kategorisine atar
    /// 
    /// KULLANIM:
    /// - İlk setup: DiscoverAndMapAllAsync → tüm cache'deki grupları eşle
    /// - Tek grup: SuggestMappingAsync → admin panelden önerme
    /// - Import hook: ResolveOrCreateMappingAsync → yeni ürün geldiğinde anında çöz
    /// </summary>
    public interface IAutoCategoryMappingEngine
    {
        /// <summary>
        /// Tek bir grup kodu için en uygun kategoriyi önerir.
        /// Sonuç: eşleşme adayları (benzerlik skoru ile) veya boş liste.
        /// </summary>
        Task<List<CategorySuggestion>> SuggestMappingAsync(
            string anagrupKod,
            string? altgrupKod = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Grup kodu için mapping çözer: varsa döner, yoksa otomatik oluşturur.
        /// Import sırasında çağrılır — her zaman bir CategoryId döner.
        /// 
        /// AKIŞ:
        /// 1. MikroCategoryMapping tablosunda var mı? → varsa döndür
        /// 2. Fuzzy match ile mevcut kategoriye eşleşiyor mu? → eşleşirse mapping yaz + döndür
        /// 3. AutoCreateCategories=true ise yeni kategori oluştur + mapping yaz
        /// 4. Hiçbiri değilse → "Diğer" kategorisi döndür
        /// </summary>
        Task<int> ResolveOrCreateMappingAsync(
            string anagrupKod,
            string? altgrupKod = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cache'deki tüm eşlenmemiş grup kodlarını keşfeder ve otomatik eşler.
        /// One-time migration veya admin tetiklemesi için.
        /// </summary>
        Task<AutoMappingResult> DiscoverAndMapAllAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Otomatik eşleme önerisi — admin panelinde gösterilir.
    /// </summary>
    public class CategorySuggestion
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategorySlug { get; set; } = string.Empty;
        /// <summary>
        /// 0.0 - 1.0 arası benzerlik skoru. 1.0 = tam eşleşme.
        /// </summary>
        public double Score { get; set; }
        /// <summary>
        /// Eşleme yöntemi: "exact", "contains", "fuzzy", "new_category"
        /// </summary>
        public string MatchType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Toplu otomatik eşleme sonucu.
    /// </summary>
    public class AutoMappingResult
    {
        public int TotalGroupCodes { get; set; }
        public int AlreadyMapped { get; set; }
        public int NewMappingsCreated { get; set; }
        public int NewCategoriesCreated { get; set; }
        public int FallbackToDiger { get; set; }
        public int Errors { get; set; }
        public List<string> ErrorDetails { get; set; } = new();
        public List<AutoMappingEntry> Mappings { get; set; } = new();
    }

    /// <summary>
    /// Tek bir otomatik eşleme kaydının detayı.
    /// </summary>
    public class AutoMappingEntry
    {
        public string AnagrupKod { get; set; } = string.Empty;
        public string? AltgrupKod { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string MatchType { get; set; } = string.Empty;
        public double Score { get; set; }
        public int ProductCount { get; set; }
    }
}
