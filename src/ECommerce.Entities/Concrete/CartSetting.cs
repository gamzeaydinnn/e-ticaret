// ==========================================================================
// CartSetting.cs - Sepet Ayarları Entity'si
// ==========================================================================
// Minimum sepet tutarı gibi site genelinde geçerli sepet kurallarını saklar.
// Admin panelinden dinamik olarak güncellenebilir.
// Tabloda tek satır (singleton pattern) olarak kullanılır.
// ==========================================================================

using System;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Sepet ayarları entity'si.
    /// Minimum sepet tutarı ve ilgili kuralları yönetir.
    /// Singleton pattern: Tabloda tek bir aktif kayıt bulunur.
    /// </summary>
    public class CartSetting : BaseEntity
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // MİNİMUM SEPET TUTARI ALANLARI
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Minimum sepet tutarı (TL cinsinden).
        /// Müşteri bu tutarın altında sipariş veremez.
        /// 0 ise minimum tutar zorunluluğu yoktur.
        /// </summary>
        public decimal MinimumCartAmount { get; set; } = 0;

        /// <summary>
        /// Minimum sepet tutarı kuralı aktif mi?
        /// false ise tutar belirlenmiş olsa bile kontrol yapılmaz.
        /// Admin panelinden açılıp kapatılabilir.
        /// </summary>
        public bool IsMinimumCartAmountActive { get; set; } = false;

        /// <summary>
        /// Minimum tutara ulaşılamadığında müşteriye gösterilecek mesaj.
        /// {amount} placeholder'ı gerçek tutarla değiştirilir.
        /// Örn: "Sipariş verebilmek için sepet tutarınız en az {amount} TL olmalıdır."
        /// </summary>
        public string MinimumCartAmountMessage { get; set; } = "Sipariş verebilmek için sepet tutarınız en az {amount} TL olmalıdır.";

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
