// ==========================================================================
// IAddressValidationService.cs - Adres Doğrulama Servis Interface'i
// ==========================================================================
// Bu interface, teslimat adreslerinin doğrulanması ve geocoding işlemlerini tanımlar.
// Eksik veya hatalı adresleri tespit eder, koordinat bilgisi ekler.
//
// Kullanım Alanları:
// - Teslimat görevi oluşturulurken adres kontrolü
// - Geocoding ile koordinat ekleme (navigasyon ve ETA için)
// - Adres standardizasyonu (il/ilçe normalizasyonu)
// ==========================================================================

using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Adres doğrulama ve geocoding servis interface'i.
    /// </summary>
    public interface IAddressValidationService
    {
        /// <summary>
        /// Adresi doğrular ve varsa koordinat bilgisi ekler.
        /// </summary>
        /// <param name="request">Doğrulanacak adres bilgileri</param>
        /// <returns>Doğrulama sonucu ve koordinatlar</returns>
        Task<AddressValidationResult> ValidateAndGeocodeAsync(AddressValidationRequest request);

        /// <summary>
        /// Sadece adres formatını kontrol eder (geocoding yapmadan).
        /// </summary>
        /// <param name="address">Kontrol edilecek adres</param>
        /// <returns>Geçerli mi?</returns>
        Task<bool> IsValidAddressFormatAsync(string address);

        /// <summary>
        /// Koordinatlardan adres bilgisi alır (reverse geocoding).
        /// </summary>
        /// <param name="latitude">Enlem</param>
        /// <param name="longitude">Boylam</param>
        /// <returns>Adres bilgisi</returns>
        Task<ReverseGeocodeResult?> ReverseGeocodeAsync(double latitude, double longitude);

        /// <summary>
        /// İl/ilçe isimlerini normalize eder.
        /// </summary>
        /// <param name="city">İl adı</param>
        /// <param name="district">İlçe adı</param>
        /// <returns>Normalize edilmiş isimler</returns>
        Task<(string normalizedCity, string normalizedDistrict)> NormalizeLocationAsync(string city, string? district);

        /// <summary>
        /// İki nokta arasındaki mesafeyi hesaplar (km).
        /// </summary>
        Task<double> CalculateDistanceAsync(double lat1, double lon1, double lat2, double lon2);

        /// <summary>
        /// Adresin teslimat bölgesini belirler.
        /// </summary>
        Task<int?> DetermineDeliveryZoneAsync(double latitude, double longitude);
    }

    // =========================================================================
    // DTO'LAR
    // =========================================================================

    /// <summary>
    /// Adres doğrulama isteği.
    /// </summary>
    public class AddressValidationRequest
    {
        /// <summary>
        /// Tam adres satırı.
        /// </summary>
        public string AddressLine { get; set; } = string.Empty;

        /// <summary>
        /// İl.
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// İlçe.
        /// </summary>
        public string? District { get; set; }

        /// <summary>
        /// Posta kodu.
        /// </summary>
        public string? PostalCode { get; set; }

        /// <summary>
        /// Ülke (varsayılan: Türkiye).
        /// </summary>
        public string Country { get; set; } = "TR";

        /// <summary>
        /// Geocoding yapılsın mı?
        /// </summary>
        public bool IncludeGeocode { get; set; } = true;
    }

    /// <summary>
    /// Adres doğrulama sonucu.
    /// </summary>
    public class AddressValidationResult
    {
        /// <summary>
        /// Adres geçerli mi?
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Doğrulama mesajı.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Hata detayları (varsa).
        /// </summary>
        public string[]? Errors { get; set; }

        /// <summary>
        /// Uyarılar (eksik bilgi vb.).
        /// </summary>
        public string[]? Warnings { get; set; }

        /// <summary>
        /// Normalize edilmiş adres.
        /// </summary>
        public string? NormalizedAddress { get; set; }

        /// <summary>
        /// Normalize edilmiş il.
        /// </summary>
        public string? NormalizedCity { get; set; }

        /// <summary>
        /// Normalize edilmiş ilçe.
        /// </summary>
        public string? NormalizedDistrict { get; set; }

        /// <summary>
        /// Enlem koordinatı (geocoding yapıldıysa).
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// Boylam koordinatı (geocoding yapıldıysa).
        /// </summary>
        public double? Longitude { get; set; }

        /// <summary>
        /// Geocoding güvenilirlik skoru (0-100).
        /// </summary>
        public int? GeocodeConfidence { get; set; }

        /// <summary>
        /// Teslimat bölgesi ID'si.
        /// </summary>
        public int? DeliveryZoneId { get; set; }

        /// <summary>
        /// Teslimat yapılabilir mi?
        /// </summary>
        public bool IsDeliverable { get; set; } = true;

        /// <summary>
        /// Teslimat yapılamama nedeni.
        /// </summary>
        public string? NonDeliverableReason { get; set; }
    }

    /// <summary>
    /// Reverse geocoding sonucu.
    /// </summary>
    public class ReverseGeocodeResult
    {
        public string AddressLine { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Neighborhood { get; set; }
        public string? Street { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public int Confidence { get; set; }
    }
}
