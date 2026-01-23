namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// Sipariş ağırlık fark durumu
    /// Tartı sonrası ödeme sürecinin durumunu takip eder
    /// </summary>
    public enum WeightAdjustmentStatus
    {
        /// <summary>
        /// Ağırlık bazlı ürün yok veya henüz işlem başlamadı
        /// </summary>
        NotApplicable = 0,

        /// <summary>
        /// Tartı bekleniyor
        /// Sipariş kurye tarafından teslim edilmek üzere, tartı henüz yapılmadı
        /// </summary>
        PendingWeighing = 1,

        /// <summary>
        /// Tartı yapıldı, fark hesaplandı
        /// Ödeme işlemi başlatılmayı bekliyor
        /// </summary>
        Weighed = 2,

        /// <summary>
        /// Fark yok
        /// Tahmini ve gerçek ağırlık eşit (veya çok küçük fark)
        /// </summary>
        NoDifference = 3,

        /// <summary>
        /// Ek ödeme bekleniyor
        /// Gerçek ağırlık tahminiden fazla, müşteriden ek tutar çekilecek
        /// </summary>
        PendingAdditionalPayment = 4,

        /// <summary>
        /// İade bekleniyor
        /// Gerçek ağırlık tahminiden az, müşteriye iade yapılacak
        /// </summary>
        PendingRefund = 5,

        /// <summary>
        /// Ödeme/İade tamamlandı
        /// Fark tutarı başarıyla tahsil edildi veya iade edildi
        /// </summary>
        Completed = 6,

        /// <summary>
        /// Admin onayı bekleniyor
        /// Fark çok yüksek veya sistem otomatik işleyemedi
        /// </summary>
        PendingAdminApproval = 7,

        /// <summary>
        /// Admin tarafından reddedildi
        /// Manuel müdahale sonucu işlem iptal edildi
        /// </summary>
        RejectedByAdmin = 8,

        /// <summary>
        /// Hata oluştu
        /// Ödeme/iade işlemi başarısız oldu
        /// </summary>
        Failed = 9
    }
}
