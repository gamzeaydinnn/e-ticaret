using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs;
using ECommerce.Core.DTOs.Weight;
using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// Ağırlık Bazlı Dinamik Ödeme Sistemi - İş Mantığı Servisi
    /// 
    /// Bu servis tüm ağırlık ayarlama süreçlerinin merkezi yönetim noktasıdır.
    /// Kurye ağırlık girişinden, ödeme farklarının tahsilata kadar tüm akışı yönetir.
    /// 
    /// ANA İŞ AKIŞI:
    /// 1. Müşteri sipariş verir → tahmini fiyat hesaplanır → PreAuth tutulur
    /// 2. Kurye tartımı yapar → RecordCourierWeightEntryAsync çağrılır
    /// 3. Fark hesaplanır → CalculatePriceDifferenceAsync
    /// 4. Fark varsa: 
    ///    a) Nakit: Kurye farkı tahsil eder/iade eder
    ///    b) Kart: FinalizeWeightBasedPaymentAsync ile karttan çekilir/iade edilir
    /// 5. Admin müdahalesi gerekirse: ProcessAdminDecisionAsync
    /// 
    /// Tolere edilmiş ağırlık farkı YOKTUR. Her gram fark hesaplanır.
    /// </summary>
    public interface IWeightAdjustmentService
    {
        #region Kurye Ağırlık Girişi

        /// <summary>
        /// Kuryenin girdiği gerçek ağırlık değerini kaydeder.
        /// 
        /// Bu metod şunları yapar:
        /// 1. OrderItem'ın ActualWeight, IsWeighed, WeighedAt, WeighedByCourierId alanlarını günceller
        /// 2. WeightAdjustment kaydı oluşturur (fark varsa)
        /// 3. Fiyat farkını hesaplar (PricePerUnit * WeightDifference)
        /// 4. Order'ın tüm item'ları tartıldıysa AllItemsWeighed=true yapar
        /// 
        /// DÖNÜŞ: Hesaplanan fiyat farkı (pozitif = müşteri ödeyecek, negatif = iade)
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="orderItemId">Sipariş kalemi ID</param>
        /// <param name="actualWeight">Kuryenin girdiği gerçek ağırlık (gram cinsinden)</param>
        /// <param name="courierId">Tartımı yapan kurye ID</param>
        /// <returns>Fiyat farkı ve işlem sonucu</returns>
        Task<WeightEntryResultDto> RecordCourierWeightEntryAsync(int orderId, int orderItemId, decimal actualWeight, int courierId);

        /// <summary>
        /// Bir siparişin tüm ağırlık bazlı kalemlerini toplu tartım.
        /// Kurye tek seferde tüm ürünleri tartıyorsa bu metodu kullanır.
        /// </summary>
        Task<List<WeightEntryResultDto>> RecordBulkWeightEntriesAsync(int orderId, List<WeightEntryRequestDto> entries, int courierId);

        #endregion

        #region Fark Hesaplama

        /// <summary>
        /// Siparişteki toplam ağırlık ve fiyat farkını hesaplar.
        /// Bu metod sadece hesaplama yapar, veritabanı değiştirmez.
        /// 
        /// Dashboard ve önizleme için kullanılır.
        /// </summary>
        Task<WeightDifferenceCalculationDto> CalculateOrderWeightDifferenceAsync(int orderId);

        /// <summary>
        /// Tek bir sipariş kalemi için fark hesaplama.
        /// </summary>
        Task<WeightDifferenceCalculationDto> CalculateItemWeightDifferenceAsync(int orderItemId);

        #endregion

        #region Ödeme Finalizasyonu

        /// <summary>
        /// Ağırlık bazlı siparişin ödeme farklarını finalize eder.
        /// 
        /// NAKİT ÖDEME İÇİN:
        /// - Kurye farkı müşteriden tahsil etti/iade etti olarak işaretler
        /// - WeightAdjustment.SettledAt, SettledByCourierId güncellenir
        /// 
        /// KART ÖDEME İÇİN:
        /// - PreAuth tutarı serbest bırakılır
        /// - FinalAmount kadar gerçek çekim yapılır
        /// - Veya iade işlemi başlatılır (müşteri lehine fark varsa)
        /// 
        /// NOT: 2-3 günlük PreAuth süresi dolmadan bu işlem yapılmalıdır!
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="courierId">İşlemi yapan kurye ID</param>
        /// <param name="courierNotes">Kurye notları (varsa)</param>
        /// <returns>Ödeme sonucu</returns>
        Task<PaymentSettlementResultDto> FinalizeWeightBasedPaymentAsync(int orderId, int courierId, string? courierNotes = null);

        /// <summary>
        /// Nakit ödeme yapılan siparişte fark tahsilatını kaydeder.
        /// Kurye "farkı aldım/verdim" butonuna bastığında çağrılır.
        /// </summary>
        Task<bool> RecordCashDifferenceSettlementAsync(int orderId, int courierId, decimal collectedAmount, string? notes = null);

        #endregion

        #region Admin Müdahalesi

        /// <summary>
        /// Admin tarafından ağırlık ayarlamasına müdahale.
        /// 
        /// Admin şunları yapabilir:
        /// - Farkı onayla (ApprovedByAdmin)
        /// - Farkı reddet / iptal et (RejectedByAdmin)
        /// - Manuel tutar ayarla (tahmini veya gerçek ağırlığı override)
        /// - Farkı müşteri lehine yaz (promosyon amaçlı)
        /// </summary>
        /// <param name="adjustmentId">WeightAdjustment ID</param>
        /// <param name="adminId">İşlemi yapan admin ID</param>
        /// <param name="decision">Admin kararı (Approve/Reject/Override)</param>
        /// <param name="overrideAmount">Manuel tutar (override durumunda)</param>
        /// <param name="adminNotes">Admin notları</param>
        Task<bool> ProcessAdminDecisionAsync(int adjustmentId, int adminId, AdminDecisionType decision, 
            decimal? overrideAmount = null, string? adminNotes = null);

        /// <summary>
        /// Siparişin tüm ağırlık ayarlamalarını admin onayına gönderir.
        /// Büyük fark durumunda otomatik olarak veya manuel olarak çağrılabilir.
        /// </summary>
        Task<bool> RequestAdminReviewAsync(int orderId, string reason);

        #endregion

        #region Sipariş Oluşturma Entegrasyonu

        /// <summary>
        /// Yeni sipariş oluşturulurken çağrılır.
        /// Ağırlık bazlı ürünler için tahmini değerleri hesaplar ve set eder.
        /// 
        /// Bu metod:
        /// - HasWeightBasedItems = true yapar (eğer ağırlık bazlı ürün varsa)
        /// - Her OrderItem için EstimatedWeight ve EstimatedPrice hesaplar
        /// - PreAuthAmount hesaplar (tüm tahmini tutarlar toplamı + olası marj)
        /// </summary>
        Task InitializeWeightBasedOrderAsync(Order order);

        /// <summary>
        /// Kart ödemeli siparişler için PreAuth miktarını hesaplar.
        /// Tahmini toplam + küçük güvenlik marjı
        /// </summary>
        Task<decimal> CalculatePreAuthAmountAsync(int orderId);

        #endregion

        #region Sorgulama

        /// <summary>
        /// Siparişin ağırlık ayarlama durumunu detaylı getirir.
        /// Kurye ve admin panellerinde kullanılır.
        /// </summary>
        Task<OrderWeightSummaryDto?> GetOrderWeightSummaryAsync(int orderId);

        /// <summary>
        /// Belirli bir ağırlık ayarlama kaydını getirir.
        /// </summary>
        Task<WeightAdjustmentDto?> GetWeightAdjustmentByIdAsync(int adjustmentId);

        /// <summary>
        /// Kuryenin bekleyen ağırlık girişi gereken siparişlerini getirir.
        /// Kurye uygulaması için.
        /// </summary>
        Task<List<PendingWeightOrderDto>> GetPendingWeightEntriesForCourierAsync(int courierId);

        /// <summary>
        /// Admin onayı bekleyen ağırlık ayarlamalarını getirir.
        /// Admin paneli için.
        /// </summary>
        Task<PagedResult<WeightAdjustmentDto>> GetPendingAdminReviewsAsync(int page = 1, int pageSize = 20);

        /// <summary>
        /// Filtrelenmiş ağırlık ayarlama listesi.
        /// Admin paneli raporlama için.
        /// </summary>
        Task<PagedResult<WeightAdjustmentDto>> GetFilteredAdjustmentsAsync(WeightAdjustmentFilterDto filter);

        #endregion

        #region İstatistik ve Raporlama

        /// <summary>
        /// Dashboard için ağırlık sistemi istatistiklerini getirir.
        /// </summary>
        Task<WeightAdjustmentStatsDto> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Belirli kurye için performans istatistikleri.
        /// (Ortalama fark, işlem sayısı, vs.)
        /// </summary>
        Task<CourierWeightPerformanceDto> GetCourierPerformanceAsync(int courierId, DateTime? startDate = null, DateTime? endDate = null);

        #endregion

        #region Validasyon

        /// <summary>
        /// Siparişin ağırlık ayarlama için uygun olup olmadığını kontrol eder.
        /// </summary>
        Task<bool> CanProcessWeightAdjustmentAsync(int orderId);

        /// <summary>
        /// Kuryenin bu sipariş için ağırlık girişi yapıp yapamayacağını kontrol eder.
        /// </summary>
        Task<bool> CanCourierEnterWeightAsync(int orderId, int courierId);

        /// <summary>
        /// Ağırlık değerinin geçerli olup olmadığını kontrol eder.
        /// (Min/Max kontrolleri, sıfır/negatif kontrolü vs.)
        /// </summary>
        Task<WeightValidationResultDto> ValidateWeightEntryAsync(int orderItemId, decimal weight);

        #endregion
    }

    #region Admin Karar Enum'u

    /// <summary>
    /// Admin tarafından verilebilecek kararlar
    /// </summary>
    public enum AdminDecisionType
    {
        /// <summary>
        /// Hesaplanan farkı onayla
        /// </summary>
        Approve = 1,

        /// <summary>
        /// Farkı reddet, orijinal fiyattan devam et
        /// </summary>
        Reject = 2,

        /// <summary>
        /// Manuel tutar belirle
        /// </summary>
        Override = 3,

        /// <summary>
        /// Farkı müşteri lehine yaz (promosyon)
        /// </summary>
        WaiveForCustomer = 4
    }

    #endregion

    #region Yardımcı DTO'lar (Servis için özel)

    /// <summary>
    /// Kurye ağırlık girişi isteği
    /// </summary>
    public class WeightEntryRequestDto
    {
        public int OrderItemId { get; set; }
        public decimal ActualWeight { get; set; }
    }

    /// <summary>
    /// Ağırlık girişi sonucu
    /// </summary>
    public class WeightEntryResultDto
    {
        public int OrderItemId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal EstimatedWeight { get; set; }
        public decimal ActualWeight { get; set; }
        public decimal WeightDifference { get; set; }
        public decimal PriceDifference { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Oluşturulan WeightAdjustment kaydının ID'si (fark varsa)
        /// </summary>
        public int? AdjustmentId { get; set; }
    }

    /// <summary>
    /// Fark hesaplama sonucu
    /// </summary>
    public class WeightDifferenceCalculationDto
    {
        public int OrderId { get; set; }
        public decimal TotalEstimatedWeight { get; set; }
        public decimal TotalActualWeight { get; set; }
        public decimal TotalWeightDifference { get; set; }
        public decimal TotalEstimatedPrice { get; set; }
        public decimal TotalActualPrice { get; set; }
        public decimal TotalPriceDifference { get; set; }
        
        /// <summary>
        /// Pozitif = müşteri ödeyecek, Negatif = müşteriye iade
        /// </summary>
        public bool CustomerOwes => TotalPriceDifference > 0;
        
        public List<ItemWeightDifferenceDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Tek kalem için fark detayı
    /// </summary>
    public class ItemWeightDifferenceDto
    {
        public int OrderItemId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal EstimatedWeight { get; set; }
        public decimal ActualWeight { get; set; }
        public decimal WeightDifference { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal PriceDifference { get; set; }
    }

    /// <summary>
    /// Ödeme finalizasyon sonucu
    /// </summary>
    public class PaymentSettlementResultDto
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Orijinal PreAuth tutarı
        /// </summary>
        public decimal PreAuthAmount { get; set; }
        
        /// <summary>
        /// Final çekilen tutar
        /// </summary>
        public decimal FinalAmount { get; set; }
        
        /// <summary>
        /// Fark tutarı
        /// </summary>
        public decimal DifferenceAmount { get; set; }
        
        /// <summary>
        /// Ödeme yöntemi (Kart/Nakit)
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;
        
        /// <summary>
        /// İşlem referans numarası
        /// </summary>
        public string? TransactionReference { get; set; }
    }

    /// <summary>
    /// Bekleyen ağırlık girişi sipariş özeti
    /// </summary>
    public class PendingWeightOrderDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal EstimatedTotal { get; set; }
        
        /// <summary>
        /// Tartılması gereken kalem sayısı
        /// </summary>
        public int PendingItemCount { get; set; }
        
        /// <summary>
        /// Tartılmış kalem sayısı
        /// </summary>
        public int WeighedItemCount { get; set; }
        
        public List<PendingWeightItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Bekleyen tartım kalemi
    /// </summary>
    public class PendingWeightItemDto
    {
        public int OrderItemId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public decimal EstimatedWeight { get; set; }
        public string WeightUnit { get; set; } = string.Empty;
        public decimal PricePerUnit { get; set; }
        public decimal EstimatedPrice { get; set; }
        public bool IsWeighed { get; set; }
        public decimal? ActualWeight { get; set; }
    }

    /// <summary>
    /// Ağırlık giriş validasyon sonucu
    /// </summary>
    public class WeightValidationResultDto
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal? MinAllowed { get; set; }
        public decimal? MaxAllowed { get; set; }
    }

    /// <summary>
    /// Kurye ağırlık performans istatistikleri
    /// </summary>
    public class CourierWeightPerformanceDto
    {
        public int CourierId { get; set; }
        public string CourierName { get; set; } = string.Empty;
        
        /// <summary>
        /// Toplam tartım sayısı
        /// </summary>
        public int TotalWeighings { get; set; }
        
        /// <summary>
        /// Ortalama ağırlık farkı (gram)
        /// </summary>
        public decimal AverageWeightDifference { get; set; }
        
        /// <summary>
        /// Ortalama fiyat farkı (TL)
        /// </summary>
        public decimal AveragePriceDifference { get; set; }
        
        /// <summary>
        /// Müşteri lehine toplam fark
        /// </summary>
        public decimal TotalCustomerFavorDifference { get; set; }
        
        /// <summary>
        /// Mağaza lehine toplam fark
        /// </summary>
        public decimal TotalStoreFavorDifference { get; set; }
        
        /// <summary>
        /// Başarılı tahsilat oranı (%)
        /// </summary>
        public decimal SettlementSuccessRate { get; set; }
    }

    #endregion
}
