using System;
using ECommerce.Entities.Enums;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Ağırlık fark kayıtları entity'si
    /// Her tartı işlemi için bir kayıt oluşturulur
    /// Admin müdahaleleri ve ödeme işlemleri bu entity üzerinden takip edilir
    /// </summary>
    public class WeightAdjustment : BaseEntity
    {
        #region Temel İlişkiler

        /// <summary>
        /// Sipariş ID'si (Foreign Key)
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Sipariş kalemi ID'si (Foreign Key)
        /// </summary>
        public int OrderItemId { get; set; }

        /// <summary>
        /// Ürün ID'si (hızlı erişim için)
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Ürün adı (snapshot - raporlama için)
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        #endregion

        #region Ağırlık Bilgileri

        /// <summary>
        /// Ağırlık birimi
        /// </summary>
        public WeightUnit WeightUnit { get; set; } = WeightUnit.Kilogram;

        /// <summary>
        /// Tahmini ağırlık (gram cinsinden)
        /// Müşterinin sipariş ettiği miktar
        /// </summary>
        public decimal EstimatedWeight { get; set; }

        /// <summary>
        /// Gerçek ağırlık (gram cinsinden)
        /// Kurye tarafından tartılan miktar
        /// </summary>
        public decimal ActualWeight { get; set; }

        /// <summary>
        /// Ağırlık farkı (gram cinsinden)
        /// Hesaplama: ActualWeight - EstimatedWeight
        /// Pozitif: Fazla geldi, Negatif: Eksik geldi
        /// </summary>
        public decimal WeightDifference { get; set; }

        /// <summary>
        /// Fark yüzdesi
        /// Hesaplama: (WeightDifference / EstimatedWeight) * 100
        /// </summary>
        public decimal DifferencePercent { get; set; }

        #endregion

        #region Fiyat Bilgileri

        /// <summary>
        /// Birim fiyat (sipariş anındaki)
        /// </summary>
        public decimal PricePerUnit { get; set; }

        /// <summary>
        /// Tahmini fiyat (TL)
        /// </summary>
        public decimal EstimatedPrice { get; set; }

        /// <summary>
        /// Gerçek fiyat (TL)
        /// </summary>
        public decimal ActualPrice { get; set; }

        /// <summary>
        /// Fiyat farkı (TL)
        /// Hesaplama: ActualPrice - EstimatedPrice
        /// Pozitif: Ek ödeme, Negatif: İade
        /// </summary>
        public decimal PriceDifference { get; set; }

        #endregion

        #region Durum ve İşlem Bilgileri

        /// <summary>
        /// Fark işlem durumu
        /// </summary>
        public WeightAdjustmentStatus Status { get; set; } = WeightAdjustmentStatus.PendingWeighing;

        /// <summary>
        /// Tartı tarihi
        /// </summary>
        public DateTime? WeighedAt { get; set; }

        /// <summary>
        /// Tartıyı yapan kurye ID'si
        /// </summary>
        public int? WeighedByCourierId { get; set; }

        /// <summary>
        /// Tartıyı yapan kurye adı (snapshot)
        /// </summary>
        public string? WeighedByCourierName { get; set; }

        /// <summary>
        /// Ödeme/İade işlemi tamamlandı mı?
        /// </summary>
        public bool IsSettled { get; set; } = false;

        /// <summary>
        /// Ödeme/İade tarihi
        /// </summary>
        public DateTime? SettledAt { get; set; }

        /// <summary>
        /// Ödeme işlem referans numarası
        /// </summary>
        public string? PaymentTransactionId { get; set; }

        #endregion

        #region Admin Müdahale Alanları

        /// <summary>
        /// Admin müdahalesi gerekiyor mu?
        /// Fark çok yüksekse veya sistem otomatik işleyemezse true
        /// </summary>
        public bool RequiresAdminApproval { get; set; } = false;

        /// <summary>
        /// Admin onayı/reddi yapıldı mı?
        /// </summary>
        public bool AdminReviewed { get; set; } = false;

        /// <summary>
        /// Admin onayladı mı? (true: onay, false: red)
        /// </summary>
        public bool? AdminApproved { get; set; }

        /// <summary>
        /// Admin tarafından düzeltilmiş fiyat
        /// Admin manuel fiyat belirleyebilir
        /// </summary>
        public decimal? AdminAdjustedPrice { get; set; }

        /// <summary>
        /// Admin notu
        /// </summary>
        public string? AdminNote { get; set; }

        /// <summary>
        /// Admin işlem tarihi
        /// </summary>
        public DateTime? AdminReviewedAt { get; set; }

        /// <summary>
        /// İşlem yapan admin ID'si
        /// </summary>
        public int? AdminUserId { get; set; }

        /// <summary>
        /// İşlem yapan admin adı (snapshot)
        /// </summary>
        public string? AdminUserName { get; set; }

        #endregion

        #region Müşteri Bilgilendirme

        /// <summary>
        /// Müşteri bilgilendirildi mi?
        /// SMS/Email ile fark bildirimi gönderildi mi?
        /// </summary>
        public bool CustomerNotified { get; set; } = false;

        /// <summary>
        /// Bilgilendirme tarihi
        /// </summary>
        public DateTime? CustomerNotifiedAt { get; set; }

        /// <summary>
        /// Bilgilendirme türü (sms, email, both)
        /// </summary>
        public string? NotificationType { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// İlişkili sipariş
        /// </summary>
        public virtual Order? Order { get; set; }

        /// <summary>
        /// İlişkili sipariş kalemi
        /// </summary>
        public virtual OrderItem? OrderItem { get; set; }

        /// <summary>
        /// İlişkili ürün
        /// </summary>
        public virtual Product? Product { get; set; }

        /// <summary>
        /// Tartıyı yapan kurye
        /// </summary>
        public virtual Courier? WeighedByCourier { get; set; }

        /// <summary>
        /// İşlem yapan admin
        /// </summary>
        public virtual User? AdminUser { get; set; }

        #endregion
    }
}
