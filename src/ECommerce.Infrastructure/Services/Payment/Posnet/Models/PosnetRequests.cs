// ═══════════════════════════════════════════════════════════════════════════════════════════════
// POSNET REQUEST MODEL'LERİ
// Yapı Kredi POSNET XML API'sine gönderilecek tüm istek tipleri
// Dokümantasyon: POSNET XML Servisleri Entegrasyon Dokümanı v2.1.1.3
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// NEDEN BU YAPIYI SEÇTİK?
// 1. Immutable record yapısı - Thread-safe ve değiştirilemez
// 2. Validation attribute'ları - Invalid request'ler daha service'e gitmeden yakalanır
// 3. Factory method pattern - Karmaşık nesne oluşturmayı basitleştirir
// 4. Kart bilgileri gibi hassas veriler için güvenli ToString() override'ı
// ═══════════════════════════════════════════════════════════════════════════════════════════════

using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Infrastructure.Services.Payment.Posnet.Models
{
    #region Base Request

    /// <summary>
    /// Tüm POSNET request'lerinin ortak base class'ı
    /// MerchantId, TerminalId zorunlu alanlar burada tanımlı
    /// </summary>
    public abstract record PosnetBaseRequest
    {
        /// <summary>
        /// Üye işyeri numarası - 10 haneli
        /// </summary>
        [Required(ErrorMessage = "MerchantId zorunludur")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "MerchantId 10 haneli olmalıdır")]
        public string MerchantId { get; init; } = string.Empty;

        /// <summary>
        /// Terminal numarası - 8 haneli
        /// </summary>
        [Required(ErrorMessage = "TerminalId zorunludur")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "TerminalId 8 haneli olmalıdır")]
        public string TerminalId { get; init; } = string.Empty;

        /// <summary>
        /// İşlem tipi: Sale, Auth, Capt, Reverse, Return, PointInquiry vs.
        /// </summary>
        public abstract PosnetTransactionType TransactionType { get; }

        /// <summary>
        /// Request oluşturulma zamanı - Loglama için
        /// </summary>
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    #endregion

    #region Transaction Types

    /// <summary>
    /// POSNET işlem tipleri
    /// XML'de hangi ana tag'in kullanılacağını belirler
    /// </summary>
    public enum PosnetTransactionType
    {
        /// <summary>Direkt satış işlemi</summary>
        Sale,
        
        /// <summary>Provizyon (ön yetkilendirme)</summary>
        Auth,
        
        /// <summary>Finansallaştırma (provizyon çekme)</summary>
        Capt,
        
        /// <summary>İptal (gün içi)</summary>
        Reverse,
        
        /// <summary>İade (gün sonu sonrası)</summary>
        Return,
        
        /// <summary>Puan sorgulama</summary>
        PointInquiry,
        
        /// <summary>Puan kullanımlı satış</summary>
        PointSale,
        
        /// <summary>İşlem durumu sorgulama</summary>
        Agreement,
        
        /// <summary>3D Secure OOS işlemi</summary>
        OOS,
        
        /// <summary>Joker Vadaa sorgulama</summary>
        JokerVadaa,
        
        /// <summary>VFT (Vade Farklı İşlem)</summary>
        VFT,
        
        /// <summary>Taksit sorgulama</summary>
        InstallmentInquiry
    }

    #endregion

    #region Card Information

    /// <summary>
    /// Kredi kartı bilgileri
    /// PCI DSS uyumlu loglama için ToString() override edilmiş
    /// </summary>
    public sealed record PosnetCardInfo
    {
        /// <summary>
        /// Kart numarası - 16 haneli, boşluksuz
        /// </summary>
        [Required(ErrorMessage = "Kart numarası zorunludur")]
        [CreditCard(ErrorMessage = "Geçersiz kart numarası")]
        public string CardNumber { get; init; } = string.Empty;

        /// <summary>
        /// Son kullanma tarihi - YYAA formatında (Örnek: 2512 = Aralık 2025)
        /// POSNET YYAA formatı bekler (yıl + ay)
        /// </summary>
        [Required(ErrorMessage = "Son kullanma tarihi zorunludur")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Son kullanma tarihi YYAA formatında olmalıdır")]
        public string ExpireDate { get; init; } = string.Empty;

        /// <summary>
        /// CVV/CVC güvenlik kodu - 3 veya 4 haneli
        /// </summary>
        [Required(ErrorMessage = "CVV zorunludur")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV 3 veya 4 haneli olmalıdır")]
        public string Cvv { get; init; } = string.Empty;

        /// <summary>
        /// Kart sahibinin adı (opsiyonel, bazı işlemlerde gerekebilir)
        /// </summary>
        public string? CardHolderName { get; init; }

        /// <summary>
        /// PCI DSS uyumlu maskelenmiş kart numarası
        /// Örnek: 4506*****1234
        /// </summary>
        public string MaskedCardNumber => 
            CardNumber.Length >= 10 
                ? $"{CardNumber[..4]}****{CardNumber[^4..]}" 
                : "****";

        /// <summary>
        /// Güvenli string gösterimi - Kart numarası maskelenir
        /// </summary>
        public override string ToString() => 
            $"Card: {MaskedCardNumber}, Exp: {ExpireDate}";

        /// <summary>
        /// Ay/Yıl formatından YYAA formatına çevirir
        /// Örnek: (12, 2025) => "2512"
        /// </summary>
        public static string FormatExpireDate(int month, int year)
        {
            // 4 haneli yılı 2 haneye çevir
            var yy = year % 100;
            return $"{yy:D2}{month:D2}";
        }
    }

    #endregion

    #region Sale Request

    /// <summary>
    /// POSNET satış işlemi request modeli
    /// Peşin veya taksitli direkt satış için kullanılır
    /// </summary>
    public sealed record PosnetSaleRequest : PosnetBaseRequest
    {
        public override PosnetTransactionType TransactionType => PosnetTransactionType.Sale;

        /// <summary>
        /// Kredi kartı bilgileri
        /// </summary>
        [Required(ErrorMessage = "Kart bilgileri zorunludur")]
        public PosnetCardInfo Card { get; init; } = new();

        /// <summary>
        /// Sipariş numarası - 24 haneye kadar alfanumerik
        /// Unique olmalı, daha önce kullanılmış ise 0127 hatası alınır
        /// </summary>
        [Required(ErrorMessage = "OrderId zorunludur")]
        [StringLength(24, MinimumLength = 1, ErrorMessage = "OrderId 1-24 karakter olmalıdır")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "OrderId sadece alfanumerik karakterler içermelidir")]
        public string OrderId { get; init; } = string.Empty;

        /// <summary>
        /// Tutar - Kuruş cinsinden (12.34 TL = 1234)
        /// POSNET kuruş formatı bekler
        /// </summary>
        [Required(ErrorMessage = "Tutar zorunludur")]
        [Range(1, 999999999, ErrorMessage = "Tutar 1-999999999 kuruş arasında olmalıdır")]
        public int Amount { get; init; }

        /// <summary>
        /// Taksit sayısı - 00 = peşin, 02-12 = taksit
        /// 2 haneli string formatında gönderilir
        /// </summary>
        [Required(ErrorMessage = "Taksit sayısı zorunludur")]
        [RegularExpression(@"^(0[0-9]|1[0-2])$", ErrorMessage = "Taksit 00-12 arasında olmalıdır")]
        public string Installment { get; init; } = "00";

        /// <summary>
        /// Para birimi kodu (TL için 949)
        /// ISO 4217 currency code
        /// </summary>
        public string CurrencyCode { get; init; } = "TL";

        /// <summary>
        /// Mail Order işareti - Online işlemler için Y
        /// MO (Mail Order) işlemleri için zorunlu
        /// </summary>
        public bool IsMailOrder { get; init; } = true;

        /// <summary>
        /// Konum bilgisi - H: Internet, P: POS
        /// </summary>
        public string TranType { get; init; } = "H"; // H = Host (Internet)

        /// <summary>
        /// Decimal tutardan kuruş formatına çevirir
        /// Örnek: 125.50 TL => 12550 kuruş
        /// </summary>
        public static int ConvertToKurus(decimal amount)
        {
            return (int)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Factory method - Kolaylaştırılmış oluşturma
        /// </summary>
        public static PosnetSaleRequest Create(
            string merchantId, string terminalId,
            string cardNumber, string expireDate, string cvv,
            string orderId, decimal amount, int installment = 0)
        {
            return new PosnetSaleRequest
            {
                MerchantId = merchantId,
                TerminalId = terminalId,
                Card = new PosnetCardInfo
                {
                    CardNumber = cardNumber.Replace(" ", "").Replace("-", ""),
                    ExpireDate = expireDate,
                    Cvv = cvv
                },
                OrderId = orderId,
                Amount = ConvertToKurus(amount),
                Installment = installment.ToString("D2")
            };
        }
    }

    #endregion

    #region Auth (Provizyon) Request

    /// <summary>
    /// POSNET provizyon (ön yetkilendirme) request modeli
    /// Tutarı bloke eder, ekstreye yansımaz
    /// Finansallaştırma (Capt) ile tamamlanmalıdır
    /// </summary>
    public sealed record PosnetAuthRequest : PosnetBaseRequest
    {
        public override PosnetTransactionType TransactionType => PosnetTransactionType.Auth;

        [Required]
        public PosnetCardInfo Card { get; init; } = new();

        [Required]
        [StringLength(24, MinimumLength = 1)]
        public string OrderId { get; init; } = string.Empty;

        [Required]
        [Range(1, 999999999)]
        public int Amount { get; init; }

        [Required]
        public string Installment { get; init; } = "00";

        public string CurrencyCode { get; init; } = "TL";
        public bool IsMailOrder { get; init; } = true;
    }

    #endregion

    #region Capt (Finansallaştırma) Request

    /// <summary>
    /// POSNET finansallaştırma request modeli
    /// Daha önce alınan provizyonu finansal değere çevirir
    /// Tutar, provizyon tutarını geçemez
    /// </summary>
    public sealed record PosnetCaptRequest : PosnetBaseRequest
    {
        public override PosnetTransactionType TransactionType => PosnetTransactionType.Capt;

        /// <summary>
        /// Provizyon alınmış sipariş numarası
        /// </summary>
        [Required]
        [StringLength(24, MinimumLength = 1)]
        public string OrderId { get; init; } = string.Empty;

        /// <summary>
        /// Finansallaştırılacak tutar (kuruş)
        /// Provizyon tutarından büyük olamaz
        /// </summary>
        [Required]
        [Range(1, 999999999)]
        public int Amount { get; init; }

        /// <summary>
        /// Taksit sayısı - Provizyondaki ile aynı olmalı
        /// </summary>
        [Required]
        public string Installment { get; init; } = "00";

        /// <summary>
        /// Provizyon referans numarası (HostLogKey)
        /// İlk provizyon işleminden dönen değer
        /// </summary>
        public string? HostLogKey { get; init; }
    }

    #endregion

    #region Reverse (İptal) Request

    /// <summary>
    /// POSNET iptal request modeli
    /// Sadece gün içinde (grup kapama öncesi) yapılabilir
    /// İşlem finansal değer kazanmaz, ekstrede görünmez
    /// </summary>
    public sealed record PosnetReverseRequest : PosnetBaseRequest
    {
        public override PosnetTransactionType TransactionType => PosnetTransactionType.Reverse;

        /// <summary>
        /// İptal edilecek işlemin sipariş numarası
        /// </summary>
        [Required]
        [StringLength(24, MinimumLength = 1)]
        public string OrderId { get; init; } = string.Empty;

        /// <summary>
        /// İptal edilecek işlemin HostLogKey değeri
        /// Satış işleminden dönen referans numarası
        /// </summary>
        [Required(ErrorMessage = "HostLogKey iptal işlemi için zorunludur")]
        public string HostLogKey { get; init; } = string.Empty;

        /// <summary>
        /// İşlem tarihi (POSNET formatı: YYMMDD)
        /// İptal edilecek işlemin tarihi
        /// </summary>
        public string? TransactionDate { get; init; }

        /// <summary>
        /// Bugünün tarihini POSNET formatında döndürür
        /// </summary>
        public static string GetTodayAsPosnetDate()
        {
            return DateTime.Now.ToString("yyMMdd");
        }
    }

    #endregion

    #region Return (İade) Request

    /// <summary>
    /// POSNET iade request modeli
    /// Gün sonu (grup kapama) sonrası yapılan işlemler için
    /// Kısmi veya tam iade yapılabilir
    /// </summary>
    public sealed record PosnetReturnRequest : PosnetBaseRequest
    {
        public override PosnetTransactionType TransactionType => PosnetTransactionType.Return;

        /// <summary>
        /// İade edilecek işlemin sipariş numarası
        /// </summary>
        [Required]
        [StringLength(24, MinimumLength = 1)]
        public string OrderId { get; init; } = string.Empty;

        /// <summary>
        /// İade tutarı (kuruş cinsinden)
        /// Orijinal satış tutarından büyük olamaz
        /// </summary>
        [Required]
        [Range(1, 999999999)]
        public int Amount { get; init; }

        /// <summary>
        /// İade edilecek işlemin HostLogKey değeri
        /// Orijinal satış işleminden dönen referans
        /// </summary>
        [Required(ErrorMessage = "HostLogKey iade işlemi için zorunludur")]
        public string HostLogKey { get; init; } = string.Empty;

        /// <summary>
        /// İade işlemi için yeni sipariş numarası
        /// İade işlemi ayrı bir transaction olduğu için unique olmalı
        /// </summary>
        public string? RefundOrderId { get; init; }
    }

    #endregion

    #region Point Inquiry Request

    /// <summary>
    /// POSNET puan sorgulama request modeli
    /// WorldCard sahiplerinin puan bakiyesini sorgular
    /// World Puan ve Marka Puan değerlerini döndürür
    /// </summary>
    public sealed record PosnetPointInquiryRequest : PosnetBaseRequest
    {
        public override PosnetTransactionType TransactionType => PosnetTransactionType.PointInquiry;

        /// <summary>
        /// Puan sorgulanacak kart bilgileri
        /// </summary>
        [Required]
        public PosnetCardInfo Card { get; init; } = new();
    }

    #endregion

    #region Agreement (İşlem Durumu Sorgulama) Request

    /// <summary>
    /// POSNET işlem durumu sorgulama request modeli
    /// Bağlantı kopması durumunda işlemin akıbetini öğrenmek için
    /// OrderId ile sorgulama yapılır
    /// </summary>
    public sealed record PosnetAgreementRequest : PosnetBaseRequest
    {
        public override PosnetTransactionType TransactionType => PosnetTransactionType.Agreement;

        /// <summary>
        /// Sorgulanacak işlemin sipariş numarası
        /// </summary>
        [Required]
        [StringLength(24, MinimumLength = 1)]
        public string OrderId { get; init; } = string.Empty;
    }

    #endregion

    #region 3D Secure OOS Request

    /// <summary>
    /// POSNET 3D Secure OOS (On-us/Off-us) request modeli
    /// 3D Secure akışını başlatmak için kullanılır
    /// </summary>
    public sealed record PosnetOosRequest : PosnetBaseRequest
    {
        public override PosnetTransactionType TransactionType => PosnetTransactionType.OOS;

        /// <summary>
        /// POSNET numarası - 3D Secure şifreleme için
        /// </summary>
        [Required]
        public string PosnetId { get; init; } = string.Empty;

        /// <summary>
        /// Kredi kartı bilgileri
        /// </summary>
        [Required]
        public PosnetCardInfo Card { get; init; } = new();

        /// <summary>
        /// Sipariş numarası
        /// </summary>
        [Required]
        [StringLength(24, MinimumLength = 1)]
        public string OrderId { get; init; } = string.Empty;

        /// <summary>
        /// Tutar (kuruş)
        /// </summary>
        [Required]
        [Range(1, 999999999)]
        public int Amount { get; init; }

        /// <summary>
        /// Taksit sayısı
        /// </summary>
        [Required]
        public string Installment { get; init; } = "00";

        /// <summary>
        /// Para birimi
        /// </summary>
        public string CurrencyCode { get; init; } = "TL";

        /// <summary>
        /// 3D Secure işlem tipi: Auth veya Sale
        /// </summary>
        public string TxnType { get; init; } = "Sale";

        /// <summary>
        /// 3D işlem sonrası dönülecek URL
        /// </summary>
        public string? ReturnUrl { get; init; }

        // ═══════════════════════════════════════════════════════════════════════
        // VFT VE JOKER VADAA DESTEĞİ
        // POSNET Dokümanı: Sayfa 18-22 - Opsiyonel kampanya parametreleri
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// VFT (Vade Farklı Taksit) Kampanya Kodu
        /// POSNET'ten alınan vade farklı taksit kampanya kodudur.
        /// Banka ile anlaşmalı kampanyalar için kullanılır.
        /// Null/empty ise VFT uygulanmaz.
        /// 
        /// Örnek: "123456" (Banka tarafından verilen kampanya kodu)
        /// </summary>
        public string? VftCode { get; init; }

        /// <summary>
        /// VFT kampanya mağaza kodu (opsiyonel)
        /// Bazı kampanyalarda mağaza bazlı ayrım için kullanılır.
        /// </summary>
        public string? VftDealerCode { get; init; }

        /// <summary>
        /// Joker Vadaa kampanyası kullanılacak mı?
        /// true = Kart sahibine özel kampanya teklifi sorgulanır
        /// false = Joker Vadaa sorgulanmaz (varsayılan)
        /// 
        /// POSNET Dokümanı: Sayfa 20 - useJokerVadaa parametresi
        /// Joker Vadaa, Yapı Kredi kartlarına özel kişiselleştirilmiş 
        /// taksit kampanyalarıdır.
        /// </summary>
        public bool UseJokerVadaa { get; init; } = false;

        /// <summary>
        /// Joker Vadaa kampanya ID (opsiyonel)
        /// UseJokerVadaa true ise ve belirli bir kampanya seçilmişse kullanılır.
        /// Null ise en uygun kampanya otomatik seçilir.
        /// </summary>
        public string? JokerVadaaCampaignId { get; init; }
    }

    #endregion

    #region 3D Secure Callback Data

    /// <summary>
    /// POSNET 3D Secure callback'inden dönen veriler
    /// Banka, ödeme sonrası bu verileri POST eder
    /// </summary>
    public sealed record PosnetOosCallbackData
    {
        /// <summary>
        /// Üye işyeri verileri (şifrelenmiş)
        /// </summary>
        public string? MerchantData { get; init; }

        /// <summary>
        /// Banka verileri (şifrelenmiş)
        /// </summary>
        public string? BankData { get; init; }

        /// <summary>
        /// İmza (MAC) - Doğrulama için
        /// </summary>
        public string? Sign { get; init; }

        /// <summary>
        /// Sipariş numarası
        /// </summary>
        public string? OrderId { get; init; }

        /// <summary>
        /// XID (işlem referansı)
        /// </summary>
        public string? Xid { get; init; }

        /// <summary>
        /// Tutar (kuruş) - callback'ten gelebilir
        /// </summary>
        public string? Amount { get; init; }

        /// <summary>
        /// Para birimi (TL/US/EU)
        /// </summary>
        public string? Currency { get; init; }

        /// <summary>
        /// Taksit sayısı
        /// </summary>
        public string? InstallmentCount { get; init; }

        /// <summary>
        /// CAVV değeri (3D Secure)
        /// </summary>
        public string? Cavv { get; init; }

        /// <summary>
        /// ECI değeri (3D Secure)
        /// </summary>
        public string? Eci { get; init; }

        /// <summary>
        /// 3D Secure sonucu (1 = Başarılı)
        /// </summary>
        public string? MdStatus { get; init; }

        /// <summary>
        /// Hata mesajı (varsa)
        /// </summary>
        public string? MdErrorMessage { get; init; }

        /// <summary>
        /// MAC verileri (doğrulama için)
        /// </summary>
        public string? Mac { get; init; }

        /// <summary>
        /// 3D doğrulamanın başarılı olup olmadığını kontrol eder
        /// MdStatus = 1 ise başarılı
        /// </summary>
        public bool Is3DVerified => MdStatus == "1";
    }

    #endregion

    #region OOS Resolve Merchant Data Request

    /// <summary>
    /// POSNET 3D Secure oosResolveMerchantData request modeli
    /// 
    /// KULLANIM AMACI:
    /// 3D Secure callback'inden gelen şifreli verileri çözmek için bankaya gönderilir.
    /// Bu servis çağrısı yapılmadan işlem finansallaştırılmamalıdır!
    /// 
    /// GÜVENLIK:
    /// - Callback'den gelen verilerin banka tarafından doğrulanması sağlanır
    /// - MAN-IN-THE-MIDDLE saldırılarına karşı koruma
    /// - xid, amount değerlerinin tutarlılık kontrolü
    /// 
    /// Dokümantasyon: POSNET 3D Secure Entegrasyon Dokümanı - Sayfa 12-14
    /// </summary>
    public sealed record PosnetOosResolveMerchantDataRequest : PosnetBaseRequest
    {
        public override PosnetTransactionType TransactionType => PosnetTransactionType.OOS;

        /// <summary>
        /// Banka tarafından 3D callback'te dönen şifreli veri
        /// HTML form içinde "BankPacket" veya "bankData" olarak gelir
        /// </summary>
        [Required(ErrorMessage = "BankData zorunludur")]
        public string BankData { get; init; } = string.Empty;

        /// <summary>
        /// İşyeri verileri - 3D Secure başlatılırken gönderilen ve geri dönen şifreli veri
        /// HTML form içinde "MerchantPacket" veya "merchantData" olarak gelir
        /// </summary>
        [Required(ErrorMessage = "MerchantData zorunludur")]
        public string MerchantData { get; init; } = string.Empty;

        /// <summary>
        /// Dijital imza - Banka tarafından oluşturulan veri doğrulama imzası
        /// HTML form içinde "Sign" olarak gelir
        /// </summary>
        [Required(ErrorMessage = "Sign zorunludur")]
        public string Sign { get; init; } = string.Empty;

        /// <summary>
        /// Message Authentication Code
        /// İşyeri tarafından hesaplanan MAC değeri
        /// 
        /// Hesaplama Formülü (POSNET Dokümanı sayfa 11):
        /// firstHash = HASH(encKey + ';' + terminalID)
        /// MAC = HASH(xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
        /// </summary>
        [Required(ErrorMessage = "MAC zorunludur")]
        public string Mac { get; init; } = string.Empty;

        /// <summary>
        /// Orijinal işlem sipariş numarası (XID) - Doğrulama için
        /// Response'dan dönen xid ile karşılaştırılmalı
        /// </summary>
        public string? OriginalXid { get; init; }

        /// <summary>
        /// Orijinal işlem tutarı (kuruş) - Doğrulama için
        /// Response'dan dönen amount ile karşılaştırılmalı
        /// </summary>
        public int? OriginalAmount { get; init; }

        /// <summary>
        /// Orijinal para birimi - Doğrulama için
        /// Response'dan dönen currency ile karşılaştırılmalı
        /// </summary>
        public string? OriginalCurrency { get; init; }

        /// <summary>
        /// Request için özet bilgi (loglama amaçlı)
        /// </summary>
        public override string ToString() =>
            $"OosResolve - MerchantId: {MerchantId}, OriginalXid: {OriginalXid ?? "N/A"}";
    }

    #endregion

    #region OOS Tran Data Request (Finansallaştırma)

    /// <summary>
    /// POSNET 3D Secure oosTranData request modeli
    /// 
    /// KULLANIM AMACI:
    /// 3D Secure doğrulaması başarılı olduktan sonra işlemi finansallaştırmak için kullanılır.
    /// Bu adım gerçekleşmeden müşterinin hesabından para çekilmez!
    /// 
    /// ÖNEMLİ:
    /// - Bu metod çağrılmadan önce oosResolveMerchantData ile veri doğrulaması yapılmalı
    /// - MAC doğrulaması başarılı olmalı
    /// - mdStatus kontrolü yapılmış olmalı (1, 2, 3, 4 başarılı kabul edilir)
    /// 
    /// Dokümantasyon: POSNET 3D Secure Entegrasyon Dokümanı - Sayfa 15-17
    /// </summary>
    public sealed record PosnetOosTranDataRequest : PosnetBaseRequest
    {
        public override PosnetTransactionType TransactionType => PosnetTransactionType.OOS;

        /// <summary>
        /// Banka tarafından dönen şifreli veri
        /// oosResolveMerchantData response'undan veya direkt 3D callback'ten alınır
        /// </summary>
        [Required(ErrorMessage = "BankData zorunludur")]
        public string BankData { get; init; } = string.Empty;

        /// <summary>
        /// World Puan kullanım tutarı (kuruş cinsinden)
        /// 
        /// KULLANIM:
        /// - Puan kullanılmıyorsa: 0
        /// - SaleWP işlem tipinde: Kullanılacak puan tutarı (kuruş)
        /// - Örnek: 12.50 TL puan = 1250
        /// </summary>
        [Range(0, 999999999, ErrorMessage = "WpAmount 0-999999999 arasında olmalı")]
        public int WpAmount { get; init; } = 0;

        /// <summary>
        /// Message Authentication Code
        /// İşyeri tarafından hesaplanan MAC değeri
        /// 
        /// Hesaplama Formülü (POSNET Dokümanı sayfa 16):
        /// firstHash = HASH(encKey + ';' + terminalID)
        /// MAC = HASH(xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
        /// </summary>
        [Required(ErrorMessage = "MAC zorunludur")]
        public string Mac { get; init; } = string.Empty;

        /// <summary>
        /// Sipariş numarası (loglama ve takip için)
        /// </summary>
        public string? OrderId { get; init; }

        /// <summary>
        /// İşlem tutarı (loglama için)
        /// </summary>
        public int? Amount { get; init; }

        /// <summary>
        /// Request için özet bilgi (loglama amaçlı)
        /// </summary>
        public override string ToString() =>
            $"OosTranData - MerchantId: {MerchantId}, OrderId: {OrderId ?? "N/A"}, WpAmount: {WpAmount}";
    }

    #endregion
}
