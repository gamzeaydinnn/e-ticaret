namespace ECommerce.API.DTOs.Sms
{
    /// <summary>
    /// SMS doğrulama durumu yanıt DTO.
    /// Belirli bir telefon numarası için aktif doğrulama durumunu döndürür.
    /// </summary>
    public class SmsStatusResponseDto
    {
        /// <summary>
        /// Aktif doğrulama kaydı var mı?
        /// </summary>
        public bool HasActiveVerification { get; set; }

        /// <summary>
        /// Mevcut doğrulama durumu.
        /// Değerler: "Pending", "Verified", "Expired", "MaxAttemptsExceeded", "Cancelled"
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Kodun geçerlilik süresi (kalan saniye).
        /// 0 veya negatifse süre dolmuş demektir.
        /// </summary>
        public int? RemainingSeconds { get; set; }

        /// <summary>
        /// Kalan deneme hakkı.
        /// </summary>
        public int? RemainingAttempts { get; set; }

        /// <summary>
        /// Tekrar OTP göndermek için beklenecek süre (cooldown).
        /// 0 ise hemen gönderilebilir.
        /// </summary>
        public int? ResendAfterSeconds { get; set; }

        /// <summary>
        /// OTP gönderilebilir mi? 
        /// Cooldown süresi dolmuşsa ve günlük limit aşılmamışsa true döner.
        /// </summary>
        public bool CanResend { get; set; }

        /// <summary>
        /// Kalan günlük SMS hakkı.
        /// </summary>
        public int? RemainingDailyCount { get; set; }

        /// <summary>
        /// Bu numara bloklu mu?
        /// </summary>
        public bool IsBlocked { get; set; }

        /// <summary>
        /// Blokaj bitiş zamanı (UTC).
        /// </summary>
        public DateTime? BlockedUntil { get; set; }

        #region Factory Methods

        /// <summary>
        /// Aktif doğrulama yok durumu.
        /// </summary>
        public static SmsStatusResponseDto NoActiveVerification()
        {
            return new SmsStatusResponseDto
            {
                HasActiveVerification = false,
                Status = "None",
                CanResend = true
            };
        }

        /// <summary>
        /// Aktif doğrulama var durumu.
        /// </summary>
        public static SmsStatusResponseDto Active(
            string status, 
            int remainingSeconds, 
            int remainingAttempts,
            int resendAfterSeconds,
            int remainingDailyCount)
        {
            return new SmsStatusResponseDto
            {
                HasActiveVerification = true,
                Status = status,
                RemainingSeconds = remainingSeconds,
                RemainingAttempts = remainingAttempts,
                ResendAfterSeconds = resendAfterSeconds,
                CanResend = resendAfterSeconds <= 0 && remainingDailyCount > 0,
                RemainingDailyCount = remainingDailyCount
            };
        }

        /// <summary>
        /// Bloklu numara durumu.
        /// </summary>
        public static SmsStatusResponseDto Blocked(DateTime until)
        {
            return new SmsStatusResponseDto
            {
                HasActiveVerification = false,
                IsBlocked = true,
                BlockedUntil = until,
                CanResend = false
            };
        }

        #endregion
    }
}
