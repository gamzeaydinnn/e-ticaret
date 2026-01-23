// ═══════════════════════════════════════════════════════════════════════════════════════════════
// POSNET MOCK SERVİSİ
// Test ve geliştirme ortamları için POSNET API simülasyonu
// Gerçek banka bağlantısı olmadan tüm senaryoları test etmeyi sağlar
// 
// KULLANIM:
// - Unit testlerde DI ile inject edilir
// - Geliştirme ortamında PosnetIsTestEnvironment=true + UseMock=true olduğunda aktif
// - Farklı senaryoları test etmek için özel kart numaraları kullanılabilir
// ═══════════════════════════════════════════════════════════════════════════════════════════════

using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Services.Payment.Posnet.Models;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services.Payment.Posnet
{
    /// <summary>
    /// POSNET Mock servis interface
    /// </summary>
    public interface IPosnetMockService
    {
        /// <summary>
        /// Mock direkt satış işlemi
        /// </summary>
        Task<PosnetSaleResponse> ProcessMockSaleAsync(string cardNumber, decimal amount);

        /// <summary>
        /// Mock 3D Secure başlatma
        /// </summary>
        Task<PosnetOosResponse> InitiateMock3DSecureAsync(string xid);

        /// <summary>
        /// Mock iptal işlemi
        /// </summary>
        Task<PosnetReverseResponse> ProcessMockCancelAsync(string hostLogKey);

        /// <summary>
        /// Mock iade işlemi
        /// </summary>
        Task<PosnetReturnResponse> ProcessMockRefundAsync(string hostLogKey, decimal amount);

        /// <summary>
        /// Mock puan sorgulama
        /// </summary>
        Task<PosnetPointInquiryResponse> QueryMockPointsAsync(string cardNumber);

        /// <summary>
        /// Mock mod aktif mi?
        /// </summary>
        bool IsMockEnabled { get; }
    }

    /// <summary>
    /// Test kart numaraları ve beklenen sonuçları
    /// </summary>
    public static class PosnetTestCards
    {
        // ═══════════════════════════════════════════════════════════════
        // BAŞARILI İŞLEMLER
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>Visa - Her zaman başarılı</summary>
        public const string SuccessVisa = "4506349116543211";
        
        /// <summary>Mastercard - Her zaman başarılı</summary>
        public const string SuccessMastercard = "5406675406675403";
        
        /// <summary>Troy - Her zaman başarılı</summary>
        public const string SuccessTroy = "6501234567890123";
        
        /// <summary>Amex - Her zaman başarılı</summary>
        public const string SuccessAmex = "378282246310005";

        // ═══════════════════════════════════════════════════════════════
        // BAŞARISIZ İŞLEMLER
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>Yetersiz bakiye hatası</summary>
        public const string InsufficientFunds = "4111111111111111";
        
        /// <summary>Kart limiti aşıldı</summary>
        public const string CardLimitExceeded = "4222222222222222";
        
        /// <summary>Kart kapalı/bloke</summary>
        public const string CardBlocked = "4333333333333333";
        
        /// <summary>Geçersiz CVV</summary>
        public const string InvalidCvv = "4444444444444444";
        
        /// <summary>Süresi dolmuş kart</summary>
        public const string ExpiredCard = "4555555555555555";
        
        /// <summary>3D Secure başarısız</summary>
        public const string ThreeDSecureFailed = "4666666666666666";
        
        /// <summary>Banka timeout</summary>
        public const string BankTimeout = "4777777777777777";
        
        /// <summary>Genel hata</summary>
        public const string GeneralError = "4888888888888888";

        // ═══════════════════════════════════════════════════════════════
        // ÖZEL DURUMLAR
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>Taksit desteklemeyen kart</summary>
        public const string NoInstallment = "4999999999999999";
        
        /// <summary>World puan bulunan kart (1000 puan)</summary>
        public const string WithWorldPoints = "5111111111111111";
        
        /// <summary>Fraud şüpheli kart</summary>
        public const string FraudSuspect = "5222222222222222";
    }

    /// <summary>
    /// POSNET Mock servis implementasyonu
    /// </summary>
    public class PosnetMockService : IPosnetMockService
    {
        private readonly ILogger<PosnetMockService> _logger;
        private readonly bool _isEnabled;

        public PosnetMockService(ILogger<PosnetMockService> logger, bool isEnabled = true)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isEnabled = isEnabled;
        }

        public bool IsMockEnabled => _isEnabled;

        /// <inheritdoc/>
        public async Task<PosnetSaleResponse> ProcessMockSaleAsync(string cardNumber, decimal amount)
        {
            _logger.LogInformation("[POSNET-MOCK] 🧪 Mock satış işlemi başlatıldı - Kart: {MaskedCard}",
                MaskCard(cardNumber));

            // Simüle edilmiş gecikme (100-500ms)
            await Task.Delay(Random.Shared.Next(100, 500));

            var response = GetMockResponseForCard(cardNumber, (int)(amount * 100));

            _logger.LogInformation("[POSNET-MOCK] 🧪 Mock satış sonucu - Başarılı: {Success}",
                response.IsSuccess);

            return response;
        }

        /// <inheritdoc/>
        public async Task<PosnetOosResponse> InitiateMock3DSecureAsync(string xid)
        {
            _logger.LogInformation("[POSNET-MOCK] 🧪 Mock 3D Secure başlatıldı - XID: {Xid}", xid);

            await Task.Delay(Random.Shared.Next(150, 400));

            return new PosnetOosResponse
            {
                Approved = true,
                RawErrorCode = "0",
                RedirectUrl = $"https://mock.3dsecure.test/auth?xid={xid}",
                Data1 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"MOCK_DATA1_{xid}")),
                Data2 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"MOCK_DATA2_{xid}")),
                Sign = "MOCK_SIGN_" + Guid.NewGuid().ToString("N")[..16].ToUpperInvariant(),
                RawXml = "<mock>3d_secure_init</mock>"
            };
        }

        /// <inheritdoc/>
        public async Task<PosnetReverseResponse> ProcessMockCancelAsync(string hostLogKey)
        {
            _logger.LogInformation("[POSNET-MOCK] 🧪 Mock iptal işlemi - HostLogKey: {HostLogKey}", 
                hostLogKey);

            await Task.Delay(Random.Shared.Next(100, 300));

            return new PosnetReverseResponse
            {
                Approved = true,
                RawErrorCode = "0",
                AuthCode = $"REV{Random.Shared.Next(100000, 999999)}",
                RawXml = "<mock>reverse_success</mock>"
            };
        }

        /// <inheritdoc/>
        public async Task<PosnetReturnResponse> ProcessMockRefundAsync(string hostLogKey, decimal amount)
        {
            _logger.LogInformation("[POSNET-MOCK] 🧪 Mock iade işlemi - Tutar: {Amount}", amount);

            await Task.Delay(Random.Shared.Next(100, 300));

            return new PosnetReturnResponse
            {
                Approved = true,
                RawErrorCode = "0",
                HostLogKey = $"MOCK_RET_{DateTime.UtcNow:yyyyMMddHHmmss}",
                AuthCode = $"RET{Random.Shared.Next(100000, 999999)}",
                Amount = (int)(amount * 100),
                RawXml = "<mock>return_success</mock>"
            };
        }

        /// <inheritdoc/>
        public async Task<PosnetPointInquiryResponse> QueryMockPointsAsync(string cardNumber)
        {
            _logger.LogInformation("[POSNET-MOCK] 🧪 Mock puan sorgulama");

            await Task.Delay(Random.Shared.Next(100, 200));

            // World puanlı test kartı mı?
            if (cardNumber == PosnetTestCards.WithWorldPoints)
            {
                return new PosnetPointInquiryResponse
                {
                    Approved = true,
                    RawErrorCode = "0",
                    IsEnrolled = true,
                    PointInfo = new PosnetPointInfo
                    {
                        WorldPoint = 1000,
                        BrandPoint = 0
                    },
                    RawXml = "<mock>point_query</mock>"
                };
            }

            return new PosnetPointInquiryResponse
            {
                Approved = true,
                RawErrorCode = "0",
                IsEnrolled = false,
                PointInfo = new PosnetPointInfo
                {
                    WorldPoint = 0,
                    BrandPoint = 0
                },
                RawXml = "<mock>point_query_empty</mock>"
            };
        }

        /// <summary>
        /// Kart numarasına göre mock response döndürür
        /// </summary>
        private PosnetSaleResponse GetMockResponseForCard(string cardNumber, int amountKurus)
        {
            // Hane formatını temizle
            cardNumber = new string(cardNumber.Where(char.IsDigit).ToArray());

            return cardNumber switch
            {
                // Başarılı kartlar
                PosnetTestCards.SuccessVisa or
                PosnetTestCards.SuccessMastercard or
                PosnetTestCards.SuccessTroy or
                PosnetTestCards.SuccessAmex or
                PosnetTestCards.WithWorldPoints =>
                    CreateSuccessResponse(amountKurus),

                // Yetersiz bakiye
                PosnetTestCards.InsufficientFunds =>
                    PosnetSaleResponse.Failure("0051", "Yetersiz bakiye"),

                // Limit aşıldı
                PosnetTestCards.CardLimitExceeded =>
                    PosnetSaleResponse.Failure("0061", "Kart limiti aşıldı"),

                // Kart bloke
                PosnetTestCards.CardBlocked =>
                    PosnetSaleResponse.Failure("0057", "Kart kapalı veya bloke edilmiş"),

                // Geçersiz CVV
                PosnetTestCards.InvalidCvv =>
                    PosnetSaleResponse.Failure("0082", "CVV hatalı"),

                // Süresi dolmuş
                PosnetTestCards.ExpiredCard =>
                    PosnetSaleResponse.Failure("0054", "Kartın son kullanma tarihi geçmiş"),

                // 3D Secure başarısız
                PosnetTestCards.ThreeDSecureFailed =>
                    PosnetSaleResponse.Failure("0096", "3D Secure doğrulama başarısız"),

                // Timeout
                PosnetTestCards.BankTimeout =>
                    PosnetSaleResponse.Failure("0091", "Banka yanıt vermedi - Timeout"),

                // Taksit desteklemiyor
                PosnetTestCards.NoInstallment =>
                    PosnetSaleResponse.Failure("0058", "Bu kart taksitli işlem desteklemiyor"),

                // Fraud şüpheli
                PosnetTestCards.FraudSuspect =>
                    PosnetSaleResponse.Failure("0034", "Şüpheli işlem - Manuel onay gerekli"),

                // Genel hata
                PosnetTestCards.GeneralError =>
                    PosnetSaleResponse.Failure("0012", "Geçersiz işlem"),

                // Tanımlanmamış kartlar - varsayılan başarılı
                _ => CreateSuccessResponse(amountKurus)
            };
        }

        private PosnetSaleResponse CreateSuccessResponse(int amountKurus)
        {
            return PosnetSaleResponse.Success(
                hostLogKey: $"MOCK_{DateTime.UtcNow:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}",
                authCode: $"M{Random.Shared.Next(100000, 999999)}",
                orderId: Guid.NewGuid().ToString("N")[..16].ToUpperInvariant(),
                amount: amountKurus,
                installment: "00",
                rawXml: "<mock>sale_success</mock>"
            );
        }

        private static string MaskCard(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 10)
                return "****";

            var digits = new string(cardNumber.Where(char.IsDigit).ToArray());
            return $"{digits[..6]}******{digits[^4..]}";
        }
    }
}
