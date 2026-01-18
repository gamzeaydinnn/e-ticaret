// ==========================================================================
// CourierStatus.cs - Kurye Durum Enum'u
// ==========================================================================

namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// Kurye'nin anlık durumu
    /// </summary>
    public enum CourierStatus
    {
        /// <summary>
        /// Aktif değil (offline, vardiya dışı)
        /// </summary>
        Inactive = 0,
        
        /// <summary>
        /// Müsait (online ama atanmış teslimat yok)
        /// </summary>
        Available = 1,
        
        /// <summary>
        /// Teslimat yapıyor (atanmış görev var)
        /// </summary>
        OnDelivery = 2,
        
        /// <summary>
        /// Mola (online ama geçici olarak teslimat alamaz)
        /// </summary>
        OnBreak = 3,
        
        /// <summary>
        /// Arızalı/Sorunlu (araç arızası, kaza, vb.)
        /// </summary>
        Unavailable = 4
    }
}
