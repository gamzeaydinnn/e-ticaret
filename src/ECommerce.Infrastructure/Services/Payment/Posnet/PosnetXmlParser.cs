// ═══════════════════════════════════════════════════════════════════════════════════════════════
// POSNET XML PARSER
// Yapı Kredi POSNET XML API response'larını parse eden servis
// Dokümantasyon: POSNET XML Servisleri Entegrasyon Dokümanı v2.1.1.3
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// NEDEN BU YAPIYI SEÇTİK?
// 1. XDocument kullanımı - LINQ to XML ile güvenli ve okunabilir parsing
// 2. Null-safe navigation - ?. operatörü ile NullReferenceException önleme
// 3. Exception isolation - Her parse işlemi try-catch ile sarmalanmış
// 4. Factory pattern - Response tipine göre doğru model oluşturma
// 5. Defensive programming - Invalid XML için graceful degradation
// ═══════════════════════════════════════════════════════════════════════════════════════════════

using System;
using System.Xml.Linq;
using ECommerce.Infrastructure.Services.Payment.Posnet.Models;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services.Payment.Posnet
{
    /// <summary>
    /// POSNET XML parser interface
    /// Dependency Injection ve Unit Test için
    /// </summary>
    public interface IPosnetXmlParser
    {
        /// <summary>Satış yanıtını parse eder</summary>
        PosnetSaleResponse ParseSaleResponse(string xml);
        
        /// <summary>Provizyon yanıtını parse eder</summary>
        PosnetAuthResponse ParseAuthResponse(string xml);
        
        /// <summary>Finansallaştırma yanıtını parse eder</summary>
        PosnetCaptResponse ParseCaptResponse(string xml);
        
        /// <summary>İptal yanıtını parse eder</summary>
        PosnetReverseResponse ParseReverseResponse(string xml);
        
        /// <summary>İade yanıtını parse eder</summary>
        PosnetReturnResponse ParseReturnResponse(string xml);
        
        /// <summary>Puan sorgulama yanıtını parse eder</summary>
        PosnetPointInquiryResponse ParsePointInquiryResponse(string xml);
        
        /// <summary>İşlem durumu sorgulama yanıtını parse eder</summary>
        PosnetAgreementResponse ParseAgreementResponse(string xml);
        
        /// <summary>3D Secure OOS yanıtını parse eder</summary>
        PosnetOosResponse ParseOosResponse(string xml);
        
        /// <summary>
        /// 3D Secure oosResolveMerchantData yanıtını parse eder
        /// Banka callback'inden gelen şifreli verilerin deşifre edilmiş halini döner
        /// POSNET Dokümantasyonu: Sayfa 12-14
        /// </summary>
        /// <param name="xml">Banka response XML'i</param>
        /// <returns>Deşifre edilmiş işlem verileri (xid, amount, mdStatus vb.)</returns>
        PosnetOosResolveMerchantDataResponse ParseOosResolveMerchantDataResponse(string xml);
        
        /// <summary>
        /// 3D Secure oosTranData (Finansallaştırma) yanıtını parse eder
        /// POSNET Dokümantasyonu: Sayfa 15-17
        /// </summary>
        /// <param name="xml">Banka response XML'i</param>
        /// <returns>Finansallaştırma sonucu (hostlogkey, authCode, mac vb.)</returns>
        PosnetOosTranDataResponse ParseOosTranDataResponse(string xml);
        
        /// <summary>Generic yanıt parse - Sadece başarı/hata kontrolü</summary>
        (bool approved, string? errorCode, string? errorMessage) ParseBasicResponse(string xml);
    }

    /// <summary>
    /// POSNET XML Parser implementasyonu
    /// Tüm POSNET response tiplerini güvenli şekilde parse eder
    /// </summary>
    public class PosnetXmlParser : IPosnetXmlParser
    {
        private readonly ILogger<PosnetXmlParser>? _logger;

        // ═══════════════════════════════════════════════════════════════════════
        // XML ELEMENT NAMES - POSNET response element isimleri
        // ═══════════════════════════════════════════════════════════════════════

        private const string APPROVED = "approved";
        private const string RESP_CODE = "respCode";
        private const string RESP_TEXT = "respText";
        private const string HOST_LOG_KEY = "hostlogkey";
        private const string AUTH_CODE = "authCode";
        private const string ORDER_ID = "orderID";
        private const string AMOUNT = "amount";
        private const string INSTALLMENT = "installment";
        private const string RRN = "rrn";
        private const string TRANS_ID = "tranId";
        private const string WORLD_POINT = "point";
        private const string BRAND_POINT = "brandPoint";
        private const string DATA1 = "data1";
        private const string DATA2 = "data2";
        private const string SIGN = "sign";

        public PosnetXmlParser(ILogger<PosnetXmlParser>? logger = null)
        {
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SALE RESPONSE PARSING
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Satış yanıtını parse eder
        /// HostLogKey, AuthCode gibi kritik alanları çıkarır
        /// </summary>
        public PosnetSaleResponse ParseSaleResponse(string xml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xml))
                {
                    return CreateFailedSaleResponse("XML boş veya null", PosnetErrorCode.InvalidXmlFormat, xml);
                }

                var doc = XDocument.Parse(xml);
                var root = doc.Root;

                if (root == null)
                {
                    return CreateFailedSaleResponse("XML root element bulunamadı", PosnetErrorCode.InvalidXmlFormat, xml);
                }

                // Approved kontrolü - "1" ise başarılı
                var approved = GetElementValue(root, APPROVED) == "1";
                var respCode = GetElementValue(root, RESP_CODE);
                var respText = GetElementValue(root, RESP_TEXT);

                if (!approved)
                {
                    return CreateFailedSaleResponse(respText ?? "İşlem reddedildi", 
                        PosnetErrorCodeExtensions.ParseFromString(respCode), xml);
                }

                // Başarılı satış - Kritik alanları çıkar
                return new PosnetSaleResponse
                {
                    Approved = true,
                    RawErrorCode = respCode ?? "0",
                    HostLogKey = GetElementValue(root, HOST_LOG_KEY),
                    AuthCode = GetElementValue(root, AUTH_CODE),
                    OrderId = GetElementValue(root, ORDER_ID),
                    Amount = ParseInt(GetElementValue(root, AMOUNT)),
                    Installment = GetElementValue(root, INSTALLMENT),
                    Rrn = GetElementValue(root, RRN),
                    TransactionId = GetElementValue(root, TRANS_ID),
                    RawXml = xml
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "POSNET Sale response parse hatası. XML: {Xml}", 
                    TruncateForLog(xml));
                
                return CreateFailedSaleResponse($"XML parse hatası: {ex.Message}", 
                    PosnetErrorCode.InvalidXmlFormat, xml);
            }
        }

        /// <summary>
        /// Başarısız satış response'u oluşturur
        /// </summary>
        private static PosnetSaleResponse CreateFailedSaleResponse(
            string errorMessage, PosnetErrorCode errorCode, string? rawXml)
        {
            return new PosnetSaleResponse
            {
                Approved = false,
                RawErrorCode = ((int)errorCode).ToString(),
                ErrorMessage = errorMessage,
                RawXml = rawXml
            };
        }

        // ═══════════════════════════════════════════════════════════════════════
        // AUTH RESPONSE PARSING
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Provizyon yanıtını parse eder
        /// </summary>
        public PosnetAuthResponse ParseAuthResponse(string xml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xml))
                {
                    return new PosnetAuthResponse
                    {
                        Approved = false,
                        RawErrorCode = ((int)PosnetErrorCode.InvalidXmlFormat).ToString(),
                        ErrorMessage = "XML boş veya null",
                        RawXml = xml
                    };
                }

                var doc = XDocument.Parse(xml);
                var root = doc.Root;

                if (root == null)
                {
                    return new PosnetAuthResponse
                    {
                        Approved = false,
                        RawErrorCode = ((int)PosnetErrorCode.InvalidXmlFormat).ToString(),
                        ErrorMessage = "XML root element bulunamadı",
                        RawXml = xml
                    };
                }

                var approved = GetElementValue(root, APPROVED) == "1";
                var respCode = GetElementValue(root, RESP_CODE);
                var respText = GetElementValue(root, RESP_TEXT);

                return new PosnetAuthResponse
                {
                    Approved = approved,
                    RawErrorCode = respCode ?? (approved ? "0" : "9999"),
                    ErrorMessage = respText,
                    HostLogKey = GetElementValue(root, HOST_LOG_KEY),
                    AuthCode = GetElementValue(root, AUTH_CODE),
                    OrderId = GetElementValue(root, ORDER_ID),
                    Amount = ParseInt(GetElementValue(root, AMOUNT)),
                    RawXml = xml
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "POSNET Auth response parse hatası");
                
                return new PosnetAuthResponse
                {
                    Approved = false,
                    RawErrorCode = ((int)PosnetErrorCode.InvalidXmlFormat).ToString(),
                    ErrorMessage = $"XML parse hatası: {ex.Message}",
                    RawXml = xml
                };
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CAPT RESPONSE PARSING
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Finansallaştırma yanıtını parse eder
        /// </summary>
        public PosnetCaptResponse ParseCaptResponse(string xml)
        {
            try
            {
                var (approved, root) = ParseAndValidateXml(xml);
                
                if (root == null)
                {
                    return new PosnetCaptResponse
                    {
                        Approved = false,
                        RawErrorCode = ((int)PosnetErrorCode.InvalidXmlFormat).ToString(),
                        ErrorMessage = "Geçersiz XML",
                        RawXml = xml
                    };
                }

                var respCode = GetElementValue(root, RESP_CODE);
                var respText = GetElementValue(root, RESP_TEXT);

                return new PosnetCaptResponse
                {
                    Approved = approved,
                    RawErrorCode = respCode ?? (approved ? "0" : "9999"),
                    ErrorMessage = respText,
                    HostLogKey = GetElementValue(root, HOST_LOG_KEY),
                    AuthCode = GetElementValue(root, AUTH_CODE),
                    OrderId = GetElementValue(root, ORDER_ID),
                    Amount = ParseInt(GetElementValue(root, AMOUNT)),
                    RawXml = xml
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "POSNET Capt response parse hatası");
                
                return new PosnetCaptResponse
                {
                    Approved = false,
                    RawErrorCode = ((int)PosnetErrorCode.InvalidXmlFormat).ToString(),
                    ErrorMessage = $"XML parse hatası: {ex.Message}",
                    RawXml = xml
                };
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // REVERSE RESPONSE PARSING
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İptal yanıtını parse eder
        /// </summary>
        public PosnetReverseResponse ParseReverseResponse(string xml)
        {
            try
            {
                var (approved, root) = ParseAndValidateXml(xml);
                
                if (root == null)
                {
                    return new PosnetReverseResponse
                    {
                        Approved = false,
                        RawErrorCode = ((int)PosnetErrorCode.InvalidXmlFormat).ToString(),
                        ErrorMessage = "Geçersiz XML",
                        RawXml = xml
                    };
                }

                var respCode = GetElementValue(root, RESP_CODE);
                var respText = GetElementValue(root, RESP_TEXT);

                return new PosnetReverseResponse
                {
                    Approved = approved,
                    RawErrorCode = respCode ?? (approved ? "0" : "9999"),
                    ErrorMessage = respText,
                    OrderId = GetElementValue(root, ORDER_ID),
                    Amount = ParseInt(GetElementValue(root, AMOUNT)),
                    AuthCode = GetElementValue(root, AUTH_CODE),
                    RawXml = xml
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "POSNET Reverse response parse hatası");
                
                return new PosnetReverseResponse
                {
                    Approved = false,
                    RawErrorCode = ((int)PosnetErrorCode.InvalidXmlFormat).ToString(),
                    ErrorMessage = $"XML parse hatası: {ex.Message}",
                    RawXml = xml
                };
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // RETURN RESPONSE PARSING
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İade yanıtını parse eder
        /// </summary>
        public PosnetReturnResponse ParseReturnResponse(string xml)
        {
            try
            {
                var (approved, root) = ParseAndValidateXml(xml);
                
                if (root == null)
                {
                    return new PosnetReturnResponse
                    {
                        Approved = false,
                        RawErrorCode = ((int)PosnetErrorCode.InvalidXmlFormat).ToString(),
                        ErrorMessage = "Geçersiz XML",
                        RawXml = xml
                    };
                }

                var respCode = GetElementValue(root, RESP_CODE);
                var respText = GetElementValue(root, RESP_TEXT);

                return new PosnetReturnResponse
                {
                    Approved = approved,
                    RawErrorCode = respCode ?? (approved ? "0" : "9999"),
                    ErrorMessage = respText,
                    HostLogKey = GetElementValue(root, HOST_LOG_KEY),
                    OrderId = GetElementValue(root, ORDER_ID),
                    Amount = ParseInt(GetElementValue(root, AMOUNT)),
                    AuthCode = GetElementValue(root, AUTH_CODE),
                    RawXml = xml
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "POSNET Return response parse hatası");
                
                return new PosnetReturnResponse
                {
                    Approved = false,
                    RawErrorCode = ((int)PosnetErrorCode.InvalidXmlFormat).ToString(),
                    ErrorMessage = $"XML parse hatası: {ex.Message}",
                    RawXml = xml
                };
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // POINT INQUIRY RESPONSE PARSING
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Puan sorgulama yanıtını parse eder
        /// World Puan ve Marka Puan değerlerini çıkarır
        /// </summary>
        public PosnetPointInquiryResponse ParsePointInquiryResponse(string xml)
        {
            try
            {
                var (approved, root) = ParseAndValidateXml(xml);
                
                if (root == null)
                {
                    return new PosnetPointInquiryResponse
                    {
                        Approved = false,
                        RawErrorCode = ((int)PosnetErrorCode.InvalidXmlFormat).ToString(),
                        ErrorMessage = "Geçersiz XML",
                        RawXml = xml
                    };
                }

                var respCode = GetElementValue(root, RESP_CODE);
                var respText = GetElementValue(root, RESP_TEXT);

                // Puan bilgilerini çıkar
                var worldPoint = ParseInt(GetElementValue(root, WORLD_POINT)) ?? 0;
                var brandPoint = ParseInt(GetElementValue(root, BRAND_POINT)) ?? 0;

                PosnetPointInfo? pointInfo = null;
                if (approved && (worldPoint > 0 || brandPoint > 0))
                {
                    pointInfo = new PosnetPointInfo
                    {
                        WorldPoint = worldPoint,
                        BrandPoint = brandPoint
                    };
                }

                return new PosnetPointInquiryResponse
                {
                    Approved = approved,
                    RawErrorCode = respCode ?? (approved ? "0" : "9999"),
                    ErrorMessage = respText,
                    PointInfo = pointInfo,
                    IsEnrolled = pointInfo != null,
                    RawXml = xml
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "POSNET PointInquiry response parse hatası");
                
                return new PosnetPointInquiryResponse
                {
                    Approved = false,
                    RawErrorCode = ((int)PosnetErrorCode.InvalidXmlFormat).ToString(),
                    ErrorMessage = $"XML parse hatası: {ex.Message}",
                    RawXml = xml
                };
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // AGREEMENT RESPONSE PARSING
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İşlem durumu sorgulama yanıtını parse eder
        /// </summary>
        public PosnetAgreementResponse ParseAgreementResponse(string xml)
        {
            try
            {
                var (approved, root) = ParseAndValidateXml(xml);
                
                if (root == null)
                {
                    return new PosnetAgreementResponse
                    {
                        Approved = false,
                        RawErrorCode = ((int)PosnetErrorCode.InvalidXmlFormat).ToString(),
                        ErrorMessage = "Geçersiz XML",
                        TransactionStatus = PosnetTransactionStatus.Unknown,
                        RawXml = xml
                    };
                }

                var respCode = GetElementValue(root, RESP_CODE);
                var respText = GetElementValue(root, RESP_TEXT);

                // İşlem durumunu belirle
                var status = DetermineTransactionStatus(approved, respCode);

                return new PosnetAgreementResponse
                {
                    Approved = approved,
                    RawErrorCode = respCode ?? (approved ? "0" : "9999"),
                    ErrorMessage = respText,
                    TransactionStatus = status,
                    OrderId = GetElementValue(root, ORDER_ID),
                    Amount = ParseInt(GetElementValue(root, AMOUNT)),
                    HostLogKey = GetElementValue(root, HOST_LOG_KEY),
                    AuthCode = GetElementValue(root, AUTH_CODE),
                    RawXml = xml
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "POSNET Agreement response parse hatası");
                
                return new PosnetAgreementResponse
                {
                    Approved = false,
                    RawErrorCode = ((int)PosnetErrorCode.InvalidXmlFormat).ToString(),
                    ErrorMessage = $"XML parse hatası: {ex.Message}",
                    TransactionStatus = PosnetTransactionStatus.Unknown,
                    RawXml = xml
                };
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // OOS RESPONSE PARSING
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 3D Secure OOS yanıtını parse eder
        /// Data1, Data2 ve Sign değerlerini çıkarır
        /// </summary>
        public PosnetOosResponse ParseOosResponse(string xml)
        {
            try
            {
                var (approved, root) = ParseAndValidateXml(xml);
                
                if (root == null)
                {
                    return new PosnetOosResponse
                    {
                        Approved = false,
                        RawErrorCode = ((int)PosnetErrorCode.InvalidXmlFormat).ToString(),
                        ErrorMessage = "Geçersiz XML",
                        RawXml = xml
                    };
                }

                var respCode = GetElementValue(root, RESP_CODE);
                var respText = GetElementValue(root, RESP_TEXT);

                // OOS'a özel alanları çıkar
                // data1, data2: Banka yönlendirmesi için şifreli veriler
                // sign: MAC imzası
                var data1 = GetElementValue(root, DATA1);
                var data2 = GetElementValue(root, DATA2);
                var sign = GetElementValue(root, SIGN);

                return new PosnetOosResponse
                {
                    Approved = approved,
                    RawErrorCode = respCode ?? (approved ? "0" : "9999"),
                    ErrorMessage = respText,
                    Data1 = data1,
                    Data2 = data2,
                    Sign = sign,
                    OrderId = GetElementValue(root, ORDER_ID),
                    RawXml = xml
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "POSNET OOS response parse hatası");
                
                return new PosnetOosResponse
                {
                    Approved = false,
                    RawErrorCode = ((int)PosnetErrorCode.InvalidXmlFormat).ToString(),
                    ErrorMessage = $"XML parse hatası: {ex.Message}",
                    RawXml = xml
                };
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // BASIC RESPONSE PARSING
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Generic yanıt parse - Sadece başarı/hata kontrolü
        /// Hızlı kontrol için kullanılır
        /// </summary>
        public (bool approved, string? errorCode, string? errorMessage) ParseBasicResponse(string xml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xml))
                {
                    return (false, "103", "XML boş veya null");
                }

                var doc = XDocument.Parse(xml);
                var root = doc.Root;

                if (root == null)
                {
                    return (false, "103", "XML root element bulunamadı");
                }

                var approved = GetElementValue(root, APPROVED) == "1";
                var respCode = GetElementValue(root, RESP_CODE);
                var respText = GetElementValue(root, RESP_TEXT);

                return (approved, respCode, respText);
            }
            catch (Exception ex)
            {
                return (false, "103", $"XML parse hatası: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // OOS RESOLVE MERCHANT DATA RESPONSE PARSING
        // 3D Secure callback verilerini deşifre eden servis yanıtı
        // Dokümantasyon: POSNET 3D Secure Entegrasyon Dokümanı - Sayfa 13-14
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// oosResolveMerchantData yanıtını parse eder.
        /// 
        /// RESPONSE FORMATI (POSNET Dokümanı sayfa 13):
        /// &lt;posnetResponse&gt;
        ///   &lt;approved&gt;1&lt;/approved&gt;
        ///   &lt;respCode&gt;&lt;/respCode&gt;
        ///   &lt;respText&gt;&lt;/respText&gt;
        ///   &lt;oosResolveMerchantDataResponse&gt;
        ///     &lt;xid&gt;YKB_0000080603153823&lt;/xid&gt;
        ///     &lt;amount&gt;5696&lt;/amount&gt;
        ///     &lt;currency&gt;TL&lt;/currency&gt;
        ///     &lt;installment&gt;00&lt;/installment&gt;
        ///     &lt;point&gt;0&lt;/point&gt;
        ///     &lt;pointAmount&gt;0&lt;/pointAmount&gt;
        ///     &lt;txStatus&gt;N&lt;/txStatus&gt;
        ///     &lt;mdStatus&gt;9&lt;/mdStatus&gt;
        ///     &lt;mdErrorMessage&gt;None 3D - Secure Transaction&lt;/mdErrorMessage&gt;
        ///     &lt;mac&gt;ED7254A3ABC264QOP67MN&lt;/mac&gt;
        ///   &lt;/oosResolveMerchantDataResponse&gt;
        /// &lt;/posnetResponse&gt;
        /// 
        /// KRİTİK GÜVENLIK KONTROLLERI:
        /// 1. Response'daki xid, amount, currency orijinal değerlerle karşılaştırılmalı
        /// 2. Response'daki mac ile işyerinin hesapladığı mac karşılaştırılmalı
        /// 3. mdStatus kontrol edilmeli (1=başarılı, 2-4=kısmi, 0/5-9=başarısız)
        /// </summary>
        public PosnetOosResolveMerchantDataResponse ParseOosResolveMerchantDataResponse(string xml)
        {
            try
            {
                // ADIM 1: Temel XML validasyonu
                if (string.IsNullOrWhiteSpace(xml))
                {
                    _logger?.LogWarning("[POSNET-PARSER] oosResolveMerchantData - Boş XML");
                    return CreateFailedResolveResponse("XML boş veya null", "103", xml);
                }

                var doc = XDocument.Parse(xml);
                var root = doc.Root;

                if (root == null)
                {
                    _logger?.LogWarning("[POSNET-PARSER] oosResolveMerchantData - Root element yok");
                    return CreateFailedResolveResponse("XML root element bulunamadı", "103", xml);
                }

                // ADIM 2: Temel yanıt kontrolleri
                var approved = GetElementValue(root, APPROVED) == "1";
                var respCode = GetElementValue(root, RESP_CODE);
                var respText = GetElementValue(root, RESP_TEXT);

                if (!approved)
                {
                    _logger?.LogWarning("[POSNET-PARSER] oosResolveMerchantData BAŞARISIZ - " +
                        "RespCode: {RespCode}, RespText: {RespText}", respCode, respText);

                    return CreateFailedResolveResponse(
                        respText ?? "İşlem başarısız",
                        respCode ?? "UNKNOWN",
                        xml);
                }

                // ADIM 3: oosResolveMerchantDataResponse bloğunu bul
                var resolveResponse = root.Element("oosResolveMerchantDataResponse");
                if (resolveResponse == null)
                {
                    // Alternatif element ismi dene
                    resolveResponse = root.Descendants()
                        .FirstOrDefault(e => e.Name.LocalName.Contains("ResolveMerchantData", 
                            StringComparison.OrdinalIgnoreCase));
                }

                if (resolveResponse == null)
                {
                    _logger?.LogWarning("[POSNET-PARSER] oosResolveMerchantDataResponse elementi bulunamadı");
                    return CreateFailedResolveResponse(
                        "oosResolveMerchantDataResponse elementi eksik",
                        "PARSE_ERROR",
                        xml);
                }

                // ADIM 4: Tüm alanları parse et
                var xid = GetElementValue(resolveResponse, "xid");
                var amount = GetElementValue(resolveResponse, AMOUNT);
                var currency = GetElementValue(resolveResponse, "currency");
                var installment = GetElementValue(resolveResponse, INSTALLMENT);
                var point = GetElementValue(resolveResponse, "point");
                var pointAmount = GetElementValue(resolveResponse, "pointAmount");
                var txStatus = GetElementValue(resolveResponse, "txStatus");
                var mdStatus = GetElementValue(resolveResponse, "mdStatus");
                var mdErrorMessage = GetElementValue(resolveResponse, "mdErrorMessage");
                var mac = GetElementValue(resolveResponse, "mac");

                _logger?.LogInformation("[POSNET-PARSER] oosResolveMerchantData BAŞARILI - " +
                    "XID: {Xid}, Amount: {Amount}, MdStatus: {MdStatus}",
                    xid, amount, mdStatus);

                // ADIM 5: Response modeli oluştur
                return new PosnetOosResolveMerchantDataResponse
                {
                    Approved = true,
                    RawErrorCode = respCode ?? "0",
                    RawXml = xml,
                    
                    // İşlem bilgileri (deşifre edilmiş)
                    Xid = xid ?? string.Empty,
                    Amount = ParseInt(amount) ?? 0,
                    Currency = currency ?? "TL",
                    Installment = installment ?? "00",
                    
                    // Puan bilgileri (World Card için)
                    Point = ParseInt(point) ?? 0,
                    PointAmount = ParseInt(pointAmount) ?? 0,
                    
                    // 3D Secure doğrulama bilgileri
                    TxStatus = txStatus,
                    MdStatus = mdStatus ?? "0",
                    MdErrorMessage = mdErrorMessage,
                    
                    // MAC doğrulama için
                    Mac = mac ?? string.Empty,
                    
                    // MdStatus açıklaması
                    MdStatusDescription = GetMdStatusDescription(mdStatus),
                    
                    // İşlem devam edebilir mi?
                    CanProceedWithPayment = IsMdStatusSuccessful(mdStatus)
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[POSNET-PARSER] oosResolveMerchantData parse hatası. XML: {Xml}",
                    TruncateForLog(xml));

                return CreateFailedResolveResponse(
                    $"XML parse hatası: {ex.Message}",
                    "PARSE_EXCEPTION",
                    xml);
            }
        }

        /// <summary>
        /// Başarısız oosResolveMerchantData response'u oluşturur
        /// </summary>
        private static PosnetOosResolveMerchantDataResponse CreateFailedResolveResponse(
            string errorMessage, string errorCode, string? rawXml)
        {
            return new PosnetOosResolveMerchantDataResponse
            {
                Approved = false,
                RawErrorCode = errorCode,
                ErrorMessage = errorMessage,
                RawXml = rawXml,
                CanProceedWithPayment = false
            };
        }

        // ═══════════════════════════════════════════════════════════════════════
        // OOS TRAN DATA RESPONSE PARSING
        // 3D Secure finansallaştırma servisi yanıtı
        // Dokümantasyon: POSNET 3D Secure Entegrasyon Dokümanı - Sayfa 16-17
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// oosTranData (Finansallaştırma) yanıtını parse eder.
        /// 
        /// RESPONSE FORMATI (POSNET Dokümanı sayfa 16):
        /// &lt;posnetResponse&gt;
        ///   &lt;approved&gt;1&lt;/approved&gt;
        ///   &lt;respCode&gt;&lt;/respCode&gt;
        ///   &lt;respText&gt;&lt;/respText&gt;
        ///   &lt;mac&gt;DF2323A3BMC782QOP42RT&lt;/mac&gt;
        ///   &lt;hostlogkey&gt;0000000002P0806031&lt;/hostlogkey&gt;
        ///   &lt;authCode&gt;901477&lt;/authCode&gt;
        ///   &lt;instInfo&gt;
        ///     &lt;inst1&gt;00&lt;/inst1&gt;
        ///     &lt;amnt1&gt;000000000000&lt;/amnt1&gt;
        ///   &lt;/instInfo&gt;
        ///   &lt;pointInfo&gt;
        ///     &lt;point&gt;00000228&lt;/point&gt;
        ///     &lt;pointAmount&gt;000000000114&lt;/pointAmount&gt;
        ///     &lt;totalPoint&gt;00000000&lt;/totalPoint&gt;
        ///     &lt;totalPointAmount&gt;000000000000&lt;/totalPointAmount&gt;
        ///   &lt;/pointInfo&gt;
        /// &lt;/posnetResponse&gt;
        /// 
        /// NOT: hostlogkey ve authCode değerleri sipariş takibi için saklanmalı!
        /// </summary>
        public PosnetOosTranDataResponse ParseOosTranDataResponse(string xml)
        {
            try
            {
                // ADIM 1: Temel XML validasyonu
                if (string.IsNullOrWhiteSpace(xml))
                {
                    _logger?.LogWarning("[POSNET-PARSER] oosTranData - Boş XML");
                    return CreateFailedTranDataResponse("XML boş veya null", "103", xml);
                }

                // DEBUG: Gelen XML'i logla
                _logger?.LogDebug("[POSNET-PARSER] oosTranData - Gelen XML: {Xml}", TruncateForLog(xml, 500));

                var doc = XDocument.Parse(xml);
                var root = doc.Root;

                if (root == null)
                {
                    _logger?.LogWarning("[POSNET-PARSER] oosTranData - Root element yok");
                    return CreateFailedTranDataResponse("XML root element bulunamadı", "103", xml);
                }

                // ADIM 2: Temel yanıt kontrolleri
                var approvedStr = GetElementValue(root, APPROVED);
                var approved = approvedStr == "1" || approvedStr == "2"; // 2 = Daha önce onaylanmış
                var respCode = GetElementValue(root, RESP_CODE);
                var respText = GetElementValue(root, RESP_TEXT);

                if (!approved)
                {
                    _logger?.LogWarning("[POSNET-PARSER] oosTranData BAŞARISIZ - " +
                        "RespCode: {RespCode}, RespText: {RespText}", respCode, respText);

                    return CreateFailedTranDataResponse(
                        respText ?? "Finansallaştırma başarısız",
                        respCode ?? "UNKNOWN",
                        xml);
                }

                // ADIM 3: Kritik finansallaştırma alanlarını parse et
                var mac = GetElementValue(root, "mac");
                var hostLogKey = GetElementValue(root, HOST_LOG_KEY);
                var authCode = GetElementValue(root, AUTH_CODE);

                // ADIM 4: Taksit bilgileri (instInfo)
                var instInfo = root.Element("instInfo");
                string? installment = null;
                int? installmentAmount = null;

                if (instInfo != null)
                {
                    installment = GetElementValue(instInfo, "inst1");
                    installmentAmount = ParseInt(GetElementValue(instInfo, "amnt1"));
                }

                // ADIM 5: Puan bilgileri (pointInfo) - World Card için
                var pointInfo = root.Element("pointInfo");
                int? earnedPoint = null;
                int? earnedPointAmount = null;
                int? totalPoint = null;
                int? totalPointAmount = null;

                if (pointInfo != null)
                {
                    earnedPoint = ParseInt(GetElementValue(pointInfo, "point"));
                    earnedPointAmount = ParseInt(GetElementValue(pointInfo, "pointAmount"));
                    totalPoint = ParseInt(GetElementValue(pointInfo, "totalPoint"));
                    totalPointAmount = ParseInt(GetElementValue(pointInfo, "totalPointAmount"));
                }

                // ADIM 6: VFT bilgileri (vft) - Vade Farklı işlemler için
                var vftInfo = root.Element("vft");
                int? vftAmount = null;
                int? vftDayCount = null;

                if (vftInfo != null)
                {
                    vftAmount = ParseInt(GetElementValue(vftInfo, "vftAmount"));
                    vftDayCount = ParseInt(GetElementValue(vftInfo, "vftDayCount"));
                }

                _logger?.LogInformation("[POSNET-PARSER] oosTranData BAŞARILI - " +
                    "HostLogKey: {HostLogKey}, AuthCode: {AuthCode}, Approved: {Approved}",
                    hostLogKey, authCode, approvedStr);

                // ADIM 7: Response modeli oluştur
                return new PosnetOosTranDataResponse
                {
                    Approved = true,
                    ApprovedCode = approvedStr, // 1=Başarılı, 2=Daha önce onaylanmış
                    RawErrorCode = respCode ?? "0",
                    RawXml = xml,
                    
                    // Kritik alanlar - Sipariş takibi için saklanmalı
                    Mac = mac,
                    HostLogKey = hostLogKey ?? string.Empty,
                    AuthCode = authCode ?? string.Empty,
                    
                    // Taksit bilgileri
                    Installment = installment,
                    InstallmentAmount = installmentAmount,
                    
                    // Puan bilgileri (World Card)
                    EarnedPoint = earnedPoint ?? 0,
                    EarnedPointAmount = earnedPointAmount ?? 0,
                    TotalPoint = totalPoint ?? 0,
                    TotalPointAmount = totalPointAmount ?? 0,
                    
                    // VFT bilgileri
                    VftAmount = vftAmount,
                    VftDayCount = vftDayCount,
                    
                    // İşlem durumu
                    TransactionStatus = approvedStr == "2" 
                        ? PosnetTransactionStatus.AlreadyProcessed 
                        : PosnetTransactionStatus.Completed
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[POSNET-PARSER] oosTranData parse hatası. XML: {Xml}",
                    TruncateForLog(xml));

                return CreateFailedTranDataResponse(
                    $"XML parse hatası: {ex.Message}",
                    "PARSE_EXCEPTION",
                    xml);
            }
        }

        /// <summary>
        /// Başarısız oosTranData response'u oluşturur
        /// </summary>
        private static PosnetOosTranDataResponse CreateFailedTranDataResponse(
            string errorMessage, string errorCode, string? rawXml)
        {
            return new PosnetOosTranDataResponse
            {
                Approved = false,
                RawErrorCode = errorCode,
                ErrorMessage = errorMessage,
                RawXml = rawXml,
                TransactionStatus = PosnetTransactionStatus.Unknown
            };
        }

        // ═══════════════════════════════════════════════════════════════════════
        // MDSTATUS HELPERS
        // 3D Secure doğrulama durumu yardımcı metodları
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// MdStatus kodunun açıklamasını döner
        /// POSNET Dokümantasyonu sayfa 13'e göre
        /// </summary>
        private static string GetMdStatusDescription(string? mdStatus)
        {
            return mdStatus switch
            {
                "1" => "Tam Doğrulama - Kart sahibi başarıyla doğrulandı",
                "2" => "Kart Sahibi veya Bankası Sisteme Kayıtlı Değil",
                "3" => "Kartın Bankası Sisteme Kayıtlı Değil",
                "4" => "Doğrulama Denemesi - Kart Sahibi Sisteme Kayıt Olmayı Seçmiş",
                "5" => "Doğrulama Yapılamıyor",
                "6" => "3D Secure Hatası",
                "7" => "Sistem Hatası",
                "8" => "Bilinmeyen Kart No",
                "9" => "Üye İşyeri 3D-Secure Sisteminde Kayıtlı Değil",
                "0" => "Doğrulama Başarısız - Kart Sahibi Şifre Hatalı veya İptal",
                null or "" => "MdStatus bilgisi yok",
                _ => $"Bilinmeyen Durum: {mdStatus}"
            };
        }

        /// <summary>
        /// MdStatus başarılı kabul edilen değerlerden biri mi kontrol eder
        /// 1, 2, 3, 4 başarılı kabul edilir (işleme devam edilebilir)
        /// </summary>
        private static bool IsMdStatusSuccessful(string? mdStatus)
        {
            return mdStatus == "1" || mdStatus == "2" || mdStatus == "3" || mdStatus == "4";
        }

        // ═══════════════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// XML'i parse edip temel doğrulama yapar
        /// </summary>
        private (bool approved, XElement? root) ParseAndValidateXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return (false, null);
            }

            try
            {
                var doc = XDocument.Parse(xml);
                var root = doc.Root;

                if (root == null)
                {
                    return (false, null);
                }

                var approved = GetElementValue(root, APPROVED) == "1";
                return (approved, root);
            }
            catch
            {
                return (false, null);
            }
        }

        /// <summary>
        /// XML element değerini güvenli şekilde alır
        /// Element yoksa veya boşsa null döner
        /// </summary>
        private static string? GetElementValue(XElement parent, string elementName)
        {
            // Önce direkt child'da ara
            var element = parent.Element(elementName);
            
            if (element != null)
            {
                return string.IsNullOrWhiteSpace(element.Value) ? null : element.Value.Trim();
            }

            // Bulunamazsa tüm descendant'larda ara (nested elements için)
            element = parent.Descendants(elementName).FirstOrDefault();
            
            if (element != null)
            {
                return string.IsNullOrWhiteSpace(element.Value) ? null : element.Value.Trim();
            }

            // Case-insensitive arama (bazı POSNET response'lar farklı case kullanabilir)
            element = parent.Descendants()
                .FirstOrDefault(e => e.Name.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase));
            
            return element != null && !string.IsNullOrWhiteSpace(element.Value) 
                ? element.Value.Trim() 
                : null;
        }

        /// <summary>
        /// String'i int'e güvenli şekilde parse eder
        /// Parse edilemezse null döner
        /// </summary>
        private static int? ParseInt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return int.TryParse(value, out var result) ? result : null;
        }

        /// <summary>
        /// İşlem durumunu belirler
        /// Response kod ve onay durumuna göre status döner
        /// </summary>
        private static PosnetTransactionStatus DetermineTransactionStatus(bool approved, string? respCode)
        {
            if (approved)
            {
                return PosnetTransactionStatus.Completed;
            }

            // Belirli hata kodlarına göre status belirle
            return respCode switch
            {
                "0" or "00" or null when approved => PosnetTransactionStatus.Completed,
                "0127" => PosnetTransactionStatus.Completed, // OrderID kullanılmış = işlem yapılmış
                "0005" or "0012" or "0014" or "0051" => PosnetTransactionStatus.Rejected,
                "0129" => PosnetTransactionStatus.Cancelled,
                "0130" => PosnetTransactionStatus.Refunded,
                _ => PosnetTransactionStatus.Unknown
            };
        }

        /// <summary>
        /// Log için XML'i kısaltır
        /// Uzun XML'lerin log'ları şişirmemesi için
        /// </summary>
        private static string TruncateForLog(string? xml, int maxLength = 500)
        {
            if (string.IsNullOrEmpty(xml)) return "[empty]";
            if (xml.Length <= maxLength) return xml;
            return xml[..maxLength] + "... [truncated]";
        }
    }
}
