// ==========================================================================
// IOrderCancellationHandler.cs - Sipariş İptali Handler Interface'i
// ==========================================================================
// Bu interface, sipariş iptal edildiğinde ilgili teslimat görevlerinin
// otomatik olarak iptal edilmesini sağlar.
//
// İş Mantığı:
// - Sipariş iptal edildiğinde event tetiklenir
// - İlgili DeliveryTask bulunur
// - DeliveryTask durumuna göre uygun işlem yapılır
// - Kurye bilgilendirilir
// - Audit log kaydedilir
// ==========================================================================

using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Sipariş iptali handler interface'i.
    /// </summary>
    public interface IOrderCancellationHandler
    {
        /// <summary>
        /// Sipariş iptal edildiğinde çağrılır.
        /// İlgili teslimat görevini iptal eder.
        /// </summary>
        /// <param name="orderId">İptal edilen sipariş ID'si</param>
        /// <param name="reason">İptal sebebi</param>
        /// <param name="cancelledByUserId">İptal eden kullanıcı ID'si</param>
        /// <returns>İşlem sonucu</returns>
        Task<OrderCancellationResult> HandleOrderCancellationAsync(
            int orderId, 
            string reason, 
            int cancelledByUserId);

        /// <summary>
        /// Teslimat görevi iptal edilebilir mi kontrol eder.
        /// </summary>
        /// <param name="orderId">Sipariş ID'si</param>
        /// <returns>İptal edilebilir mi ve neden</returns>
        Task<(bool canCancel, string? reason)> CanCancelDeliveryAsync(int orderId);

        /// <summary>
        /// İptal edilen siparişin teslimat görevini kısmen geri alır.
        /// (İptal geri alındığında)
        /// </summary>
        /// <param name="orderId">Sipariş ID'si</param>
        /// <returns>Başarılı mı?</returns>
        Task<bool> RevertCancellationAsync(int orderId);
    }

    /// <summary>
    /// Sipariş iptali sonucu.
    /// </summary>
    public class OrderCancellationResult
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Sonuç mesajı.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// İptal edilen teslimat görevi ID'si.
        /// </summary>
        public int? CancelledDeliveryTaskId { get; set; }

        /// <summary>
        /// Önceki teslimat durumu.
        /// </summary>
        public string? PreviousStatus { get; set; }

        /// <summary>
        /// Bilgilendirilen kurye ID'si.
        /// </summary>
        public int? NotifiedCourierId { get; set; }

        /// <summary>
        /// Kuryeye bildirim gönderildi mi?
        /// </summary>
        public bool CourierNotified { get; set; }

        /// <summary>
        /// İptal edilememe sebebi (başarısız ise).
        /// </summary>
        public string? FailureReason { get; set; }
    }
}
