using System;
using System.Collections.Generic;
using ECommerce.Entities.Concrete;

namespace ECommerce.Entities.Concrete
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int? ParentId { get; set; }
        public int SortOrder { get; set; } = 0;
        public string Slug { get; set; } = string.Empty;
        public new bool IsActive { get; set; } = true; // BaseEntity'deki IsActive'i gizler (override değil, yeni property)

        // ═══════════════════════════════════════════════════════════════════════
        // MİKRO ERP STOK ANA/ALT GRUP ENTEGRASYON ALANLARI
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Mikro ERP STOK_ANA_GRUPLARI.san_kod karşılığı (örn: "100", "1400", "2200").
        /// Hem ana kategorilerde hem alt kategorilerde dolu tutulur.
        /// </summary>
        public string? MikroAnaGrupKod { get; set; }

        /// <summary>
        /// Mikro ERP STOK_ALT_GRUPLARI.sta_kod karşılığı (örn: "101", "1403").
        /// Ana kategorilerde (ParentId == null) null, alt kategorilerde doludur.
        /// </summary>
        public string? MikroAltGrupKod { get; set; }

        /// <summary>
        /// Mikro ERP'deki orijinal grup ismi (san_isim / sta_isim).
        /// Admin vitrin adını (Name) özelleştirse bile Mikro'daki ham isim burada saklanır.
        /// </summary>
        public string? MikroIsim { get; set; }

        /// <summary>
        /// Bu kategori Mikro ERP ile senkronize edildi mi / Mikro'ya yazıldı mı?
        /// </summary>
        public bool IsMikroSynced { get; set; } = false;

        /// <summary>
        /// Son Mikro senkronizasyon zamanı (UTC).
        /// </summary>
        public DateTime? LastMikroSyncAt { get; set; }

        /// <summary>
        /// Üst menü (Navbar) birleşik hap anahtarı.
        /// Örn: 1400 (Atıştırmalık) ve 1100 (Kuruyemiş) -> "atistirmalik"
        ///      1300 (Dondurulmuş Gıda) ve 800 (Dondurma) -> "dondurma-ve-dondurulmus-gida"
        ///      1000 (Ev Bakım) ve 1500 (Kağıt Islak Mendil) -> "temizlik"
        ///      2000 (Ev Yaşam) -> "ev-ve-mutfak"
        /// </summary>
        public string? NavbarGroupKey { get; set; }

        // ParentCategoryId ile hiyerarşi desteği.
        public ICollection<Product> Products { get; set; } = new HashSet<Product>();

        // Navigation Properties
        public virtual Category? Parent { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; } = new HashSet<Category>();
    }
}
