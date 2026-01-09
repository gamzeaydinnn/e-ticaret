using System;
using System.Threading.Tasks;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// SMS doğrulama servisi interface.
    /// SOLID: Interface Segregation - SMS doğrulama işlemleri için ayrı interface
    /// </summary>
    public interface ISmsVerificationService
    {
        /// <summary>
        /// OTP kodu oluşturur ve SMS gönderir.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası (5xxxxxxxxx formatında)</param>
        /// <param name="purpose">Doğrulama amacı</param>
        /// <param name="ipAddress">İstek yapan IP adresi</param>
        /// <param name="userAgent">Tarayıcı/Cihaz bilgisi</param>
        /// <param name="userId">Kullanıcı ID (varsa)</param>
        /// <returns>Gönderim sonucu</returns>
        Task<SmsVerificationResult> SendVerificationCodeAsync(
            string phoneNumber,
            SmsVerificationPurpose purpose,
            string? ipAddress = null,
            string? userAgent = null,
            int? userId = null);

        /// <summary>
        /// OTP kodunu doğrular.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası</param>
        /// <param name="code">Kullanıcının girdiği kod</param>
        /// <param name="purpose">Doğrulama amacı</param>
        /// <param name="ipAddress">İstek yapan IP adresi</param>
        /// <returns>Doğrulama sonucu</returns>
        Task<SmsVerificationResult> VerifyCodeAsync(
            string phoneNumber,
            string code,
            SmsVerificationPurpose purpose,
            string? ipAddress = null);

        /// <summary>
        /// Telefon numarasının doğrulanıp doğrulanmadığını kontrol eder.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası</param>
        /// <param name="purpose">Doğrulama amacı</param>
        /// <returns>Doğrulanmış mı?</returns>
        Task<bool> IsPhoneVerifiedAsync(string phoneNumber, SmsVerificationPurpose purpose);

        /// <summary>
        /// Belirli bir numara için OTP gönderilebilir mi kontrol eder.
        /// Rate limiting ve cooldown kontrolü yapar.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası</param>
        /// <param name="ipAddress">IP adresi</param>
        /// <returns>Rate limit bilgisi</returns>
        Task<RateLimitCheckResult> CanSendVerificationAsync(string phoneNumber, string? ipAddress = null);

        /// <summary>
        /// Mevcut aktif doğrulama kaydının durumunu döndürür.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası</param>
        /// <param name="purpose">Doğrulama amacı</param>
        /// <returns>Durum bilgisi</returns>
        Task<VerificationStatusResult> GetVerificationStatusAsync(
            string phoneNumber, 
            SmsVerificationPurpose purpose);

        /// <summary>
        /// Süresi dolmuş kayıtları temizler.
        /// Background job tarafından çağrılır.
        /// </summary>
        /// <returns>Temizlenen kayıt sayısı</returns>
        Task<int> CleanupExpiredVerificationsAsync();
    }

    #region Result Models

    /// <summary>
    /// SMS doğrulama işlem sonucu
    /// </summary>
    public class SmsVerificationResult
    {
        /// <summary>İşlem başarılı mı?</summary>
        public bool Success { get; set; }

        /// <summary>Kullanıcıya gösterilecek mesaj</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Hata kodu (varsa)</summary>
        public string? ErrorCode { get; set; }

        /// <summary>Kod geçerlilik süresi (saniye)</summary>
        public int? ExpiresInSeconds { get; set; }

        /// <summary>Kalan deneme hakkı</summary>
        public int? RemainingAttempts { get; set; }

        /// <summary>Tekrar gönderim için beklenecek süre (saniye)</summary>
        public int? RetryAfterSeconds { get; set; }

        /// <summary>NetGSM Job ID (SMS takibi için)</summary>
        public string? JobId { get; set; }

        /// <summary>Doğrulama kaydı ID'si</summary>
        public int? VerificationId { get; set; }

        #region Factory Methods

        public static SmsVerificationResult SuccessSend(int expiresIn, string? jobId = null)
            => new()
            {
                Success = true,
                Message = "Doğrulama kodu telefonunuza gönderildi.",
                ExpiresInSeconds = expiresIn,
                JobId = jobId
            };

        public static SmsVerificationResult SuccessVerify()
            => new()
            {
                Success = true,
                Message = "Telefon numaranız başarıyla doğrulandı."
            };

        public static SmsVerificationResult RateLimited(int retryAfter)
            => new()
            {
                Success = false,
                Message = $"Çok fazla istek gönderildi. Lütfen {retryAfter} saniye bekleyin.",
                ErrorCode = "RATE_LIMITED",
                RetryAfterSeconds = retryAfter
            };

        public static SmsVerificationResult InvalidCode(int remaining)
            => new()
            {
                Success = false,
                Message = "Girdiğiniz kod hatalı.",
                ErrorCode = "INVALID_CODE",
                RemainingAttempts = remaining
            };

        public static SmsVerificationResult ExpiredCode()
            => new()
            {
                Success = false,
                Message = "Kodun süresi doldu. Lütfen yeni kod isteyin.",
                ErrorCode = "CODE_EXPIRED"
            };

        public static SmsVerificationResult MaxAttemptsExceeded()
            => new()
            {
                Success = false,
                Message = "Maksimum deneme sayısına ulaştınız. Lütfen yeni kod isteyin.",
                ErrorCode = "MAX_ATTEMPTS"
            };

        public static SmsVerificationResult SmsSendFailed(string error)
            => new()
            {
                Success = false,
                Message = "SMS gönderilemedi. Lütfen daha sonra tekrar deneyin.",
                ErrorCode = "SMS_FAILED"
            };

        public static SmsVerificationResult PhoneBlocked(DateTime until)
            => new()
            {
                Success = false,
                Message = $"Bu numara geçici olarak bloklandı. {until:HH:mm} tarihine kadar bekleyin.",
                ErrorCode = "PHONE_BLOCKED"
            };

        public static SmsVerificationResult Error(string message, string errorCode = "ERROR")
            => new()
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };

        #endregion
    }

    /// <summary>
    /// Rate limit kontrol sonucu
    /// </summary>
    public class RateLimitCheckResult
    {
        /// <summary>OTP gönderilebilir mi?</summary>
        public bool CanSend { get; set; }

        /// <summary>Gönderilememe nedeni</summary>
        public string? Reason { get; set; }

        /// <summary>Tekrar göndermek için beklenecek süre (saniye)</summary>
        public int RetryAfterSeconds { get; set; }

        /// <summary>Bugün kalan SMS hakkı</summary>
        public int RemainingDailyCount { get; set; }

        /// <summary>Numara bloklu mu?</summary>
        public bool IsBlocked { get; set; }

        /// <summary>Blokaj bitiş zamanı</summary>
        public DateTime? BlockedUntil { get; set; }
    }

    /// <summary>
    /// Doğrulama durum sonucu
    /// </summary>
    public class VerificationStatusResult
    {
        /// <summary>Aktif doğrulama var mı?</summary>
        public bool HasActiveVerification { get; set; }

        /// <summary>Doğrulama durumu</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Kalan süre (saniye)</summary>
        public int? RemainingSeconds { get; set; }

        /// <summary>Kalan deneme hakkı</summary>
        public int? RemainingAttempts { get; set; }

        /// <summary>Tekrar gönderim için beklenecek süre</summary>
        public int? ResendAfterSeconds { get; set; }
    }

    #endregion
}
