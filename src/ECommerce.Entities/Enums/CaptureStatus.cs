namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// Ödeme çekim (capture) durumu
    /// Pre-authorization sonrası gerçek ödeme çekimi için kullanılır
    /// Provizyon → Capture akışının takibi
    /// </summary>
    public enum CaptureStatus
    {
        /// <summary>
        /// Henüz çekim yapılmadı
        /// Provizyon alındı ama tutar çekilmedi
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Çekim başarılı
        /// Tutar müşteri kartından çekildi
        /// </summary>
        Success = 1,

        /// <summary>
        /// Çekim başarısız
        /// Kart limiti, süre aşımı veya teknik hata
        /// </summary>
        Failed = 2,

        /// <summary>
        /// Kısmi çekim yapıldı
        /// Authorize edilen tutarın bir kısmı çekildi
        /// (Ör: Stok eksikliği nedeniyle sipariş bölündü)
        /// </summary>
        PartialCapture = 3,

        /// <summary>
        /// Çekim iptal edildi
        /// Sipariş iptal edildi, provizyon geri bırakıldı
        /// </summary>
        Voided = 4,

        /// <summary>
        /// Çekim gerekmiyor
        /// Kapıda ödeme veya ödeme gerektirmeyen sipariş
        /// </summary>
        NotRequired = 5
    }
}
