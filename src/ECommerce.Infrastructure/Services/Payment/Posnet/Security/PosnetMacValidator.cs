// ═══════════════════════════════════════════════════════════════════════════════
// POSNET MAC (MESSAGE AUTHENTICATION CODE) DOĞRULAMA SERVİSİ
// Yapı Kredi POSNET 3D Secure işlemlerinde güvenlik doğrulaması için kullanılır
// Dokümantasyon: POSNET XML Servisleri Entegrasyon Dokümanı v2.1.1.3 - Sayfa 35-42
// 
// MAC, işlem bütünlüğünü ve kaynağını doğrulamak için kullanılan kriptografik hash'tir.
// Yanlış MAC değeri ödeme sahtekarlığına işaret edebilir - MAN-IN-THE-MIDDLE koruması sağlar.
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Security.Cryptography;
using System.Text;
using ECommerce.Infrastructure.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services.Payment.Posnet.Security
{
    /// <summary>
    /// POSNET 3D Secure MAC doğrulama ve hesaplama servisi.
    /// OOS (On-us/Off-us) işlemlerinde banka ile güvenli iletişim sağlar.
    /// 
    /// POSNET Dokümantasyonu v2.1.1.3 - Sayfa 35-42'ye göre MAC formülleri:
    /// - FirstHash = HASH(encKey + ';' + terminalID)
    /// - MAC = HASH(xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
    /// - ResponseMAC = HASH(mdStatus + ';' + xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
    /// </summary>
    public interface IPosnetMacValidator
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // POSNET DOKÜMANTASYONUNA UYGUN YENİ MAC METODLARI (v2.1.1.3)
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// First Hash hesaplar - Tüm MAC hesaplamalarının temeli
        /// Formül: HASH(encKey + ';' + terminalID)
        /// POSNET Doküman Sayfa 36 - Bu hash tüm diğer MAC hesaplamalarında kullanılır
        /// </summary>
        /// <param name="terminalId">Terminal ID (POSNET'ten alınan)</param>
        /// <returns>Base64 encoded SHA-256 hash</returns>
        string CalculateFirstHash(string terminalId);

        /// <summary>
        /// Request MAC hesaplar - OOS işlemi başlatırken bankaya gönderilecek MAC
        /// Formül: HASH(xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
        /// POSNET Doküman Sayfa 37
        /// </summary>
        /// <param name="xid">İşlem referans numarası (20 karakter max)</param>
        /// <param name="amount">Tutar (YKr cinsinden, örn: "10000" = 100.00 TL)</param>
        /// <param name="currency">Para birimi kodu (TL=TRY, US=USD, EU=EUR)</param>
        /// <param name="merchantNo">Üye işyeri numarası</param>
        /// <param name="terminalId">Terminal ID</param>
        /// <returns>Base64 encoded SHA-256 MAC</returns>
        string CalculateRequestMac(string xid, string amount, string currency, 
            string merchantNo, string terminalId);

        /// <summary>
        /// Response MAC hesaplar - Banka yanıtını doğrulamak için
        /// Formül: HASH(mdStatus + ';' + xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
        /// POSNET Doküman Sayfa 38 - 3D Secure callback doğrulaması için kritik
        /// </summary>
        /// <param name="mdStatus">3D Secure doğrulama durumu (1-9, 0)</param>
        /// <param name="xid">İşlem referans numarası</param>
        /// <param name="amount">Tutar (YKr cinsinden)</param>
        /// <param name="currency">Para birimi kodu</param>
        /// <param name="merchantNo">Üye işyeri numarası</param>
        /// <param name="terminalId">Terminal ID</param>
        /// <returns>Base64 encoded SHA-256 MAC</returns>
        string CalculateResponseMac(string mdStatus, string xid, string amount, 
            string currency, string merchantNo, string terminalId);

        /// <summary>
        /// Banka'dan gelen Response MAC'i doğrular
        /// Hesaplanan MAC ile bankadan gelen MAC karşılaştırılır
        /// Eşleşmezse MAN-IN-THE-MIDDLE saldırısı olabilir!
        /// </summary>
        /// <param name="bankMac">Bankadan gelen MAC değeri</param>
        /// <param name="mdStatus">3D Secure durumu</param>
        /// <param name="xid">İşlem referans numarası</param>
        /// <param name="amount">Tutar</param>
        /// <param name="currency">Para birimi</param>
        /// <param name="merchantNo">Üye işyeri numarası</param>
        /// <param name="terminalId">Terminal ID</param>
        /// <returns>MAC doğrulama sonucu</returns>
        PosnetMacValidationResult ValidateResponseMac(string bankMac, string mdStatus, 
            string xid, string amount, string currency, string merchantNo, string terminalId);

        // ═══════════════════════════════════════════════════════════════════════════════
        // ESKİ METODLAR (Geriye uyumluluk için korunuyor - DEPRECATED)
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// [DEPRECATED] OOS Request için MAC1 değeri hesaplar
        /// Yeni kod için CalculateRequestMac kullanın
        /// </summary>
        [Obsolete("CalculateRequestMac metodunu kullanın - POSNET v2.1.1.3 uyumu için")]
        string CalculateMac1(string merchantId, string terminalId, string amount, 
            string currencyCode, string xid, string tranType);

        /// <summary>
        /// [DEPRECATED] OOS Resolution için MAC2 değeri hesaplar
        /// Yeni kod için CalculateResponseMac kullanın
        /// </summary>
        [Obsolete("CalculateResponseMac metodunu kullanın - POSNET v2.1.1.3 uyumu için")]
        string CalculateMac2(string merchantId, string terminalId, string mdStatus);

        /// <summary>
        /// [DEPRECATED] 3D Secure tamamlama için MAC3 değeri hesaplar
        /// Yeni kod için CalculateRequestMac kullanın
        /// </summary>
        [Obsolete("CalculateRequestMac metodunu kullanın - POSNET v2.1.1.3 uyumu için")]
        string CalculateMac3(string bankData, string merchantData, string sign);

        // ═══════════════════════════════════════════════════════════════════════════════
        // YARDIMCI METODLAR
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Bankadan gelen MAC değerini doğrular (güvenlik kritik)
        /// Timing attack koruması ile sabit zamanlı karşılaştırma yapar
        /// </summary>
        /// <param name="expectedMac">Bankadan gelen MAC değeri</param>
        /// <param name="calculatedMac">Bizim hesapladığımız MAC değeri</param>
        /// <returns>MAC'ler eşleşiyor mu?</returns>
        bool ValidateMac(string expectedMac, string calculatedMac);

        /// <summary>
        /// 3D Secure callback'ten gelen tüm parametreleri doğrular
        /// MAC doğrulama + MdStatus değerlendirme yapar
        /// </summary>
        Posnet3DSecureMacValidationResult ValidateCallback(Posnet3DSecureCallbackData callbackData);

        /// <summary>
        /// XID (Transaction ID) oluşturur - Her işlem için benzersiz
        /// Format: YYYYMMDD + OrderId (5 digit) + Random (6 char) = 20 karakter
        /// </summary>
        string GenerateXid(int orderId);

        /// <summary>
        /// Kart verilerini hash'ler (PCI DSS uyumu için loglama güvenliği)
        /// SHA-256 ile tek yönlü hash - geri dönüştürülemez
        /// </summary>
        string HashCardData(string cardNumber);
    }

    /// <summary>
    /// MAC doğrulama sonucu (basit validasyon için)
    /// </summary>
    public class PosnetMacValidationResult
    {
        /// <summary>
        /// Doğrulama başarılı mı?
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Hata mesajı (başarısız ise)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Hesaplanan MAC değeri (debug için)
        /// </summary>
        public string? CalculatedMac { get; set; }

        /// <summary>
        /// Beklenen MAC değeri (debug için)
        /// </summary>
        public string? ExpectedMac { get; set; }

        /// <summary>
        /// Doğrulama timestamp'i
        /// </summary>
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Başarılı sonuç oluşturur
        /// </summary>
        public static PosnetMacValidationResult Success() => new()
        {
            IsValid = true
        };

        /// <summary>
        /// Başarısız sonuç oluşturur
        /// </summary>
        public static PosnetMacValidationResult Failure(string errorMessage, 
            string? expected = null, string? calculated = null) => new()
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            ExpectedMac = expected,
            CalculatedMac = calculated
        };
    }

    /// <summary>
    /// MAC doğrulama sonucu
    /// </summary>
    public class Posnet3DSecureMacValidationResult
    {
        /// <summary>
        /// Doğrulama başarılı mı?
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Hata mesajı (başarısız ise)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// MAC doğrulama detayı
        /// </summary>
        public string? ValidationDetails { get; set; }

        /// <summary>
        /// 3D Secure durumu (MdStatus)
        /// 1 = Tam doğrulama başarılı
        /// 2,3,4 = Kısmi doğrulama (kart sahibi veya banka katılmıyor)
        /// 5,6,7,8,9,0 = Başarısız
        /// </summary>
        public string? MdStatus { get; set; }

        /// <summary>
        /// MD Status açıklaması
        /// </summary>
        public string? MdStatusDescription { get; set; }

        /// <summary>
        /// 3D Secure işlem sonucu güvenli mi?
        /// MdStatus 1,2,3,4 değerlerinde true
        /// </summary>
        public bool Is3DSecureSuccessful => MdStatus == "1" || MdStatus == "2" || 
                                             MdStatus == "3" || MdStatus == "4";

        /// <summary>
        /// Tam 3D Secure doğrulaması yapıldı mı?
        /// Sadece MdStatus = 1 olduğunda true
        /// </summary>
        public bool IsFullyAuthenticated => MdStatus == "1";

        /// <summary>
        /// İşlem devam edebilir mi?
        /// </summary>
        public bool CanProceedWithPayment => IsValid && Is3DSecureSuccessful;

        public static Posnet3DSecureMacValidationResult Success(string mdStatus) => new()
        {
            IsValid = true,
            MdStatus = mdStatus,
            MdStatusDescription = GetMdStatusDescription(mdStatus)
        };

        public static Posnet3DSecureMacValidationResult Failure(string errorMessage) => new()
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };

        private static string GetMdStatusDescription(string mdStatus)
        {
            return mdStatus switch
            {
                "1" => "Tam Doğrulama - Kart sahibi başarıyla doğrulandı",
                "2" => "Kart Sahibi veya Bankası Sisteme Kayıtlı Değil",
                "3" => "Kart Sahibi Bankası Sisteme Kayıtlı Değil",
                "4" => "Doğrulama Denemesi - Kart Sahibi Sisteme Kayıtlı Değil",
                "5" => "Doğrulama Yapılamadı",
                "6" => "3D Secure Hatası",
                "7" => "Sistem Hatası",
                "8" => "Bilinmeyen Kart No",
                "9" => "Üye İşyeri 3D-Secure Sisteminde Kayıtlı Değil",
                "0" => "Doğrulama Başarısız - Kart Sahibi Şifre Hatalı",
                _ => $"Bilinmeyen Durum: {mdStatus}"
            };
        }
    }

    /// <summary>
    /// 3D Secure callback verileri (Bankadan gelen POST parametreleri)
    /// </summary>
    public class Posnet3DSecureCallbackData
    {
        // ═══════════════════════════════════════════════════════════════
        // OOS PARAMETRELER (Bankadan POST ile gelir)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Banka tarafından oluşturulan 3D Secure verileri (şifreli)
        /// </summary>
        public string? BankData { get; set; }

        /// <summary>
        /// İşyeri tarafından gönderilen ve geri dönen veriler
        /// </summary>
        public string? MerchantData { get; set; }

        /// <summary>
        /// Dijital imza değeri
        /// </summary>
        public string? Sign { get; set; }

        /// <summary>
        /// MAC doğrulama değeri (Bankadan hesaplanmış)
        /// </summary>
        public string? Mac { get; set; }

        // ═══════════════════════════════════════════════════════════════
        // 3D SECURE DURUM BİLGİLERİ
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 3D Secure doğrulama durumu (kritik alan)
        /// 1 = Başarılı, 2-4 = Kısmi, 0/5-9 = Başarısız
        /// </summary>
        public string? MdStatus { get; set; }

        /// <summary>
        /// 3D Secure hata kodu
        /// </summary>
        public string? MdErrorMessage { get; set; }

        /// <summary>
        /// İşlem referans numarası (XID)
        /// </summary>
        public string? Xid { get; set; }

        /// <summary>
        /// ECI (Electronic Commerce Indicator) değeri
        /// </summary>
        public string? Eci { get; set; }

        /// <summary>
        /// CAVV (Cardholder Authentication Verification Value)
        /// </summary>
        public string? Cavv { get; set; }

        // ═══════════════════════════════════════════════════════════════
        // İŞLEM BİLGİLERİ
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// İşlem tutarı (YKr - Yeni Kuruş cinsinden)
        /// </summary>
        public string? Amount { get; set; }

        /// <summary>
        /// Para birimi kodu (TL = TL, USD = US, EUR = EU)
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Taksit sayısı
        /// </summary>
        public string? InstallmentCount { get; set; }

        /// <summary>
        /// Sipariş ID (MerchantData içinden parse edilir)
        /// </summary>
        public int? OrderId { get; set; }
    }

    /// <summary>
    /// POSNET MAC Validator implementasyonu
    /// SHA-256 + Base64 encoding kullanır
    /// POSNET Dokümantasyonu v2.1.1.3 - Sayfa 35-42'ye tam uyumlu
    /// 
    /// MAC Formülleri:
    /// - FirstHash = SHA256(encKey + ';' + terminalID) -> Base64
    /// - RequestMAC = SHA256(xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash) -> Base64
    /// - ResponseMAC = SHA256(mdStatus + ';' + xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash) -> Base64
    /// </summary>
    public class PosnetMacValidator : IPosnetMacValidator
    {
        private readonly PaymentSettings _settings;
        private readonly ILogger<PosnetMacValidator> _logger;

        // Ayraç karakteri - POSNET dokümanına göre noktalı virgül kullanılmalı
        private const char MAC_SEPARATOR = ';';

        public PosnetMacValidator(
            IOptions<PaymentSettings> options,
            ILogger<PosnetMacValidator> logger)
        {
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Enckey kontrolü - kritik güvenlik parametresi
            if (string.IsNullOrWhiteSpace(_settings.PosnetEncKey))
            {
                _logger.LogWarning("[POSNET-MAC] EncKey yapılandırılmamış! 3D Secure işlemleri başarısız olacak.");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // POSNET v2.1.1.3 DOKÜMANTASYONUNA UYGUN YENİ MAC METODLARI
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public string CalculateFirstHash(string terminalId)
        {
            // ═══════════════════════════════════════════════════════════════
            // FIRST HASH FORMÜLÜ (POSNET Doküman Sayfa 36):
            // FirstHash = Base64(SHA256(encKey + ';' + terminalID))
            // 
            // Bu hash, tüm diğer MAC hesaplamalarının temelini oluşturur.
            // EncKey bankadan alınan gizli anahtardır ve güvenli saklanmalıdır.
            // 
            // ENCKEY FORMAT DESTEĞİ:
            // 1. Hex string: "0A0A0A0A0A0A0A0A" → olduğu gibi kullanılır
            // 2. Virgüllü byte: "10,10,10,10,10,10,10,10" → hex'e çevrilir
            // 3. Düz string: "test123" → olduğu gibi kullanılır
            // ═══════════════════════════════════════════════════════════════

            if (string.IsNullOrWhiteSpace(terminalId))
            {
                throw new ArgumentNullException(nameof(terminalId), 
                    "Terminal ID boş olamaz - FirstHash hesaplaması için gerekli");
            }

            var rawEncKey = _settings.PosnetEncKey;
            if (string.IsNullOrWhiteSpace(rawEncKey))
            {
                throw new InvalidOperationException(
                    "PosnetEncKey yapılandırılmamış! appsettings.json'da Payment:PosnetEncKey değerini ayarlayın.");
            }

            // ENCKEY formatını normalize et
            var encKey = NormalizeEncKey(rawEncKey);

            // Formül: encKey + ';' + terminalID
            var dataToHash = $"{encKey}{MAC_SEPARATOR}{terminalId}";

            var firstHash = ComputeHash(dataToHash);

            _logger.LogDebug("[POSNET-MAC] FirstHash hesaplandı - TerminalID: {TerminalId}, " +
                "EncKey format: {Format}, Hash uzunluğu: {HashLen}", 
                MaskSensitiveData(terminalId, 4), 
                rawEncKey.Contains(',') ? "comma-separated" : "string",
                firstHash.Length);

            return firstHash;
        }

        /// <summary>
        /// ENCKEY formatını normalize eder
        /// Yapı Kredi farklı formatlarda ENCKEY verebilir:
        /// - "10,10,10,10,10,10,10,10" → Her byte decimal olarak verilmiş
        /// - "0A0A0A0A0A0A0A0A" → Hex string
        /// - "testkey123" → Düz string
        /// </summary>
        /// <param name="rawEncKey">Ham ENCKEY değeri</param>
        /// <returns>Normalize edilmiş ENCKEY</returns>
        private static string NormalizeEncKey(string rawEncKey)
        {
            if (string.IsNullOrWhiteSpace(rawEncKey))
                return rawEncKey;

            // Virgülle ayrılmış byte formatı mı kontrol et
            // Örnek: "10,10,10,10,10,10,10,10"
            if (rawEncKey.Contains(','))
            {
                try
                {
                    var parts = rawEncKey.Split(',');
                    var bytes = new byte[parts.Length];
                    
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (byte.TryParse(parts[i].Trim(), out var b))
                        {
                            bytes[i] = b;
                        }
                        else
                        {
                            // Parse edilemezse orijinal string'i döndür
                            return rawEncKey;
                        }
                    }

                    // Byte array'i hex string'e çevir (uppercase)
                    return BitConverter.ToString(bytes).Replace("-", "");
                }
                catch
                {
                    // Hata durumunda orijinal string'i döndür
                    return rawEncKey;
                }
            }

            // Zaten hex veya düz string ise olduğu gibi döndür
            return rawEncKey;
        }

        /// <inheritdoc/>
        public string CalculateRequestMac(string xid, string amount, string currency, 
            string merchantNo, string terminalId)
        {
            // ═══════════════════════════════════════════════════════════════
            // REQUEST MAC FORMÜLÜ (POSNET Doküman Sayfa 37):
            // MAC = Base64(SHA256(xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash))
            // 
            // Bu MAC, OOS işlemi başlatırken bankaya gönderilir.
            // Banka bu MAC'i kullanarak isteğin bütünlüğünü doğrular.
            // ═══════════════════════════════════════════════════════════════

            // Parametre validasyonu - tüm alanlar zorunlu
            ValidateMacParameters(nameof(CalculateRequestMac), 
                (nameof(xid), xid),
                (nameof(amount), amount),
                (nameof(currency), currency),
                (nameof(merchantNo), merchantNo),
                (nameof(terminalId), terminalId));

            // Önce FirstHash hesapla
            var firstHash = CalculateFirstHash(terminalId);

            // Request MAC formülü: xid;amount;currency;merchantNo;firstHash
            var dataToHash = string.Join(MAC_SEPARATOR.ToString(), 
                xid, amount, currency, merchantNo, firstHash);

            var mac = ComputeHash(dataToHash);

            _logger.LogDebug("[POSNET-MAC] RequestMAC hesaplandı - XID: {Xid}, Amount: {Amount}, " +
                "Currency: {Currency}", xid, amount, currency);

            return mac;
        }

        /// <inheritdoc/>
        public string CalculateResponseMac(string mdStatus, string xid, string amount, 
            string currency, string merchantNo, string terminalId)
        {
            // ═══════════════════════════════════════════════════════════════
            // RESPONSE MAC FORMÜLÜ (POSNET Doküman Sayfa 38):
            // MAC = Base64(SHA256(mdStatus + ';' + xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash))
            // 
            // Bu MAC, bankadan gelen 3D Secure callback yanıtını doğrulamak için kullanılır.
            // Eşleşmezse MAN-IN-THE-MIDDLE saldırısı olasılığı vardır!
            // ═══════════════════════════════════════════════════════════════

            // Parametre validasyonu - tüm alanlar zorunlu
            ValidateMacParameters(nameof(CalculateResponseMac), 
                (nameof(mdStatus), mdStatus),
                (nameof(xid), xid),
                (nameof(amount), amount),
                (nameof(currency), currency),
                (nameof(merchantNo), merchantNo),
                (nameof(terminalId), terminalId));

            // Önce FirstHash hesapla
            var firstHash = CalculateFirstHash(terminalId);

            // Response MAC formülü: mdStatus;xid;amount;currency;merchantNo;firstHash
            var dataToHash = string.Join(MAC_SEPARATOR.ToString(), 
                mdStatus, xid, amount, currency, merchantNo, firstHash);

            var mac = ComputeHash(dataToHash);

            _logger.LogDebug("[POSNET-MAC] ResponseMAC hesaplandı - MdStatus: {MdStatus}, XID: {Xid}", 
                mdStatus, xid);

            return mac;
        }

        /// <inheritdoc/>
        public PosnetMacValidationResult ValidateResponseMac(string bankMac, string mdStatus, 
            string xid, string amount, string currency, string merchantNo, string terminalId)
        {
            // ═══════════════════════════════════════════════════════════════
            // BANKA RESPONSE MAC DOĞRULAMASI
            // 
            // Bankadan gelen MAC değeri ile bizim hesapladığımız MAC karşılaştırılır.
            // Bu doğrulama MAN-IN-THE-MIDDLE saldırılarına karşı kritik koruma sağlar.
            // 
            // Timing attack koruması için sabit zamanlı karşılaştırma kullanılır.
            // ═══════════════════════════════════════════════════════════════

            try
            {
                // Bankadan gelen MAC boş olamaz
                if (string.IsNullOrWhiteSpace(bankMac))
                {
                    _logger.LogWarning("[POSNET-MAC] Banka MAC değeri boş - XID: {Xid}", xid);
                    return PosnetMacValidationResult.Failure(
                        "Bankadan gelen MAC değeri boş - İşlem güvenliği doğrulanamıyor");
                }

                // Bizim hesapladığımız MAC
                var calculatedMac = CalculateResponseMac(mdStatus, xid, amount, 
                    currency, merchantNo, terminalId);

                // Güvenli karşılaştırma (timing attack koruması)
                var isValid = ValidateMac(bankMac, calculatedMac);

                if (!isValid)
                {
                    _logger.LogWarning("[POSNET-MAC] Response MAC doğrulama BAŞARISIZ! " +
                        "XID: {Xid}, MdStatus: {MdStatus}. " +
                        "OLASI MAN-IN-THE-MIDDLE SALDIRISI!", xid, mdStatus);

                    return PosnetMacValidationResult.Failure(
                        "MAC doğrulama başarısız - İşlem güvenliği sağlanamadı. " +
                        "Olası veri manipülasyonu tespit edildi.",
                        MaskMacForLog(bankMac),
                        MaskMacForLog(calculatedMac));
                }

                _logger.LogInformation("[POSNET-MAC] Response MAC doğrulama başarılı - " +
                    "XID: {Xid}, MdStatus: {MdStatus}", xid, mdStatus);

                return PosnetMacValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[POSNET-MAC] Response MAC doğrulama hatası - XID: {Xid}", xid);
                return PosnetMacValidationResult.Failure($"MAC doğrulama hatası: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ESKİ METODLAR (Geriye uyumluluk için - DEPRECATED)
        // Yeni kod için yukarıdaki metodları kullanın
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        [Obsolete("CalculateRequestMac metodunu kullanın - POSNET v2.1.1.3 uyumu için")]
        public string CalculateMac1(string merchantId, string terminalId, string amount,
            string currencyCode, string xid, string tranType)
        {
            // Geriye uyumluluk: Eski formülü koruyoruz ama yeni koda yönlendiriyoruz
            // Eski formül: MerchantId + TerminalId + Amount + CurrencyCode + XID + TranType + Enckey (ayraçsız)
            // Yeni formül: xid;amount;currency;merchantNo;firstHash (noktalı virgül ayraçlı)
            
            _logger.LogWarning("[POSNET-MAC] CalculateMac1 kullanımı deprecated. " +
                "CalculateRequestMac metoduna geçin.");

            // Yeni formata dönüştür
            return CalculateRequestMac(xid, amount, currencyCode, merchantId, terminalId);
        }

        /// <inheritdoc/>
        [Obsolete("CalculateResponseMac metodunu kullanın - POSNET v2.1.1.3 uyumu için")]
        public string CalculateMac2(string merchantId, string terminalId, string mdStatus)
        {
            // Geriye uyumluluk: Bu metodun parametreleri eksik olduğu için
            // sadece uyarı logla ve eski formülü uygula
            
            _logger.LogWarning("[POSNET-MAC] CalculateMac2 kullanımı deprecated. " +
                "CalculateResponseMac metoduna geçin. " +
                "Bu metod tam parametreler olmadan çalışıyor!");

            // Eski formül (eksik parametrelerle)
            var encKey = _settings.PosnetEncKey ?? string.Empty;
            var dataToHash = $"{merchantId}{terminalId}{mdStatus}{encKey}";
            return ComputeHash(dataToHash);
        }

        /// <inheritdoc/>
        [Obsolete("CalculateRequestMac metodunu kullanın - POSNET v2.1.1.3 uyumu için")]
        public string CalculateMac3(string bankData, string merchantData, string sign)
        {
            // Geriye uyumluluk: OOS-TDS için kullanılıyordu
            
            _logger.LogWarning("[POSNET-MAC] CalculateMac3 kullanımı deprecated. " +
                "CalculateRequestMac metoduna geçin.");

            var encKey = _settings.PosnetEncKey ?? string.Empty;
            var dataToHash = $"{bankData}{merchantData}{sign}{encKey}";
            return ComputeHash(dataToHash);
        }

        /// <inheritdoc/>
        public bool ValidateMac(string expectedMac, string calculatedMac)
        {
            // Güvenlik: Timing attack'ları önlemek için sabit zamanlı karşılaştırma
            // String.Equals yerine CryptographicOperations kullanıyoruz
            
            if (string.IsNullOrWhiteSpace(expectedMac) || string.IsNullOrWhiteSpace(calculatedMac))
            {
                _logger.LogWarning("[POSNET-MAC] Doğrulama başarısız - Boş MAC değeri");
                return false;
            }

            // Base64 decode edip byte karşılaştırması yap (timing-safe)
            try
            {
                var expectedBytes = Convert.FromBase64String(expectedMac);
                var calculatedBytes = Convert.FromBase64String(calculatedMac);

                // Sabit zamanlı karşılaştırma - side-channel attack koruması
                var isValid = CryptographicOperations.FixedTimeEquals(expectedBytes, calculatedBytes);

                if (!isValid)
                {
                    _logger.LogWarning("[POSNET-MAC] Doğrulama başarısız - MAC değerleri eşleşmiyor! " +
                        "Olası MAN-IN-THE-MIDDLE saldırısı.");
                }

                return isValid;
            }
            catch (FormatException ex)
            {
                // Base64 decode hatası - geçersiz MAC formatı
                _logger.LogError(ex, "[POSNET-MAC] Geçersiz Base64 formatı - ExpectedMac: {Expected}", 
                    TruncateForLog(expectedMac));
                return false;
            }
        }

        /// <inheritdoc/>
        public Posnet3DSecureMacValidationResult ValidateCallback(Posnet3DSecureCallbackData callbackData)
        {
            // ═══════════════════════════════════════════════════════════════
            // 3D SECURE CALLBACK DOĞRULAMA ADIMLARI (POSNET v2.1.1.3):
            // 1. Null/Empty kontrolleri
            // 2. Response MAC doğrulama (yeni formül ile)
            // 3. MdStatus değerlendirme
            // 
            // NOT: Yeni POSNET dokümantasyonuna göre Response MAC formülü:
            // HASH(mdStatus + ';' + xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
            // ═══════════════════════════════════════════════════════════════

            try
            {
                // ADIM 1: Zorunlu alan kontrolleri
                if (callbackData == null)
                {
                    return Posnet3DSecureMacValidationResult.Failure("Callback verisi null");
                }

                if (string.IsNullOrWhiteSpace(callbackData.BankData))
                {
                    return Posnet3DSecureMacValidationResult.Failure("BankData parametresi eksik");
                }

                if (string.IsNullOrWhiteSpace(callbackData.MerchantData))
                {
                    return Posnet3DSecureMacValidationResult.Failure("MerchantData parametresi eksik");
                }

                if (string.IsNullOrWhiteSpace(callbackData.Sign))
                {
                    return Posnet3DSecureMacValidationResult.Failure("Sign parametresi eksik");
                }

                // MAC ve MdStatus callback'te her zaman dönmeyebilir (banka akışına göre değişir).
                // Bu durumda doğrulama oosResolveMerchantData adımında yapılır.

                // ADIM 2: Response MAC doğrulama
                // Eğer XID, Amount, Currency bilgileri mevcutsa yeni formülü kullan
                if (!string.IsNullOrWhiteSpace(callbackData.Mac) &&
                    !string.IsNullOrWhiteSpace(callbackData.MdStatus) &&
                    !string.IsNullOrWhiteSpace(callbackData.Xid) &&
                    !string.IsNullOrWhiteSpace(callbackData.Amount) &&
                    !string.IsNullOrWhiteSpace(callbackData.Currency))
                {
                    // Yeni POSNET v2.1.1.3 formülü ile doğrulama
                    var merchantNo = _settings.PosnetMerchantId ?? string.Empty;
                    var terminalId = _settings.PosnetTerminalId ?? string.Empty;

                    var macValidationResult = ValidateResponseMac(
                        callbackData.Mac,
                        callbackData.MdStatus,
                        callbackData.Xid,
                        callbackData.Amount,
                        callbackData.Currency,
                        merchantNo,
                        terminalId);

                    if (!macValidationResult.IsValid)
                    {
                        _logger.LogWarning("[POSNET-3DS] Response MAC doğrulama başarısız! " +
                            "XID: {Xid}, OrderId: {OrderId}, Hata: {Error}",
                            callbackData.Xid, callbackData.OrderId, macValidationResult.ErrorMessage);

                        return Posnet3DSecureMacValidationResult.Failure(
                            "Response MAC doğrulama başarısız - İşlem güvenliği sağlanamadı");
                    }

                    _logger.LogInformation("[POSNET-3DS] Response MAC doğrulama başarılı (v2.1.1.3) - " +
                        "XID: {Xid}", callbackData.Xid);
                }
                else if (!string.IsNullOrWhiteSpace(callbackData.Mac))
                {
                    // Fallback: Eski MAC3 formülü ile doğrulama (geriye uyumluluk)
                    _logger.LogWarning("[POSNET-3DS] XID/Amount/Currency eksik, eski MAC3 formülü kullanılıyor. " +
                        "OrderId: {OrderId}", callbackData.OrderId);

#pragma warning disable CS0618 // Obsolete uyarısını bastır - geriye uyumluluk için gerekli
                    var calculatedMac = CalculateMac3(
                        callbackData.BankData,
                        callbackData.MerchantData,
                        callbackData.Sign);
#pragma warning restore CS0618

                    var macValid = ValidateMac(callbackData.Mac, calculatedMac);

                    if (!macValid)
                    {
                        _logger.LogWarning("[POSNET-3DS] MAC3 doğrulama başarısız! OrderId: {OrderId}",
                            callbackData.OrderId);

                        return Posnet3DSecureMacValidationResult.Failure(
                            "MAC doğrulama başarısız - İşlem güvenliği sağlanamadı");
                    }
                }

                // ADIM 3: MdStatus değerlendirme (yoksa resolve adımında kontrol edilecek)
                if (string.IsNullOrWhiteSpace(callbackData.MdStatus))
                {
                    return new Posnet3DSecureMacValidationResult
                    {
                        IsValid = true,
                        MdStatus = null,
                        MdStatusDescription = "MdStatus callback'te gelmedi; resolve adımında kontrol edilecek"
                    };
                }

                var result = Posnet3DSecureMacValidationResult.Success(callbackData.MdStatus);

                _logger.LogInformation("[POSNET-3DS] Callback doğrulandı - MdStatus: {MdStatus}, " +
                    "3DSecure: {Is3D}, FullAuth: {FullAuth}, OrderId: {OrderId}",
                    callbackData.MdStatus,
                    result.Is3DSecureSuccessful,
                    result.IsFullyAuthenticated,
                    callbackData.OrderId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[POSNET-3DS] Callback doğrulama hatası");
                return Posnet3DSecureMacValidationResult.Failure($"Doğrulama hatası: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public string GenerateXid(int orderId)
        {
            // XID formatı: YYYYMMDD-OrderId-Random (toplam 20 karakter max)
            // POSNET XID maksimum 20 karakter kabul eder
            
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            
            // Format: YYYYMMDD-XXXXX-RANDOM (20 char limit)
            var xid = $"{timestamp}{orderId:D5}{random}";
            
            // XID 20 karakteri geçmemeli
            if (xid.Length > 20)
            {
                xid = xid[..20];
            }

            _logger.LogDebug("[POSNET-XID] Oluşturuldu: {Xid} (OrderId: {OrderId})", xid, orderId);

            return xid;
        }

        /// <inheritdoc/>
        public string HashCardData(string cardNumber)
        {
            // PCI DSS uyumu için kart numarası loglarda hash'lenerek saklanır
            // SHA-256 ile tek yönlü hash - geri dönüştürülemez
            
            if (string.IsNullOrWhiteSpace(cardNumber))
            {
                return "EMPTY";
            }

            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(cardNumber));
            
            // İlk 16 karakter yeterli (collision riski düşük)
            return Convert.ToHexString(bytes)[..16];
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // PRİVATE HELPER METODLAR
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// SHA-256 hash hesaplar ve Base64 encode eder
        /// POSNET standart MAC formatı
        /// </summary>
        private static string ComputeHash(string data)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// MAC parametrelerini doğrular - tüm alanlar zorunlu
        /// </summary>
        /// <param name="methodName">Çağıran metod adı (log için)</param>
        /// <param name="parameters">Parametre adı ve değer çiftleri</param>
        /// <exception cref="ArgumentNullException">Herhangi bir parametre null/empty ise</exception>
        private void ValidateMacParameters(string methodName, params (string Name, string? Value)[] parameters)
        {
            foreach (var (name, value) in parameters)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _logger.LogError("[POSNET-MAC] {MethodName} - {ParamName} parametresi boş!", 
                        methodName, name);
                    throw new ArgumentNullException(name, 
                        $"MAC hesaplaması için {name} parametresi zorunludur");
                }
            }
        }

        /// <summary>
        /// Hassas veriyi log için maskeler
        /// Örnek: "1234567890" -> "1234****90"
        /// </summary>
        /// <param name="value">Maskelenecek değer</param>
        /// <param name="visibleChars">Baştan ve sondan görünür kalacak karakter sayısı</param>
        private static string MaskSensitiveData(string value, int visibleChars = 4)
        {
            if (string.IsNullOrEmpty(value)) return "NULL";
            if (value.Length <= visibleChars * 2) return new string('*', value.Length);
            
            return $"{value[..visibleChars]}****{value[^visibleChars..]}";
        }

        /// <summary>
        /// MAC değerini log için maskeler (güvenlik)
        /// Sadece ilk 8 ve son 4 karakter gösterilir
        /// </summary>
        private static string MaskMacForLog(string mac)
        {
            if (string.IsNullOrEmpty(mac)) return "NULL";
            if (mac.Length <= 12) return "****";
            
            return $"{mac[..8]}...{mac[^4..]}";
        }

        /// <summary>
        /// Log için hassas veriyi kısaltır
        /// </summary>
        private static string TruncateForLog(string value, int maxLength = 20)
        {
            if (string.IsNullOrEmpty(value)) return "NULL";
            if (value.Length <= maxLength) return value;
            return value[..maxLength] + "...";
        }
    }

    /// <summary>
    /// MerchantData parser - Geri dönen merchant verisini parse eder
    /// </summary>
    public static class PosnetMerchantDataParser
    {
        /// <summary>
        /// MerchantData'dan OrderId çıkarır
        /// Format: "OrderId=123|Amount=1000|..."
        /// </summary>
        public static int? ExtractOrderId(string? merchantData)
        {
            if (string.IsNullOrWhiteSpace(merchantData))
                return null;

            try
            {
                // Base64 decode et (MerchantData Base64 encoded olabilir)
                string decoded;
                try
                {
                    var bytes = Convert.FromBase64String(merchantData);
                    decoded = Encoding.UTF8.GetString(bytes);
                }
                catch
                {
                    // Base64 değilse direkt kullan
                    decoded = merchantData;
                }

                // "OrderId=123" formatını parse et
                var parts = decoded.Split('|', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (part.StartsWith("OrderId=", StringComparison.OrdinalIgnoreCase))
                    {
                        var value = part["OrderId=".Length..];
                        if (int.TryParse(value, out var orderId))
                        {
                            return orderId;
                        }
                    }
                }

                // Alternatif: Sadece sayı ise OrderId olarak kabul et
                if (int.TryParse(decoded, out var directOrderId))
                {
                    return directOrderId;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// MerchantData oluşturur (3D Secure başlatırken kullanılır)
        /// </summary>
        public static string CreateMerchantData(int orderId, decimal amount, string? extra = null)
        {
            var data = $"OrderId={orderId}|Amount={amount:F2}|Ts={DateTime.UtcNow.Ticks}";
            
            if (!string.IsNullOrWhiteSpace(extra))
            {
                data += $"|{extra}";
            }

            // Base64 encode et
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
        }
    }
}
