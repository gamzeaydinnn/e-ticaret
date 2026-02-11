// ==========================================================================
// RefundRequestStatus.cs - İade Talebi Durum Enum'ı
// ==========================================================================
// Müşteri iade taleplerinin yaşam döngüsü:
// Pending → Approved/Rejected → Refunded (POSNET üzerinden para iadesi)
//
// NEDEN ayrı bir enum: OrderStatus ile bağımsız çalışması gerekir.
// Bir sipariş "Delivered" durumunda kalırken iade talebi "Pending" olabilir.
// ==========================================================================

namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// İade talebi durum enum'ı.
    /// Müşteri tarafından oluşturulan iade taleplerinin süreç takibi için kullanılır.
    /// </summary>
    public enum RefundRequestStatus
    {
        /// <summary>
        /// Talep oluşturuldu, admin/müşteri hizmetleri incelemesi bekleniyor.
        /// Kargo yola çıkmış siparişlerde bu durumda kalır.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Admin/müşteri hizmetleri talebi onayladı.
        /// POSNET üzerinden para iadesi tetiklenecek.
        /// </summary>
        Approved = 1,

        /// <summary>
        /// Admin/müşteri hizmetleri talebi reddetti.
        /// Müşteriye ret sebebi gösterilir.
        /// </summary>
        Rejected = 2,

        /// <summary>
        /// Para iadesi POSNET üzerinden başarıyla tamamlandı.
        /// Bu terminal durumdur, geri dönüş yoktur.
        /// </summary>
        Refunded = 3,

        /// <summary>
        /// Kargo yola çıkmadan otomatik iptal + reverse yapıldı.
        /// Müşteri müdahalesine gerek kalmadan sistem tarafından işlendi.
        /// </summary>
        AutoCancelled = 4,

        /// <summary>
        /// POSNET para iadesi sırasında hata oluştu.
        /// Admin müdahalesi gerektirir, manuel deneme yapılabilir.
        /// </summary>
        RefundFailed = 5
    }
}
