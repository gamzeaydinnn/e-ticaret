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


    
  
}
