using System;
using System.Collections.Generic;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

//	• Orders (Id GUID, UserId, TotalAmount, ShippingAmount, PaymentStatus, OrderStatus, CreatedAt, ReservationId (Guid?), AddressId)
/*    public int Id { get; set; }
    public int? UserId { get; set; } // guest ise null
    public User User { get; set; }
    public string CustomerName { get; set; }
    public string CustomerPhone { get; set; }
    public string CustomerEmail { get; set; }
    public string Address { get; set; }
    public string PaymentMethod { get; set; }
    public string Status { get; set; } // e.g. Pending, Preparing, OutForDelivery, Delivered, Cancelled
    public int? CourierId { get; set; }
    public Courier Courier { get; set; }
*/
namespace ECommerce.Entities.Concrete
{
    public class Order : BaseEntity
    {
        public string OrderNumber { get; set; } = string.Empty;
        public Guid? ClientOrderId { get; set; }
        public int? UserId { get; set; }
        public bool IsGuestOrder { get; set; }
        public decimal VatAmount { get; set; } = 0m;
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal TotalPrice { get; set; } = 0m;
        public decimal DiscountAmount { get; set; } = 0m;
        public decimal FinalPrice { get; set; } = 0m;
        public decimal CouponDiscountAmount { get; set; } = 0m;
        public decimal CampaignDiscountAmount { get; set; } = 0m;
        public string? AppliedCouponCode { get; set; }
        public string Currency { get; set; } = "TRY";
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        
        // Kargo bilgileri
        public string ShippingMethod { get; set; } = "car"; // car veya motorcycle
        public decimal ShippingCost { get; set; } = 30m; // Kargo ücreti
        
        // Kurye bilgileri
        public int? CourierId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? PickedUpAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? DeliveryNotes { get; set; }
        public string Priority { get; set; } = "normal"; // normal, urgent, low

        // Gerçek kargo takip numarası
        public string? TrackingNumber { get; set; }

        // Payment / reservation metadata
        public ECommerce.Entities.Enums.PaymentStatus PaymentStatus { get; set; } = ECommerce.Entities.Enums.PaymentStatus.Pending;
        public Guid? ReservationId { get; set; }

        #region Ağırlık Bazlı Ödeme Alanları

        /// <summary>
        /// Siparişte ağırlık bazlı ürün var mı?
        /// true ise tartı ve fark hesaplama işlemleri yapılacak
        /// </summary>
        public bool HasWeightBasedItems { get; set; } = false;

        /// <summary>
        /// Ağırlık fark durumu
        /// Tartı sonrası ödeme sürecinin takibi için
        /// </summary>
        public WeightAdjustmentStatus WeightAdjustmentStatus { get; set; } = WeightAdjustmentStatus.NotApplicable;

        /// <summary>
        /// Ön provizyon tutarı (TL)
        /// Sipariş oluşturulurken tahmini tutara göre alınan provizyon
        /// Kredi kartı ödemelerinde Pre-Authorization tutarı
        /// </summary>
        public decimal PreAuthAmount { get; set; } = 0m;

        /// <summary>
        /// Kesin tutar (TL)
        /// Tartı sonrası hesaplanan gerçek toplam tutar
        /// Bu tutar karttan çekilir veya kapıda ödenir
        /// </summary>
        public decimal FinalAmount { get; set; } = 0m;

        /// <summary>
        /// Toplam ağırlık farkı (gram)
        /// Tüm ağırlık bazlı ürünlerin fark toplamı
        /// Pozitif: Fazla, Negatif: Eksik
        /// </summary>
        public decimal TotalWeightDifference { get; set; } = 0m;

        /// <summary>
        /// Toplam fiyat farkı (TL)
        /// Tüm ağırlık bazlı ürünlerin fiyat farkı toplamı
        /// Pozitif: Ek ödeme, Negatif: İade
        /// </summary>
        public decimal TotalPriceDifference { get; set; } = 0m;

        /// <summary>
        /// Ödeme türü
        /// credit_card, cash_on_delivery vb.
        /// </summary>
        public string PaymentMethod { get; set; } = "cash_on_delivery";

        /// <summary>
        /// POSNET işlem referans numarası
        /// Pre-auth ve post-auth işlemleri için
        /// </summary>
        public string? PosnetTransactionId { get; set; }

        /// <summary>
        /// Pre-Authorization HostLogKey
        /// POSNET provizyonu için referans anahtarı
        /// Finansallaştırma (capture) ve iade işlemlerinde kullanılır
        /// </summary>
        public string? PreAuthHostLogKey { get; set; }

        /// <summary>
        /// Ağırlık farkı (TL olarak)
        /// Pozitif: Müşteriye iade, Negatif: Müşteriden tahsilat
        /// </summary>
        public decimal WeightDifference { get; set; } = 0m;

        /// <summary>
        /// Pre-authorization tarihi
        /// Provizyon alındığı tarih (48 saat geçerlilik kontrolü için)
        /// </summary>
        public DateTime? PreAuthDate { get; set; }

        /// <summary>
        /// Tüm tartılar tamamlandı mı?
        /// true: Siparişin kesin tutarı hesaplanabilir
        /// </summary>
        public bool AllItemsWeighed { get; set; } = false;

        /// <summary>
        /// Tartı tamamlanma tarihi
        /// Son ürün tartıldığı an
        /// </summary>
        public DateTime? WeighingCompletedAt { get; set; }

        /// <summary>
        /// Fark ödemesi/iadesi yapıldı mı?
        /// </summary>
        public bool DifferenceSettled { get; set; } = false;

        /// <summary>
        /// Fark ödemesi/iadesi tarihi
        /// </summary>
        public DateTime? DifferenceSettledAt { get; set; }

        #endregion

        // Address normalization
        public int? AddressId { get; set; }
        public virtual Address? Address { get; set; }

        // Navigation Properties
        public virtual User? User { get; set; }
        public virtual Courier? Courier { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new HashSet<OrderItem>();
        public virtual ICollection<StockReservation> StockReservations { get; set; } = new HashSet<StockReservation>();
        public virtual ICollection<WeightReport> WeightReports { get; set; } = new HashSet<WeightReport>();
        public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new HashSet<OrderStatusHistory>();
        
        /// <summary>
        /// Bu siparişte kullanılan kuponların geçmişi
        /// Tek kupon destekli sistemde tek kayıt olur, ancak geçmiş için collection tutulur
        /// </summary>
        public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new HashSet<CouponUsage>();

        /// <summary>
        /// Bu siparişin ağırlık fark kayıtları
        /// Her ağırlık bazlı ürün için bir kayıt oluşturulur
        /// </summary>
        public virtual ICollection<WeightAdjustment> WeightAdjustments { get; set; } = new HashSet<WeightAdjustment>();
    }
}
