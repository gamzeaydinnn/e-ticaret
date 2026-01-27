// ==========================================================================
// ShippingSetting.cs - Kargo Ücreti Ayarları Entity'si
// ==========================================================================
// Araç tipi bazlı (motorcycle/car) kargo ücretlerini veritabanında saklar.
// Admin panelinden dinamik olarak güncellenebilir, kurye bazlı DEĞİL.
// ==========================================================================

using System;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Kargo ücreti ayarları entity'si.
    /// Her araç tipi için ayrı fiyat belirlenmesini sağlar.
    /// Kurye bazlı değil, sistemde tanımlı araç tipleri bazlıdır.
    /// </summary>
    public class ShippingSetting : BaseEntity
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // TEMEL ALANLAR
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Araç tipi: "motorcycle" (motosiklet) veya "car" (araba/araç).
        /// PaymentPage.jsx'deki shippingMethod ile uyumlu olmalı.
        /// </summary>
        public string VehicleType { get; set; } = string.Empty;

        /// <summary>
        /// Araç tipi için görüntülenecek Türkçe isim.
        /// Örn: "Motosiklet ile Teslimat", "Araç ile Teslimat"
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Kargo ücreti (TL cinsinden).
        /// Admin panelinden güncellenebilir.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Tahmini teslimat süresi.
        /// Örn: "30-45 dakika", "1-2 saat"
        /// </summary>
        public string EstimatedDeliveryTime { get; set; } = string.Empty;

        /// <summary>
        /// Açıklama veya ek bilgi.
        /// Örn: "Hızlı teslimat, küçük paketler için ideal"
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Sıralama önceliği (küçük değer önce gösterilir).
        /// Sepet sayfasında seçeneklerin görüntülenme sırasını belirler.
        /// </summary>
        public int SortOrder { get; set; } = 0;

        // ═══════════════════════════════════════════════════════════════════════════════
        // AĞIRLIK/BOYUT LİMİTLERİ (Gelecekte Kullanım İçin)
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Maksimum taşınabilir ağırlık (kg).
        /// Null ise sınırsız.
        /// </summary>
        public decimal? MaxWeight { get; set; }

        /// <summary>
        /// Maksimum paket boyutu (cm³).
        /// Null ise sınırsız.
        /// </summary>
        public decimal? MaxVolume { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // AUDIT ALANLARI
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Son güncellemeyi yapan kullanıcının ID'si.
        /// Admin panelinden kim değiştirdi takibi için.
        /// </summary>
        public int? UpdatedByUserId { get; set; }

        /// <summary>
        /// Son güncellemeyi yapan kullanıcının adı (denormalize).
        /// Hızlı erişim için, User join'i gerekmeden görüntüleme amaçlı.
        /// </summary>
        public string? UpdatedByUserName { get; set; }
    }
}
