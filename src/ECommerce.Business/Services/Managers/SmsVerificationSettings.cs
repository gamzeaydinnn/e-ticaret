namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// SMS doğrulama ayarları
    /// </summary>
    public class SmsVerificationSettings
    {
        /// <summary>OTP kodunun geçerlilik süresi (saniye). Varsayılan: 180 (3 dk)</summary>
        public int ExpirationSeconds { get; set; } = 180;

        /// <summary>Aynı numaraya tekrar OTP göndermek için bekleme süresi (saniye)</summary>
        public int ResendCooldownSeconds { get; set; } = 60;

        /// <summary>Günlük maksimum OTP gönderim sayısı</summary>
        public int DailyMaxOtpCount { get; set; } = 5;

        /// <summary>Saatlik maksimum OTP gönderim sayısı</summary>
        public int HourlyMaxOtpCount { get; set; } = 3;

        /// <summary>Maksimum yanlış deneme sayısı</summary>
        public int MaxWrongAttempts { get; set; } = 3;

        /// <summary>OTP kod uzunluğu</summary>
        public int CodeLength { get; set; } = 6;

        /// <summary>Uygulama adı (SMS'de görünecek)</summary>
        public string AppName { get; set; } = "E-Ticaret";
    }
}
