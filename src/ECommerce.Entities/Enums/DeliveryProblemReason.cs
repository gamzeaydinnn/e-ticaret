namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// Teslimat problemi sebepleri enum'ı.
    /// Kurye teslimat yapamadığında seçebileceği sebepler.
    /// 
    /// KULLANIM:
    /// - Kurye "Sorun Var" butonuna bastığında bu sebeplerden birini seçer
    /// - Admin panelinde problem detayı görüntülenir
    /// - Raporlama ve analiz için kategorize edilir
    /// 
    /// ÖNCELİK SIRALASI:
    /// - CustomerNotAvailable: Müşteri evde değil - Yeniden deneme planlanabilir
    /// - AddressNotFound: Adres bulunamadı - Admin müdahalesi gerekir
    /// - AccessDenied: Bina/site girişi yapılamıyor - Müşteriyle iletişim
    /// - RefusedByCustomer: Müşteri teslim almak istemiyor - İade süreci
    /// - DamagedPackage: Paket hasarlı - İade/yeni gönderim
    /// - PaymentIssue: Ödeme sorunu - Admin müdahalesi
    /// - WeatherConditions: Hava koşulları - Erteleme
    /// - VehicleBreakdown: Araç arızası - Yeniden atama
    /// - Other: Diğer - Manuel açıklama gerektirir
    /// </summary>
    public enum DeliveryProblemReason
    {
        /// <summary>
        /// Müşteri evde/işyerinde değil
        /// Tekrar teslimat denenebilir
        /// </summary>
        CustomerNotAvailable = 1,

        /// <summary>
        /// Adres bulunamadı veya hatalı
        /// Müşteriyle iletişim kurulmalı
        /// </summary>
        AddressNotFound = 2,

        /// <summary>
        /// Bina/site girişi yapılamıyor
        /// Kapı kodu yok, güvenlik izin vermiyor vs.
        /// </summary>
        AccessDenied = 3,

        /// <summary>
        /// Müşteri siparişi teslim almak istemiyor
        /// İade süreci başlatılmalı
        /// </summary>
        RefusedByCustomer = 4,

        /// <summary>
        /// Paket hasarlı veya açık
        /// Kalite kontrol + iade/değişim
        /// </summary>
        DamagedPackage = 5,

        /// <summary>
        /// Kapıda ödeme alınamadı
        /// Müşteri nakit/kart sorunu
        /// </summary>
        PaymentIssue = 6,

        /// <summary>
        /// Kötü hava koşulları nedeniyle erteleme
        /// Yağmur, kar, fırtına vs.
        /// </summary>
        WeatherConditions = 7,

        /// <summary>
        /// Kurye araç arızası
        /// Başka kuryeye atanmalı
        /// </summary>
        VehicleBreakdown = 8,

        /// <summary>
        /// Müşteri telefona cevap vermiyor
        /// Birden fazla arama yapıldı
        /// </summary>
        CustomerUnreachable = 9,

        /// <summary>
        /// Apartman/site yolu kapalı
        /// İnşaat, resmi engel vs.
        /// </summary>
        RoadBlocked = 10,

        /// <summary>
        /// Diğer sebepler
        /// Manuel açıklama girilmeli
        /// </summary>
        Other = 99
    }

    /// <summary>
    /// DeliveryProblemReason için extension metotları.
    /// </summary>
    public static class DeliveryProblemReasonExtensions
    {
        /// <summary>
        /// Problem sebebinin Türkçe açıklamasını döner.
        /// </summary>
        public static string GetDescription(this DeliveryProblemReason reason)
        {
            return reason switch
            {
                DeliveryProblemReason.CustomerNotAvailable => "Müşteri evde/işyerinde değil",
                DeliveryProblemReason.AddressNotFound => "Adres bulunamadı veya hatalı",
                DeliveryProblemReason.AccessDenied => "Bina/site girişi yapılamıyor",
                DeliveryProblemReason.RefusedByCustomer => "Müşteri teslim almak istemiyor",
                DeliveryProblemReason.DamagedPackage => "Paket hasarlı veya açık",
                DeliveryProblemReason.PaymentIssue => "Kapıda ödeme alınamadı",
                DeliveryProblemReason.WeatherConditions => "Hava koşulları nedeniyle erteleme",
                DeliveryProblemReason.VehicleBreakdown => "Araç arızası",
                DeliveryProblemReason.CustomerUnreachable => "Müşteriye ulaşılamıyor",
                DeliveryProblemReason.RoadBlocked => "Yol kapalı/erişim sorunu",
                DeliveryProblemReason.Other => "Diğer",
                _ => "Bilinmeyen sebep"
            };
        }

        /// <summary>
        /// Problem sebebinin yeniden deneme gerektirip gerektirmediğini kontrol eder.
        /// </summary>
        public static bool IsRetryable(this DeliveryProblemReason reason)
        {
            return reason switch
            {
                DeliveryProblemReason.CustomerNotAvailable => true,
                DeliveryProblemReason.AccessDenied => true,
                DeliveryProblemReason.WeatherConditions => true,
                DeliveryProblemReason.VehicleBreakdown => true,
                DeliveryProblemReason.CustomerUnreachable => true,
                DeliveryProblemReason.RoadBlocked => true,
                _ => false // Diğerleri manual müdahale gerektirir
            };
        }

        /// <summary>
        /// Problem sebebinin admin müdahalesi gerektirip gerektirmediğini kontrol eder.
        /// </summary>
        public static bool RequiresAdminAction(this DeliveryProblemReason reason)
        {
            return reason switch
            {
                DeliveryProblemReason.AddressNotFound => true,
                DeliveryProblemReason.RefusedByCustomer => true,
                DeliveryProblemReason.DamagedPackage => true,
                DeliveryProblemReason.PaymentIssue => true,
                DeliveryProblemReason.Other => true,
                _ => false
            };
        }
    }
}
