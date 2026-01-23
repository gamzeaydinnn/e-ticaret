// ═══════════════════════════════════════════════════════════════════════════════
// POSNET 3D SECURE DTO MODELLERİ
// Yapı Kredi POSNET 3D Secure (OOS) işlemleri için veri transfer objeleri
// Dokümantasyon: POSNET XML Servisleri Entegrasyon Dokümanı v2.1.1.3 - Sayfa 35-50
// 
// OOS (On-us / Off-us) sistemi, 3D Secure doğrulamasını yönetir.
// Müşteri kart şifresini banka sayfasında girer, sonuç işyerine POST edilir.
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Infrastructure.Services.Payment.Posnet.Models
{
    // ═══════════════════════════════════════════════════════════════════════════════
    // 3D SECURE BAŞLATMA REQUEST DTO'LARI
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 3D Secure ödeme başlatma isteği - Frontend'den gelir
    /// </summary>
    public class Posnet3DSecureInitiateRequestDto
    {
        /// <summary>
        /// Sipariş ID
        /// </summary>
        [Required(ErrorMessage = "Sipariş ID zorunludur")]
        public int OrderId { get; set; }

        /// <summary>
        /// Ödeme tutarı (TL cinsinden, örn: 150.50)
        /// </summary>
        [Required(ErrorMessage = "Tutar zorunludur")]
        [Range(0.01, 999999.99, ErrorMessage = "Geçersiz tutar")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Kart numarası (16 hane)
        /// POSNET kendi validasyonunu yapar, Luhn kontrolü kaldırıldı (test kartları için)
        /// </summary>
        [Required(ErrorMessage = "Kart numarası zorunludur")]
        [RegularExpression(@"^\d{15,16}$", ErrorMessage = "Kart numarası 15-16 hane olmalıdır")]
        public string CardNumber { get; set; } = string.Empty;

        /// <summary>
        /// Kart üzerindeki isim
        /// </summary>
        [Required(ErrorMessage = "Kart sahibi adı zorunludur")]
        [StringLength(50, MinimumLength = 2)]
        public string CardHolderName { get; set; } = string.Empty;

        /// <summary>
        /// Son kullanma ayı (01-12)
        /// </summary>
        [Required(ErrorMessage = "Son kullanma ayı zorunludur")]
        [RegularExpression(@"^(0[1-9]|1[0-2])$", ErrorMessage = "Geçersiz ay formatı")]
        public string ExpireMonth { get; set; } = string.Empty;

        /// <summary>
        /// Son kullanma yılı (YY formatı, örn: 25)
        /// </summary>
        [Required(ErrorMessage = "Son kullanma yılı zorunludur")]
        [RegularExpression(@"^\d{2}$", ErrorMessage = "Geçersiz yıl formatı")]
        public string ExpireYear { get; set; } = string.Empty;

        /// <summary>
        /// CVV/CVC kodu (3-4 hane)
        /// </summary>
        [Required(ErrorMessage = "CVV zorunludur")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "Geçersiz CVV")]
        public string Cvv { get; set; } = string.Empty;

        /// <summary>
        /// Taksit sayısı (0 veya 1 = Tek çekim, 2-12 = Taksit)
        /// </summary>
        [Range(0, 12, ErrorMessage = "Taksit sayısı 0-12 arasında olmalı")]
        public int InstallmentCount { get; set; } = 0;

        /// <summary>
        /// Para birimi (varsayılan: TRY)
        /// </summary>
        public string Currency { get; set; } = "TRY";

        /// <summary>
        /// World Puan kullanılacak mı?
        /// </summary>
        public bool UseWorldPoints { get; set; } = false;

        /// <summary>
        /// Kullanılacak puan miktarı (UseWorldPoints true ise)
        /// </summary>
        public decimal? PointAmount { get; set; }

        /// <summary>
        /// Joker Vadaa kampanya ID (opsiyonel)
        /// </summary>
        public string? JokerVadaaCampaignId { get; set; }

        // ═══════════════════════════════════════════════════════════════════════
        // VFT VE JOKER VADAA KAMPANYA DESTEĞİ
        // Frontend'den gelen opsiyonel kampanya parametreleri
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// VFT (Vade Farklı Taksit) kampanya kodu
        /// Banka ile anlaşmalı kampanyalar için kullanılır.
        /// </summary>
        public string? VftCode { get; set; }

        /// <summary>
        /// VFT mağaza kodu (opsiyonel)
        /// </summary>
        public string? VftDealerCode { get; set; }

        /// <summary>
        /// Joker Vadaa kampanyası kullanılsın mı?
        /// true = Kart sahibine özel kampanya sorgulanır
        /// </summary>
        public bool UseJokerVadaa { get; set; } = false;
    }

    /// <summary>
    /// 3D Secure başlatma sonucu - Frontend'e döner
    /// </summary>
    public class Posnet3DSecureInitiateResponseDto
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Hata mesajı (başarısız ise)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Hata kodu
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// 3D Secure yönlendirme URL'i (başarılı ise)
        /// Müşteri bu URL'e POST ile yönlendirilmeli
        /// </summary>
        public string? RedirectUrl { get; set; }

        /// <summary>
        /// 3D Secure form verileri (HTML form olarak render edilecek)
        /// </summary>
        public Posnet3DSecureFormData? FormData { get; set; }

        /// <summary>
        /// İşlem referans numarası (XID)
        /// </summary>
        public string? TransactionId { get; set; }

        /// <summary>
        /// Sipariş ID
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// 3D Secure yönlendirmesi gerekiyor mu?
        /// </summary>
        public bool RequiresRedirect { get; set; }

        /// <summary>
        /// 3D Secure auto-submit HTML formu
        /// Bu HTML doğrudan DOM'a eklenip form submit edilebilir
        /// </summary>
        public string? ThreeDSecureHtml { get; set; }

        public static Posnet3DSecureInitiateResponseDto SuccessResponse(
            string redirectUrl, 
            Posnet3DSecureFormData formData,
            string transactionId,
            int orderId) => new()
        {
            Success = true,
            RedirectUrl = redirectUrl,
            FormData = formData,
            TransactionId = transactionId,
            OrderId = orderId,
            RequiresRedirect = true
        };

        public static Posnet3DSecureInitiateResponseDto FailureResponse(
            string errorMessage, 
            string? errorCode = null) => new()
        {
            Success = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode,
            RequiresRedirect = false
        };
    }

    /// <summary>
    /// 3D Secure HTML form verileri
    /// Frontend bu verileri hidden form ile POST eder
    /// </summary>
    public class Posnet3DSecureFormData
    {
        /// <summary>
        /// Form action URL (Banka 3D Secure sayfası)
        /// </summary>
        public string ActionUrl { get; set; } = string.Empty;

        /// <summary>
        /// HTTP method (POST)
        /// </summary>
        public string Method { get; set; } = "POST";

        /// <summary>
        /// Üye işyeri numarası
        /// </summary>
        public string MerchantId { get; set; } = string.Empty;

        /// <summary>
        /// POSNET numarası
        /// </summary>
        public string PosnetId { get; set; } = string.Empty;

        /// <summary>
        /// İşlem referans numarası
        /// </summary>
        public string Xid { get; set; } = string.Empty;

        /// <summary>
        /// İşlem tutarı (YKr cinsinden)
        /// </summary>
        public string Amount { get; set; } = string.Empty;

        /// <summary>
        /// Para birimi kodu (TL, US, EU)
        /// </summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Taksit sayısı
        /// </summary>
        public string InstallmentCount { get; set; } = string.Empty;

        /// <summary>
        /// Kart numarası (şifreli)
        /// </summary>
        public string CardNumber { get; set; } = string.Empty;

        /// <summary>
        /// Son kullanma tarihi (YYMM formatı)
        /// </summary>
        public string ExpireDate { get; set; } = string.Empty;

        /// <summary>
        /// CVV
        /// </summary>
        public string Cvv { get; set; } = string.Empty;

        /// <summary>
        /// Kart sahibi adı
        /// </summary>
        public string CardHolderName { get; set; } = string.Empty;

        /// <summary>
        /// İşlem tipi (Sale, Auth vs.)
        /// </summary>
        public string TranType { get; set; } = string.Empty;

        /// <summary>
        /// MAC değeri
        /// </summary>
        public string Mac { get; set; } = string.Empty;

        /// <summary>
        /// Dönüş URL (callback)
        /// </summary>
        public string ReturnUrl { get; set; } = string.Empty;

        /// <summary>
        /// Ek işyeri verileri (OrderId vs.)
        /// </summary>
        public string MerchantData { get; set; } = string.Empty;

        /// <summary>
        /// OpenNewWindow parametresi
        /// </summary>
        public string OpenNewWindow { get; set; } = "0";

        // ═══════════════════════════════════════════════════════════════════════
        // OOS RESPONSE VERİLERİ
        // Bu veriler POSNET oosRequestData yanıtından gelir ve banka formuna POST edilir
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Şifreli veri 1 - POSNET OOS yanıtından gelir
        /// Form field name: "posnetData" veya "data1"
        /// </summary>
        public string? Data1 { get; set; }

        /// <summary>
        /// Şifreli veri 2 - POSNET OOS yanıtından gelir
        /// Form field name: "posnetData2" veya "data2"
        /// </summary>
        public string? Data2 { get; set; }

        /// <summary>
        /// İmza - POSNET OOS yanıtından gelir (MAC veya Sign)
        /// Form field name: "sign"
        /// </summary>
        public string? Sign { get; set; }

        // ═══════════════════════════════════════════════════════════════════════
        // VFT VE JOKER VADAA KAMPANYA DESTEĞİ
        // POSNET Dokümanı: Sayfa 18-22 - Opsiyonel kampanya form parametreleri
        // Bu alanlar HTML form'a opsiyonel olarak eklenir
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// VFT (Vade Farklı Taksit) Kampanya Kodu
        /// Banka ile anlaşmalı vade farklı taksit kampanyası için kullanılır.
        /// Boş/null ise form'a eklenmez.
        /// 
        /// Form field name: "vftCode"
        /// </summary>
        public string? VftCode { get; set; }

        /// <summary>
        /// VFT Mağaza Kodu (opsiyonel)
        /// Bazı VFT kampanyalarında mağaza bazlı ayrım için kullanılır.
        /// 
        /// Form field name: "vftDealerCode"
        /// </summary>
        public string? VftDealerCode { get; set; }

        /// <summary>
        /// Joker Vadaa kullanım flag'i
        /// "1" = Joker Vadaa kampanyası sorgulanacak
        /// "0" veya null = Sorgulanmayacak (varsayılan)
        /// 
        /// POSNET Dokümanı: Sayfa 20
        /// Joker Vadaa, kart sahibine özel kişiselleştirilmiş taksit kampanyalarıdır.
        /// 
        /// Form field name: "useJokerVadaa"
        /// </summary>
        public string? UseJokerVadaa { get; set; }

        /// <summary>
        /// Joker Vadaa Kampanya ID (opsiyonel)
        /// Belirli bir kampanya seçilmişse bu alanda kampanya ID'si gönderilir.
        /// Null ise en uygun kampanya banka tarafından otomatik seçilir.
        /// 
        /// Form field name: "jokerVadaaCampaignId"
        /// </summary>
        public string? JokerVadaaCampaignId { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // 3D SECURE CALLBACK DTO'LARI (Bankadan POST ile gelir)
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 3D Secure callback isteği - Bankadan gelen POST verileri
    /// [FromForm] ile bind edilir
    /// </summary>
    public class Posnet3DSecureCallbackRequestDto
    {
        // ═══════════════════════════════════════════════════════════════
        // OOS ZORUNLU PARAMETRELER
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Banka tarafından şifrelenmiş veriler
        /// </summary>
        [FromForm(Name = "BankPacket")]
        public string? BankPacket { get; set; }

        /// <summary>
        /// Alternatif isim: BankData
        /// </summary>
        [FromForm(Name = "BankData")]
        public string? BankData { get; set; }

        /// <summary>
        /// İşyeri verileri (geri dönen)
        /// </summary>
        [FromForm(Name = "MerchantPacket")]
        public string? MerchantPacket { get; set; }

        /// <summary>
        /// Alternatif isim: MerchantData
        /// </summary>
        [FromForm(Name = "MerchantData")]
        public string? MerchantData { get; set; }

        /// <summary>
        /// Dijital imza
        /// </summary>
        [FromForm(Name = "Sign")]
        public string? Sign { get; set; }

        /// <summary>
        /// MAC değeri
        /// </summary>
        [FromForm(Name = "Mac")]
        public string? Mac { get; set; }

        // ═══════════════════════════════════════════════════════════════
        // 3D SECURE DURUM BİLGİLERİ
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 3D Secure doğrulama durumu (en kritik alan!)
        /// </summary>
        [FromForm(Name = "MdStatus")]
        public string? MdStatus { get; set; }

        /// <summary>
        /// 3D Secure hata mesajı
        /// </summary>
        [FromForm(Name = "MdErrorMessage")]
        public string? MdErrorMessage { get; set; }

        /// <summary>
        /// ErrMsg alternatif
        /// </summary>
        [FromForm(Name = "ErrMsg")]
        public string? ErrMsg { get; set; }

        /// <summary>
        /// İşlem referans numarası
        /// </summary>
        [FromForm(Name = "Xid")]
        public string? Xid { get; set; }

        /// <summary>
        /// ECI değeri
        /// </summary>
        [FromForm(Name = "Eci")]
        public string? Eci { get; set; }

        /// <summary>
        /// CAVV değeri
        /// </summary>
        [FromForm(Name = "Cavv")]
        public string? Cavv { get; set; }

        // ═══════════════════════════════════════════════════════════════
        // EK PARAMETRELER
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// İşlem tutarı
        /// </summary>
        [FromForm(Name = "Amount")]
        public string? Amount { get; set; }

        /// <summary>
        /// Para birimi
        /// </summary>
        [FromForm(Name = "Currency")]
        public string? Currency { get; set; }

        /// <summary>
        /// Taksit sayısı
        /// </summary>
        [FromForm(Name = "InstallmentCount")]
        public string? InstallmentCount { get; set; }

        /// <summary>
        /// POSNET ID
        /// </summary>
        [FromForm(Name = "PosnetId")]
        public string? PosnetId { get; set; }

        /// <summary>
        /// Effective BankData (BankPacket veya BankData hangisi doluysa)
        /// </summary>
        public string? EffectiveBankData => !string.IsNullOrEmpty(BankPacket) ? BankPacket : BankData;

        /// <summary>
        /// Effective MerchantData (MerchantPacket veya MerchantData hangisi doluysa)
        /// </summary>
        public string? EffectiveMerchantData => !string.IsNullOrEmpty(MerchantPacket) ? MerchantPacket : MerchantData;

        /// <summary>
        /// Effective ErrorMessage (MdErrorMessage veya ErrMsg hangisi doluysa)
        /// </summary>
        public string? EffectiveErrorMessage => !string.IsNullOrEmpty(MdErrorMessage) ? MdErrorMessage : ErrMsg;
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // 3D SECURE SONUÇ DTO'LARI
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 3D Secure işlem sonucu - Callback işleme sonrası oluşturulur
    /// </summary>
    public class Posnet3DSecureResultDto
    {
        /// <summary>
        /// İşlem başarılı mı? (MAC doğru + MdStatus uygun + Ödeme tamamlandı)
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Sonuç mesajı
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Hata kodu (başarısız ise)
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Sipariş ID
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>
        /// İşlem referans numarası
        /// </summary>
        public string? TransactionId { get; set; }

        /// <summary>
        /// Banka referans numarası (HostLogKey)
        /// </summary>
        public string? BankReferenceId { get; set; }

        /// <summary>
        /// Onay kodu (AuthCode)
        /// </summary>
        public string? AuthorizationCode { get; set; }

        /// <summary>
        /// Ödeme tutarı
        /// </summary>
        public decimal? Amount { get; set; }

        /// <summary>
        /// 3D Secure doğrulama durumu
        /// </summary>
        public string? MdStatus { get; set; }

        /// <summary>
        /// 3D Secure durum açıklaması
        /// </summary>
        public string? MdStatusDescription { get; set; }

        /// <summary>
        /// Tam doğrulama yapıldı mı? (MdStatus = 1)
        /// </summary>
        public bool IsFullyAuthenticated { get; set; }

        /// <summary>
        /// Yönlendirme URL'i (Frontend bu URL'e yönlendirecek)
        /// </summary>
        public string? RedirectUrl { get; set; }

        /// <summary>
        /// İşlem zamanı
        /// </summary>
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        public static Posnet3DSecureResultDto SuccessResult(
            int orderId,
            string transactionId,
            string? bankReferenceId,
            string? authCode,
            decimal amount,
            string mdStatus,
            string redirectUrl) => new()
        {
            Success = true,
            Message = "Ödeme başarıyla tamamlandı",
            OrderId = orderId,
            TransactionId = transactionId,
            BankReferenceId = bankReferenceId,
            AuthorizationCode = authCode,
            Amount = amount,
            MdStatus = mdStatus,
            MdStatusDescription = GetMdStatusDescription(mdStatus),
            IsFullyAuthenticated = mdStatus == "1",
            RedirectUrl = redirectUrl
        };

        public static Posnet3DSecureResultDto FailureResult(
            string errorMessage,
            string? errorCode = null,
            int? orderId = null,
            string? mdStatus = null) => new()
        {
            Success = false,
            Message = errorMessage,
            ErrorCode = errorCode,
            OrderId = orderId,
            MdStatus = mdStatus,
            MdStatusDescription = mdStatus != null ? GetMdStatusDescription(mdStatus) : null,
            IsFullyAuthenticated = false
        };

        private static string GetMdStatusDescription(string mdStatus)
        {
            return mdStatus switch
            {
                "1" => "Tam Doğrulama Başarılı",
                "2" => "Kart/Banka Sisteme Kayıtlı Değil",
                "3" => "Banka Sisteme Kayıtlı Değil",
                "4" => "Doğrulama Denemesi",
                "5" => "Doğrulama Yapılamadı",
                "6" => "3D Secure Hatası",
                "7" => "Sistem Hatası",
                "8" => "Bilinmeyen Kart",
                "9" => "İşyeri Kayıtlı Değil",
                "0" => "Şifre Hatalı",
                _ => $"Bilinmeyen Durum ({mdStatus})"
            };
        }
    }

    /// <summary>
    /// 3D Secure sonuç sayfası için view model
    /// </summary>
    public class Posnet3DSecurePageViewModel
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Sipariş ID
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>
        /// Görüntülenecek mesaj
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Detaylı açıklama
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Yönlendirme URL'i (JavaScript ile yönlendirme için)
        /// </summary>
        public string? RedirectUrl { get; set; }

        /// <summary>
        /// Otomatik yönlendirme süresi (saniye)
        /// </summary>
        public int RedirectDelaySeconds { get; set; } = 3;

        /// <summary>
        /// İşlem referans numarası (müşteriye gösterilecek)
        /// </summary>
        public string? TransactionReference { get; set; }

        /// <summary>
        /// Ödeme tutarı (formatlanmış, örn: "150,50 TL")
        /// </summary>
        public string? FormattedAmount { get; set; }
    }
}
