// ═══════════════════════════════════════════════════════════════════════════════════════════════
// AĞIRLIK BAZLI ÖDEME SERVİSİ INTERFACE
// Ağırlık bazlı ürünler için dinamik ödeme işlemlerini yöneten servis arayüzü
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// NEDEN BU YAPIYI SEÇTİK?
// 1. Separation of Concerns - Ağırlık bazlı ödeme mantığı ayrı bir katmanda
// 2. Strategy Pattern - Farklı ödeme yöntemleri (kart, nakit) için uyumlu
// 3. Testability - Mock'lanabilir interface
// 4. Extensibility - Yeni ödeme türleri kolayca eklenebilir
// ═══════════════════════════════════════════════════════════════════════════════════════════════

using System;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Entities.Enums;

namespace ECommerce.Core.Interfaces
{
    // ═══════════════════════════════════════════════════════════════════════════════
    // ÖN PROVİZYON (PRE-AUTH) SONUÇ DTO
    // Kart üzerinde tutar bloke etme işleminin sonucu
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ön provizyon (Pre-Authorization) işlem sonucu
    /// Ağırlık bazlı ürünlerde tahmini tutar üzerinden bloke yapılır
    /// </summary>
    public class PreAuthorizationResult
    {
        /// <summary>İşlem başarılı mı?</summary>
        public bool IsSuccess { get; set; }

        /// <summary>Hata mesajı (başarısız ise)</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>Hata kodu (başarısız ise)</summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// HostLogKey - POSNET işlem referansı
        /// Finansallaştırma ve iade için bu değer kullanılır
        /// </summary>
        public string? HostLogKey { get; set; }

        /// <summary>
        /// Yetkilendirme kodu
        /// Banka tarafından verilen onay kodu
        /// </summary>
        public string? AuthCode { get; set; }

        /// <summary>Bloke edilen tutar (tahmini tutar)</summary>
        public decimal BlockedAmount { get; set; }

        /// <summary>Sipariş ID</summary>
        public int OrderId { get; set; }

        /// <summary>İşlem tarihi</summary>
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        /// <summary>İşlem süresi (ms)</summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>Provizyon geçerlilik süresi (varsayılan 48 saat)</summary>
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(48);

        /// <summary>Başarılı sonuç factory</summary>
        public static PreAuthorizationResult Success(int orderId, decimal amount, string hostLogKey, string? authCode = null)
        {
            return new PreAuthorizationResult
            {
                IsSuccess = true,
                OrderId = orderId,
                BlockedAmount = amount,
                HostLogKey = hostLogKey,
                AuthCode = authCode,
                TransactionDate = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(48)
            };
        }

        /// <summary>Başarısız sonuç factory</summary>
        public static PreAuthorizationResult Failure(int orderId, string errorMessage, string? errorCode = null)
        {
            return new PreAuthorizationResult
            {
                IsSuccess = false,
                OrderId = orderId,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode,
                TransactionDate = DateTime.UtcNow
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // KESİN ÇEKİM (POST-AUTH) SONUÇ DTO
    // Bloke tutarın kesin çekime dönüştürülmesi
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Kesin çekim (Post-Authorization/Capture) işlem sonucu
    /// Ağırlık bazlı ürünlerde gerçek tutar üzerinden çekim yapılır
    /// </summary>
    public class PostAuthorizationResult
    {
        /// <summary>İşlem başarılı mı?</summary>
        public bool IsSuccess { get; set; }

        /// <summary>Hata mesajı (başarısız ise)</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>Hata kodu (başarısız ise)</summary>
        public string? ErrorCode { get; set; }

        /// <summary>HostLogKey - POSNET işlem referansı</summary>
        public string? HostLogKey { get; set; }

        /// <summary>Yetkilendirme kodu</summary>
        public string? AuthCode { get; set; }

        /// <summary>Önceden bloke edilen tutar</summary>
        public decimal OriginalBlockedAmount { get; set; }

        /// <summary>Kesin çekilen tutar (gerçek tutar)</summary>
        public decimal CapturedAmount { get; set; }

        /// <summary>Fark tutarı (pozitif = müşteriye iade, negatif = ek tahsilat)</summary>
        public decimal DifferenceAmount { get; set; }

        /// <summary>İade yapıldı mı? (fark pozitif ise)</summary>
        public bool RefundProcessed { get; set; }

        /// <summary>İade tutarı</summary>
        public decimal RefundedAmount { get; set; }

        /// <summary>Sipariş ID</summary>
        public int OrderId { get; set; }

        /// <summary>İşlem tarihi</summary>
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        /// <summary>İşlem süresi (ms)</summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>Başarılı sonuç factory</summary>
        public static PostAuthorizationResult Success(int orderId, decimal originalAmount, decimal capturedAmount)
        {
            return new PostAuthorizationResult
            {
                IsSuccess = true,
                OrderId = orderId,
                OriginalBlockedAmount = originalAmount,
                CapturedAmount = capturedAmount,
                DifferenceAmount = originalAmount - capturedAmount,
                TransactionDate = DateTime.UtcNow
            };
        }

        /// <summary>Başarısız sonuç factory</summary>
        public static PostAuthorizationResult Failure(int orderId, string errorMessage, string? errorCode = null)
        {
            return new PostAuthorizationResult
            {
                IsSuccess = false,
                OrderId = orderId,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode,
                TransactionDate = DateTime.UtcNow
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // KISMİ İADE SONUÇ DTO
    // Fazla çekilen tutarın iadesi
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Kısmi iade (Partial Refund) işlem sonucu
    /// Ağırlık farkı pozitif olduğunda müşteriye iade yapılır
    /// </summary>
    public class PartialRefundResult
    {
        /// <summary>İşlem başarılı mı?</summary>
        public bool IsSuccess { get; set; }

        /// <summary>Hata mesajı (başarısız ise)</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>Hata kodu (başarısız ise)</summary>
        public string? ErrorCode { get; set; }

        /// <summary>İade HostLogKey</summary>
        public string? RefundHostLogKey { get; set; }

        /// <summary>İade tutarı</summary>
        public decimal RefundedAmount { get; set; }

        /// <summary>Orijinal işlem tutarı</summary>
        public decimal OriginalAmount { get; set; }

        /// <summary>Sipariş ID</summary>
        public int OrderId { get; set; }

        /// <summary>İşlem tarihi</summary>
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        /// <summary>İşlem süresi (ms)</summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>Başarılı sonuç factory</summary>
        public static PartialRefundResult Success(int orderId, decimal refundedAmount, decimal originalAmount, string? refundHostLogKey = null)
        {
            return new PartialRefundResult
            {
                IsSuccess = true,
                OrderId = orderId,
                RefundedAmount = refundedAmount,
                OriginalAmount = originalAmount,
                RefundHostLogKey = refundHostLogKey,
                TransactionDate = DateTime.UtcNow
            };
        }

        /// <summary>Başarısız sonuç factory</summary>
        public static PartialRefundResult Failure(int orderId, string errorMessage, string? errorCode = null)
        {
            return new PartialRefundResult
            {
                IsSuccess = false,
                OrderId = orderId,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode,
                TransactionDate = DateTime.UtcNow
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // KAPIDA ÖDEME FARK HESAPLAMA SONUCU
    // Nakit ödemelerde ağırlık farkı hesabı
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Kapıda ödeme fark hesaplama sonucu
    /// Nakit ödemelerde müşteriden alınacak/verilecek fark tutarı
    /// </summary>
    public class CashPaymentDifferenceResult
    {
        /// <summary>Hesaplama başarılı mı?</summary>
        public bool IsSuccess { get; set; }

        /// <summary>Hata mesajı (başarısız ise)</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>Sipariş ID</summary>
        public int OrderId { get; set; }

        /// <summary>Tahmini tutar (sipariş anındaki)</summary>
        public decimal EstimatedAmount { get; set; }

        /// <summary>Gerçek tutar (tartım sonrası)</summary>
        public decimal ActualAmount { get; set; }

        /// <summary>
        /// Fark tutarı
        /// Pozitif: Müşteriye para üstü verilecek
        /// Negatif: Müşteriden ek ödeme alınacak
        /// </summary>
        public decimal DifferenceAmount { get; set; }

        /// <summary>Fark yönü (müşteriye/müşteriden)</summary>
        public PaymentDifferenceDirection Direction { get; set; }

        /// <summary>Fark yüzdesi</summary>
        public decimal DifferencePercent { get; set; }

        /// <summary>
        /// Admin onayı gerekiyor mu?
        /// Yüksek fark durumlarında true
        /// </summary>
        public bool RequiresAdminApproval { get; set; }

        /// <summary>Fark açıklaması (kullanıcı dostu mesaj)</summary>
        public string DifferenceDescription { get; set; } = string.Empty;
    }

    /// <summary>
    /// Ödeme farkı yönü
    /// </summary>
    public enum PaymentDifferenceDirection
    {
        /// <summary>Fark yok</summary>
        NoDifference = 0,

        /// <summary>Müşteriye iade/para üstü</summary>
        RefundToCustomer = 1,

        /// <summary>Müşteriden ek tahsilat</summary>
        ChargeFromCustomer = 2
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // ANA INTERFACE
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ağırlık Bazlı Dinamik Ödeme Servisi Interface
    /// 
    /// Bu servis, ağırlık bazlı satılan ürünler (meyve, sebze, et vb.) için
    /// dinamik ödeme işlemlerini yönetir.
    /// 
    /// AKIŞ:
    /// 1. Sipariş verildiğinde tahmini tutar üzerinden Pre-Auth (bloke)
    /// 2. Kurye teslimat öncesi gerçek ağırlığı girer
    /// 3. Post-Auth ile gerçek tutar çekilir
    /// 4. Fark varsa kısmi iade veya ek tahsilat yapılır
    /// 
    /// KAPIDA ÖDEME (NAKİT) İÇİN:
    /// - Pre-Auth yapılmaz
    /// - Fark hesaplanır ve kurye müşteriden tahsil eder/verir
    /// </summary>
    public interface IWeightBasedPaymentService
    {
        // ═══════════════════════════════════════════════════════════════════════
        // PRE-AUTHORIZATION (ÖN PROVİZYON)
        // Tahmini tutar üzerinden kart bloke etme
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Ön provizyon işlemi başlatır
        /// 
        /// KULLANIM SENARYOSU:
        /// Müşteri sipariş verdiğinde, tahmini tutar (+ güvenlik marjı) 
        /// karttan bloke edilir. Gerçek çekim teslimat sonrası yapılır.
        /// 
        /// GÜVENLİK MARJI:
        /// Tahmini tutarın %10-20'si kadar fazlası bloke edilir ki
        /// gerçek tutar fazla geldiğinde karşılanabilsin.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="estimatedAmount">Tahmini tutar</param>
        /// <param name="securityMarginPercent">Güvenlik marjı yüzdesi (varsayılan %15)</param>
        /// <param name="hostLogKey">Mevcut HostLogKey (varsa, 3D Secure sonrası)</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>Ön provizyon sonucu</returns>
        Task<PreAuthorizationResult> ProcessPreAuthorizationAsync(
            int orderId,
            decimal estimatedAmount,
            decimal securityMarginPercent = 15,
            string? hostLogKey = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 3D Secure ile ön provizyon başlatır
        /// 
        /// NOT: Bu metod 3D Secure akışını başlatır, callback sonrası
        /// CompletePreAuthorizationAsync çağrılmalıdır.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="estimatedAmount">Tahmini tutar</param>
        /// <param name="cardNumber">Kart numarası</param>
        /// <param name="expireDate">Son kullanma tarihi (MMYY formatı)</param>
        /// <param name="cvv">CVV kodu</param>
        /// <param name="securityMarginPercent">Güvenlik marjı yüzdesi</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>3D Secure yönlendirme bilgileri</returns>
        Task<PreAuthorizationResult> InitiatePreAuthorizationWith3DSecureAsync(
            int orderId,
            decimal estimatedAmount,
            string cardNumber,
            string expireDate,
            string cvv,
            decimal securityMarginPercent = 15,
            CancellationToken cancellationToken = default);

        // ═══════════════════════════════════════════════════════════════════════
        // POST-AUTHORIZATION (KESİN ÇEKİM)
        // Gerçek tutar üzerinden finansallaştırma
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Kesin çekim işlemi yapar
        /// 
        /// KULLANIM SENARYOSU:
        /// Kurye ürünleri tartıp gerçek ağırlığı girdikten sonra,
        /// bu metod çağrılarak gerçek tutar karttan çekilir.
        /// 
        /// FARK YÖNETİMİ:
        /// - Gerçek tutar &lt; Bloke tutar: Fark serbest bırakılır
        /// - Gerçek tutar &gt; Bloke tutar: Ek provizyon gerekir (admin onayı)
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="actualAmount">Gerçek tutar (tartım sonrası)</param>
        /// <param name="hostLogKey">Ön provizyon HostLogKey</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>Kesin çekim sonucu</returns>
        Task<PostAuthorizationResult> ProcessPostAuthorizationAsync(
            int orderId,
            decimal actualAmount,
            string hostLogKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Fark ödemesi işler
        /// 
        /// DURUM 1 (Gerçek > Tahmini):
        /// Müşteriden ek ödeme alınır. Admin onayı gerekebilir.
        /// 
        /// DURUM 2 (Gerçek < Tahmini):
        /// Müşteriye kısmi iade yapılır.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="differenceAmount">Fark tutarı (pozitif veya negatif)</param>
        /// <param name="hostLogKey">Orijinal işlem HostLogKey</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>Fark işleme sonucu</returns>
        Task<PostAuthorizationResult> ProcessDifferencePaymentAsync(
            int orderId,
            decimal differenceAmount,
            string hostLogKey,
            CancellationToken cancellationToken = default);

        // ═══════════════════════════════════════════════════════════════════════
        // PARTIAL REFUND (KISMİ İADE)
        // Fazla çekilen tutarın iadesi
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Kısmi iade işlemi yapar
        /// 
        /// KULLANIM SENARYOSU:
        /// Gerçek tutar, tahmini tutardan düşük olduğunda
        /// fark müşteriye iade edilir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="refundAmount">İade tutarı</param>
        /// <param name="hostLogKey">Orijinal işlem HostLogKey</param>
        /// <param name="reason">İade nedeni</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>Kısmi iade sonucu</returns>
        Task<PartialRefundResult> ProcessPartialRefundAsync(
            int orderId,
            decimal refundAmount,
            string hostLogKey,
            string? reason = null,
            CancellationToken cancellationToken = default);

        // ═══════════════════════════════════════════════════════════════════════
        // KAPIDA ÖDEME (NAKİT)
        // Nakit ödemelerde fark hesaplama
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Kapıda nakit ödeme için fark hesaplar
        /// 
        /// KULLANIM SENARYOSU:
        /// Nakit ödemelerde kart işlemi olmadığından,
        /// fark kurye tarafından müşteriden tahsil edilir veya verilir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="estimatedAmount">Tahmini tutar</param>
        /// <param name="actualAmount">Gerçek tutar</param>
        /// <param name="adminApprovalThresholdPercent">Admin onayı için fark eşiği (%)</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>Fark hesaplama sonucu</returns>
        Task<CashPaymentDifferenceResult> CalculateCashPaymentDifferenceAsync(
            int orderId,
            decimal estimatedAmount,
            decimal actualAmount,
            decimal adminApprovalThresholdPercent = 20,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Kapıda ödeme fark ödemesini tamamlar
        /// 
        /// KURYE ARAYÜZÜNDEN:
        /// Kurye farkı müşteriden tahsil ettikten sonra
        /// bu metod çağrılarak işlem kaydedilir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="differenceAmount">Tahsil edilen/verilen fark</param>
        /// <param name="direction">Fark yönü</param>
        /// <param name="courierNotes">Kurye notları</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>İşlem başarılı mı?</returns>
        Task<bool> CompleteCashPaymentDifferenceAsync(
            int orderId,
            decimal differenceAmount,
            PaymentDifferenceDirection direction,
            string? courierNotes = null,
            CancellationToken cancellationToken = default);

        // ═══════════════════════════════════════════════════════════════════════
        // YARDIMCI METODLAR
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Siparişin ağırlık bazlı ödeme durumunu kontrol eder
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>Ödeme durumu bilgisi</returns>
        Task<WeightBasedPaymentStatus> GetPaymentStatusAsync(
            int orderId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Provizyon süresinin dolup dolmadığını kontrol eder
        /// POSNET'te provizyon süresi genelde 7 gün, biz 48 saat kabul ediyoruz
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>Provizyon geçerli mi?</returns>
        Task<bool> IsPreAuthorizationValidAsync(
            int orderId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Süresi dolan provizyonları iptal eder
        /// Scheduled job olarak çalıştırılmalı
        /// </summary>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>İptal edilen işlem sayısı</returns>
        Task<int> CancelExpiredPreAuthorizationsAsync(
            CancellationToken cancellationToken = default);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // ÖDEME DURUMU DTO
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ağırlık bazlı ödeme durumu bilgisi
    /// </summary>
    public class WeightBasedPaymentStatus
    {
        /// <summary>Sipariş ID</summary>
        public int OrderId { get; set; }

        /// <summary>Ödeme yöntemi</summary>
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>Kart ödemesi mi?</summary>
        public bool IsCardPayment { get; set; }

        /// <summary>Ön provizyon alındı mı?</summary>
        public bool PreAuthorizationCompleted { get; set; }

        /// <summary>Ön provizyon tutarı</summary>
        public decimal PreAuthorizationAmount { get; set; }

        /// <summary>Ön provizyon tarihi</summary>
        public DateTime? PreAuthorizationDate { get; set; }

        /// <summary>Ön provizyon HostLogKey</summary>
        public string? PreAuthorizationHostLogKey { get; set; }

        /// <summary>Provizyon süresi doldu mu?</summary>
        public bool PreAuthorizationExpired { get; set; }

        /// <summary>Kesin çekim yapıldı mı?</summary>
        public bool PostAuthorizationCompleted { get; set; }

        /// <summary>Kesin çekim tutarı</summary>
        public decimal PostAuthorizationAmount { get; set; }

        /// <summary>Kesin çekim tarihi</summary>
        public DateTime? PostAuthorizationDate { get; set; }

        /// <summary>Fark işlendi mi?</summary>
        public bool DifferenceProcessed { get; set; }

        /// <summary>Fark tutarı</summary>
        public decimal DifferenceAmount { get; set; }

        /// <summary>Fark yönü</summary>
        public PaymentDifferenceDirection DifferenceDirection { get; set; }

        /// <summary>Admin onayı bekleniyor mu?</summary>
        public bool PendingAdminApproval { get; set; }

        /// <summary>İşlem tamamlandı mı?</summary>
        public bool IsCompleted { get; set; }

        /// <summary>Durum açıklaması</summary>
        public string StatusDescription { get; set; } = string.Empty;
    }
}
