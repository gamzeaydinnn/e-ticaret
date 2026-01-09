namespace ECommerce.API.DTOs.Sms
{
    /// <summary>
    /// SMS doğrulama API yanıt DTO.
    /// Tüm SMS işlemlerinde standart yanıt formatı olarak kullanılır.
    /// </summary>
    public class SmsVerificationResponseDto
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Kullanıcıya gösterilecek mesaj
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Hata kodu (başarısız işlemlerde).
        /// Örnek: "RATE_LIMITED", "INVALID_CODE", "CODE_EXPIRED", "MAX_ATTEMPTS"
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Doğrulama kodunun geçerlilik süresi (saniye).
        /// Sadece başarılı gönderim işlemlerinde dolu döner.
        /// </summary>
        public int? ExpiresInSeconds { get; set; }

        /// <summary>
        /// Kalan deneme hakkı.
        /// Yanlış kod girişlerinde döner.
        /// </summary>
        public int? RemainingAttempts { get; set; }

        /// <summary>
        /// Tekrar gönderim için beklenecek süre (saniye).
        /// Rate limit veya cooldown durumlarında döner.
        /// </summary>
        public int? RetryAfterSeconds { get; set; }

        /// <summary>
        /// Kalan günlük SMS hakkı.
        /// Bilgilendirme amaçlıdır.
        /// </summary>
        public int? RemainingDailyCount { get; set; }

        /// <summary>
        /// Doğrulama ID'si (isteğe bağlı, takip için).
        /// </summary>
        public int? VerificationId { get; set; }

        #region Factory Methods

        /// <summary>
        /// Başarılı OTP gönderim yanıtı oluşturur.
        /// </summary>
        public static SmsVerificationResponseDto SuccessSend(int expiresIn, int? dailyRemaining = null)
        {
            return new SmsVerificationResponseDto
            {
                Success = true,
                Message = "Doğrulama kodu telefonunuza gönderildi.",
                ExpiresInSeconds = expiresIn,
                RemainingDailyCount = dailyRemaining
            };
        }

        /// <summary>
        /// Başarılı doğrulama yanıtı oluşturur.
        /// </summary>
        public static SmsVerificationResponseDto SuccessVerify()
        {
            return new SmsVerificationResponseDto
            {
                Success = true,
                Message = "Telefon numaranız başarıyla doğrulandı."
            };
        }

        /// <summary>
        /// Rate limit hatası yanıtı oluşturur.
        /// </summary>
        public static SmsVerificationResponseDto RateLimited(int retryAfterSeconds, string? reason = null)
        {
            return new SmsVerificationResponseDto
            {
                Success = false,
                Message = reason ?? $"Çok fazla istek gönderildi. Lütfen {retryAfterSeconds} saniye bekleyin.",
                ErrorCode = "RATE_LIMITED",
                RetryAfterSeconds = retryAfterSeconds
            };
        }

        /// <summary>
        /// Yanlış kod hatası yanıtı oluşturur.
        /// </summary>
        public static SmsVerificationResponseDto InvalidCode(int remainingAttempts)
        {
            return new SmsVerificationResponseDto
            {
                Success = false,
                Message = $"Girdiğiniz kod hatalı. {remainingAttempts} deneme hakkınız kaldı.",
                ErrorCode = "INVALID_CODE",
                RemainingAttempts = remainingAttempts
            };
        }

        /// <summary>
        /// Süresi dolmuş kod hatası yanıtı oluşturur.
        /// </summary>
        public static SmsVerificationResponseDto ExpiredCode()
        {
            return new SmsVerificationResponseDto
            {
                Success = false,
                Message = "Kodun süresi doldu. Lütfen yeni kod isteyin.",
                ErrorCode = "CODE_EXPIRED"
            };
        }

        /// <summary>
        /// Maksimum deneme aşıldı hatası yanıtı oluşturur.
        /// </summary>
        public static SmsVerificationResponseDto MaxAttemptsExceeded()
        {
            return new SmsVerificationResponseDto
            {
                Success = false,
                Message = "Maksimum deneme sayısına ulaştınız. Lütfen yeni kod isteyin.",
                ErrorCode = "MAX_ATTEMPTS"
            };
        }

        /// <summary>
        /// SMS gönderim hatası yanıtı oluşturur.
        /// </summary>
        public static SmsVerificationResponseDto SmsSendFailed(string? errorDetails = null)
        {
            return new SmsVerificationResponseDto
            {
                Success = false,
                Message = "SMS gönderilemedi. Lütfen daha sonra tekrar deneyin.",
                ErrorCode = "SMS_FAILED"
            };
        }

        /// <summary>
        /// Numara bloklu hatası yanıtı oluşturur.
        /// </summary>
        public static SmsVerificationResponseDto PhoneBlocked(DateTime until)
        {
            return new SmsVerificationResponseDto
            {
                Success = false,
                Message = $"Bu numara geçici olarak bloklandı. {until:HH:mm} tarihine kadar bekleyin.",
                ErrorCode = "PHONE_BLOCKED"
            };
        }

        /// <summary>
        /// Genel hata yanıtı oluşturur.
        /// </summary>
        public static SmsVerificationResponseDto Error(string message, string errorCode = "ERROR")
        {
            return new SmsVerificationResponseDto
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
        }

        #endregion
    }
}
