// ═══════════════════════════════════════════════════════════════════════════════════════════════
// POSNET XML BUILDER
// Yapı Kredi POSNET XML API için request XML'leri oluşturan servis
// Dokümantasyon: POSNET XML Servisleri Entegrasyon Dokümanı v2.1.1.3
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// NEDEN BU YAPIYI SEÇTİK?
// 1. StringBuilder kullanımı - String concatenation yerine performanslı XML oluşturma
// 2. Interface pattern - Mock'lanabilir, test edilebilir
// 3. Fluent builder pattern - Okunabilir, zincirleme method çağrısı
// 4. Static helper metodlar - Sık kullanılan işlemler için utility
// 5. XML escape - Güvenlik için özel karakter encode
// ═══════════════════════════════════════════════════════════════════════════════════════════════

using System;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using ECommerce.Infrastructure.Services.Payment.Posnet.Models;

namespace ECommerce.Infrastructure.Services.Payment.Posnet
{
    /// <summary>
    /// POSNET XML builder interface
    /// Dependency Injection ve Unit Test için
    /// </summary>
    public interface IPosnetXmlBuilder
    {
        /// <summary>Satış işlemi XML'i oluşturur</summary>
        string BuildSaleXml(PosnetSaleRequest request);
        
        /// <summary>Provizyon (ön yetkilendirme) XML'i oluşturur</summary>
        string BuildAuthXml(PosnetAuthRequest request);
        
        /// <summary>Finansallaştırma XML'i oluşturur</summary>
        string BuildCaptXml(PosnetCaptRequest request);
        
        /// <summary>İptal XML'i oluşturur</summary>
        string BuildReverseXml(PosnetReverseRequest request);
        
        /// <summary>İade XML'i oluşturur</summary>
        string BuildReturnXml(PosnetReturnRequest request);
        
        /// <summary>Puan sorgulama XML'i oluşturur</summary>
        string BuildPointInquiryXml(PosnetPointInquiryRequest request);
        
        /// <summary>İşlem durumu sorgulama XML'i oluşturur</summary>
        string BuildAgreementXml(PosnetAgreementRequest request);
        
        /// <summary>3D Secure OOS talebi XML'i oluşturur</summary>
        string BuildOosRequestXml(PosnetOosRequest request);
        
        /// <summary>3D Secure OOS TDS (Transaction Data Signing) XML'i oluşturur</summary>
        string BuildOosTdsXml(string merchantId, string terminalId, string posnetId, 
            string orderId, int amount, string currencyCode, string installment,
            string txnType, string cardHolderName, string ccNumber, string expDate, string cvc);
        
        /// <summary>
        /// 3D Secure sonrası OOS Resolve Merchant Data XML'i oluşturur
        /// Banka callback'inden gelen şifreli verileri çözmek için kullanılır
        /// POSNET Dokümantasyonu: Sayfa 12-14 - oosResolveMerchantData servisi
        /// </summary>
        /// <param name="request">Resolve request modeli (bankData, merchantData, sign, mac)</param>
        /// <returns>oosResolveMerchantData XML string</returns>
        string BuildOosResolveMerchantDataXml(PosnetOosResolveMerchantDataRequest request);
        
        /// <summary>
        /// 3D Secure finansallaştırma (oosTranData) XML'i oluşturur
        /// 3D doğrulama sonrası işlemi finansallaştırmak için kullanılır
        /// POSNET Dokümantasyonu: Sayfa 15-17 - oosTranData servisi
        /// </summary>
        /// <param name="request">Finansallaştırma request modeli (bankData, wpAmount, mac)</param>
        /// <returns>oosTranData XML string</returns>
        string BuildOosTranDataXml(PosnetOosTranDataRequest request);
        
        /// <summary>XML'i HTTP POST için URL encode eder</summary>
        string EncodeForPost(string xml);
    }

    /// <summary>
    /// POSNET XML Builder implementasyonu
    /// Tüm POSNET işlem tiplerinin XML request'lerini oluşturur
    /// </summary>
    public class PosnetXmlBuilder : IPosnetXmlBuilder
    {
        // ═══════════════════════════════════════════════════════════════════════
        // CONSTANTS - POSNET XML sabit değerleri
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>XML declaration - POSNET ISO-8859-9 encoding bekliyor (3sapi.txt dokümanı)</summary>
        private const string XML_DECLARATION = "<?xml version=\"1.0\" encoding=\"ISO-8859-9\"?>";
        
        /// <summary>POSNET root element açılış</summary>
        private const string POSNET_OPEN = "<posnetRequest>";
        
        /// <summary>POSNET root element kapanış</summary>
        private const string POSNET_CLOSE = "</posnetRequest>";

        /// <summary>Para birimi - TL kodu</summary>
        private const string CURRENCY_TL = "TL"; // POSNET'te TL = TL

        // ═══════════════════════════════════════════════════════════════════════
        // SALE XML - Satış işlemi
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Satış işlemi XML'i oluşturur
        /// Peşin veya taksitli direkt satış için kullanılır
        /// </summary>
        /// <param name="request">Satış request modeli</param>
        /// <returns>POSNET formatında XML string</returns>
        public string BuildSaleXml(PosnetSaleRequest request)
        {
            // Validasyon - Request null veya invalid ise exception
            PosnetRequestValidator.ValidateAndThrow(request);

            var sb = new StringBuilder();
            
            // XML header ve root element
            sb.Append(XML_DECLARATION);
            sb.Append(POSNET_OPEN);
            
            // Üye işyeri bilgileri - Her request'te zorunlu
            AppendCredentials(sb, request.MerchantId, request.TerminalId);
            
            // Satış işlem bloğu
            sb.Append("<sale>");
            
            // Sipariş numarası - 24 karakter, unique olmalı
            AppendElement(sb, "orderID", request.OrderId);
            
            // Tutar - Kuruş cinsinden (12.34 TL = 1234)
            AppendElement(sb, "amount", request.Amount.ToString());
            
            // Para birimi
            AppendElement(sb, "currencyCode", GetCurrencyCode(request.CurrencyCode));
            
            // Kart bilgileri
            AppendElement(sb, "ccno", request.Card.CardNumber);
            AppendElement(sb, "expDate", request.Card.ExpireDate);
            AppendElement(sb, "cvc", request.Card.Cvv);
            
            // Taksit sayısı - 00 = peşin, 02-12 = taksit
            AppendElement(sb, "installment", request.Installment);
            
            // Mail Order flag - Internet işlemleri için Y
            if (request.IsMailOrder)
            {
                AppendElement(sb, "mailorderflag", "Y");
            }
            
            // İşlem tipi - H = Host (Internet)
            AppendElement(sb, "tranType", request.TranType);
            
            sb.Append("</sale>");
            sb.Append(POSNET_CLOSE);

            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // AUTH XML - Provizyon (Ön Yetkilendirme)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Provizyon (ön yetkilendirme) XML'i oluşturur
        /// Tutarı bloke eder, ekstreye yansımaz
        /// Finansallaştırma (Capt) ile tamamlanmalıdır
        /// </summary>
        public string BuildAuthXml(PosnetAuthRequest request)
        {
            PosnetRequestValidator.ValidateAndThrow(request);

            var sb = new StringBuilder();
            sb.Append(XML_DECLARATION);
            sb.Append(POSNET_OPEN);
            
            AppendCredentials(sb, request.MerchantId, request.TerminalId);
            
            // Provizyon işlem bloğu - "auth" tag'i
            sb.Append("<auth>");
            
            AppendElement(sb, "orderID", request.OrderId);
            AppendElement(sb, "amount", request.Amount.ToString());
            AppendElement(sb, "currencyCode", GetCurrencyCode(request.CurrencyCode));
            AppendElement(sb, "ccno", request.Card.CardNumber);
            AppendElement(sb, "expDate", request.Card.ExpireDate);
            AppendElement(sb, "cvc", request.Card.Cvv);
            AppendElement(sb, "installment", request.Installment);
            
            if (request.IsMailOrder)
            {
                AppendElement(sb, "mailorderflag", "Y");
            }
            
            sb.Append("</auth>");
            sb.Append(POSNET_CLOSE);

            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CAPT XML - Finansallaştırma
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Finansallaştırma XML'i oluşturur
        /// Daha önce alınan provizyonu finansal değere çevirir
        /// </summary>
        public string BuildCaptXml(PosnetCaptRequest request)
        {
            var sb = new StringBuilder();
            sb.Append(XML_DECLARATION);
            sb.Append(POSNET_OPEN);
            
            AppendCredentials(sb, request.MerchantId, request.TerminalId);
            
            // Finansallaştırma işlem bloğu - "capt" tag'i
            sb.Append("<capt>");
            
            AppendElement(sb, "orderID", request.OrderId);
            AppendElement(sb, "amount", request.Amount.ToString());
            AppendElement(sb, "installment", request.Installment);
            
            // HostLogKey - Provizyon işleminden dönen referans
            // Bu değer finansallaştırma için zorunlu
            if (!string.IsNullOrEmpty(request.HostLogKey))
            {
                AppendElement(sb, "hostLogKey", request.HostLogKey);
            }
            
            sb.Append("</capt>");
            sb.Append(POSNET_CLOSE);

            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // REVERSE XML - İptal (Gün içi)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İptal XML'i oluşturur
        /// Sadece gün içinde (grup kapama öncesi) yapılabilir
        /// İşlem finansal değer kazanmaz, ekstrede görünmez
        /// </summary>
        public string BuildReverseXml(PosnetReverseRequest request)
        {
            var sb = new StringBuilder();
            sb.Append(XML_DECLARATION);
            sb.Append(POSNET_OPEN);
            
            AppendCredentials(sb, request.MerchantId, request.TerminalId);
            
            // İptal işlem bloğu - "reverse" tag'i
            sb.Append("<reverse>");
            
            // İptal için transaction referansı gerekli
            // HostLogKey: Orijinal işlemden dönen referans numarası
            AppendElement(sb, "hostLogKey", request.HostLogKey);
            
            // İşlem tarihi (opsiyonel) - YYMMDD formatında
            if (!string.IsNullOrEmpty(request.TransactionDate))
            {
                AppendElement(sb, "tranDate", request.TransactionDate);
            }
            
            sb.Append("</reverse>");
            sb.Append(POSNET_CLOSE);

            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // RETURN XML - İade (Gün sonu sonrası)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İade XML'i oluşturur
        /// Gün sonu (grup kapama) sonrası yapılan işlemler için
        /// Kısmi veya tam iade yapılabilir
        /// </summary>
        public string BuildReturnXml(PosnetReturnRequest request)
        {
            var sb = new StringBuilder();
            sb.Append(XML_DECLARATION);
            sb.Append(POSNET_OPEN);
            
            AppendCredentials(sb, request.MerchantId, request.TerminalId);
            
            // İade işlem bloğu - "return" tag'i
            sb.Append("<return>");
            
            // İade için HostLogKey zorunlu
            AppendElement(sb, "hostLogKey", request.HostLogKey);
            
            // İade tutarı - Kısmi iade için orijinalden az olabilir
            AppendElement(sb, "amount", request.Amount.ToString());
            
            // İade işlemi için yeni sipariş numarası (opsiyonel ama önerilen)
            // Her iade ayrı bir transaction olarak kaydedilir
            if (!string.IsNullOrEmpty(request.RefundOrderId))
            {
                AppendElement(sb, "orderID", request.RefundOrderId);
            }
            
            sb.Append("</return>");
            sb.Append(POSNET_CLOSE);

            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // POINT INQUIRY XML - Puan Sorgulama
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Puan sorgulama XML'i oluşturur
        /// WorldCard sahiplerinin puan bakiyesini sorgular
        /// </summary>
        public string BuildPointInquiryXml(PosnetPointInquiryRequest request)
        {
            PosnetRequestValidator.ValidateAndThrow(request);

            var sb = new StringBuilder();
            sb.Append(XML_DECLARATION);
            sb.Append(POSNET_OPEN);
            
            AppendCredentials(sb, request.MerchantId, request.TerminalId);
            
            // Puan sorgulama bloğu - "pointUsage" içinde "pointInquiry" tipi
            sb.Append("<pointUsage>");
            
            // Puan sorgulama tipi
            AppendElement(sb, "pointType", "pointInquiry");
            
            // Kart bilgileri - Puan sorgulamak için kart gerekli
            AppendElement(sb, "ccno", request.Card.CardNumber);
            AppendElement(sb, "expDate", request.Card.ExpireDate);
            AppendElement(sb, "cvc", request.Card.Cvv);
            
            sb.Append("</pointUsage>");
            sb.Append(POSNET_CLOSE);

            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // AGREEMENT XML - İşlem Durumu Sorgulama
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İşlem durumu sorgulama XML'i oluşturur
        /// Bağlantı kopması durumunda işlemin akıbetini öğrenmek için
        /// </summary>
        public string BuildAgreementXml(PosnetAgreementRequest request)
        {
            var sb = new StringBuilder();
            sb.Append(XML_DECLARATION);
            sb.Append(POSNET_OPEN);
            
            AppendCredentials(sb, request.MerchantId, request.TerminalId);
            
            // Mutabakat sorgulama bloğu - "agreement" tag'i
            sb.Append("<agreement>");
            
            // Sorgulanacak sipariş numarası
            AppendElement(sb, "orderID", request.OrderId);
            
            sb.Append("</agreement>");
            sb.Append(POSNET_CLOSE);

            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // OOS XML - 3D Secure İşlemleri
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 3D Secure OOS talebi XML'i oluşturur
        /// 3D Secure akışını başlatmak için kullanılır
        /// VFT ve Joker Vadaa kampanya desteği dahil
        /// </summary>
        public string BuildOosRequestXml(PosnetOosRequest request)
        {
            PosnetRequestValidator.ValidateAndThrow(request);

            var sb = new StringBuilder();
            sb.Append(XML_DECLARATION);
            sb.Append(POSNET_OPEN);
            
            AppendCredentials(sb, request.MerchantId, request.TerminalId);
            
            // OOS (On-us/Off-us) işlem bloğu
            sb.Append("<oosRequestData>");
            
            // POSNET ID - 3D Secure şifreleme için zorunlu
            AppendElement(sb, "posnetid", request.PosnetId);
            
            // XID - İşlem referans numarası (OrderId ile aynı olabilir)
            AppendElement(sb, "XID", request.OrderId);
            
            // Tutar ve para birimi
            AppendElement(sb, "amount", request.Amount.ToString());
            AppendElement(sb, "currencyCode", GetCurrencyCode(request.CurrencyCode));
            AppendElement(sb, "installment", request.Installment);
            
            // İşlem tipi
            AppendElement(sb, "tranType", request.TxnType);
            
            // Kart sahibi adı (opsiyonel ama önerilen)
            if (!string.IsNullOrEmpty(request.Card.CardHolderName))
            {
                AppendElement(sb, "cardHolderName", XmlEscape(request.Card.CardHolderName));
            }
            
            // Kart bilgileri
            AppendElement(sb, "ccno", request.Card.CardNumber);
            AppendElement(sb, "expDate", request.Card.ExpireDate);
            AppendElement(sb, "cvc", request.Card.Cvv);
            
            // NOTE: 3sapi dokümanına göre returnURL oosRequestData içinde yer almaz.

            // ═══════════════════════════════════════════════════════════════════════
            // VFT (VADE FARKLI TAKSİT) KAMPANYA DESTEĞİ
            // POSNET Dokümanı: Sayfa 18-19
            // Banka ile anlaşmalı vade farklı taksit kampanyaları için kullanılır
            // ═══════════════════════════════════════════════════════════════════════
            if (!string.IsNullOrWhiteSpace(request.VftCode))
            {
                // VFT kampanya kodu - Banka tarafından verilen kod
                AppendElement(sb, "vftCode", request.VftCode);
                
                // VFT mağaza kodu (opsiyonel) - Bazı kampanyalarda gerekli
                if (!string.IsNullOrWhiteSpace(request.VftDealerCode))
                {
                    AppendElement(sb, "vftDealerCode", request.VftDealerCode);
                }
            }

            // ═══════════════════════════════════════════════════════════════════════
            // JOKER VADAA KAMPANYA DESTEĞİ
            // POSNET Dokümanı: Sayfa 20-22
            // Yapı Kredi kartlarına özel kişiselleştirilmiş taksit kampanyalarıdır
            // Kart sahibine özel kampanya teklifleri sunulur
            // ═══════════════════════════════════════════════════════════════════════
            if (request.UseJokerVadaa)
            {
                // Joker Vadaa sorgulaması aktif
                AppendElement(sb, "useJokerVadaa", "1");
                
                // Belirli bir kampanya ID varsa gönder (opsiyonel)
                // Null ise banka en uygun kampanyayı otomatik seçer
                if (!string.IsNullOrWhiteSpace(request.JokerVadaaCampaignId))
                {
                    AppendElement(sb, "jokerVadaaCampaignId", request.JokerVadaaCampaignId);
                }
            }
            
            sb.Append("</oosRequestData>");
            sb.Append(POSNET_CLOSE);

            return sb.ToString();
        }

        /// <summary>
        /// 3D Secure OOS TDS (Transaction Data Signing) XML'i oluşturur
        /// Banka callback'inden sonra işlemi tamamlamak için kullanılır
        /// </summary>
        public string BuildOosTdsXml(
            string merchantId, string terminalId, string posnetId,
            string orderId, int amount, string currencyCode, string installment,
            string txnType, string cardHolderName, string ccNumber, string expDate, string cvc)
        {
            var sb = new StringBuilder();
            sb.Append(XML_DECLARATION);
            sb.Append(POSNET_OPEN);
            
            AppendCredentials(sb, merchantId, terminalId);
            
            // OOS TDS bloğu - 3D sonrası işlem tamamlama
            sb.Append("<oosTranData>");
            
            AppendElement(sb, "posnetid", posnetId);
            AppendElement(sb, "XID", orderId);
            AppendElement(sb, "amount", amount.ToString());
            AppendElement(sb, "currencyCode", GetCurrencyCode(currencyCode));
            AppendElement(sb, "installment", installment);
            AppendElement(sb, "tranType", txnType);
            
            if (!string.IsNullOrEmpty(cardHolderName))
            {
                AppendElement(sb, "cardHolderName", XmlEscape(cardHolderName));
            }
            
            AppendElement(sb, "ccno", ccNumber);
            AppendElement(sb, "expDate", expDate);
            AppendElement(sb, "cvc", cvc);
            
            sb.Append("</oosTranData>");
            sb.Append(POSNET_CLOSE);

            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // OOS RESOLVE MERCHANT DATA - 3D Secure Callback Veri Çözümleme
        // Dokümantasyon: POSNET 3D Secure Entegrasyon Dokümanı - Sayfa 12-14
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 3D Secure callback'inden gelen şifreli verileri çözmek için XML oluşturur.
        /// 
        /// NEDEN BU SERVİS GEREKLİ?
        /// - Banka 3D doğrulama sonrası merchantData ve bankData'yı şifreli olarak döner
        /// - Bu veriler oosResolveMerchantData servisi ile deşifre edilmelidir
        /// - Deşifre edilen xid, amount, currency değerleri orijinal verilerle karşılaştırılmalı
        /// - Bu adım MAN-IN-THE-MIDDLE saldırılarına karşı kritik güvenlik sağlar
        /// 
        /// REQUEST FORMATI (POSNET Dokümanı sayfa 12):
        /// &lt;posnetRequest&gt;
        ///   &lt;mid&gt;MerchantId&lt;/mid&gt;
        ///   &lt;tid&gt;TerminalId&lt;/tid&gt;
        ///   &lt;oosResolveMerchantData&gt;
        ///     &lt;bankData&gt;...şifreli veri...&lt;/bankData&gt;
        ///     &lt;merchantData&gt;...şifreli veri...&lt;/merchantData&gt;
        ///     &lt;sign&gt;...imza...&lt;/sign&gt;
        ///     &lt;mac&gt;...MAC değeri...&lt;/mac&gt;
        ///   &lt;/oosResolveMerchantData&gt;
        /// &lt;/posnetRequest&gt;
        /// </summary>
        /// <param name="request">Resolve request modeli</param>
        /// <returns>XML string</returns>
        public string BuildOosResolveMerchantDataXml(PosnetOosResolveMerchantDataRequest request)
        {
            // GÜVENLIK: Null/empty kontrolleri - XSS ve injection koruması
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "OosResolveMerchantData request null olamaz");
            }

            if (string.IsNullOrWhiteSpace(request.BankData))
            {
                throw new ArgumentException("BankData alanı boş olamaz", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.MerchantData))
            {
                throw new ArgumentException("MerchantData alanı boş olamaz", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Sign))
            {
                throw new ArgumentException("Sign alanı boş olamaz", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Mac))
            {
                throw new ArgumentException("MAC alanı boş olamaz", nameof(request));
            }

            var sb = new StringBuilder();
            sb.Append(XML_DECLARATION);
            sb.Append(POSNET_OPEN);
            
            // Üye işyeri kimlik bilgileri
            AppendCredentials(sb, request.MerchantId, request.TerminalId);
            
            // oosResolveMerchantData bloğu başlangıcı
            sb.Append("<oosResolveMerchantData>");
            
            // bankData: Banka tarafından 3D callback'te dönen şifreli veri
            // Bu veri finansallaştırma için gerekli bilgileri içerir
            AppendElement(sb, "bankData", request.BankData);
            
            // merchantData: İşyeri tarafından gönderilen ve geri dönen şifreli veri
            // İşlem bilgilerini (xid, amount, currency vb.) içerir
            AppendElement(sb, "merchantData", request.MerchantData);
            
            // sign: Dijital imza - Banka tarafından üretilen veri doğrulama imzası
            AppendElement(sb, "sign", request.Sign);
            
            // mac: Message Authentication Code
            // İşyeri tarafından hesaplanan MAC ile karşılaştırma için kullanılır
            // Formül: HASH(xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
            AppendElement(sb, "mac", request.Mac);
            
            sb.Append("</oosResolveMerchantData>");
            sb.Append(POSNET_CLOSE);

            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // OOS TRAN DATA - 3D Secure Finansallaştırma
        // Dokümantasyon: POSNET 3D Secure Entegrasyon Dokümanı - Sayfa 15-17
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 3D Secure sonrası işlemi finansallaştırmak için XML oluşturur.
        /// 
        /// NEDEN BU SERVİS GEREKLİ?
        /// - 3D Secure doğrulaması başarılı olduktan sonra işlem henüz finansallaşmamıştır
        /// - oosTranData servisi ile işlem finansallaştırılır (para gerçekten çekilir)
        /// - MAC doğrulaması yapılmadan bu adıma geçilmemelidir!
        /// 
        /// REQUEST FORMATI (POSNET Dokümanı sayfa 15):
        /// &lt;posnetRequest&gt;
        ///   &lt;mid&gt;MerchantId&lt;/mid&gt;
        ///   &lt;tid&gt;TerminalId&lt;/tid&gt;
        ///   &lt;oosTranData&gt;
        ///     &lt;bankData&gt;...şifreli veri...&lt;/bankData&gt;
        ///     &lt;wpAmount&gt;0&lt;/wpAmount&gt;
        ///     &lt;mac&gt;...MAC değeri...&lt;/mac&gt;
        ///   &lt;/oosTranData&gt;
        /// &lt;/posnetRequest&gt;
        /// </summary>
        /// <param name="request">Finansallaştırma request modeli</param>
        /// <returns>XML string</returns>
        public string BuildOosTranDataXml(PosnetOosTranDataRequest request)
        {
            // GÜVENLIK: Null/empty kontrolleri
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "OosTranData request null olamaz");
            }

            if (string.IsNullOrWhiteSpace(request.BankData))
            {
                throw new ArgumentException("BankData alanı boş olamaz", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Mac))
            {
                throw new ArgumentException("MAC alanı boş olamaz", nameof(request));
            }

            var sb = new StringBuilder();
            sb.Append(XML_DECLARATION);
            sb.Append(POSNET_OPEN);
            
            // Üye işyeri kimlik bilgileri
            AppendCredentials(sb, request.MerchantId, request.TerminalId);
            
            // oosTranData bloğu başlangıcı
            sb.Append("<oosTranData>");
            
            // bankData: oosResolveMerchantData veya 3D callback'ten gelen veri
            // Finansallaştırma için bankaya geri gönderilir
            // NOT: bankData genellikle Base64 encoded ve çok uzun olabilir
            AppendElement(sb, "bankData", request.BankData);
            
            // wpAmount: World Puan kullanım tutarı (kuruş cinsinden)
            // POSNET dokümantasyonuna göre HER ZAMAN gönderilmeli!
            // Puan kullanılmıyorsa 0 gönderilir
            AppendElement(sb, "wpAmount", request.WpAmount.ToString());
            
            // mac: Message Authentication Code
            // Formül: HASH(xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
            AppendElement(sb, "mac", request.Mac);
            
            sb.Append("</oosTranData>");
            sb.Append(POSNET_CLOSE);

            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ENCODING - HTTP POST için URL Encoding
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// XML'i HTTP POST için URL encode eder
        /// POSNET API, xmldata parametresi ile URL encoded XML bekler
        /// </summary>
        /// <param name="xml">Encode edilecek XML</param>
        /// <returns>URL encoded string</returns>
        public string EncodeForPost(string xml)
        {
            // UTF-8 encoding ile URL encode
            // POSNET beklentisi: xmldata=...encoded_xml...
            return HttpUtility.UrlEncode(xml, Encoding.UTF8);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // HELPER METHODS - Yardımcı metodlar
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Üye işyeri kimlik bilgilerini XML'e ekler
        /// Her request'te zorunlu alanlar
        /// </summary>
        private static void AppendCredentials(StringBuilder sb, string merchantId, string terminalId)
        {
            // mid: Üye işyeri numarası (10 hane)
            AppendElement(sb, "mid", merchantId);
            
            // tid: Terminal numarası (8 hane)
            AppendElement(sb, "tid", terminalId);
        }

        /// <summary>
        /// XML element'i StringBuilder'a ekler
        /// Null veya boş değerler için element eklenmez
        /// </summary>
        /// <param name="sb">StringBuilder instance</param>
        /// <param name="name">Element adı</param>
        /// <param name="value">Element değeri</param>
        private static void AppendElement(StringBuilder sb, string name, string? value)
        {
            if (string.IsNullOrEmpty(value)) return;
            
            sb.Append('<');
            sb.Append(name);
            sb.Append('>');
            sb.Append(XmlEscape(value));
            sb.Append("</");
            sb.Append(name);
            sb.Append('>');
        }

        /// <summary>
        /// XML özel karakterlerini escape eder
        /// Güvenlik ve XML validity için zorunlu
        /// </summary>
        /// <param name="value">Escape edilecek değer</param>
        /// <returns>Escaped string</returns>
        private static string XmlEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            return value
                .Replace("&", "&amp;")   // & işareti - önce bu yapılmalı!
                .Replace("<", "&lt;")    // Küçüktür işareti
                .Replace(">", "&gt;")    // Büyüktür işareti
                .Replace("\"", "&quot;") // Çift tırnak
                .Replace("'", "&apos;"); // Tek tırnak
        }

        /// <summary>
        /// Para birimi kodunu POSNET formatına çevirir
        /// POSNET, ISO 4217 yerine kendi kodlarını kullanır
        /// </summary>
        /// <param name="currencyCode">ISO currency code (TRY, USD, EUR)</param>
        /// <returns>POSNET currency code</returns>
        private static string GetCurrencyCode(string currencyCode)
        {
            // POSNET para birimi kodları (3D ve XML servis dokümanına göre)
            // TL/TRY = TL, USD = US, EUR = EU
            return currencyCode?.ToUpperInvariant() switch
            {
                "TRY" or "TL" or "YT" => "TL",
                "USD" or "US" => "US",
                "EUR" or "EU" => "EU",
                "GBP" => "GB",
                _ => "TL" // Varsayılan TL
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // POSNET MAC CALCULATOR
    // 3D Secure işlemleri için MAC (Message Authentication Code) hesaplama
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// POSNET MAC hesaplama servisi
    /// 3D Secure işlemlerinde veri bütünlüğü doğrulaması için
    /// </summary>
    public static class PosnetMacCalculator
    {
        /// <summary>
        /// OOS Request için MAC hesaplar
        /// POSNET'in beklediği formatta SHA-256 hash
        /// </summary>
        /// <param name="encKey">3D Secure şifreleme anahtarı</param>
        /// <param name="xid">Transaction ID (OrderID)</param>
        /// <param name="amount">Tutar (kuruş)</param>
        /// <param name="currencyCode">Para birimi</param>
        /// <param name="merchantId">Üye işyeri no</param>
        /// <param name="posnetId">POSNET ID</param>
        /// <returns>Base64 encoded MAC</returns>
        public static string CalculateOosMac(
            string encKey, string xid, int amount, 
            string currencyCode, string merchantId, string posnetId)
        {
            // MAC hesaplama formatı: XID;Amount;Currency;MerchantId;PosnetId
            // Bu string SHA-256 ile hash'lenir, sonra encKey ile encrypt edilir
            var data = $"{xid};{amount};{currencyCode};{merchantId};{posnetId}";
            
            return CalculateSha256Mac(data, encKey);
        }

        /// <summary>
        /// OOS Callback için MAC doğrular
        /// Bankadan gelen verilerin bütünlüğünü kontrol eder
        /// </summary>
        /// <param name="merchantData">Şifrelenmiş üye işyeri verisi</param>
        /// <param name="bankData">Şifrelenmiş banka verisi</param>
        /// <param name="sign">Banka imzası</param>
        /// <param name="encKey">Şifreleme anahtarı</param>
        /// <returns>MAC geçerli mi?</returns>
        public static bool ValidateCallbackMac(
            string merchantData, string bankData, string sign, string encKey)
        {
            try
            {
                // Callback MAC formatı: MerchantData + BankData hash
                var data = merchantData + bankData;
                var calculatedMac = CalculateSha256Mac(data, encKey);
                
                // Güvenli string karşılaştırma - Timing attack koruması
                return SecureCompare(calculatedMac, sign);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// SHA-256 MAC hesaplar
        /// POSNET standart MAC algoritması
        /// </summary>
        private static string CalculateSha256Mac(string data, string key)
        {
            // Key'i hash'le (POSNET bazı senaryolarda key hash'i bekler)
            using var sha256 = SHA256.Create();
            
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            
            // Key + Data birleştirip hash'le
            var combined = new byte[keyBytes.Length + dataBytes.Length];
            Buffer.BlockCopy(keyBytes, 0, combined, 0, keyBytes.Length);
            Buffer.BlockCopy(dataBytes, 0, combined, keyBytes.Length, dataBytes.Length);
            
            var hash = sha256.ComputeHash(combined);
            
            // Base64 encode
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Timing attack korumalı string karşılaştırma
        /// Sabit zamanlı karşılaştırma ile güvenlik sağlar
        /// </summary>
        private static bool SecureCompare(string a, string b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            var result = 0;
            for (var i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }
            
            return result == 0;
        }
    }
}
