// ═══════════════════════════════════════════════════════════════════════════════════════════════
// POSNET HATA KODLARI ENUMERATİON
// Yapı Kredi POSNET XML Servisleri için tanımlı tüm hata kodları
// Dokümantasyon: POSNET XML Servisleri Entegrasyon Dokümanı v2.1.1.3
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// NEDEN BU YAPIYI SEÇTİK?
// 1. Hata kodlarını string olarak karşılaştırmak yerine type-safe enum kullanımı
// 2. Her hata kodunun açıklaması attribute ile taşınır - debugging kolaylığı
// 3. Extension method ile kolay mesaj dönüşümü
// ═══════════════════════════════════════════════════════════════════════════════════════════════

using System.ComponentModel;

namespace ECommerce.Infrastructure.Services.Payment.Posnet.Models
{
    /// <summary>
    /// POSNET işlem sonuç kodları
    /// 0 = Başarılı, diğerleri = Hata
    /// Prefix: 0xxx = Banka ret kodları, 1xx = Teknik hatalar
    /// </summary>
    public enum PosnetErrorCode
    {
        // ═══════════════════════════════════════════════════════════════════════
        // BAŞARILI İŞLEM
        // ═══════════════════════════════════════════════════════════════════════
        
        [Description("İşlem başarılı")]
        Success = 0,

        // ═══════════════════════════════════════════════════════════════════════
        // TEKNİK HATALAR (1xx)
        // ═══════════════════════════════════════════════════════════════════════

        [Description("İşlem başarılı")]
        OK = 100,

        [Description("Sistem hatası")]
        SystemError = 101,

        [Description("Veritabanı hatası")]
        DatabaseError = 102,

        [Description("Geçersiz XML formatı")]
        InvalidXmlFormat = 103,

        [Description("Zorunlu alan eksik")]
        MissingRequiredField = 104,

        [Description("Geçersiz parametre değeri")]
        InvalidParameterValue = 105,

        [Description("Geçersiz üye işyeri numarası")]
        InvalidMerchantId = 106,

        [Description("Geçersiz terminal numarası")]
        InvalidTerminalId = 107,

        [Description("Geçersiz POSNET numarası")]
        InvalidPosnetId = 108,

        [Description("Geçersiz kart numarası")]
        InvalidCardNumber = 109,

        [Description("Geçersiz son kullanma tarihi")]
        InvalidExpiryDate = 110,

        [Description("Geçersiz CVV")]
        InvalidCvv = 111,

        [Description("Geçersiz tutar")]
        InvalidAmount = 112,

        [Description("Geçersiz sipariş numarası")]
        InvalidOrderId = 113,

        [Description("Geçersiz taksit sayısı")]
        InvalidInstallment = 114,

        [Description("İşlem zaman aşımına uğradı")]
        Timeout = 115,

        [Description("Bağlantı hatası")]
        ConnectionError = 116,

        [Description("MAC doğrulama hatası")]
        MacValidationFailed = 117,

        [Description("3D Secure doğrulama hatası")]
        ThreeDSecureError = 118,

        // ═══════════════════════════════════════════════════════════════════════
        // BANKA RET KODLARI (0xxx)
        // Dokümandan alınan standart banka ret kodları
        // ═══════════════════════════════════════════════════════════════════════

        [Description("RED - ONAYLANMADI (Limit yetersiz veya hatalı kart bilgisi)")]
        DeclinedNotApproved = 5,

        [Description("RED - Hatalı işlem")]
        DeclinedInvalidTransaction = 12,

        [Description("RED - Geçersiz kart numarası")]
        DeclinedInvalidCardNumber = 14,

        [Description("RED - Böyle bir kart yok")]
        DeclinedNoSuchCard = 15,

        [Description("RED - Müşteri iptal etti")]
        DeclinedCustomerCancelled = 17,

        [Description("RED - Geçersiz tutar")]
        DeclinedInvalidAmount = 51,

        [Description("RED - Kartın süresi dolmuş")]
        DeclinedExpiredCard = 54,

        [Description("RED - Hatalı şifre")]
        DeclinedIncorrectPin = 55,

        [Description("RED - Kart limit aşımı")]
        DeclinedExceedLimit = 61,

        [Description("RED - Kısıtlı kart")]
        DeclinedRestrictedCard = 62,

        [Description("RED - Güvenlik ihlali")]
        DeclinedSecurityViolation = 63,

        [Description("RED - İşlem limiti aşıldı")]
        DeclinedTransactionLimitExceeded = 65,

        [Description("RED - Şifre deneme sayısı aşıldı")]
        DeclinedPinTriesExceeded = 75,

        [Description("RED - Hesap bulunamadı")]
        DeclinedAccountNotFound = 78,

        [Description("RED - Genel ret")]
        DeclinedGeneral = 91,

        [Description("RED - Banka sistemine ulaşılamıyor")]
        DeclinedBankUnavailable = 96,

        // ═══════════════════════════════════════════════════════════════════════
        // POSNET ÖZEL HATA KODLARI (01xx - 09xx)
        // ═══════════════════════════════════════════════════════════════════════

        [Description("Sipariş numarası daha önce kullanılmış")]
        OrderIdAlreadyUsed = 127,

        [Description("Üye işyeri bu işlem tipini yapamaz")]
        MerchantNotAuthorized = 128,

        [Description("İptal edilecek işlem bulunamadı")]
        TransactionNotFoundForCancel = 129,

        [Description("İade edilecek işlem bulunamadı")]
        TransactionNotFoundForRefund = 130,

        [Description("İade tutarı satış tutarını aşıyor")]
        RefundExceedsSaleAmount = 131,

        [Description("Provizyon süresi dolmuş")]
        AuthorizationExpired = 132,

        [Description("Finansallaştırma tutarı provizyon tutarını aşıyor")]
        CaptureExceedsAuth = 133,

        [Description("Puan yetersiz")]
        InsufficientPoints = 134,

        [Description("Kart puan programına kayıtlı değil")]
        CardNotInPointProgram = 135,

        [Description("3D Secure zorunlu")]
        ThreeDSecureRequired = 136,

        [Description("3D Secure doğrulaması başarısız")]
        ThreeDSecureVerificationFailed = 137,

        [Description("OOS işlemi başarısız")]
        OosOperationFailed = 138,

        [Description("Döviz işlemi desteklenmiyor")]
        CurrencyNotSupported = 139,

        // ═══════════════════════════════════════════════════════════════════════
        // EK HATA KODLARI - oosResolveMerchantData & oosTranData için
        // ═══════════════════════════════════════════════════════════════════════

        [Description("Geçersiz istek parametreleri")]
        InvalidRequest = 140,

        [Description("Güvenlik ihlali tespit edildi")]
        SecurityViolation = 141,

        [Description("oosResolveMerchantData işlemi başarısız")]
        OosResolveDataFailed = 142,

        [Description("oosTranData işlemi başarısız")]
        OosTranDataFailed = 143,

        // ═══════════════════════════════════════════════════════════════════════
        // UNKNOWN - Tanımlanmamış hata kodu
        // ═══════════════════════════════════════════════════════════════════════

        [Description("Bilinmeyen hata kodu")]
        Unknown = 9999
    }

    /// <summary>
    /// PosnetErrorCode için extension metodları
    /// Hata kodundan mesaj çıkarma ve kategori belirleme işlemleri
    /// </summary>
    public static class PosnetErrorCodeExtensions
    {
        /// <summary>
        /// Hata kodunun açıklama mesajını döndürür
        /// Description attribute'undan okunur
        /// </summary>
        public static string GetDescription(this PosnetErrorCode errorCode)
        {
            var field = errorCode.GetType().GetField(errorCode.ToString());
            if (field == null) return "Bilinmeyen hata";

            var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attribute?.Description ?? errorCode.ToString();
        }

        /// <summary>
        /// Hata kodunun başarılı işlem olup olmadığını kontrol eder
        /// </summary>
        public static bool IsSuccess(this PosnetErrorCode errorCode)
        {
            return errorCode == PosnetErrorCode.Success || errorCode == PosnetErrorCode.OK;
        }

        /// <summary>
        /// Hata kodunun tekrar denenebilir olup olmadığını kontrol eder
        /// Bazı hatalar (timeout, bağlantı hatası) için retry mantıklıdır
        /// </summary>
        public static bool IsRetryable(this PosnetErrorCode errorCode)
        {
            return errorCode == PosnetErrorCode.Timeout ||
                   errorCode == PosnetErrorCode.ConnectionError ||
                   errorCode == PosnetErrorCode.SystemError ||
                   errorCode == PosnetErrorCode.DeclinedBankUnavailable;
        }

        /// <summary>
        /// Banka ret kodu mu kontrol eder (müşteriye gösterilebilir)
        /// </summary>
        public static bool IsBankDecline(this PosnetErrorCode errorCode)
        {
            var code = (int)errorCode;
            return code >= 5 && code <= 99;
        }

        /// <summary>
        /// String hata kodunu enum'a çevirir
        /// POSNET response'dan gelen kod parse edilir
        /// </summary>
        public static PosnetErrorCode ParseFromString(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return PosnetErrorCode.Unknown;

            // "0" veya "00" veya "000" = başarılı
            if (code.Trim().TrimStart('0') == "" || code == "0") 
                return PosnetErrorCode.Success;

            // Sayısal değere çevir ve enum'da ara
            if (int.TryParse(code, out int numericCode))
            {
                if (Enum.IsDefined(typeof(PosnetErrorCode), numericCode))
                    return (PosnetErrorCode)numericCode;
            }

            return PosnetErrorCode.Unknown;
        }
    }
}
