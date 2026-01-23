// ═══════════════════════════════════════════════════════════════════════════════════════════════
// POSNET RESPONSE MODEL'LERİ
// Yapı Kredi POSNET XML API'sinden dönen tüm yanıt tipleri
// Dokümantasyon: POSNET XML Servisleri Entegrasyon Dokümanı v2.1.1.3
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// NEDEN BU YAPIYI SEÇTİK?
// 1. Immutable record yapısı - Response'lar değiştirilmemeli
// 2. Nullable alanlar - POSNET her zaman tüm alanları doldurmuyor
// 3. Factory pattern - XML parse sonucu kolay nesne oluşturma
// 4. Result pattern - Success/Failure durumları tek tip yönetim
// ═══════════════════════════════════════════════════════════════════════════════════════════════

using System;

namespace ECommerce.Infrastructure.Services.Payment.Posnet.Models
{
    #region Base Response

    /// <summary>
    /// Tüm POSNET response'larının ortak base class'ı
    /// Approved, ErrorCode, ErrorMessage tüm yanıtlarda ortak
    /// </summary>
    public abstract record PosnetBaseResponse
    {
        /// <summary>
        /// İşlem onaylandı mı? (1 = Evet, 0 = Hayır)
        /// POSNET'ten "approved" tag'i ile döner
        /// </summary>
        public bool Approved { get; init; }

        /// <summary>
        /// Hata kodu - 0 veya 00 = Başarılı
        /// POSNET'ten "respCode" veya "hostlogkey" ile kontrol
        /// </summary>
        public string? RawErrorCode { get; init; }

        /// <summary>
        /// Parse edilmiş hata kodu
        /// </summary>
        public PosnetErrorCode ErrorCode => PosnetErrorCodeExtensions.ParseFromString(RawErrorCode);

        /// <summary>
        /// Hata mesajı - POSNET'ten "respText" ile döner
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Ham XML yanıtı - Debug ve loglama için
        /// </summary>
        public string? RawXml { get; init; }

        /// <summary>
        /// Response alınma zamanı - Loglama için
        /// </summary>
        public DateTime ReceivedAt { get; } = DateTime.UtcNow;

        /// <summary>
        /// İşlem başarılı mı? (Approved && ErrorCode == Success)
        /// </summary>
        public bool IsSuccess => Approved && ErrorCode.IsSuccess();

        /// <summary>
        /// İşlem başarısız mı?
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Kullanıcıya gösterilebilecek hata mesajı
        /// Banka ret kodları için anlaşılır mesaj döner
        /// </summary>
        public string UserFriendlyMessage => 
            IsSuccess 
                ? "İşlem başarıyla tamamlandı" 
                : ErrorCode.GetDescription();
    }

    #endregion

    #region Sale Response

    /// <summary>
    /// POSNET satış işlemi yanıt modeli
    /// </summary>
    public sealed record PosnetSaleResponse : PosnetBaseResponse
    {
        /// <summary>
        /// Host log key - İptal ve iade işlemlerinde kullanılır
        /// Bu değer mutlaka saklanmalı!
        /// </summary>
        public string? HostLogKey { get; init; }

        /// <summary>
        /// Yetki kodu (Authorization Code)
        /// Bankadan alınan onay kodu
        /// </summary>
        public string? AuthCode { get; init; }

        /// <summary>
        /// Referans numarası (RRN - Retrieval Reference Number)
        /// </summary>
        public string? Rrn { get; init; }

        /// <summary>
        /// İşlem ID
        /// </summary>
        public string? TransactionId { get; init; }

        /// <summary>
        /// Sipariş numarası (gönderilen OrderId)
        /// </summary>
        public string? OrderId { get; init; }

        /// <summary>
        /// Taksit sayısı
        /// </summary>
        public string? Installment { get; init; }

        /// <summary>
        /// İşlem tutarı (kuruş)
        /// </summary>
        public int? Amount { get; init; }

        /// <summary>
        /// Toplam tutar (taksitli işlemlerde)
        /// </summary>
        public int? TotalAmount { get; init; }

        /// <summary>
        /// İşlem tarihi (POSNET formatı)
        /// </summary>
        public string? TransactionDate { get; init; }

        /// <summary>
        /// Puan bilgisi (varsa)
        /// </summary>
        public PosnetPointInfo? PointInfo { get; init; }

        /// <summary>
        /// Başarılı response oluşturur - Factory method
        /// </summary>
        public static PosnetSaleResponse Success(
            string hostLogKey, string authCode, string orderId, 
            int amount, string? installment = null, string? rawXml = null)
        {
            return new PosnetSaleResponse
            {
                Approved = true,
                RawErrorCode = "0",
                HostLogKey = hostLogKey,
                AuthCode = authCode,
                OrderId = orderId,
                Amount = amount,
                Installment = installment ?? "00",
                RawXml = rawXml
            };
        }

        /// <summary>
        /// Başarısız response oluşturur - Factory method
        /// </summary>
        public static PosnetSaleResponse Failure(string errorCode, string errorMessage, string? rawXml = null)
        {
            return new PosnetSaleResponse
            {
                Approved = false,
                RawErrorCode = errorCode,
                ErrorMessage = errorMessage,
                RawXml = rawXml
            };
        }
    }

    #endregion

    #region Auth Response

    /// <summary>
    /// POSNET provizyon (ön yetkilendirme) yanıt modeli
    /// </summary>
    public sealed record PosnetAuthResponse : PosnetBaseResponse
    {
        /// <summary>
        /// Host log key - Finansallaştırma için gerekli
        /// </summary>
        public string? HostLogKey { get; init; }

        /// <summary>
        /// Yetki kodu
        /// </summary>
        public string? AuthCode { get; init; }

        /// <summary>
        /// Sipariş numarası
        /// </summary>
        public string? OrderId { get; init; }

        /// <summary>
        /// Bloke edilen tutar (kuruş)
        /// </summary>
        public int? Amount { get; init; }

        /// <summary>
        /// Provizyon geçerlilik süresi (saat)
        /// Varsayılan: 7 gün (168 saat)
        /// </summary>
        public int? ValidityHours { get; init; }
    }

    #endregion

    #region Capt Response

    /// <summary>
    /// POSNET finansallaştırma yanıt modeli
    /// </summary>
    public sealed record PosnetCaptResponse : PosnetBaseResponse
    {
        /// <summary>
        /// Yeni host log key
        /// </summary>
        public string? HostLogKey { get; init; }

        /// <summary>
        /// Yetki kodu
        /// </summary>
        public string? AuthCode { get; init; }

        /// <summary>
        /// Sipariş numarası
        /// </summary>
        public string? OrderId { get; init; }

        /// <summary>
        /// Finansallaştırılan tutar (kuruş)
        /// </summary>
        public int? Amount { get; init; }
    }

    #endregion

    #region Reverse Response

    /// <summary>
    /// POSNET iptal yanıt modeli
    /// </summary>
    public sealed record PosnetReverseResponse : PosnetBaseResponse
    {
        /// <summary>
        /// İptal edilen işlemin sipariş numarası
        /// </summary>
        public string? OrderId { get; init; }

        /// <summary>
        /// İptal edilen tutart (kuruş)
        /// </summary>
        public int? Amount { get; init; }

        /// <summary>
        /// Yetki kodu
        /// </summary>
        public string? AuthCode { get; init; }
    }

    #endregion

    #region Return Response

    /// <summary>
    /// POSNET iade yanıt modeli
    /// </summary>
    public sealed record PosnetReturnResponse : PosnetBaseResponse
    {
        /// <summary>
        /// İade işleminin host log key'i
        /// </summary>
        public string? HostLogKey { get; init; }

        /// <summary>
        /// İade işlemi sipariş numarası
        /// </summary>
        public string? OrderId { get; init; }

        /// <summary>
        /// İade edilen tutar (kuruş)
        /// </summary>
        public int? Amount { get; init; }

        /// <summary>
        /// Yetki kodu
        /// </summary>
        public string? AuthCode { get; init; }

        /// <summary>
        /// Orijinal işlemin sipariş numarası
        /// </summary>
        public string? OriginalOrderId { get; init; }
    }

    #endregion

    #region Point Inquiry Response

    /// <summary>
    /// POSNET puan sorgulama yanıt modeli
    /// </summary>
    public sealed record PosnetPointInquiryResponse : PosnetBaseResponse
    {
        /// <summary>
        /// Puan bilgisi
        /// </summary>
        public PosnetPointInfo? PointInfo { get; init; }

        /// <summary>
        /// Kart World programına kayıtlı mı?
        /// </summary>
        public bool IsEnrolled { get; init; }
    }

    /// <summary>
    /// World puan detay bilgisi
    /// </summary>
    public sealed record PosnetPointInfo
    {
        /// <summary>
        /// World puan bakiyesi
        /// </summary>
        public int WorldPoint { get; init; }

        /// <summary>
        /// Marka puanı bakiyesi
        /// </summary>
        public int BrandPoint { get; init; }

        /// <summary>
        /// Toplam kullanılabilir puan
        /// </summary>
        public int TotalPoint => WorldPoint + BrandPoint;

        /// <summary>
        /// Puanın TL karşılığı (1 puan = 1 kuruş varsayım)
        /// </summary>
        public decimal PointAsTL => TotalPoint / 100m;

        /// <summary>
        /// Puan kullanılabilir mi? (en az 100 puan = 1 TL)
        /// </summary>
        public bool CanUsePoints => TotalPoint >= 100;
    }

    #endregion

    #region Agreement Response

    /// <summary>
    /// POSNET işlem durumu sorgulama yanıt modeli
    /// Bağlantı kopması durumunda işlemin akıbetini öğrenmek için
    /// </summary>
    public sealed record PosnetAgreementResponse : PosnetBaseResponse
    {
        /// <summary>
        /// Sorgulanan işlemin durumu
        /// </summary>
        public PosnetTransactionStatus TransactionStatus { get; init; }

        /// <summary>
        /// Sipariş numarası
        /// </summary>
        public string? OrderId { get; init; }

        /// <summary>
        /// İşlem tutarı (kuruş)
        /// </summary>
        public int? Amount { get; init; }

        /// <summary>
        /// Host log key (varsa)
        /// </summary>
        public string? HostLogKey { get; init; }

        /// <summary>
        /// Yetki kodu (varsa)
        /// </summary>
        public string? AuthCode { get; init; }

        /// <summary>
        /// İşlem tarihi
        /// </summary>
        public string? TransactionDate { get; init; }
    }

    /// <summary>
    /// POSNET işlem durumu enum'u
    /// Agreement sorgusundan dönen durum bilgisi
    /// </summary>
    public enum PosnetTransactionStatus
    {
        /// <summary>İşlem bulunamadı</summary>
        NotFound,
        
        /// <summary>İşlem başarılı tamamlanmış</summary>
        Completed,
        
        /// <summary>İşlem beklemede</summary>
        Pending,
        
        /// <summary>İşlem reddedilmiş</summary>
        Rejected,
        
        /// <summary>İşlem iptal edilmiş</summary>
        Cancelled,
        
        /// <summary>İşlem iade edilmiş</summary>
        Refunded,
        
        /// <summary>Durum bilinmiyor</summary>
        Unknown,

        /// <summary>İşlem daha önce işlendi (AlreadyProcessed)</summary>
        AlreadyProcessed
    }

    #endregion

    #region OOS (3D Secure) Response

    /// <summary>
    /// POSNET 3D Secure OOS başlatma yanıt modeli
    /// </summary>
    public sealed record PosnetOosResponse : PosnetBaseResponse
    {
        /// <summary>
        /// 3D Secure yönlendirme için gerekli data1
        /// Form'da hidden field olarak gönderilir
        /// </summary>
        public string? Data1 { get; init; }

        /// <summary>
        /// 3D Secure yönlendirme için gerekli data2
        /// </summary>
        public string? Data2 { get; init; }

        /// <summary>
        /// İmza (MAC hash)
        /// </summary>
        public string? Sign { get; init; }

        /// <summary>
        /// Banka 3D Secure sayfası URL'i
        /// Kullanıcı bu sayfaya yönlendirilir
        /// </summary>
        public string? RedirectUrl { get; init; }

        /// <summary>
        /// Sipariş numarası
        /// </summary>
        public string? OrderId { get; init; }

        /// <summary>
        /// 3D Secure yönlendirme gerekiyor mu?
        /// </summary>
        public bool RequiresRedirect => IsSuccess && !string.IsNullOrEmpty(Data1);

        /// <summary>
        /// 3D Secure form HTML'i oluşturur (POSNET 3sapi dokümanına uygun)
        /// Auto-submit form ile kullanıcı bankaya yönlendirilir
        /// CSP uyumlu - inline script yok
        /// </summary>
        public string GenerateAutoSubmitForm(
            string actionUrl,
            string merchantId,
            string posnetId,
            string merchantReturnUrl,
            string? lang = "tr",
            string? openANewWindow = "0",
            string? url = "",
            string? vftCode = null,
            string? useJokerVadaa = null)
        {
            if (!RequiresRedirect) return string.Empty;

            // CSP uyumlu form - JavaScript olmadan, sadece noscript için submit butonu
            // Frontend tarafında form.submit() çağrılacak
            return $@"
<form id=""posnetForm"" name=""posnetForm"" method=""POST"" action=""{actionUrl}"">
    <input type=""hidden"" name=""mid"" value=""{merchantId}"" />
    <input type=""hidden"" name=""posnetID"" value=""{posnetId}"" />
    <input type=""hidden"" name=""posnetData"" value=""{Data1}"" />
    <input type=""hidden"" name=""posnetData2"" value=""{Data2 ?? string.Empty}"" />
    <input type=""hidden"" name=""digest"" value=""{Sign}"" />
    {(string.IsNullOrWhiteSpace(vftCode) ? "" : $@"<input type=""hidden"" name=""vftCode"" value=""{vftCode}"" />")}
    {(string.IsNullOrWhiteSpace(useJokerVadaa) ? "" : $@"<input type=""hidden"" name=""useJokerVadaa"" value=""{useJokerVadaa}"" />")}
    <input type=""hidden"" name=""merchantReturnURL"" value=""{merchantReturnUrl}"" />
    <input type=""hidden"" name=""lang"" value=""{lang}"" />
    <input type=""hidden"" name=""url"" value=""{url}"" />
    <input type=""hidden"" name=""openANewWindow"" value=""{openANewWindow}"" />
    <noscript>
        <p>JavaScript devre dışı. Lütfen butona tıklayın.</p>
        <input type=""submit"" value=""Ödeme Sayfasına Git"" />
    </noscript>
</form>";
        }
    }

    #endregion

    #region Generic Result Wrapper

    /// <summary>
    /// Generic POSNET işlem sonucu wrapper'ı
    /// Tüm işlemlerde tutarlı hata yönetimi sağlar
    /// </summary>
    /// <typeparam name="T">Response tipi</typeparam>
    public sealed record PosnetResult<T> where T : PosnetBaseResponse
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// Response verisi (başarılı ise)
        /// </summary>
        public T? Data { get; init; }

        /// <summary>
        /// Hata mesajı (başarısız ise)
        /// </summary>
        public string? Error { get; init; }

        /// <summary>
        /// Hata kodu
        /// </summary>
        public PosnetErrorCode ErrorCode { get; init; } = PosnetErrorCode.Success;

        /// <summary>
        /// Exception (varsa)
        /// </summary>
        public Exception? Exception { get; init; }

        /// <summary>
        /// İşlem süresi (ms)
        /// </summary>
        public long ElapsedMilliseconds { get; init; }

        /// <summary>
        /// Başarılı sonuç oluşturur
        /// </summary>
        public static PosnetResult<T> Success(T data, long elapsedMs = 0)
        {
            return new PosnetResult<T>
            {
                IsSuccess = true,
                Data = data,
                ElapsedMilliseconds = elapsedMs
            };
        }

        /// <summary>
        /// Başarısız sonuç oluşturur
        /// </summary>
        public static PosnetResult<T> Failure(string error, PosnetErrorCode errorCode, Exception? ex = null, long elapsedMs = 0)
        {
            return new PosnetResult<T>
            {
                IsSuccess = false,
                Error = error,
                ErrorCode = errorCode,
                Exception = ex,
                ElapsedMilliseconds = elapsedMs
            };
        }

        /// <summary>
        /// Response'dan result oluşturur
        /// </summary>
        public static PosnetResult<T> FromResponse(T response, long elapsedMs = 0)
        {
            if (response.IsSuccess)
                return Success(response, elapsedMs);

            return new PosnetResult<T>
            {
                IsSuccess = false,
                Data = response,
                Error = response.ErrorMessage ?? response.ErrorCode.GetDescription(),
                ErrorCode = response.ErrorCode,
                ElapsedMilliseconds = elapsedMs
            };
        }
    }

    #endregion

    #region OOS Resolve Merchant Data Response

    /// <summary>
    /// POSNET oosResolveMerchantData yanıt modeli
    /// 3D Secure callback verilerinin deşifre edilmiş hali
    /// 
    /// KULLANIM:
    /// 1. Response alındıktan sonra Xid ve Amount orijinal değerlerle karşılaştırılmalı
    /// 2. Mac değeri işyeri tarafından hesaplanan MAC ile karşılaştırılmalı
    /// 3. MdStatus başarılı (1,2,3,4) ise finansallaştırmaya geçilebilir
    /// 
    /// GÜVENLİK KONTROL LİSTESİ:
    /// ✓ Xid orijinal sipariş numarası ile aynı mı?
    /// ✓ Amount orijinal tutar ile aynı mı?
    /// ✓ Currency orijinal para birimi ile aynı mı?
    /// ✓ Mac doğru hesaplanmış mı?
    /// ✓ MdStatus başarılı bir değer mi?
    /// 
    /// Dokümantasyon: POSNET 3D Secure Entegrasyon Dokümanı - Sayfa 13-14
    /// </summary>
    public sealed record PosnetOosResolveMerchantDataResponse : PosnetBaseResponse
    {
        // ═══════════════════════════════════════════════════════════════
        // İŞLEM BİLGİLERİ (Deşifre Edilmiş)
        // Bu değerler orijinal değerlerle karşılaştırılmalı!
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Alışveriş sipariş numarası (Transaction ID)
        /// Orijinal XID ile karşılaştırılmalı!
        /// </summary>
        public string Xid { get; init; } = string.Empty;

        /// <summary>
        /// Alışveriş tutarı (kuruş cinsinden)
        /// Örnek: 1234 = 12.34 TL
        /// Orijinal tutar ile karşılaştırılmalı!
        /// </summary>
        public int Amount { get; init; }

        /// <summary>
        /// Para birimi kodu
        /// TL = Türk Lirası, US = Amerikan Doları, EU = Euro
        /// Orijinal para birimi ile karşılaştırılmalı!
        /// </summary>
        public string Currency { get; init; } = "TL";

        /// <summary>
        /// Taksit sayısı
        /// 00 = Peşin, 02-12 = Taksit
        /// </summary>
        public string Installment { get; init; } = "00";

        // ═══════════════════════════════════════════════════════════════
        // PUAN BİLGİLERİ (World Card için)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Kullanılabilir puan miktarı
        /// Örnek: 340 puan
        /// </summary>
        public int Point { get; init; }

        /// <summary>
        /// Kullanılabilir puan tutarı (kuruş cinsinden)
        /// Örnek: 170 = 1.70 TL
        /// </summary>
        public int PointAmount { get; init; }

        // ═══════════════════════════════════════════════════════════════
        // 3D SECURE DOĞRULAMA BİLGİLERİ
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// ThreeD Secure işlem durumu
        /// </summary>
        public string? TxStatus { get; init; }

        /// <summary>
        /// ThreeD Secure onay durumu - EN KRİTİK ALAN!
        /// 
        /// DEĞERLER:
        /// 0 = Kart doğrulama başarısız, İŞLEME DEVAM ETMEYİN
        /// 1 = Doğrulama başarılı, İŞLEME DEVAM EDEBİLİRSİNİZ ✓
        /// 2 = Kart sahibi veya bankası sisteme kayıtlı değil (işleme devam edilebilir)
        /// 3 = Kartın bankası sisteme kayıtlı değil (işleme devam edilebilir)
        /// 4 = Doğrulama denemesi (işleme devam edilebilir)
        /// 5 = Doğrulama yapılamıyor
        /// 6 = 3D Secure hatası
        /// 7 = Sistem hatası
        /// 8 = Bilinmeyen kart no
        /// 9 = Üye işyeri 3D-Secure sistemine kayıtlı değil
        /// </summary>
        public string MdStatus { get; init; } = "0";

        /// <summary>
        /// ThreeD Secure hata mesajı
        /// MdStatus başarısız olduğunda hata detayı içerir
        /// </summary>
        public string? MdErrorMessage { get; init; }

        /// <summary>
        /// MdStatus açıklaması (Türkçe)
        /// </summary>
        public string? MdStatusDescription { get; init; }

        // ═══════════════════════════════════════════════════════════════
        // MAC DOĞRULAMA
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Banka tarafından hesaplanan MAC değeri
        /// 
        /// DOĞRULAMA FORMÜLÜ (POSNET Dokümanı sayfa 14-15):
        /// İşyeri tarafından hesaplanacak MAC:
        /// HASH(mdStatus + ';' + xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
        /// firstHash = HASH(encKey + ';' + terminalID)
        /// 
        /// Bu MAC, response'daki MAC ile karşılaştırılmalı!
        /// Eşleşmiyorsa response banka tarafından gelmemiş demektir (MAN-IN-THE-MIDDLE saldırısı!)
        /// </summary>
        public string Mac { get; init; } = string.Empty;

        // ═══════════════════════════════════════════════════════════════
        // YARDIMCI PROPERTYLER
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Finansallaştırmaya devam edilebilir mi?
        /// MdStatus 1, 2, 3 veya 4 ise true
        /// </summary>
        public bool CanProceedWithPayment { get; init; }

        /// <summary>
        /// Tam 3D Secure doğrulaması yapıldı mı?
        /// Sadece MdStatus = 1 olduğunda true
        /// </summary>
        public bool IsFullyAuthenticated => MdStatus == "1";

        /// <summary>
        /// Kısmi doğrulama mı?
        /// MdStatus 2, 3, 4 olduğunda true (işleme devam edilebilir ama tam doğrulama yok)
        /// </summary>
        public bool IsPartiallyAuthenticated => MdStatus == "2" || MdStatus == "3" || MdStatus == "4";

        /// <summary>
        /// Puan kullanılabilir mi?
        /// Point ve PointAmount -1 değilse kullanılabilir
        /// </summary>
        public bool IsPointAvailable => Point > 0 && PointAmount > 0;
    }

    #endregion

    #region OOS Tran Data Response (Finansallaştırma)

    /// <summary>
    /// POSNET oosTranData (Finansallaştırma) yanıt modeli
    /// 3D Secure sonrası işlemin finansallaştırılma sonucu
    /// 
    /// BAŞARI DURUMUNDA:
    /// - HostLogKey mutlaka saklanmalı (iptal/iade işlemleri için gerekli)
    /// - AuthCode sipariş detaylarında gösterilebilir
    /// 
    /// APPROVED DEĞERLERİ:
    /// 1 = Başarılı, onaylandı
    /// 2 = Daha önce başarıyla onaylanmış (duplicate işlem)
    /// 0 = Başarısız, onaylanmadı
    /// 
    /// Dokümantasyon: POSNET 3D Secure Entegrasyon Dokümanı - Sayfa 16-17
    /// </summary>
    public sealed record PosnetOosTranDataResponse : PosnetBaseResponse
    {
        // ═══════════════════════════════════════════════════════════════
        // KRİTİK ALANLAR - MUTLAKA SAKLANMALI!
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Approved durumu detaylı (1, 2, 0)
        /// 2 = Daha önce onaylanmış anlamına gelir
        /// </summary>
        public string? ApprovedCode { get; init; }

        /// <summary>
        /// Banka tarafından hesaplanan MAC değeri
        /// Doğrulama için kullanılabilir (opsiyonel)
        /// 
        /// DOĞRULAMA FORMÜLÜ:
        /// HASH(hostLogkey + ';' + xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
        /// </summary>
        public string? Mac { get; init; }

        /// <summary>
        /// Host Log Key - EN KRİTİK ALAN!
        /// İptal ve iade işlemlerinde zorunlu
        /// Sipariş kaydında mutlaka saklanmalı!
        /// </summary>
        public string HostLogKey { get; init; } = string.Empty;

        /// <summary>
        /// Yetki kodu (Authorization Code)
        /// Bankadan alınan onay kodu
        /// Müşteriye dekont/fatura'da gösterilebilir
        /// </summary>
        public string AuthCode { get; init; } = string.Empty;

        // ═══════════════════════════════════════════════════════════════
        // TAKSİT BİLGİLERİ
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Taksit sayısı
        /// </summary>
        public string? Installment { get; init; }

        /// <summary>
        /// Taksit tutarı (kuruş cinsinden)
        /// </summary>
        public int? InstallmentAmount { get; init; }

        // ═══════════════════════════════════════════════════════════════
        // PUAN BİLGİLERİ (World Card için)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// İşlemden kazanılan puan
        /// </summary>
        public int EarnedPoint { get; init; }

        /// <summary>
        /// Kazanılan puan tutarı (kuruş cinsinden)
        /// </summary>
        public int EarnedPointAmount { get; init; }

        /// <summary>
        /// Toplam kullanılabilir puan
        /// </summary>
        public int TotalPoint { get; init; }

        /// <summary>
        /// Toplam kullanılabilir puan tutarı (kuruş cinsinden)
        /// </summary>
        public int TotalPointAmount { get; init; }

        // ═══════════════════════════════════════════════════════════════
        // VFT BİLGİLERİ (Vade Farklı İşlemler için)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// İşlem için hesaplanan ek vade tutarı (kuruş cinsinden)
        /// </summary>
        public int? VftAmount { get; init; }

        /// <summary>
        /// İşlem için hesaplanan ek vade gün sayısı
        /// </summary>
        public int? VftDayCount { get; init; }

        // ═══════════════════════════════════════════════════════════════
        // İŞLEM DURUMU
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// İşlem durumu
        /// </summary>
        public PosnetTransactionStatus TransactionStatus { get; init; } = PosnetTransactionStatus.Unknown;

        /// <summary>
        /// İşlem daha önce tamamlanmış mı?
        /// ApprovedCode = "2" ise true
        /// </summary>
        public bool IsAlreadyProcessed => ApprovedCode == "2";
    }

    #endregion
}
