// ═══════════════════════════════════════════════════════════════════════════════════════════════
// YAPI KREDİ POSNET PAYMENT SERVICE
// Yapı Kredi Bankası POSNET XML API entegrasyonu ana servis sınıfı
// Dokümantasyon: POSNET XML Servisleri Entegrasyon Dokümanı v2.1.1.3
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// NEDEN BU YAPIYI SEÇTİK?
// 1. IPaymentService implementasyonu - Mevcut ödeme altyapısı ile uyumlu
// 2. Interface segregation - POSNET özel metodlar ayrı interface'de
// 3. Comprehensive logging - Her işlem detaylı loglanır
// 4. Transaction safety - DB işlemleri atomic
// 5. Retry-ready - Transient hatalar için yeniden denenebilir yapı
// ═══════════════════════════════════════════════════════════════════════════════════════════════

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Payment;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using ECommerce.Infrastructure.Config;
using ECommerce.Infrastructure.Services.Payment.Posnet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services.Payment.Posnet
{
    /// <summary>
    /// POSNET özel metodları için interface
    /// Standart IPaymentService dışındaki POSNET işlemleri
    /// </summary>
    public interface IPosnetPaymentService : IPaymentService
    {
        /// <summary>Direkt satış işlemi (2D - 3D Secure olmadan)</summary>
        Task<PosnetResult<PosnetSaleResponse>> ProcessDirectSaleAsync(
            int orderId, 
            string cardNumber, string expireDate, string cvv,
            int installment = 0,
            CancellationToken cancellationToken = default);

        /// <summary>3D Secure satış başlatma</summary>
        Task<PosnetResult<PosnetOosResponse>> Initiate3DSecureAsync(
            int orderId,
            string cardNumber, string expireDate, string cvv,
            decimal? amount = null,
            string txnType = "Sale",
            int installment = 0,
            CancellationToken cancellationToken = default);

        /// <summary>3D Secure callback sonrası satışı tamamla</summary>
        Task<PosnetResult<PosnetSaleResponse>> Complete3DSecureSaleAsync(
            string orderId,
            PosnetOosCallbackData callbackData,
            CancellationToken cancellationToken = default);

        /// <summary>Provizyon (ön yetkilendirme) işlemi</summary>
        Task<PosnetResult<PosnetAuthResponse>> ProcessAuthAsync(
            int orderId,
            string cardNumber, string expireDate, string cvv,
            decimal? amount = null,
            int installment = 0,
            CancellationToken cancellationToken = default);

        /// <summary>Finansallaştırma (provizyon çekme)</summary>
        Task<PosnetResult<PosnetCaptResponse>> ProcessCaptureAsync(
            int orderId,
            string hostLogKey,
            decimal? amount = null,
            CancellationToken cancellationToken = default);

        /// <summary>İptal işlemi (gün içi)</summary>
        Task<PosnetResult<PosnetReverseResponse>> ProcessReverseAsync(
            int orderId,
            string hostLogKey,
            CancellationToken cancellationToken = default);

        /// <summary>İade işlemi (gün sonu sonrası)</summary>
        Task<PosnetResult<PosnetReturnResponse>> ProcessRefundAsync(
            int orderId,
            string hostLogKey,
            decimal amount,
            CancellationToken cancellationToken = default);

        /// <summary>Puan sorgulama</summary>
        Task<PosnetResult<PosnetPointInquiryResponse>> QueryPointsAsync(
            string cardNumber, string expireDate, string cvv,
            CancellationToken cancellationToken = default);

        /// <summary>İşlem durumu sorgulama</summary>
        Task<PosnetResult<PosnetAgreementResponse>> QueryTransactionAsync(
            string orderId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 3D Secure callback verilerini çözer ve doğrular (oosResolveMerchantData)
        /// 
        /// KULLANIM:
        /// 1. Banka callback'inden dönen merchantData, bankData, sign değerlerini alır
        /// 2. Bankaya oosResolveMerchantData servisi ile sorgu yapar
        /// 3. Dönen xid, amount, currency değerlerini orijinal değerlerle karşılaştırır
        /// 4. MAC doğrulaması yapar
        /// 
        /// GÜVENLİK:
        /// Bu metod çağrılmadan finansallaştırma yapılmamalı!
        /// Dokümantasyon: POSNET 3D Secure Entegrasyon Dokümanı - Sayfa 12-14
        /// </summary>
        /// <param name="request">Callback'den gelen veriler ve orijinal işlem bilgileri</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>Deşifre edilmiş işlem verileri ve doğrulama sonucu</returns>
        Task<PosnetResult<PosnetOosResolveMerchantDataResponse>> ResolveOosMerchantDataAsync(
            PosnetOosResolveMerchantDataRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 3D Secure işlemini finansallaştırır (oosTranData)
        /// 
        /// KULLANIM:
        /// 1. ResolveOosMerchantDataAsync başarılı olduktan sonra çağrılır
        /// 2. MAC doğrulaması geçtikten sonra çağrılır
        /// 3. Müşteri hesabından para bu adımda çekilir
        /// 
        /// ÖNEMLİ:
        /// - ResolveOosMerchantDataAsync çağrılmadan bu metod çağrılmamalı!
        /// - MdStatus kontrolü yapılmadan bu metod çağrılmamalı!
        /// Dokümantasyon: POSNET 3D Secure Entegrasyon Dokümanı - Sayfa 15-17
        /// </summary>
        /// <param name="request">Finansallaştırma için gerekli veriler</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>Finansallaştırma sonucu (HostLogKey, AuthCode vb.)</returns>
        Task<PosnetResult<PosnetOosTranDataResponse>> ProcessOosTranDataAsync(
            PosnetOosTranDataRequest request,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Yapı Kredi POSNET Payment Service implementasyonu
    /// Tüm POSNET işlem tiplerini destekler
    /// </summary>
    public class YapiKrediPosnetService : IPosnetPaymentService
    {
        // ═══════════════════════════════════════════════════════════════════════
        // DEPENDENCIES
        // ═══════════════════════════════════════════════════════════════════════

        private readonly PaymentSettings _settings;
        private readonly ECommerceDbContext _db;
        private readonly IPosnetXmlBuilder _xmlBuilder;
        private readonly IPosnetXmlParser _xmlParser;
        private readonly IPosnetHttpClient _httpClient;
        private readonly ILogger<YapiKrediPosnetService> _logger;

        // ═══════════════════════════════════════════════════════════════════════
        // CONSTANTS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Provider adı - DB ve routing için</summary>
        public const string PROVIDER_NAME = "YapiKredi_POSNET";

        /// <summary>OrderID prefix - POSNET'te unique olması için</summary>
        private const string ORDER_ID_PREFIX = "YKB";

        private static string NormalizeCurrencyCode(string? currency)
        {
            var normalized = currency?.Trim().ToUpperInvariant();
            return normalized switch
            {
                "TRY" or "TL" or null => "TL",
                "USD" or "US" => "US",
                "EUR" or "EU" => "EU",
                _ => "TL"
            };
        }

        private static bool IsPosnetOrderId(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.StartsWith(ORDER_ID_PREFIX, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<string> GetOrCreatePosnetOrderIdAsync(Order order, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(order.PosnetTransactionId))
            {
                return order.PosnetTransactionId;
            }

            order.PosnetTransactionId = GeneratePosnetOrderId(order.Id);
            await _db.SaveChangesAsync(cancellationToken);
            return order.PosnetTransactionId;
        }

        private async Task<string?> ResolveOriginalPosnetOrderIdAsync(int orderId, CancellationToken cancellationToken)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(order?.PosnetTransactionId))
            {
                return order.PosnetTransactionId;
            }

            return await _db.Payments
                .Where(p => p.OrderId == orderId &&
                    (p.TransactionType == "auth" || p.TransactionType == "sale" || p.TransactionType == "capt") &&
                    (!string.IsNullOrWhiteSpace(p.TransactionId) || IsPosnetOrderId(p.ProviderPaymentId)))
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => !string.IsNullOrWhiteSpace(p.TransactionId) ? p.TransactionId : p.ProviderPaymentId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<int?> ResolveOrderIdByPosnetOrderIdAsync(string? posnetOrderId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(posnetOrderId))
            {
                return null;
            }

            var orderId = await _db.Orders
                .AsNoTracking()
                .Where(o => o.PosnetTransactionId == posnetOrderId)
                .Select(o => (int?)o.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (orderId.HasValue)
            {
                return orderId;
            }

            return await _db.Payments
                .AsNoTracking()
                .Where(p => p.TransactionId == posnetOrderId || p.ProviderPaymentId == posnetOrderId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => (int?)p.OrderId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public YapiKrediPosnetService(
            IOptions<PaymentSettings> settings,
            ECommerceDbContext db,
            IPosnetXmlBuilder xmlBuilder,
            IPosnetXmlParser xmlParser,
            IPosnetHttpClient httpClient,
            ILogger<YapiKrediPosnetService> logger)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _xmlBuilder = xmlBuilder ?? throw new ArgumentNullException(nameof(xmlBuilder));
            _xmlParser = xmlParser ?? throw new ArgumentNullException(nameof(xmlParser));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ValidateConfiguration();
        }

        /// <summary>
        /// Konfigürasyon değerlerini doğrular
        /// Eksik veya hatalı değerler için erken uyarı
        /// </summary>
        private void ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(_settings.PosnetMerchantId))
            {
                _logger.LogWarning("[POSNET] MerchantId yapılandırılmamış!");
            }

            if (string.IsNullOrEmpty(_settings.PosnetTerminalId))
            {
                _logger.LogWarning("[POSNET] TerminalId yapılandırılmamış!");
            }

            if (string.IsNullOrEmpty(_settings.PosnetXmlServiceUrl))
            {
                _logger.LogWarning("[POSNET] XML Service URL yapılandırılmamış!");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // IPaymentService IMPLEMENTATION
        // Mevcut ödeme altyapısı ile uyumluluk
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Standart ödeme işlemi
        /// NOT: POSNET için kart bilgileri gerektiğinden bu metod sınırlı kullanılabilir
        /// 3D Secure akışı için InitiateAsync kullanılmalı
        /// </summary>
        public virtual Task<bool> ProcessPaymentAsync(int orderId, decimal amount)
        {
            _logger.LogWarning(
                "[POSNET] ProcessPaymentAsync çağrıldı ancak kart bilgileri olmadan işlem yapılamaz. " +
                "3D Secure için InitiateAsync kullanın. OrderId: {OrderId}", orderId);

            // POSNET'te kart bilgileri olmadan işlem yapılamaz
            // Bu metod sadece interface uyumluluğu için
            return Task.FromResult(false);
        }

        /// <summary>
        /// Ödeme durumu kontrolü
        /// ProviderPaymentId = POSNET HostLogKey
        /// </summary>
        public virtual async Task<bool> CheckPaymentStatusAsync(string paymentId)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
            {
                return false;
            }

            try
            {
                // Veritabanından ödeme kaydını kontrol et
                var payment = await _db.Payments
                    .FirstOrDefaultAsync(p => 
                        p.ProviderPaymentId == paymentId && 
                        p.Provider == PROVIDER_NAME);

                if (payment == null)
                {
                    _logger.LogDebug("[POSNET] Ödeme kaydı bulunamadı. PaymentId: {PaymentId}", paymentId);
                    return false;
                }

                return payment.Status == "Success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[POSNET] Ödeme durumu kontrolü hatası. PaymentId: {PaymentId}", paymentId);
                return false;
            }
        }

        /// <summary>
        /// Toplam ödeme sayısı
        /// </summary>
        public virtual Task<int> GetPaymentCountAsync()
        {
            return _db.Payments
                .Where(p => p.Provider == PROVIDER_NAME)
                .CountAsync();
        }

        /// <summary>
        /// Detaylı ödeme işlemi - PaymentStatus enum döner
        /// </summary>
        public virtual async Task<PaymentStatus> ProcessPaymentDetailedAsync(int orderId, decimal amount)
        {
            var success = await ProcessPaymentAsync(orderId, amount);
            return success ? PaymentStatus.Paid : PaymentStatus.Failed;
        }

        /// <summary>
        /// Ödeme durumu sorgulama - PaymentStatus enum döner
        /// </summary>
        public virtual async Task<PaymentStatus> GetPaymentStatusAsync(string paymentId)
        {
            var success = await CheckPaymentStatusAsync(paymentId);
            return success ? PaymentStatus.Paid : PaymentStatus.Failed;
        }

        /// <summary>
        /// 3D Secure başlatma / Hosted checkout
        /// Müşteriyi banka 3D Secure sayfasına yönlendirmek için kullanılır
        /// </summary>
        public virtual async Task<PaymentInitResult> InitiateAsync(int orderId, decimal amount, string currency)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Sipariş bilgilerini al
                var order = await _db.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return new PaymentInitResult
                    {
                        Success = false,
                        ErrorMessage = "Sipariş bulunamadı",
                        Provider = PROVIDER_NAME,
                        OrderId = orderId
                    };
                }

                // POSNET için 3D Secure OOS talebi oluşturmamız gerekiyor
                // Ancak kart bilgileri frontend'den gelecek, bu noktada sadece hazırlık yapıyoruz
                _logger.LogInformation(
                    "[POSNET] 3D Secure başlatma isteği. OrderId: {OrderId}, Amount: {Amount}",
                    orderId, amount);

                // 3D Secure URL ve gerekli bilgileri döndür
                // Frontend bu bilgileri kullanarak kart formunu gösterecek
                return new PaymentInitResult
                {
                    Success = true,
                    Provider = PROVIDER_NAME,
                    OrderId = orderId,
                    Amount = amount,
                    Currency = currency ?? "TRY",
                    RequiresRedirect = true,
                    // 3D Secure callback URL
                    RedirectUrl = _settings.PosnetCallbackUrl,
                    // Token olarak benzersiz sipariş ID'si
                    Token = GeneratePosnetOrderId(orderId),
                    // Checkout URL (frontend form gösterecek)
                    CheckoutUrl = $"/checkout/payment?orderId={orderId}&provider=posnet"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, 
                    "[POSNET] InitiateAsync hatası. OrderId: {OrderId}, ElapsedMs: {ElapsedMs}",
                    orderId, stopwatch.ElapsedMilliseconds);

                return new PaymentInitResult
                {
                    Success = false,
                    ErrorMessage = $"Ödeme başlatma hatası: {ex.Message}",
                    Provider = PROVIDER_NAME,
                    OrderId = orderId
                };
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // POSNET SPECIFIC METHODS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Direkt satış işlemi (2D - 3D Secure olmadan)
        /// DİKKAT: 3D Secure olmadan işlem yapılması önerilmez
        /// </summary>
        public virtual async Task<PosnetResult<PosnetSaleResponse>> ProcessDirectSaleAsync(
            int orderId,
            string cardNumber, string expireDate, string cvv,
            int installment = 0,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation(
                "[POSNET] Direkt satış başlatılıyor. OrderId: {OrderId}, Installment: {Installment}",
                orderId, installment);

            try
            {
                // Sipariş bilgilerini al
                var order = await _db.Orders.FindAsync(new object[] { orderId }, cancellationToken);
                if (order == null)
                {
                    return PosnetResult<PosnetSaleResponse>.Failure(
                        "Sipariş bulunamadı",
                        PosnetErrorCode.InvalidOrderId);
                }

                var posnetOrderId = await GetOrCreatePosnetOrderIdAsync(order, cancellationToken);

                // Sale request oluştur
                var request = PosnetSaleRequest.Create(
                    _settings.PosnetMerchantId,
                    _settings.PosnetTerminalId,
                    cardNumber,
                    expireDate,
                    cvv,
                    posnetOrderId,
                    order.TotalPrice,
                    installment);

                // XML oluştur
                var xml = _xmlBuilder.BuildSaleXml(request);

                // POSNET API'ye gönder
                var httpResponse = await _httpClient.SendAsync(xml, cancellationToken);

                if (!httpResponse.IsSuccess)
                {
                    _logger.LogWarning(
                        "[POSNET] HTTP hatası. OrderId: {OrderId}, Error: {Error}",
                        orderId, httpResponse.ErrorMessage);

                    return PosnetResult<PosnetSaleResponse>.Failure(
                        httpResponse.ErrorMessage ?? "HTTP hatası",
                        PosnetErrorCode.ConnectionError,
                        httpResponse.Exception,
                        stopwatch.ElapsedMilliseconds);
                }

                // Response parse et
                var saleResponse = _xmlParser.ParseSaleResponse(httpResponse.ResponseXml!);

                stopwatch.Stop();

                // Ödeme kaydı oluştur
                await SavePaymentRecordAsync(
                    orderId,
                    saleResponse.HostLogKey ?? posnetOrderId,
                    order.TotalPrice,
                    saleResponse.IsSuccess ? "Paid" : "Failed",
                    httpResponse.ResponseXml,
                    cancellationToken,
                    hostLogKey: saleResponse.HostLogKey,
                    authCode: saleResponse.AuthCode,
                    transactionType: "sale",
                    transactionId: posnetOrderId);

                if (saleResponse.IsSuccess)
                {
                    _logger.LogInformation(
                        "[POSNET] Satış başarılı. OrderId: {OrderId}, HostLogKey: {HostLogKey}, AuthCode: {AuthCode}, ElapsedMs: {ElapsedMs}",
                        orderId, saleResponse.HostLogKey, saleResponse.AuthCode, stopwatch.ElapsedMilliseconds);

                    // Sipariş durumunu güncelle
                    await UpdateOrderPaymentStatusAsync(orderId, PaymentStatus.Paid, cancellationToken);

                    // MADDE 9: Satış'tan dönen HostLogKey'i Order üzerine de kaydet
                    try
                    {
                        var saleOrder = await _db.Orders.FindAsync(new object[] { orderId }, cancellationToken);
                        if (saleOrder != null && !string.IsNullOrWhiteSpace(saleResponse.HostLogKey))
                        {
                            saleOrder.PosnetTransactionId = posnetOrderId;
                            // Direkt satışta PreAuthHostLogKey = Sale HostLogKey (iade için kullanılır)
                            saleOrder.PreAuthHostLogKey = saleResponse.HostLogKey;
                            await _db.SaveChangesAsync(cancellationToken);
                        }
                    }
                    catch (Exception saleOrderEx)
                    {
                        _logger.LogError(saleOrderEx,
                            "[POSNET] Sale sonrası Order.PreAuthHostLogKey kaydedilemedi. OrderId={OrderId}",
                            orderId);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "[POSNET] Satış reddedildi. OrderId: {OrderId}, ErrorCode: {ErrorCode}, ErrorMessage: {ErrorMessage}",
                        orderId, saleResponse.ErrorCode, saleResponse.ErrorMessage);
                }

                return PosnetResult<PosnetSaleResponse>.FromResponse(saleResponse, stopwatch.ElapsedMilliseconds);
            }
            catch (PosnetValidationException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "[POSNET] Validasyon hatası. OrderId: {OrderId}", orderId);

                return PosnetResult<PosnetSaleResponse>.Failure(
                    ex.Message,
                    PosnetErrorCode.InvalidParameterValue,
                    ex,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[POSNET] Satış işlemi hatası. OrderId: {OrderId}", orderId);

                return PosnetResult<PosnetSaleResponse>.Failure(
                    $"Beklenmeyen hata: {ex.Message}",
                    PosnetErrorCode.SystemError,
                    ex,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// 3D Secure satış başlatma
        /// OOS request oluşturur ve banka yönlendirme verilerini döner
        /// </summary>
        public virtual async Task<PosnetResult<PosnetOosResponse>> Initiate3DSecureAsync(
            int orderId,
            string cardNumber, string expireDate, string cvv,
            decimal? amount = null,
            string txnType = "Sale",
            int installment = 0,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation(
                "[POSNET] 3D Secure başlatılıyor. OrderId: {OrderId}",
                orderId);

            try
            {
                // Sipariş bilgilerini al
                var order = await _db.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

                if (order == null)
                {
                    return PosnetResult<PosnetOosResponse>.Failure(
                        "Sipariş bulunamadı",
                        PosnetErrorCode.InvalidOrderId);
                }

                var posnetOrderId = await GetOrCreatePosnetOrderIdAsync(order, cancellationToken);

                // ── KG ÜRÜN FIX: Amount hesaplama mantığı ─────────────────────────────
                // Öncelik sırası:
                // 1. Method parametresi (amount) - Üst katmandan hesaplanmış (PreAuthAmount gibi)
                // 2. KG ürün ise Order.PreAuthAmount (tartı öncesi hesaplanan provizyon tutarı)
                // 3. Normal ürün ise Order.FinalPrice (indirim/kupon uygulanmış)
                // 4. Son çare Order.TotalPrice
                decimal effectiveAmount;
                
                if (amount.HasValue && amount.Value > 0)
                {
                    // Üst katmandan gelen tutar (KG ürünlerde PreAuthAmount olabilir)
                    effectiveAmount = amount.Value;
                    _logger.LogInformation(
                        "[POSNET-3DS] Parametre amount kullanılıyor: {Amount} TL, OrderId: {OrderId}",
                        effectiveAmount, orderId);
                }
                else if (order.HasWeightBasedItems && order.PreAuthAmount > 0)
                {
                    // KG ürün VE PreAuthAmount hesaplanmış
                    effectiveAmount = order.PreAuthAmount;
                    _logger.LogInformation(
                        "[POSNET-3DS] KG ürün - PreAuthAmount kullanılıyor: {PreAuthAmount} TL, " +
                        "OrderId: {OrderId} (FinalPrice: {FinalPrice} TL, TotalPrice: {TotalPrice} TL)",
                        effectiveAmount, orderId, order.FinalPrice, order.TotalPrice);
                }
                else
                {
                    // Normal ürün - FinalPrice veya TotalPrice
                    effectiveAmount = order.FinalPrice > 0 ? order.FinalPrice : order.TotalPrice;
                    _logger.LogInformation(
                        "[POSNET-3DS] Normal ürün - FinalPrice/TotalPrice kullanılıyor: {Amount} TL, " +
                        "OrderId: {OrderId}",
                        effectiveAmount, orderId);
                }

                // OOS request oluştur
                var request = new PosnetOosRequest
                {
                    MerchantId = _settings.PosnetMerchantId,
                    TerminalId = _settings.PosnetTerminalId,
                    PosnetId = _settings.PosnetId,
                    Card = new PosnetCardInfo
                    {
                        CardNumber = cardNumber.Replace(" ", "").Replace("-", ""),
                        ExpireDate = expireDate,
                        Cvv = cvv,
                        CardHolderName = order.User?.FullName ?? order.CustomerName ?? "GUEST USER"
                    },
                    OrderId = posnetOrderId,
                    Amount = PosnetSaleRequest.ConvertToKurus(effectiveAmount),
                    Installment = installment.ToString("D2"),
                    CurrencyCode = NormalizeCurrencyCode(order.Currency),
                    TxnType = string.IsNullOrWhiteSpace(txnType) ? "Sale" : txnType.Trim(),
                    ReturnUrl = _settings.PosnetCallbackUrl
                };

                // Validasyon
                PosnetRequestValidator.ValidateAndThrow(request);

                // XML oluştur
                var xml = _xmlBuilder.BuildOosRequestXml(request);

                // DEBUG: XML request'i detaylı logla
                _logger.LogWarning("[POSNET-OOS] Request XML:\n{Xml}", xml);
                _logger.LogWarning("[POSNET-OOS] MID: {Mid}, TID: {Tid}, Amount: {Amount}, OrderId: {OrderId}", 
                    request.MerchantId, request.TerminalId, request.Amount, request.OrderId);

                // OOS şifreleme isteği XML servisine gönderilir (3D endpoint'e değil)
                var httpResponse = await _httpClient.SendAsync(xml, cancellationToken);

                // DEBUG: Response XML'i detaylı logla
                if (httpResponse.ResponseXml != null)
                {
                    _logger.LogWarning("[POSNET-OOS] Response XML:\n{Xml}", httpResponse.ResponseXml);
                }

                if (!httpResponse.IsSuccess)
                {
                    return PosnetResult<PosnetOosResponse>.Failure(
                        httpResponse.ErrorMessage ?? "HTTP hatası",
                        PosnetErrorCode.ConnectionError,
                        httpResponse.Exception,
                        stopwatch.ElapsedMilliseconds);
                }

                // Response parse et
                var oosResponse = _xmlParser.ParseOosResponse(httpResponse.ResponseXml!);

                // POSNET OOS response'unda XID dönmediği için manuel set ediyoruz
                // XID bizim tarafımızdan generate edildi ve request'te gönderildi
                oosResponse = oosResponse with { OrderId = posnetOrderId };

                stopwatch.Stop();

                // Pending ödeme kaydı oluştur
                await SavePaymentRecordAsync(
                    orderId,
                    posnetOrderId,
                    amount ?? order.TotalPrice,
                    "Pending",
                    httpResponse.ResponseXml,
                    cancellationToken,
                    transactionType: request.TxnType,
                    authorizedAmount: request.TxnType.Equals("Auth", StringComparison.OrdinalIgnoreCase)
                        ? amount ?? order.TotalPrice
                        : null,
                    transactionId: posnetOrderId);

                if (oosResponse.IsSuccess && oosResponse.RequiresRedirect)
                {
                    _logger.LogInformation(
                        "[POSNET] 3D Secure yönlendirme hazır. OrderId: {OrderId}, PosnetOrderId: {PosnetOrderId}, ElapsedMs: {ElapsedMs}",
                        orderId, posnetOrderId, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogWarning(
                        "[POSNET] 3D Secure başlatma başarısız. OrderId: {OrderId}, ErrorCode: {ErrorCode}",
                        orderId, oosResponse.ErrorCode);
                }

                return PosnetResult<PosnetOosResponse>.FromResponse(oosResponse, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[POSNET] 3D Secure başlatma hatası. OrderId: {OrderId}", orderId);

                return PosnetResult<PosnetOosResponse>.Failure(
                    $"3D Secure hatası: {ex.Message}",
                    PosnetErrorCode.ThreeDSecureError,
                    ex,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// 3D Secure callback sonrası satışı tamamla
        /// Banka callback'inden gelen verilerle işlemi finalize eder
        /// </summary>
        /// <summary>
        /// 3D Secure callback sonrası satışı tamamlar
        /// 
        /// TAM AKIŞ (POSNET Dokümanı v2.1.1.3 - Sayfa 12-17):
        /// 1. MAC doğrulama - Callback verilerinin tutarlılığını kontrol eder
        /// 2. MdStatus kontrolü - 3D doğrulamanın başarılı olup olmadığını kontrol eder
        /// 3. oosResolveMerchantData - Şifreli callback verilerini çözer ve doğrular
        /// 4. oosTranData - Finansallaştırma yapar ve parayı çeker
        /// 
        /// GÜVENLİK NOTU:
        /// Bu metod atomik olarak tüm adımları gerçekleştirir.
        /// Herhangi bir adımda hata oluşursa işlem iptal edilir.
        /// </summary>
        public virtual async Task<PosnetResult<PosnetSaleResponse>> Complete3DSecureSaleAsync(
            string orderId,
            PosnetOosCallbackData callbackData,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString("N")[..12];

            _logger.LogInformation(
                "[POSNET-3DS] {CorrelationId} - 3D Secure tamamlanıyor. OrderId: {OrderId}, MdStatus: {MdStatus}",
                correlationId, orderId, callbackData.MdStatus);

            try
            {
                // ═══════════════════════════════════════════════════════════════
                // ADIM 1: TEMEL DOĞRULAMALAR
                // ═══════════════════════════════════════════════════════════════

                if (callbackData == null)
                {
                    return PosnetResult<PosnetSaleResponse>.Failure(
                        "Callback verisi null olamaz",
                        PosnetErrorCode.InvalidRequest,
                        elapsedMs: stopwatch.ElapsedMilliseconds);
                }

                // 3D doğrulama başarılı mı? (MdStatus = 1)
                if (!callbackData.Is3DVerified)
                {
                    _logger.LogWarning(
                        "[POSNET-3DS] {CorrelationId} - 3D doğrulama başarısız. " +
                        "OrderId: {OrderId}, MdStatus: {MdStatus}, Error: {Error}",
                        correlationId, orderId, callbackData.MdStatus, callbackData.MdErrorMessage);

                    return PosnetResult<PosnetSaleResponse>.Failure(
                        callbackData.MdErrorMessage ?? "3D Secure doğrulaması başarısız",
                        PosnetErrorCode.ThreeDSecureVerificationFailed,
                        elapsedMs: stopwatch.ElapsedMilliseconds);
                }

                // MAC doğrulama - Callback verilerinin banka tarafından imzalandığını doğrula
                if (!ValidateCallbackMac(callbackData))
                {
                    _logger.LogError(
                        "[POSNET-3DS] {CorrelationId} - MAC doğrulama BAŞARISIZ! " +
                        "Olası güvenlik ihlali. OrderId: {OrderId}",
                        correlationId, orderId);

                    return PosnetResult<PosnetSaleResponse>.Failure(
                        "Güvenlik doğrulaması başarısız",
                        PosnetErrorCode.MacValidationFailed,
                        elapsedMs: stopwatch.ElapsedMilliseconds);
                }

                _logger.LogDebug(
                    "[POSNET-3DS] {CorrelationId} - Temel doğrulamalar başarılı",
                    correlationId);

                // ═══════════════════════════════════════════════════════════════
                // ADIM 2: SİPARİŞ BİLGİLERİNİ AL
                // ═══════════════════════════════════════════════════════════════

                if (!int.TryParse(orderId, out var numericOrderId))
                {
                    return PosnetResult<PosnetSaleResponse>.Failure(
                        "Geçersiz sipariş numarası",
                        PosnetErrorCode.InvalidOrderId,
                        elapsedMs: stopwatch.ElapsedMilliseconds);
                }

                var order = await _db.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == numericOrderId, cancellationToken);

                if (order == null)
                {
                    _logger.LogWarning(
                        "[POSNET-3DS] {CorrelationId} - Sipariş bulunamadı. OrderId: {OrderId}",
                        correlationId, orderId);

                    return PosnetResult<PosnetSaleResponse>.Failure(
                        "Sipariş bulunamadı",
                        PosnetErrorCode.InvalidOrderId,
                        elapsedMs: stopwatch.ElapsedMilliseconds);
                }

                var latestPayment = await _db.Payments
                    .Where(p => p.OrderId == numericOrderId &&
                        (p.Provider == PROVIDER_NAME || p.Provider == "YapiKredi"))
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                // NEDEN: 3D başlatılırken bankaya hangi tutar gönderildiyse callback doğrulamasında da
                // aynı tutar kullanılmalı. order.TotalPrice/finalPrice fallback only.
                var expectedAmount = latestPayment?.Amount
                    ?? (order.FinalPrice > 0 ? order.FinalPrice : order.TotalPrice);
                var amountInKurus = PosnetSaleRequest.ConvertToKurus(expectedAmount);

                // ═══════════════════════════════════════════════════════════════
                // ADIM 3: OOS RESOLVE MERCHANT DATA - Şifreli Verileri Çöz
                // POSNET Dokümanı: Sayfa 12-14
                // ═══════════════════════════════════════════════════════════════

                _logger.LogInformation(
                    "[POSNET-3DS] {CorrelationId} - oosResolveMerchantData başlatılıyor...",
                    correlationId);

                // MAC hesapla - POSNET formülü kullanarak (XID/Amount/Currency orijinal değerlerle)
                var xidForMac = !string.IsNullOrWhiteSpace(callbackData.Xid) ? callbackData.Xid : orderId;
                var amountForMac = NormalizeAmountForMac(callbackData.Amount) ?? amountInKurus.ToString();
                var currencyForMac = NormalizeCurrencyForMac(callbackData.Currency) ?? "TL";

                var resolveMac = CalculateMacForResolve(
                    xidForMac,
                    amountForMac,
                    currencyForMac);

                var resolveRequest = new PosnetOosResolveMerchantDataRequest
                {
                    MerchantId = _settings.PosnetMerchantId,
                    TerminalId = _settings.PosnetTerminalId,
                    BankData = callbackData.BankData ?? string.Empty,
                    MerchantData = callbackData.MerchantData ?? string.Empty,
                    Sign = callbackData.Sign ?? string.Empty,
                    Mac = resolveMac,
                    OriginalXid = xidForMac,
                    OriginalAmount = int.TryParse(amountForMac, out var macAmount) ? macAmount : amountInKurus,
                    OriginalCurrency = currencyForMac
                };

                _logger.LogWarning(
                    "[POSNET-RESOLVE] Callback verileri - BankData: {HasBankData}, MerchantData: {HasMerchantData}, Sign: {HasSign}",
                    !string.IsNullOrWhiteSpace(resolveRequest.BankData),
                    !string.IsNullOrWhiteSpace(resolveRequest.MerchantData),
                    !string.IsNullOrWhiteSpace(resolveRequest.Sign));

                var resolveResult = await ResolveOosMerchantDataAsync(resolveRequest, cancellationToken);

                if (!resolveResult.IsSuccess || resolveResult.Data == null)
                {
                    var resolveError = resolveResult.Error ?? "oosResolveMerchantData başarısız";

                    _logger.LogWarning(
                        "[POSNET-3DS] {CorrelationId} - oosResolveMerchantData BAŞARISIZ: {Error}",
                        correlationId, resolveError);

                    // ErrorCode Success değilse kullan, değilse default hata kodu
                    var errorCode = resolveResult.ErrorCode != PosnetErrorCode.Success 
                        ? resolveResult.ErrorCode 
                        : PosnetErrorCode.OosResolveDataFailed;

                    return PosnetResult<PosnetSaleResponse>.Failure(
                        resolveError,
                        errorCode,
                        elapsedMs: stopwatch.ElapsedMilliseconds);
                }

                var resolveResponse = resolveResult.Data;

                // Çözümlenen verilerin tutarlılığını kontrol et
                if (!resolveResponse.CanProceedWithPayment)
                {
                    _logger.LogWarning(
                        "[POSNET-3DS] {CorrelationId} - 3DS doğrulama başarısız (resolve). " +
                        "MdStatus: {MdStatus} - {Description}",
                        correlationId, resolveResponse.MdStatus, resolveResponse.MdStatusDescription);

                    return PosnetResult<PosnetSaleResponse>.Failure(
                        resolveResponse.MdStatusDescription ?? "3D Secure doğrulama başarısız",
                        PosnetErrorCode.ThreeDSecureVerificationFailed,
                        elapsedMs: stopwatch.ElapsedMilliseconds);
                }

                _logger.LogInformation(
                    "[POSNET-3DS] {CorrelationId} - oosResolveMerchantData başarılı. " +
                    "XID: {Xid}, Amount: {Amount}",
                    correlationId, resolveResponse.Xid, resolveResponse.Amount);

                // ═══════════════════════════════════════════════════════════════
                // ADIM 4: OOS TRAN DATA - FİNANSALLAŞTIRMA (Para Çekimi)
                // POSNET Dokümanı: Sayfa 15-17
                // ═══════════════════════════════════════════════════════════════

                _logger.LogInformation(
                    "[POSNET-3DS] {CorrelationId} - oosTranData (Finansallaştırma) başlatılıyor...",
                    correlationId);

                // Finansallaştırma için MAC hesapla
                var tranDataMac = CalculateMacForTranData(
                    resolveResponse.Xid ?? orderId,
                    resolveResponse.Amount.ToString(),
                    resolveResponse.Currency ?? "TL");

                var tranDataRequest = new PosnetOosTranDataRequest
                {
                    MerchantId = _settings.PosnetMerchantId,
                    TerminalId = _settings.PosnetTerminalId,
                    BankData = callbackData.BankData ?? string.Empty,
                    WpAmount = 0, // World Puan kullanılmıyorsa 0
                    Mac = tranDataMac,
                    OrderId = orderId,
                    Amount = resolveResponse.Amount
                };

                var tranDataResult = await ProcessOosTranDataAsync(tranDataRequest, cancellationToken);

                if (!tranDataResult.IsSuccess || tranDataResult.Data == null)
                {
                    var tranDataError = tranDataResult.Error ?? "Finansallaştırma başarısız";

                    _logger.LogWarning(
                        "[POSNET-3DS] {CorrelationId} - oosTranData BAŞARISIZ: {Error}",
                        correlationId, tranDataError);

                    // ErrorCode Success değilse kullan, değilse default hata kodu
                    var errorCode = tranDataResult.ErrorCode != PosnetErrorCode.Success 
                        ? tranDataResult.ErrorCode 
                        : PosnetErrorCode.OosTranDataFailed;

                    return PosnetResult<PosnetSaleResponse>.Failure(
                        tranDataError,
                        errorCode,
                        elapsedMs: stopwatch.ElapsedMilliseconds);
                }

                var tranDataResponse = tranDataResult.Data;

                // ═══════════════════════════════════════════════════════════════
                // ADIM 5: BAŞARI RESPONSE OLUŞTUR
                // ═══════════════════════════════════════════════════════════════

                stopwatch.Stop();

                _logger.LogInformation(
                    "[POSNET-3DS] {CorrelationId} - ✅ 3D Secure BAŞARIYLA TAMAMLANDI! " +
                    "OrderId: {OrderId}, HostLogKey: {HostLogKey}, AuthCode: {AuthCode}, " +
                    "Duration: {Duration}ms",
                    correlationId, orderId, tranDataResponse.HostLogKey,
                    tranDataResponse.AuthCode, stopwatch.ElapsedMilliseconds);

                var response = PosnetSaleResponse.Success(
                    hostLogKey: tranDataResponse.HostLogKey ?? orderId,
                    authCode: tranDataResponse.AuthCode ?? "000000",
                    orderId: orderId,
                    amount: resolveResponse.Amount);

                return PosnetResult<PosnetSaleResponse>.FromResponse(
                    response,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _logger.LogError(ex,
                    "[POSNET-3DS] {CorrelationId} - 3D Secure tamamlama HATASI! " +
                    "OrderId: {OrderId}, Exception: {Message}",
                    correlationId, orderId, ex.Message);

                return PosnetResult<PosnetSaleResponse>.Failure(
                    "3D Secure işlemi tamamlanırken bir hata oluştu",
                    PosnetErrorCode.SystemError,
                    ex,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// oosResolveMerchantData için MAC hesaplar
        /// POSNET API Dokümanı v2.1.1.3 - Sayfa 12
        /// MAC Formülü: HASH(xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
        /// </summary>
        private string CalculateMacForResolve(string xid, string amount, string currency)
        {
            // ADIM 1: firstHash = HASH(encKey + ';' + terminalId)
            var firstHashInput = $"{_settings.PosnetEncKey};{_settings.PosnetTerminalId}";
            var firstHash = ComputeSha256(firstHashInput);

            // ADIM 2: MAC = HASH(xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
            var macInput = $"{xid};{amount};{currency};{_settings.PosnetMerchantId};{firstHash}";
            return ComputeSha256(macInput);
        }

        private static string? NormalizeAmountForMac(string? amount)
        {
            if (string.IsNullOrWhiteSpace(amount)) return null;
            var digitsOnly = new string(amount.Where(char.IsDigit).ToArray());
            return string.IsNullOrWhiteSpace(digitsOnly) ? null : digitsOnly;
        }

        private static string? NormalizeCurrencyForMac(string? currency)
        {
            if (string.IsNullOrWhiteSpace(currency)) return null;
            var normalized = currency.Trim().ToUpperInvariant();
            return normalized switch
            {
                "YT" => "TL",
                "TRY" => "TL",
                _ => normalized
            };
        }

        /// <summary>
        /// oosTranData için MAC hesaplar
        /// POSNET API Dokümanı v2.1.1.3 - Sayfa 15
        /// MAC Formülü: HASH(xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
        /// </summary>
        private string CalculateMacForTranData(string xid, string amount, string currency)
        {
            // ADIM 1: firstHash = HASH(encKey + ';' + terminalId)
            var firstHashInput = $"{_settings.PosnetEncKey};{_settings.PosnetTerminalId}";
            var firstHash = ComputeSha256(firstHashInput);

            // ADIM 2: MAC = HASH(xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
            var macInput = $"{xid};{amount};{currency};{_settings.PosnetMerchantId};{firstHash}";
            return ComputeSha256(macInput);
        }

        /// <summary>
        /// SHA-256 hash hesaplar ve Base64 formatında döndürür
        /// </summary>
        private static string ComputeSha256(string input)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Provizyon (ön yetkilendirme) işlemi
        /// Tutarı bloke eder, finansallaştırma ile çekilir
        /// </summary>
        public virtual async Task<PosnetResult<PosnetAuthResponse>> ProcessAuthAsync(
            int orderId,
            string cardNumber, string expireDate, string cvv,
            decimal? amount = null,
            int installment = 0,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[POSNET] Provizyon başlatılıyor. OrderId: {OrderId}",
                orderId);

            try
            {
                var order = await _db.Orders.FindAsync(new object[] { orderId }, cancellationToken);
                if (order == null)
                {
                    return PosnetResult<PosnetAuthResponse>.Failure(
                        "Sipariş bulunamadı",
                        PosnetErrorCode.InvalidOrderId);
                }

                var posnetOrderId = await GetOrCreatePosnetOrderIdAsync(order, cancellationToken);

                var request = new PosnetAuthRequest
                {
                    MerchantId = _settings.PosnetMerchantId,
                    TerminalId = _settings.PosnetTerminalId,
                    Card = new PosnetCardInfo
                    {
                        CardNumber = cardNumber.Replace(" ", ""),
                        ExpireDate = expireDate,
                        Cvv = cvv
                    },
                    OrderId = posnetOrderId,
                    // NEDEN: Tartılı siparişlerde auth tutarı sipariş toplamından farklı olabilir.
                    // amount verilmişse provizyon blokesi o tutar üzerinden alınır.
                    Amount = PosnetSaleRequest.ConvertToKurus(amount ?? order.TotalPrice),
                    Installment = installment.ToString("D2")
                };

                var xml = _xmlBuilder.BuildAuthXml(request);
                var httpResponse = await _httpClient.SendAsync(xml, cancellationToken);

                if (!httpResponse.IsSuccess)
                {
                    return PosnetResult<PosnetAuthResponse>.Failure(
                        httpResponse.ErrorMessage ?? "HTTP hatası",
                        PosnetErrorCode.ConnectionError,
                        httpResponse.Exception,
                        stopwatch.ElapsedMilliseconds);
                }

                var authResponse = _xmlParser.ParseAuthResponse(httpResponse.ResponseXml!);
                stopwatch.Stop();

                await SavePaymentRecordAsync(
                    orderId,
                    authResponse.HostLogKey ?? posnetOrderId,
                    amount ?? order.TotalPrice,
                    authResponse.IsSuccess ? "Authorized" : "Failed",
                    httpResponse.ResponseXml,
                    cancellationToken,
                    hostLogKey: authResponse.HostLogKey,
                    authCode: authResponse.AuthCode,
                    transactionType: "auth",
                    authorizedAmount: amount ?? order.TotalPrice,
                    transactionId: posnetOrderId);

                if (authResponse.IsSuccess)
                {
                    _logger.LogInformation(
                        "[POSNET] Provizyon başarılı. OrderId: {OrderId}, HostLogKey: {HostLogKey}, AuthCode: {AuthCode}",
                        orderId, authResponse.HostLogKey, authResponse.AuthCode);

                    // ═══════════════════════════════════════════════════════════════
                    // MADDE 9 DÜZELTMESİ: Auth başarısında Order.PreAuthHostLogKey
                    // ve Order.AuthorizedAmount kaydedilmesi zorunlu.
                    // Banka dokümantasyonu: capt ve reverse işlemlerinde
                    // bu key kullanılır.
                    // ═══════════════════════════════════════════════════════════════
                    try
                    {
                        var orderToUpdate = await _db.Orders.FindAsync(new object[] { orderId }, cancellationToken);
                        if (orderToUpdate != null)
                        {
                            orderToUpdate.PreAuthHostLogKey = authResponse.HostLogKey;
                            orderToUpdate.AuthorizedAmount = amount ?? order.TotalPrice;
                            orderToUpdate.PreAuthDate = DateTime.UtcNow;
                            orderToUpdate.PosnetTransactionId = posnetOrderId;
                            await _db.SaveChangesAsync(cancellationToken);

                            _logger.LogInformation(
                                "[POSNET] Order.PreAuthHostLogKey güncellendi. OrderId={OrderId}, HostLogKey={Key}",
                                orderId, authResponse.HostLogKey);
                        }
                    }
                    catch (Exception orderEx)
                    {
                        // Auth kaydedildi ama order güncellenemedi - kritik log
                        _logger.LogError(orderEx,
                            "[POSNET] KRITIK: Auth başarılı ama Order.PreAuthHostLogKey kaydedilemedi! " +
                            "OrderId={OrderId}, HostLogKey={Key}. Manuel müdahale gerekli!",
                            orderId, authResponse.HostLogKey);
                    }
                }

                return PosnetResult<PosnetAuthResponse>.FromResponse(authResponse, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[POSNET] Provizyon hatası. OrderId: {OrderId}", orderId);

                return PosnetResult<PosnetAuthResponse>.Failure(
                    ex.Message,
                    PosnetErrorCode.SystemError,
                    ex,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Finansallaştırma (provizyon çekme)
        /// Daha önce alınan provizyonu çeker
        /// </summary>
        public virtual async Task<PosnetResult<PosnetCaptResponse>> ProcessCaptureAsync(
            int orderId,
            string hostLogKey,
            decimal? amount = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[POSNET] Finansallaştırma başlatılıyor. OrderId: {OrderId}, HostLogKey: {HostLogKey}",
                orderId, hostLogKey);

            try
            {
                var order = await _db.Orders.FindAsync(new object[] { orderId }, cancellationToken);
                if (order == null)
                {
                    return PosnetResult<PosnetCaptResponse>.Failure(
                        "Sipariş bulunamadı",
                        PosnetErrorCode.InvalidOrderId);
                }

                var captureAmount = amount ?? order.TotalPrice;
                var originalPosnetOrderId = await ResolveOriginalPosnetOrderIdAsync(orderId, cancellationToken)
                    ?? order.PosnetTransactionId
                    ?? GeneratePosnetOrderId(orderId);

                var request = new PosnetCaptRequest
                {
                    MerchantId = _settings.PosnetMerchantId,
                    TerminalId = _settings.PosnetTerminalId,
                    OrderId = originalPosnetOrderId,
                    Amount = PosnetSaleRequest.ConvertToKurus(captureAmount),
                    CurrencyCode = NormalizeCurrencyCode(order.Currency),
                    Installment = "00",
                    HostLogKey = hostLogKey,
                    // ── MADDE 8: OrderDate UTC→TR saat dilimi ───────────────────────────
                    // UTC ve TR (UTC+3) gün sınırında uyumsuz tarih gönderilmesini önler.
                    OrderDate = ToTurkeyDateString(order.OrderDate)
                };

                var xml = _xmlBuilder.BuildCaptXml(request);
                var httpResponse = await _httpClient.SendAsync(xml, cancellationToken);

                if (!httpResponse.IsSuccess)
                {
                    return PosnetResult<PosnetCaptResponse>.Failure(
                        httpResponse.ErrorMessage ?? "HTTP hatası",
                        PosnetErrorCode.ConnectionError,
                        httpResponse.Exception,
                        stopwatch.ElapsedMilliseconds);
                }

                var captResponse = _xmlParser.ParseCaptResponse(httpResponse.ResponseXml!);
                stopwatch.Stop();

                // Capt sonucu Payments tablosuna yaz (iade için gerekli)
                await SavePaymentRecordAsync(
                    orderId,
                    captResponse.HostLogKey ?? hostLogKey,
                    captureAmount,
                    captResponse.IsSuccess ? "Paid" : "Failed",
                    httpResponse.ResponseXml,
                    cancellationToken,
                    hostLogKey: captResponse.HostLogKey ?? hostLogKey,
                    authCode: captResponse.AuthCode,
                    transactionType: "capt",
                    transactionId: originalPosnetOrderId);

                if (captResponse.IsSuccess)
                {
                    _logger.LogInformation(
                        "[POSNET] Finansallaştırma başarılı. OrderId: {OrderId}, HostLogKey: {HostLogKey}, AuthCode: {AuthCode}",
                        orderId, captResponse.HostLogKey, captResponse.AuthCode);

                    await UpdateOrderPaymentStatusAsync(orderId, PaymentStatus.Paid, cancellationToken);

                    // ═══════════════════════════════════════════════════════════════
                    // MADDE 9 DÜZELTMESİ: Capture'dan dönen yeni HostLogKey
                    // Order üzerinde de saklanır. İade işlemi bu key ile yapılır.
                    // Banka dok: "iade işleminde orijinal işlemin bilgileriyle gelinmesi
                    // gerekmektedir" → Capture HostLogKey iade için kullanılır.
                    // ═══════════════════════════════════════════════════════════════
                    try
                    {
                        var capturedOrder = await _db.Orders.FindAsync(new object[] { orderId }, cancellationToken);
                        if (capturedOrder != null && !string.IsNullOrWhiteSpace(captResponse.HostLogKey))
                        {
                            // Capture HostLogKey'i sakla (iade işlemlerinde bu key kullanılır)
                            // PreAuthHostLogKey'i koruyarak ayrı bir alana yazabiliriz,
                            // ancak Payment tablosundaki 'capt' kaydı üzerinden bulunabilir.
                            capturedOrder.CapturedAmount = captureAmount;
                            capturedOrder.CaptureStatus = CaptureStatus.Success;
                            capturedOrder.CapturedAt = DateTime.UtcNow;
                            await _db.SaveChangesAsync(cancellationToken);
                        }
                    }
                    catch (Exception captOrderEx)
                    {
                        _logger.LogError(captOrderEx,
                            "[POSNET] Capture sonrası Order güncellenemedi. OrderId={OrderId}",
                            orderId);
                    }
                }

                return PosnetResult<PosnetCaptResponse>.FromResponse(captResponse, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[POSNET] Finansallaştırma hatası. OrderId: {OrderId}", orderId);

                return PosnetResult<PosnetCaptResponse>.Failure(
                    ex.Message,
                    PosnetErrorCode.SystemError,
                    ex,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// İptal işlemi (gün içi)
        /// Sadece aynı gün içinde yapılabilir
        /// </summary>
        public virtual async Task<PosnetResult<PosnetReverseResponse>> ProcessReverseAsync(
            int orderId,
            string hostLogKey,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[POSNET] İptal işlemi başlatılıyor. OrderId: {OrderId}, HostLogKey: {HostLogKey}",
                orderId, hostLogKey);

            try
            {
                var order = await _db.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
                var originalPosnetOrderId = await ResolveOriginalPosnetOrderIdAsync(orderId, cancellationToken)
                    ?? order?.PosnetTransactionId
                    ?? GeneratePosnetOrderId(orderId);
                var relatedPayment = await _db.Payments
                    .AsNoTracking()
                    .Where(p => p.OrderId == orderId &&
                        (p.HostLogKey == hostLogKey || p.ProviderPaymentId == hostLogKey))
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);
                var reverseTransaction = relatedPayment?.TransactionType?.ToLowerInvariant() switch
                {
                    "auth" => "auth",
                    "capt" => "capt",
                    "return" => "return",
                    _ => "sale"
                };

                var request = new PosnetReverseRequest
                {
                    MerchantId = _settings.PosnetMerchantId,
                    TerminalId = _settings.PosnetTerminalId,
                    Transaction = reverseTransaction,
                    OrderId = originalPosnetOrderId,
                    HostLogKey = hostLogKey,
                    // ── MADDE 8: OrderDate UTC→TR saat dilimi ───────────────────────────
                    OrderDate = order != null ? ToTurkeyDateString(order.OrderDate) : null,
                    AuthCode = relatedPayment?.AuthCode
                };

                var xml = _xmlBuilder.BuildReverseXml(request);
                var httpResponse = await _httpClient.SendAsync(xml, cancellationToken);

                if (!httpResponse.IsSuccess)
                {
                    return PosnetResult<PosnetReverseResponse>.Failure(
                        httpResponse.ErrorMessage ?? "HTTP hatası",
                        PosnetErrorCode.ConnectionError,
                        httpResponse.Exception,
                        stopwatch.ElapsedMilliseconds);
                }

                var reverseResponse = _xmlParser.ParseReverseResponse(httpResponse.ResponseXml!);
                stopwatch.Stop();

                if (reverseResponse.IsSuccess)
                {
                    _logger.LogInformation(
                        "[POSNET] İptal başarılı. OrderId: {OrderId}",
                        orderId);

                    await UpdateOrderPaymentStatusAsync(orderId, PaymentStatus.Cancelled, cancellationToken);
                    await UpdatePaymentStatusAsync(hostLogKey, "Cancelled", cancellationToken);
                }

                return PosnetResult<PosnetReverseResponse>.FromResponse(reverseResponse, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[POSNET] İptal hatası. OrderId: {OrderId}", orderId);

                return PosnetResult<PosnetReverseResponse>.Failure(
                    ex.Message,
                    PosnetErrorCode.SystemError,
                    ex,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// İade işlemi (gün sonu sonrası)
        /// Kısmi veya tam iade yapılabilir
        /// </summary>
        public virtual async Task<PosnetResult<PosnetReturnResponse>> ProcessRefundAsync(
            int orderId,
            string hostLogKey,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[POSNET] İade işlemi başlatılıyor. OrderId: {OrderId}, Amount: {Amount}",
                orderId, amount);

            try
            {
                var order = await _db.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
                var originalPosnetOrderId = await ResolveOriginalPosnetOrderIdAsync(orderId, cancellationToken)
                    ?? order?.PosnetTransactionId
                    ?? GeneratePosnetOrderId(orderId);

                // ── MADDE 15: KGL siparişlerde iade için doğru HostLogKey seçimi ──────────
                // POSNET dok: "Finansallaştırma (Capt) sonrası iade için Capture HostLogKey
                // kullanılmalıdır." KGL ürün için önce 'capt' kaydı aranir; yoksa
                // orijinal 'auth'/'sale' kaydına fallback yapılır.
                var isWeightBasedOrder = order?.HasWeightBasedItems == true;
                string effectiveHostLogKey = hostLogKey;

                if (isWeightBasedOrder)
                {
                    var captPayment = await _db.Payments
                        .AsNoTracking()
                        .Where(p => p.OrderId == orderId &&
                                    p.TransactionType == "capt" &&
                                    p.Status == "Paid" &&
                                    !string.IsNullOrWhiteSpace(p.HostLogKey))
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (captPayment != null && !string.IsNullOrWhiteSpace(captPayment.HostLogKey))
                    {
                        effectiveHostLogKey = captPayment.HostLogKey!;
                        _logger.LogInformation(
                            "[POSNET] KGL iade: Capture HostLogKey kullanılıyor. " +
                            "OrderId: {OrderId}, CaptHostLogKey: {Key}",
                            orderId, effectiveHostLogKey);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[POSNET] KGL iade: Capture HostLogKey bulunamadı, parametre olarak gelen key kullanılıyor. " +
                            "OrderId: {OrderId}, FallbackKey: {Key}",
                            orderId, effectiveHostLogKey);
                    }
                }

                var request = new PosnetReturnRequest
                {
                    MerchantId = _settings.PosnetMerchantId,
                    TerminalId = _settings.PosnetTerminalId,
                    OrderId = originalPosnetOrderId,
                    Amount = PosnetSaleRequest.ConvertToKurus(amount),
                    HostLogKey = effectiveHostLogKey,
                    CurrencyCode = NormalizeCurrencyCode(order?.Currency),
                    // ── MADDE 8: OrderDate UTC→TR saat dilimi ───────────────────────────
                    OrderDate = order != null ? ToTurkeyDateString(order.OrderDate) : null
                };

                var xml = _xmlBuilder.BuildReturnXml(request);
                var httpResponse = await _httpClient.SendAsync(xml, cancellationToken);

                if (!httpResponse.IsSuccess)
                {
                    return PosnetResult<PosnetReturnResponse>.Failure(
                        httpResponse.ErrorMessage ?? "HTTP hatası",
                        PosnetErrorCode.ConnectionError,
                        httpResponse.Exception,
                        stopwatch.ElapsedMilliseconds);
                }

                var returnResponse = _xmlParser.ParseReturnResponse(httpResponse.ResponseXml!);
                stopwatch.Stop();

                // İade sonucu Payments tablosuna ayrı kayıt olarak yaz
                // OriginalPaymentId: iade edilen orijinal ödeme kaydına bağlantı
                var originalPayment = await _db.Payments
                    .Where(p => p.OrderId == orderId &&
                           (p.TransactionType == "capt" || p.TransactionType == "sale") &&
                           p.Status == "Paid")
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                await SavePaymentRecordAsync(
                    orderId,
                    returnResponse.HostLogKey ?? hostLogKey,
                    amount,
                    returnResponse.IsSuccess ? "Refunded" : "Failed",
                    httpResponse.ResponseXml,
                    cancellationToken,
                    hostLogKey: returnResponse.HostLogKey ?? hostLogKey,
                    authCode: returnResponse.AuthCode,
                    transactionType: "return",
                    originalPaymentId: originalPayment?.Id,
                    transactionId: originalPosnetOrderId);

                if (returnResponse.IsSuccess)
                {
                    _logger.LogInformation(
                        "[POSNET] İade başarılı. OrderId: {OrderId}, RefundedAmount: {Amount}, " +
                        "HostLogKey: {HostLogKey}",
                        orderId, amount, returnResponse.HostLogKey);

                    // Tam iade mi kısmi iade mi kontrol et
                    var orderToUpdate = await _db.Orders.FindAsync(new object[] { orderId }, cancellationToken);
                    if (orderToUpdate != null && amount >= orderToUpdate.TotalPrice)
                    {
                        await UpdateOrderPaymentStatusAsync(orderId, PaymentStatus.Refunded, cancellationToken);
                    }
                }

                return PosnetResult<PosnetReturnResponse>.FromResponse(returnResponse, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[POSNET] İade hatası. OrderId: {OrderId}", orderId);

                return PosnetResult<PosnetReturnResponse>.Failure(
                    ex.Message,
                    PosnetErrorCode.SystemError,
                    ex,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Puan sorgulama
        /// WorldCard sahiplerinin puan bakiyesini sorgular
        /// </summary>
        public virtual async Task<PosnetResult<PosnetPointInquiryResponse>> QueryPointsAsync(
            string cardNumber, string expireDate, string cvv,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("[POSNET] Puan sorgulama. Kart maskelenmiş: {Card}",
                    (cardNumber ?? string.Empty).Replace(" ", "").Length >= 4
                        ? $"****{cardNumber.Replace(" ", "")[^4..]}"
                        : cardNumber);

                var request = new PosnetPointInquiryRequest
                {
                    MerchantId = _settings.PosnetMerchantId,
                    TerminalId = _settings.PosnetTerminalId,
                    Card = new PosnetCardInfo
                    {
                        CardNumber = cardNumber.Replace(" ", ""),
                        ExpireDate = expireDate,
                        Cvv = cvv
                    }
                };

                var xml = _xmlBuilder.BuildPointInquiryXml(request);
                var httpResponse = await _httpClient.SendAsync(xml, cancellationToken);

                if (!httpResponse.IsSuccess)
                {
                    return PosnetResult<PosnetPointInquiryResponse>.Failure(
                        httpResponse.ErrorMessage ?? "HTTP hatası",
                        PosnetErrorCode.ConnectionError,
                        httpResponse.Exception,
                        stopwatch.ElapsedMilliseconds);
                }

                var pointResponse = _xmlParser.ParsePointInquiryResponse(httpResponse.ResponseXml!);
                stopwatch.Stop();

                if (pointResponse.IsEnrolled)
                {
                    _logger.LogInformation("[POSNET] Puan sorgulama başarılı. TotalPoint: {TotalPoint}",
                        pointResponse.PointInfo?.TotalPoint ?? 0);
                }

                return PosnetResult<PosnetPointInquiryResponse>.FromResponse(pointResponse, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[POSNET] Puan sorgulama hatası");

                return PosnetResult<PosnetPointInquiryResponse>.Failure(
                    ex.Message,
                    PosnetErrorCode.SystemError,
                    ex,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// İşlem durumu sorgulama
        /// Bağlantı kopması durumunda işlemin akıbetini öğrenmek için
        /// </summary>
        public virtual async Task<PosnetResult<PosnetAgreementResponse>> QueryTransactionAsync(
            string orderId,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[POSNET] İşlem durumu sorgulama. OrderId: {OrderId}",
                orderId);

            try
            {
                var request = new PosnetAgreementRequest
                {
                    MerchantId = _settings.PosnetMerchantId,
                    TerminalId = _settings.PosnetTerminalId,
                    OrderId = orderId
                };

                var xml = _xmlBuilder.BuildAgreementXml(request);
                var httpResponse = await _httpClient.SendAsync(xml, cancellationToken);

                if (!httpResponse.IsSuccess)
                {
                    return PosnetResult<PosnetAgreementResponse>.Failure(
                        httpResponse.ErrorMessage ?? "HTTP hatası",
                        PosnetErrorCode.ConnectionError,
                        httpResponse.Exception,
                        stopwatch.ElapsedMilliseconds);
                }

                var agreementResponse = _xmlParser.ParseAgreementResponse(httpResponse.ResponseXml!);
                stopwatch.Stop();

                _logger.LogInformation(
                    "[POSNET] İşlem durumu: {Status}. OrderId: {OrderId}",
                    agreementResponse.TransactionStatus, orderId);

                return PosnetResult<PosnetAgreementResponse>.FromResponse(agreementResponse, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[POSNET] İşlem durumu sorgulama hatası. OrderId: {OrderId}", orderId);

                return PosnetResult<PosnetAgreementResponse>.Failure(
                    ex.Message,
                    PosnetErrorCode.SystemError,
                    ex,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// POSNET için benzersiz sipariş numarası oluşturur
        /// POSNET XID alanı max 20 karakter kabul ediyor
        /// 
        /// ── MADDE 14: OrderId > 99999 sınırı düzeltmesi ────────────────────────
        /// Eski format: YKB(3) + D5(5) + MMddHHmmss(10) + RR(2) = 20 karakter
        ///              orderId > 99999 ise D5 → 6 hane → TOPLAM 21 → POSNET HATA!
        /// Yeni format: YKB(3) + D7(7) + MMddHHmm(8) + RR(2) = 20 karakter
        ///              orderId 9,999,999'a kadar desteklenir.
        /// </summary>
        private static string GeneratePosnetOrderId(int orderId)
        {
            // POSNET XID max 20 karakter, sadece alfanumerik
            // Format: YKB{OrderId:D7}{MMddHHmm}{Random:XX} = 3 + 7 + 8 + 2 = 20 karakter
            // MADDE 14: timestamp 10→8 hane kısaltıldı, orderId 5→7 hane genişletildi.
            var timestamp = DateTime.UtcNow.ToString("MMddHHmm"); // 8 hane
            var random = Random.Shared.Next(10, 99).ToString();   // 2 hane
            return $"{ORDER_ID_PREFIX}{orderId:D7}{timestamp}{random}";
        }

        /// <summary>
        /// UTC DateTime'ı Türkiye saat dilimine (UTC+3) çevirerek yyyyMMdd formatında döndürür.
        /// ── MADDE 8: Gün sınırında UTC/TR uyuşmazlığını önler ──────────────────
        /// POSNET Capt/Return XML'de orderDate orijinal işlem tarihi (TR saati) olmalı.
        /// Örnek: 23:30 UTC = 02:30+3 TR → farklı gün → banka "sipariş tarihi uyuşmuyor" der.
        /// </summary>
        private static string ToTurkeyDateString(DateTime utcDate)
        {
            try
            {
                // Windows: "Turkey Standard Time", Linux/Docker: "Europe/Istanbul"
                TimeZoneInfo turkeyTz;
                try { turkeyTz = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
                catch { turkeyTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }

                var trDate = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(utcDate, DateTimeKind.Utc),
                    turkeyTz);
                return trDate.ToString("yyyyMMdd");
            }
            catch
            {
                // Saat dilimi bulunamazsa UTC+3 manuel offset uygula
                return utcDate.AddHours(3).ToString("yyyyMMdd");
            }
        }

        /// <summary>
        /// Ödeme kaydı oluşturur veya günceller
        /// </summary>
        /// <summary>
        /// Ödeme kaydı oluşturur.
        /// Banka dokümantasyonu: HostLogKey, AuthCode sonraki işlemlerde kullanılmak üzere saklanmalıdır.
        /// </summary>
        private async Task SavePaymentRecordAsync(
            int orderId,
            string providerPaymentId,
            decimal amount,
            string status,
            string? rawResponse,
            CancellationToken cancellationToken,
            string? hostLogKey = null,
            string? authCode = null,
            string? transactionType = null,
            int? originalPaymentId = null,
            decimal? authorizedAmount = null,
            string? transactionId = null)
        {
            try
            {
                var payment = new Payments
                {
                    OrderId = orderId,
                    Provider = PROVIDER_NAME,
                    ProviderPaymentId = providerPaymentId,
                    Amount = amount,
                    Status = status,
                    CreatedAt = DateTime.UtcNow,
                    PaidAt = (status == "Success" || status == "Paid") ? DateTime.UtcNow : null,
                    RawResponse = rawResponse,
                    // ═══════════════════════════════════════════════════════════
                    // MADDE 9 DÜZELTMESİ: HostLogKey ve AuthCode artık her zaman
                    // Payments tablosuna yazılıyor. İade/iptal işlemlerinde
                    // bu kayıt üzerinden orijinal HostLogKey bulunabilir.
                    // ═══════════════════════════════════════════════════════════
                    HostLogKey = hostLogKey,
                    AuthCode = authCode,
                    TransactionType = transactionType,
                    TransactionId = transactionId,
                    OriginalPaymentId = originalPaymentId,
                    // Auth (provizyon) için AuthorizationReference ve AuthorizedAmount
                    AuthorizationReference = hostLogKey,  // POSNET'te HostLogKey = AuthRef
                    AuthorizedAmount = authorizedAmount ?? amount,
                    AuthorizedAt = (status == "Authorized") ? DateTime.UtcNow : null
                };

                _db.Payments.Add(payment);
                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogDebug(
                    "[POSNET] Ödeme kaydı oluşturuldu. OrderId: {OrderId}, Status: {Status}, " +
                    "HostLogKey: {HostLogKey}, AuthCode: {AuthCode}, TxType: {TxType}",
                    orderId, status, hostLogKey ?? "-", authCode ?? "-", transactionType ?? "-");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[POSNET] Ödeme kaydı oluşturma hatası. OrderId: {OrderId}",
                    orderId);
                // Ana işlemi etkilememesi için exception yutulur
            }
        }

        /// <summary>
        /// Sipariş ödeme durumunu günceller
        /// </summary>
        private async Task UpdateOrderPaymentStatusAsync(
            int orderId,
            PaymentStatus status,
            CancellationToken cancellationToken)
        {
            try
            {
                var order = await _db.Orders.FindAsync(new object[] { orderId }, cancellationToken);
                if (order != null)
                {
                    order.PaymentStatus = status;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[POSNET] Sipariş durumu güncelleme hatası. OrderId: {OrderId}",
                    orderId);
            }
        }

        /// <summary>
        /// Ödeme durumunu günceller
        /// </summary>
        private async Task UpdatePaymentStatusAsync(
            string providerPaymentId,
            string status,
            CancellationToken cancellationToken)
        {
            try
            {
                var payment = await _db.Payments
                    .FirstOrDefaultAsync(p => 
                        p.ProviderPaymentId == providerPaymentId && 
                        p.Provider == PROVIDER_NAME, 
                        cancellationToken);

                if (payment != null)
                {
                    payment.Status = status;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[POSNET] Ödeme durumu güncelleme hatası. PaymentId: {PaymentId}",
                    providerPaymentId);
            }
        }

        /// <summary>
        /// 3D Secure callback MAC doğrulaması
        /// </summary>
        private bool ValidateCallbackMac(PosnetOosCallbackData callbackData)
        {
            if (string.IsNullOrEmpty(_settings.PosnetEncKey))
            {
                _logger.LogWarning("[POSNET] EncKey yapılandırılmamış, MAC doğrulaması atlanıyor");
                return true; // Test ortamında geçici olarak
            }

            return PosnetMacCalculator.ValidateCallbackMac(
                callbackData.MerchantData ?? "",
                callbackData.BankData ?? "",
                callbackData.Sign ?? "",
                _settings.PosnetEncKey);
        }

        // ═══════════════════════════════════════════════════════════════════════════════════════
        // OOS RESOLVE MERCHANT DATA - 3D Secure Callback Veri Çözümleme
        // Dokümantasyon: POSNET 3D Secure Entegrasyon Dokümanı - Sayfa 12-14
        // ═══════════════════════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<PosnetResult<PosnetOosResolveMerchantDataResponse>> ResolveOosMerchantDataAsync(
            PosnetOosResolveMerchantDataRequest request,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString("N")[..12];

            _logger.LogInformation(
                "[POSNET-RESOLVE] {CorrelationId} - oosResolveMerchantData başlatılıyor. OriginalXid: {Xid}",
                correlationId, request?.OriginalXid);

            try
            {
                // ═══════════════════════════════════════════════════════════════
                // ADIM 1: VALIDASYON
                // ═══════════════════════════════════════════════════════════════

                if (request == null)
                {
                    return PosnetResult<PosnetOosResolveMerchantDataResponse>.Failure(
                        "Request null olamaz",
                        PosnetErrorCode.InvalidRequest);
                }

                if (string.IsNullOrWhiteSpace(request.BankData))
                {
                    return PosnetResult<PosnetOosResolveMerchantDataResponse>.Failure(
                        "BankData parametresi boş olamaz",
                        PosnetErrorCode.InvalidRequest);
                }

                if (string.IsNullOrWhiteSpace(request.MerchantData))
                {
                    return PosnetResult<PosnetOosResolveMerchantDataResponse>.Failure(
                        "MerchantData parametresi boş olamaz",
                        PosnetErrorCode.InvalidRequest);
                }

                if (string.IsNullOrWhiteSpace(request.Sign))
                {
                    return PosnetResult<PosnetOosResolveMerchantDataResponse>.Failure(
                        "Sign parametresi boş olamaz",
                        PosnetErrorCode.InvalidRequest);
                }

                if (string.IsNullOrWhiteSpace(request.Mac))
                {
                    return PosnetResult<PosnetOosResolveMerchantDataResponse>.Failure(
                        "MAC parametresi boş olamaz",
                        PosnetErrorCode.InvalidRequest);
                }

                // ═══════════════════════════════════════════════════════════════
                // ADIM 2: XML REQUEST OLUŞTUR
                // ═══════════════════════════════════════════════════════════════

                // Request modeline MerchantId ve TerminalId ekle
                var resolveRequest = request with
                {
                    MerchantId = _settings.PosnetMerchantId ?? string.Empty,
                    TerminalId = _settings.PosnetTerminalId ?? string.Empty
                };

                var xml = _xmlBuilder.BuildOosResolveMerchantDataXml(resolveRequest);

                _logger.LogDebug(
                    "[POSNET-RESOLVE] {CorrelationId} - XML oluşturuldu, gönderiliyor...",
                    correlationId);

                // ═══════════════════════════════════════════════════════════════
                // ADIM 3: BANKAYA GÖNDER
                // ═══════════════════════════════════════════════════════════════

                var httpResponse = await _httpClient.SendAsync(xml, cancellationToken);

                if (!httpResponse.IsSuccess)
                {
                    _logger.LogError(
                        "[POSNET-RESOLVE] {CorrelationId} - HTTP hatası: {Error}",
                        correlationId, httpResponse.ErrorMessage);

                    return PosnetResult<PosnetOosResolveMerchantDataResponse>.Failure(
                        httpResponse.ErrorMessage ?? "HTTP iletişim hatası",
                        PosnetErrorCode.ConnectionError,
                        httpResponse.Exception,
                        stopwatch.ElapsedMilliseconds);
                }

                // ═══════════════════════════════════════════════════════════════
                // ADIM 4: RESPONSE PARSE ET
                // ═══════════════════════════════════════════════════════════════

                var response = _xmlParser.ParseOosResolveMerchantDataResponse(httpResponse.ResponseXml ?? "");

                if (!response.Approved)
                {
                    _logger.LogWarning(
                        "[POSNET-RESOLVE] {CorrelationId} - Banka reddi: {ErrorCode} - {ErrorMessage}",
                        correlationId, response.RawErrorCode, response.ErrorMessage);

                    return PosnetResult<PosnetOosResolveMerchantDataResponse>.FromResponse(
                        response,
                        stopwatch.ElapsedMilliseconds);
                }

                // ═══════════════════════════════════════════════════════════════
                // ADIM 5: VERİ TUTARLILIĞI KONTROLÜ (GÜVENLİK KRİTİK!)
                // ═══════════════════════════════════════════════════════════════

                // Orijinal XID kontrolü
                if (!string.IsNullOrWhiteSpace(request.OriginalXid) &&
                    !string.Equals(response.Xid, request.OriginalXid, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError(
                        "[POSNET-RESOLVE] {CorrelationId} - XID UYUŞMAZLIĞI! " +
                        "Orijinal: {OriginalXid}, Response: {ResponseXid}. " +
                        "OLASI MAN-IN-THE-MIDDLE SALDIRISI!",
                        correlationId, request.OriginalXid, response.Xid);

                    return PosnetResult<PosnetOosResolveMerchantDataResponse>.Failure(
                        "Güvenlik hatası: İşlem verileri tutarsız (XID)",
                        PosnetErrorCode.SecurityViolation,
                        elapsedMs: stopwatch.ElapsedMilliseconds);
                }

                // Orijinal tutar kontrolü
                if (request.OriginalAmount.HasValue &&
                    response.Amount != request.OriginalAmount.Value)
                {
                    _logger.LogError(
                        "[POSNET-RESOLVE] {CorrelationId} - TUTAR UYUŞMAZLIĞI! " +
                        "Orijinal: {OriginalAmount}, Response: {ResponseAmount}. " +
                        "OLASI MAN-IN-THE-MIDDLE SALDIRISI!",
                        correlationId, request.OriginalAmount, response.Amount);

                    return PosnetResult<PosnetOosResolveMerchantDataResponse>.Failure(
                        "Güvenlik hatası: İşlem verileri tutarsız (Tutar)",
                        PosnetErrorCode.SecurityViolation,
                        elapsedMs: stopwatch.ElapsedMilliseconds);
                }

                // Orijinal para birimi kontrolü
                if (!string.IsNullOrWhiteSpace(request.OriginalCurrency) &&
                    !string.Equals(response.Currency, request.OriginalCurrency, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError(
                        "[POSNET-RESOLVE] {CorrelationId} - PARA BİRİMİ UYUŞMAZLIĞI! " +
                        "Orijinal: {OriginalCurrency}, Response: {ResponseCurrency}",
                        correlationId, request.OriginalCurrency, response.Currency);

                    return PosnetResult<PosnetOosResolveMerchantDataResponse>.Failure(
                        "Güvenlik hatası: İşlem verileri tutarsız (Para birimi)",
                        PosnetErrorCode.SecurityViolation,
                        elapsedMs: stopwatch.ElapsedMilliseconds);
                }

                // ═══════════════════════════════════════════════════════════════
                // ADIM 6: MDSTATUS KONTROLÜ
                // ═══════════════════════════════════════════════════════════════

                if (!response.CanProceedWithPayment)
                {
                    _logger.LogWarning(
                        "[POSNET-RESOLVE] {CorrelationId} - 3D Secure başarısız. " +
                        "MdStatus: {MdStatus} - {MdStatusDescription}",
                        correlationId, response.MdStatus, response.MdStatusDescription);
                }

                // ═══════════════════════════════════════════════════════════════
                // BAŞARI
                // ═══════════════════════════════════════════════════════════════

                _logger.LogInformation(
                    "[POSNET-RESOLVE] {CorrelationId} - ✅ Başarılı! " +
                    "XID: {Xid}, Amount: {Amount}, MdStatus: {MdStatus}, CanProceed: {CanProceed}",
                    correlationId, response.Xid, response.Amount, 
                    response.MdStatus, response.CanProceedWithPayment);

                stopwatch.Stop();
                return PosnetResult<PosnetOosResolveMerchantDataResponse>.Success(
                    response,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[POSNET-RESOLVE] {CorrelationId} - Beklenmeyen hata",
                    correlationId);

                return PosnetResult<PosnetOosResolveMerchantDataResponse>.Failure(
                    $"Beklenmeyen hata: {ex.Message}",
                    PosnetErrorCode.SystemError,
                    ex,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════════════
        // OOS TRAN DATA - 3D Secure Finansallaştırma
        // Dokümantasyon: POSNET 3D Secure Entegrasyon Dokümanı - Sayfa 15-17
        // ═══════════════════════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<PosnetResult<PosnetOosTranDataResponse>> ProcessOosTranDataAsync(
            PosnetOosTranDataRequest request,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString("N")[..12];

            _logger.LogInformation(
                "[POSNET-TRANDATA] {CorrelationId} - oosTranData (Finansallaştırma) başlatılıyor. " +
                "OrderId: {OrderId}, WpAmount: {WpAmount}",
                correlationId, request?.OrderId, request?.WpAmount);

            try
            {
                // ═══════════════════════════════════════════════════════════════
                // ADIM 1: VALIDASYON
                // ═══════════════════════════════════════════════════════════════

                if (request == null)
                {
                    return PosnetResult<PosnetOosTranDataResponse>.Failure(
                        "Request null olamaz",
                        PosnetErrorCode.InvalidRequest);
                }

                if (string.IsNullOrWhiteSpace(request.BankData))
                {
                    return PosnetResult<PosnetOosTranDataResponse>.Failure(
                        "BankData parametresi boş olamaz",
                        PosnetErrorCode.InvalidRequest);
                }

                if (string.IsNullOrWhiteSpace(request.Mac))
                {
                    return PosnetResult<PosnetOosTranDataResponse>.Failure(
                        "MAC parametresi boş olamaz",
                        PosnetErrorCode.InvalidRequest);
                }

                // ═══════════════════════════════════════════════════════════════
                // ADIM 2: XML REQUEST OLUŞTUR
                // ═══════════════════════════════════════════════════════════════

                // Request modeline MerchantId ve TerminalId ekle
                var tranDataRequest = request with
                {
                    MerchantId = _settings.PosnetMerchantId ?? string.Empty,
                    TerminalId = _settings.PosnetTerminalId ?? string.Empty
                };

                var xml = _xmlBuilder.BuildOosTranDataXml(tranDataRequest);

                // DEBUG: Gönderilen XML'i logla
                _logger.LogDebug(
                    "[POSNET-TRANDATA] {CorrelationId} - Gönderilen XML: {Xml}",
                    correlationId, xml?.Substring(0, Math.Min(500, xml?.Length ?? 0)));

                _logger.LogDebug(
                    "[POSNET-TRANDATA] {CorrelationId} - XML oluşturuldu, gönderiliyor...",
                    correlationId);

                // ═══════════════════════════════════════════════════════════════
                // ADIM 3: BANKAYA GÖNDER
                // NOT: Raw XML gönderiyoruz, HTTP client encode edecek
                // ═══════════════════════════════════════════════════════════════

                var httpResponse = await _httpClient.SendAsync(xml, cancellationToken);

                if (!httpResponse.IsSuccess)
                {
                    _logger.LogError(
                        "[POSNET-TRANDATA] {CorrelationId} - HTTP hatası: {Error}",
                        correlationId, httpResponse.ErrorMessage);

                    return PosnetResult<PosnetOosTranDataResponse>.Failure(
                        httpResponse.ErrorMessage ?? "HTTP iletişim hatası",
                        PosnetErrorCode.ConnectionError,
                        httpResponse.Exception,
                        stopwatch.ElapsedMilliseconds);
                }

                // ═══════════════════════════════════════════════════════════════
                // ADIM 4: RESPONSE PARSE ET
                // ═══════════════════════════════════════════════════════════════

                // DEBUG: POSNET'ten gelen XML'i logla
                _logger.LogDebug(
                    "[POSNET-TRANDATA] {CorrelationId} - POSNET Response XML: {Xml}",
                    correlationId, httpResponse.ResponseXml?.Substring(0, Math.Min(500, httpResponse.ResponseXml?.Length ?? 0)) ?? "null");

                var response = _xmlParser.ParseOosTranDataResponse(httpResponse.ResponseXml ?? "");

                if (!response.Approved)
                {
                    _logger.LogWarning(
                        "[POSNET-TRANDATA] {CorrelationId} - Finansallaştırma REDDEDİLDİ! " +
                        "ErrorCode: {ErrorCode} - {ErrorMessage}",
                        correlationId, response.RawErrorCode, response.ErrorMessage);

                    var failedOrderId = await ResolveOrderIdByPosnetOrderIdAsync(request.OrderId, cancellationToken);
                    if (failedOrderId.HasValue)
                    {
                        await SavePaymentRecordAsync(
                            failedOrderId.Value,
                            $"FAILED_{correlationId}",
                            request.Amount ?? 0,
                            "Failed_Finalization",
                            httpResponse.ResponseXml,
                            cancellationToken,
                            transactionId: request.OrderId);
                    }

                    return PosnetResult<PosnetOosTranDataResponse>.FromResponse(
                        response,
                        stopwatch.ElapsedMilliseconds);
                }

                // ═══════════════════════════════════════════════════════════════
                // ADIM 5: BAŞARILI FİNANSALLAŞTIRMA
                // ═══════════════════════════════════════════════════════════════

                _logger.LogInformation(
                    "[POSNET-TRANDATA] {CorrelationId} - ✅ Finansallaştırma BAŞARILI! " +
                    "HostLogKey: {HostLogKey}, AuthCode: {AuthCode}, Approved: {Approved}",
                    correlationId, response.HostLogKey, response.AuthCode, response.ApprovedCode);

                var successOrderId = await ResolveOrderIdByPosnetOrderIdAsync(request.OrderId, cancellationToken);
                if (successOrderId.HasValue)
                {
                    await SavePaymentRecordAsync(
                        successOrderId.Value,
                        response.HostLogKey,
                        request.Amount ?? 0,
                        response.IsAlreadyProcessed ? "AlreadyProcessed" : "Success",
                        httpResponse.ResponseXml,
                        cancellationToken,
                        hostLogKey: response.HostLogKey,
                        authCode: response.AuthCode,
                        transactionId: request.OrderId);

                    // Sipariş durumunu güncelle
                    await UpdateOrderPaymentStatusAsync(
                        successOrderId.Value,
                        PaymentStatus.Paid,
                        cancellationToken);
                }

                stopwatch.Stop();
                return PosnetResult<PosnetOosTranDataResponse>.Success(
                    response,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[POSNET-TRANDATA] {CorrelationId} - Beklenmeyen hata",
                    correlationId);

                return PosnetResult<PosnetOosTranDataResponse>.Failure(
                    $"Beklenmeyen hata: {ex.Message}",
                    PosnetErrorCode.SystemError,
                    ex,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
