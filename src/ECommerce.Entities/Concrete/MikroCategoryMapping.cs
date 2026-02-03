using ECommerce.Entities.Concrete;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Mikro ERP kategori kodu ile e-ticaret kategori eşlemesi.
    /// 
    /// NEDEN: Mikro ERP'de kategoriler "kod" bazlı (ör: "MEYVE", "SARKUTERI"),
    /// e-ticaret'te ise integer ID bazlı. Bu tablo aradaki eşlemeyi sağlar.
    /// 
    /// KULLANIM:
    /// - Ürün sync'te: Mikro sto_anagrup_kod → CategoryId bulma
    /// - Sipariş sync'te: Product.CategoryId → Mikro grup kodu bulma
    /// 
    /// ÖRNEK:
    /// MikroAnagrupKod = "MEYVE-SEBZE", CategoryId = 5
    /// MikroAltgrupKod = "MEYVE", CategoryId = 12
    /// </summary>
    public class MikroCategoryMapping : BaseEntity
    {
        /// <summary>
        /// Mikro ERP'deki ana grup kodu.
        /// Örnek: "MEYVE-SEBZE", "SARKUTERI", "TEMEL-GIDA"
        /// </summary>
        public string MikroAnagrupKod { get; set; } = string.Empty;

        /// <summary>
        /// Mikro ERP'deki alt grup kodu (opsiyonel).
        /// Örnek: "MEYVE", "SEBZE", "PEYNIR", "SUCUK"
        /// </summary>
        public string? MikroAltgrupKod { get; set; }

        /// <summary>
        /// Mikro ERP'deki marka kodu (opsiyonel).
        /// Örnek: "ULKER", "PINAR", "SANA"
        /// </summary>
        public string? MikroMarkaKod { get; set; }

        /// <summary>
        /// E-ticaret tarafındaki kategori ID.
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// E-ticaret tarafındaki marka ID (opsiyonel).
        /// </summary>
        public int? BrandId { get; set; }

        /// <summary>
        /// Eşleme önceliği.
        /// NEDEN: Birden fazla eşleme kuralı çakışırsa en yüksek
        /// öncelikli olan seçilir.
        /// 
        /// Örnek:
        /// Priority=1: MikroAnagrupKod="MEYVE-SEBZE" → CategoryId=5
        /// Priority=2: MikroAnagrupKod="MEYVE-SEBZE", MikroAltgrupKod="MEYVE" → CategoryId=12
        /// 
        /// "MEYVE" alt grubu için Priority 2 olan seçilir (daha spesifik)
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Mikro'dan gelen açıklama (debug için).
        /// </summary>
        public string? MikroGrupAciklama { get; set; }

        /// <summary>
        /// Notlar (admin için).
        /// </summary>
        public string? Notes { get; set; }

        // ==================== NAVİGASYON ====================

        /// <summary>
        /// İlişkili e-ticaret kategorisi.
        /// </summary>
        public virtual Category? Category { get; set; }

        /// <summary>
        /// İlişkili e-ticaret markası.
        /// </summary>
        public virtual Brand? Brand { get; set; }
    }
}
