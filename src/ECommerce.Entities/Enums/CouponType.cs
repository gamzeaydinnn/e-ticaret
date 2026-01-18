// =============================================================================
// Kupon Türleri Enum - E-Ticaret Kupon Sistemi
// =============================================================================
// Bu enum, sistemde desteklenen farklı kupon türlerini tanımlar.
// Her kupon türü farklı bir indirim mantığı uygular.
// Yeni kupon türleri eklenirken bu enum'a yeni değer eklenmeli ve
// CouponManager'da ilgili hesaplama mantığı implement edilmelidir.
// =============================================================================

namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// Kupon türlerini tanımlayan enum.
    /// Her tür farklı bir indirim hesaplama mantığı gerektirir.
    /// </summary>
    public enum CouponType
    {
        /// <summary>
        /// Yüzde bazlı indirim (ör: %10, %20)
        /// Hesaplama: TotalPrice * (Value / 100)
        /// </summary>
        Percentage = 0,

        /// <summary>
        /// Sabit tutar indirimi (ör: 50₺, 100₺)
        /// Hesaplama: TotalPrice - Value
        /// </summary>
        FixedAmount = 1,

        /// <summary>
        /// İlk sipariş indirimi - Sadece ilk siparişte geçerli
        /// Kullanıcının daha önce sipariş vermemiş olması gerekir
        /// </summary>
        FirstOrder = 2,

        /// <summary>
        /// X al Y öde kampanyası (ör: 3 al 2 öde)
        /// Value alanı: X değerini, MinOrderAmount alanı: Y değerini tutar
        /// Ek bilgi: ConditionsJson içinde ürün/kategori bilgisi tutulabilir
        /// </summary>
        BuyXGetY = 3,

        /// <summary>
        /// Ücretsiz kargo indirimi
        /// Kargo ücretini sıfırlar
        /// </summary>
        FreeShipping = 4
    }
}
