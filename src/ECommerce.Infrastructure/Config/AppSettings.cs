namespace ECommerce.Infrastructure.Config
{ //Email, ödeme, JWT gibi ayarlar tek bir yerde toplanır.
    public class AppSettings
    {
        // JWT ile ilgili ayarlar
        public string JwtSecret { get; set; } = string.Empty;
        public int JwtExpirationInMinutes { get; set; }

        // Email servisi ayarları
        public EmailSettings EmailSettings { get; set; } = new EmailSettings();

        // Ödeme ayarları
        public PaymentSettings PaymentSettings { get; set; } = new PaymentSettings();

        // Genel uygulama ayarları
        public string BaseUrl { get; set; } = string.Empty;
    }

    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; } = string.Empty;
        public string SmtpPass { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
    }

  
}
