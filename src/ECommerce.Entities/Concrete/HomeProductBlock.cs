using System;
using System.Collections.Generic;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Ana Sayfa Ürün Bloğu Entity'si
    /// ------------------------------------------------
    /// Ana sayfada gösterilecek ürün bloklarını temsil eder.
    /// Her blok sol tarafta bir poster/banner, sağ tarafta ürün kartları içerir.
    /// 
    /// Blok Tipleri:
    /// - manual: Admin tek tek ürün seçer (HomeBlockProduct tablosu kullanılır)
    /// - category: Belirli bir kategorideki ürünler otomatik gelir (CategoryId alanı)
    /// - discounted: İndirimli ürünler otomatik (SpecialPrice != null && SpecialPrice < Price)
    /// - newest: En son eklenen ürünler (CreatedAt'e göre sıralanır)
    /// - bestseller: En çok satan ürünler (sipariş sayısına göre - gelecekte)
    /// 
    /// Mimari Not:
    /// - BaseEntity'den türetilmedi çünkü Banner gibi kendi ID ve audit alanları var
    /// - Admin panelinden tam CRUD desteği olacak
    /// - Her blok için ayrı poster seçilebilir (Banner entity'si ile ilişki)
    /// </summary>
    public class HomeProductBlock
    {
        #region Temel Alanlar

        /// <summary>
        /// Blok benzersiz kimliği (Primary Key)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Blok adı - Admin panelinde ve frontend'de gösterilir
        /// Örnek: "İndirimli Ürünler", "Süt Ürünleri", "Atıştırmalıklar"
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL dostu benzersiz tanımlayıcı
        /// Örnek: "indirimli-urunler", "sut-urunleri"
        /// "Tümünü Gör" linkinde kullanılır: /kategori/{slug} veya /blok/{slug}
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Blok açıklaması - Admin panelinde bilgi amaçlı
        /// Frontend'de gösterilmeyebilir
        /// </summary>
        public string? Description { get; set; }

        #endregion

        #region Blok Tipi ve İçerik Kaynağı

        /// <summary>
        /// Blok içerik tipi - Ürünlerin nereden geleceğini belirler
        /// Değerler: "manual", "category", "discounted", "newest", "bestseller"
        /// 
        /// manual: HomeBlockProduct tablosundan çekilir (admin elle seçer)
        /// category: CategoryId alanındaki kategorinin ürünleri
        /// discounted: SpecialPrice olan tüm ürünler
        /// newest: CreatedAt'e göre en yeni ürünler
        /// bestseller: OrderItems sayısına göre en çok satanlar
        /// </summary>
        public string BlockType { get; set; } = "manual";

        /// <summary>
        /// Kategori bazlı bloklar için hedef kategori ID'si
        /// Sadece BlockType = "category" olduğunda kullanılır
        /// Null ise manuel veya otomatik tip demektir
        /// </summary>
        public int? CategoryId { get; set; }

        #endregion

        #region Görsel ve Banner

        /// <summary>
        /// Blok için poster/banner ID'si (Banner tablosu ile ilişki)
        /// Admin panelinden seçilir veya yeni banner yüklenir
        /// Null olabilir - bu durumda poster gösterilmez
        /// </summary>
        public int? BannerId { get; set; }

        /// <summary>
        /// Alternatif: Direkt görsel URL'i (Banner kullanılmadan)
        /// BannerId null ise bu alan kontrol edilir
        /// Admin basit bir görsel yüklemek isterse kullanılır
        /// </summary>
        public string? PosterImageUrl { get; set; }

        /// <summary>
        /// Poster arka plan rengi (hex kodu)
        /// Örnek: "#00BCD4" (turkuaz), "#FF5722" (turuncu)
        /// Poster yoksa veya yüklenirken arka plan rengi olarak kullanılır
        /// </summary>
        public string? BackgroundColor { get; set; }

        #endregion

        #region Gösterim Ayarları

        /// <summary>
        /// Gösterim sırası - Ana sayfada blokların sıralanması
        /// Küçükten büyüğe sıralanır (0, 1, 2, 3, ...)
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Blokta gösterilecek maksimum ürün sayısı
        /// Varsayılan: 6 (görsel düzen için ideal)
        /// "Tümünü Gör" ile geri kalanı görülebilir
        /// </summary>
        public int MaxProductCount { get; set; } = 6;

        /// <summary>
        /// "Tümünü Gör" butonu için hedef URL
        /// Örnek: "/kategori/sut-urunleri" veya "/kampanya/indirimli"
        /// Null ise slug'dan otomatik oluşturulur
        /// </summary>
        public string? ViewAllUrl { get; set; }

        /// <summary>
        /// "Tümünü Gör" buton metni
        /// Varsayılan: "Tümünü Gör"
        /// Özelleştirilebilir: "Tüm İndirimleri Gör", "Daha Fazla" vb.
        /// </summary>
        public string ViewAllText { get; set; } = "Tümünü Gör";

        /// <summary>
        /// Blok aktif mi? Pasif bloklar ana sayfada gösterilmez
        /// Admin hızlıca gizleyebilir
        /// </summary>
        public bool IsActive { get; set; } = true;

        #endregion

        #region Tarih Bazlı Gösterim (Opsiyonel)

        /// <summary>
        /// Gösterime başlangıç tarihi
        /// Belirtilmişse bu tarihten önce blok gösterilmez
        /// Kampanya bazlı bloklar için kullanışlı
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Gösterim bitiş tarihi
        /// Belirtilmişse bu tarihten sonra blok otomatik gizlenir
        /// </summary>
        public DateTime? EndDate { get; set; }

        #endregion

        #region Audit Alanları

        /// <summary>
        /// Oluşturulma tarihi (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Son güncelleme tarihi (UTC)
        /// Her değişiklikte güncellenir
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// İlişkili Banner/Poster entity'si
        /// BannerId üzerinden lazy loading ile yüklenir
        /// </summary>
        public virtual Banner? Banner { get; set; }

        /// <summary>
        /// Kategori bazlı bloklar için ilişkili kategori
        /// CategoryId üzerinden lazy loading ile yüklenir
        /// </summary>
        public virtual Category? Category { get; set; }

        /// <summary>
        /// Manuel seçimli bloklar için ürün ilişkileri
        /// BlockType = "manual" olduğunda bu koleksiyon kullanılır
        /// HomeBlockProduct ara tablosu üzerinden Many-to-Many ilişki
        /// </summary>
        public virtual ICollection<HomeBlockProduct> BlockProducts { get; set; } = new HashSet<HomeBlockProduct>();

        #endregion
    }
}
