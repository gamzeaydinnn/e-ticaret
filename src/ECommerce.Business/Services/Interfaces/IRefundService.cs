// ==========================================================================
// IRefundService.cs - İade Servisi Interface'i
// ==========================================================================
// İade talebi oluşturma, listeleme ve admin işlem sözleşmesi.
// SOLID: Interface Segregation - sadece iade ile ilgili metodlar.
// ==========================================================================

using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Order;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// İade talebi servis katmanı sözleşmesi.
    /// Müşteri iade talebi oluşturma, admin onay/ret ve para iadesi işlemlerini kapsar.
    /// </summary>
    public interface IRefundService
    {
        // ═══════════════════════════════════════════════════════════════════════
        // MÜŞTERİ İŞLEMLERİ
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Müşteri iade talebi oluşturur.
        /// Kargo durumuna göre iki akıştan birini tetikler:
        ///   1. Kargo yola çıkmamış → Otomatik iptal + POSNET reverse + stok iade
        ///   2. Kargo yola çıkmış  → İade talebi kaydı oluşturur, admin onayı bekler
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="userId">Talep eden kullanıcı ID</param>
        /// <param name="dto">İade talebi detayları</param>
        /// <returns>Oluşturulan iade talebi bilgisi ve işlem sonucu</returns>
        Task<RefundRequestResult> CreateRefundRequestAsync(int orderId, int userId, CreateRefundRequestDto dto);

        /// <summary>
        /// Kullanıcının kendi iade taleplerini listeler.
        /// </summary>
        /// <param name="userId">Kullanıcı ID</param>
        /// <returns>Kullanıcının iade talepleri</returns>
        Task<IEnumerable<RefundRequestListDto>> GetUserRefundRequestsAsync(int userId);

        /// <summary>
        /// Belirli bir siparişin iade taleplerini getirir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <returns>Siparişe ait iade talepleri</returns>
        Task<IEnumerable<RefundRequestListDto>> GetRefundRequestsByOrderAsync(int orderId);

        // ═══════════════════════════════════════════════════════════════════════
        // ADMİN İŞLEMLERİ
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tüm iade taleplerini listeler (admin paneli).
        /// Status filtreleme desteği ile.
        /// </summary>
        /// <param name="status">Opsiyonel durum filtresi</param>
        /// <returns>İade talepleri listesi</returns>
        Task<IEnumerable<RefundRequestListDto>> GetAllRefundRequestsAsync(RefundRequestStatus? status = null);

        /// <summary>
        /// Bekleyen iade taleplerini listeler (admin bildirim/dashboard).
        /// </summary>
        /// <returns>Bekleyen iade talepleri</returns>
        Task<IEnumerable<RefundRequestListDto>> GetPendingRefundRequestsAsync();

        /// <summary>
        /// Admin iade talebi onaylar veya reddeder.
        /// Onay durumunda POSNET üzerinden para iadesi tetiklenir.
        /// </summary>
        /// <param name="refundRequestId">İade talebi ID</param>
        /// <param name="adminUserId">İşlemi yapan admin ID</param>
        /// <param name="dto">Onay/ret detayları</param>
        /// <returns>İşlem sonucu</returns>
        Task<RefundRequestResult> ProcessRefundRequestAsync(int refundRequestId, int adminUserId, ProcessRefundDto dto);

        /// <summary>
        /// Başarısız para iadesi yeniden denenir (admin müdahalesi).
        /// RefundFailed durumundaki talepler için.
        /// </summary>
        /// <param name="refundRequestId">İade talebi ID</param>
        /// <param name="adminUserId">İşlemi yapan admin ID</param>
        /// <returns>İşlem sonucu</returns>
        Task<RefundRequestResult> RetryRefundAsync(int refundRequestId, int adminUserId);

        /// <summary>
        /// Admin/Market görevlisi siparişi iptal eder ve para iadesini tetikler.
        /// Siparişin durumuna bakılmaksızın iptal + POSNET reverse/return yapılır.
        /// </summary>
        /// <param name="orderId">İptal edilecek sipariş ID</param>
        /// <param name="adminUserId">İşlemi yapan admin/görevli ID</param>
        /// <param name="reason">İptal sebebi</param>
        /// <returns>İşlem sonucu</returns>
        Task<RefundRequestResult> AdminCancelOrderWithRefundAsync(int orderId, int adminUserId, string reason);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // İADE TALEBİ İŞLEM SONUCU
    // Tüm iade operasyonları için tutarlı sonuç nesnesi
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// İade talebi işlem sonucu.
    /// Başarı/hata durumu, mesaj ve iade talebi bilgisi içerir.
    /// </summary>
    public class RefundRequestResult
    {
        /// <summary>İşlem başarılı mı?</summary>
        public bool Success { get; set; }

        /// <summary>Kullanıcıya gösterilecek mesaj</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Hata kodu (programatik kontrol için)</summary>
        public string? ErrorCode { get; set; }

        /// <summary>Oluşturulan/güncellenen iade talebi bilgisi</summary>
        public RefundRequestListDto? RefundRequest { get; set; }

        /// <summary>
        /// Otomatik iptal mi yapıldı?
        /// true ise: Kargo çıkmamış, sistem otomatik iptal + reverse yaptı.
        /// false ise: Kargo çıkmış, admin onayı bekleniyor.
        /// </summary>
        public bool AutoCancelled { get; set; }

        /// <summary>Müşteri hizmetleri iletişim bilgileri (kargo çıkmış durumda)</summary>
        public object? ContactInfo { get; set; }

        public static RefundRequestResult Succeeded(RefundRequestListDto request, string message, bool autoCancelled = false)
        {
            return new RefundRequestResult
            {
                Success = true,
                Message = message,
                RefundRequest = request,
                AutoCancelled = autoCancelled
            };
        }

        public static RefundRequestResult Failed(string message, string? errorCode = null)
        {
            return new RefundRequestResult
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }
}
