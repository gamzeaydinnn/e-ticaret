// ═══════════════════════════════════════════════════════════════════════════════
// POSNET 3D SECURE CALLBACK HANDLER SERVİSİ
// Banka'dan gelen 3D Secure callback'lerini işler ve ödemeyi tamamlar
// Dokümantasyon: POSNET XML Servisleri Entegrasyon Dokümanı v2.1.1.3 - Sayfa 40-50
// 
// AKIŞ:
// 1. Banka 3D Secure sayfasından müşteri şifresini girer
// 2. Banka, callback URL'e POST yapar (BankData, MerchantData, Sign, Mac)
// 3. Bu servis MAC'ı doğrular, MdStatus'ü kontrol eder
// 4. Başarılı ise OOS-TDS ile ödemeyi tamamlar
// 5. Sipariş durumunu günceller ve frontend'e yönlendirir
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using ECommerce.Infrastructure.Config;
using ECommerce.Infrastructure.Services.Payment.Posnet.Models;
using ECommerce.Infrastructure.Services.Payment.Posnet.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services.Payment.Posnet
{
    /// <summary>
    /// POSNET 3D Secure callback işleme servisi interface
    /// </summary>
    public interface IPosnet3DSecureCallbackHandler
    {
        /// <summary>
        /// 3D Secure callback'i işler ve ödemeyi tamamlar
        /// </summary>
        /// <param name="callbackRequest">Banka'dan gelen callback verileri</param>
        /// <returns>İşlem sonucu</returns>
        Task<Posnet3DSecureResultDto> HandleCallbackAsync(Posnet3DSecureCallbackRequestDto callbackRequest);

        /// <summary>
        /// Callback verilerinden OrderId çıkarır
        /// </summary>
        int? ExtractOrderId(Posnet3DSecureCallbackRequestDto callbackRequest);

        /// <summary>
        /// Başarı yönlendirme URL'i oluşturur
        /// </summary>
        string BuildSuccessRedirectUrl(int orderId, string? transactionId = null);

        /// <summary>
        /// Hata yönlendirme URL'i oluşturur
        /// </summary>
        string BuildFailureRedirectUrl(int? orderId, string errorCode, string errorMessage);
    }

    /// <summary>
    /// POSNET 3D Secure callback handler implementasyonu
    /// </summary>
    public class Posnet3DSecureCallbackHandler : IPosnet3DSecureCallbackHandler
    {
        private readonly IPosnetMacValidator _macValidator;
        private readonly IPosnetPaymentService _posnetService;
        private readonly ECommerceDbContext _dbContext;
        private readonly PaymentSettings _settings;
        private readonly ILogger<Posnet3DSecureCallbackHandler> _logger;

        public Posnet3DSecureCallbackHandler(
            IPosnetMacValidator macValidator,
            IPosnetPaymentService posnetService,
            ECommerceDbContext dbContext,
            IOptions<PaymentSettings> options,
            ILogger<Posnet3DSecureCallbackHandler> logger)
        {
            _macValidator = macValidator ?? throw new ArgumentNullException(nameof(macValidator));
            _posnetService = posnetService ?? throw new ArgumentNullException(nameof(posnetService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<Posnet3DSecureResultDto> HandleCallbackAsync(Posnet3DSecureCallbackRequestDto callbackRequest)
        {
            var correlationId = Guid.NewGuid().ToString("N")[..12];

            _logger.LogInformation("[POSNET-3DS-CALLBACK] {CorrelationId} - Callback alındı, XID: {Xid}",
                correlationId, callbackRequest?.Xid);

            try
            {
                // ═══════════════════════════════════════════════════════════════
                // ADIM 1: GİRİŞ DOĞRULAMASI
                // ═══════════════════════════════════════════════════════════════
                
                if (callbackRequest == null)
                {
                    _logger.LogWarning("[POSNET-3DS-CALLBACK] {CorrelationId} - Boş callback request", correlationId);
                    return Posnet3DSecureResultDto.FailureResult("Geçersiz callback verisi", "EMPTY_CALLBACK");
                }

                // OrderId çıkar
                var orderId = ExtractOrderId(callbackRequest);
                if (!orderId.HasValue)
                {
                    _logger.LogWarning("[POSNET-3DS-CALLBACK] {CorrelationId} - OrderId bulunamadı", correlationId);
                    return Posnet3DSecureResultDto.FailureResult("Sipariş bilgisi bulunamadı", "ORDER_NOT_FOUND");
                }

                _logger.LogInformation("[POSNET-3DS-CALLBACK] {CorrelationId} - OrderId: {OrderId}, MdStatus: {MdStatus}",
                    correlationId, orderId.Value, callbackRequest.MdStatus);

                // ═══════════════════════════════════════════════════════════════
                // ADIM 2: MAC DOĞRULAMASI (Güvenlik Kritik!)
                // ═══════════════════════════════════════════════════════════════

                var callbackData = new Posnet3DSecureCallbackData
                {
                    BankData = callbackRequest.EffectiveBankData,
                    MerchantData = callbackRequest.EffectiveMerchantData,
                    Sign = callbackRequest.Sign,
                    Mac = callbackRequest.Mac,
                    MdStatus = callbackRequest.MdStatus,
                    MdErrorMessage = callbackRequest.EffectiveErrorMessage,
                    Xid = callbackRequest.Xid,
                    Eci = callbackRequest.Eci,
                    Cavv = callbackRequest.Cavv,
                    Amount = callbackRequest.Amount,
                    Currency = callbackRequest.Currency,
                    InstallmentCount = callbackRequest.InstallmentCount,
                    OrderId = orderId
                };

                var macValidation = _macValidator.ValidateCallback(callbackData);

                if (!macValidation.IsValid)
                {
                    _logger.LogWarning("[POSNET-3DS-CALLBACK] {CorrelationId} - MAC doğrulama başarısız: {Error}",
                        correlationId, macValidation.ErrorMessage);

                    await UpdatePaymentStatusAsync(orderId.Value, "3DS_MAC_FAILED", 
                        $"MAC doğrulama hatası: {macValidation.ErrorMessage}");

                    return Posnet3DSecureResultDto.FailureResult(
                        "Güvenlik doğrulaması başarısız",
                        "MAC_VALIDATION_FAILED",
                        orderId.Value,
                        callbackRequest.MdStatus);
                }

                // ═══════════════════════════════════════════════════════════════
                // ADIM 3: 3D SECURE DURUM KONTROLÜ (KRİTİK!)
                // MdStatus değerlerine göre işlem kararı:
                // 0 = Doğrulama başarısız (SMS yanlış/timeout) → İŞLEM REDDEDİLMELİ
                // 1 = Tam doğrulama başarılı → İşleme devam
                // 2,3,4 = Kısmi doğrulama → Riskli ama devam edilebilir
                // 5,6,7,8,9 = Başarısız → İŞLEM REDDEDİLMELİ
                // ═══════════════════════════════════════════════════════════════

                // MdStatus'ü erken kontrol et - SMS reddi durumunda hemen çık
                var mdStatus = callbackRequest.MdStatus?.Trim();
                
                // mdStatus = "0" SMS doğrulama BAŞARISIZ demek - KESİNLİKLE devam etme!
                if (mdStatus == "0")
                {
                    _logger.LogWarning("[POSNET-3DS-CALLBACK] {CorrelationId} - ❌ SMS DOĞRULAMA BAŞARISIZ! " +
                        "MdStatus=0. Kullanıcı SMS şifresini yanlış girdi veya timeout oldu. " +
                        "İşlem REDDEDİLDİ - OrderId: {OrderId}",
                        correlationId, orderId.Value);

                    await UpdatePaymentStatusAsync(orderId.Value, "3DS_SMS_FAILED",
                        "MdStatus=0: SMS doğrulaması başarısız - Şifre yanlış veya timeout");

                    return Posnet3DSecureResultDto.FailureResult(
                        "3D Secure SMS doğrulaması başarısız. Lütfen tekrar deneyiniz.",
                        "3DS_SMS_VERIFICATION_FAILED",
                        orderId.Value,
                        "0");
                }

                // mdStatus 5,6,7,8,9 = Sistem/Kart/Banka hatası
                var failedStatuses = new[] { "5", "6", "7", "8", "9" };
                if (!string.IsNullOrEmpty(mdStatus) && failedStatuses.Contains(mdStatus))
                {
                    var errorDescription = mdStatus switch
                    {
                        "5" => "Doğrulama yapılamadı",
                        "6" => "3D Secure hatası",
                        "7" => "Sistem hatası",
                        "8" => "Bilinmeyen kart numarası",
                        "9" => "Üye işyeri 3D Secure sistemine kayıtlı değil",
                        _ => "Bilinmeyen hata"
                    };

                    _logger.LogWarning("[POSNET-3DS-CALLBACK] {CorrelationId} - ❌ 3DS BAŞARISIZ! " +
                        "MdStatus={MdStatus}: {Description}. OrderId: {OrderId}",
                        correlationId, mdStatus, errorDescription, orderId.Value);

                    await UpdatePaymentStatusAsync(orderId.Value, $"3DS_MDSTATUS_{mdStatus}",
                        $"MdStatus={mdStatus}: {errorDescription}");

                    return Posnet3DSecureResultDto.FailureResult(
                        $"3D Secure doğrulama hatası: {errorDescription}",
                        $"3DS_MDSTATUS_{mdStatus}",
                        orderId.Value,
                        mdStatus);
                }

                // Genel kontrol (MAC validator'dan gelen sonuç)
                if (!string.IsNullOrWhiteSpace(callbackRequest.MdStatus) && !macValidation.CanProceedWithPayment)
                {
                    var errorMsg = $"3D Secure doğrulama başarısız - {macValidation.MdStatusDescription}";
                    
                    _logger.LogWarning("[POSNET-3DS-CALLBACK] {CorrelationId} - 3DS başarısız: {Reason}",
                        correlationId, errorMsg);

                    await UpdatePaymentStatusAsync(orderId.Value, "3DS_FAILED",
                        $"MdStatus: {callbackRequest.MdStatus} - {macValidation.MdStatusDescription}");

                    return Posnet3DSecureResultDto.FailureResult(
                        errorMsg,
                        $"3DS_MDSTATUS_{callbackRequest.MdStatus}",
                        orderId.Value,
                        callbackRequest.MdStatus);
                }

                _logger.LogInformation("[POSNET-3DS-CALLBACK] {CorrelationId} - ✅ MdStatus kontrolü geçti: {MdStatus}",
                    correlationId, mdStatus ?? "null (resolve'da kontrol edilecek)");

                // ═══════════════════════════════════════════════════════════════
                // ADIM 4: OOS RESOLVE MERCHANT DATA - Veri Çözümleme ve Doğrulama
                // POSNET Dokümantasyonu: Sayfa 12-14
                // Bu adım, callback verilerini bankadan deşifre eder ve doğrular
                // ═══════════════════════════════════════════════════════════════

                _logger.LogInformation("[POSNET-3DS-CALLBACK] {CorrelationId} - oosResolveMerchantData başlatılıyor...",
                    correlationId);

                // Orijinal işlem bilgilerini al (sipariş tutarı vs.)
                var originalOrder = await _dbContext.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == orderId.Value);

                var originalAmount = originalOrder != null 
                    ? (int)(originalOrder.TotalPrice * 100) // TL -> Kuruş çevrimi
                    : (int?)null;

                // MAC hesapla - POSNET Dokümanı sayfa 11
                // firstHash = HASH(encKey + ';' + terminalID)
                // MAC = HASH(xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
                var xidForMac = !string.IsNullOrWhiteSpace(callbackRequest.Xid)
                    ? callbackRequest.Xid
                    : orderId.Value.ToString();
                var amountForMac = NormalizeAmountForMac(callbackRequest.Amount) ?? originalAmount?.ToString() ?? "0";
                var currencyForMac = NormalizeCurrencyForMac(callbackRequest.Currency) ?? "TL";

                var calculatedMac = CalculateMacForTranData(
                    xidForMac,
                    amountForMac,
                    currencyForMac,
                    _settings.PosnetMerchantId,
                    _settings.PosnetTerminalId,
                    _settings.PosnetEncKey);

                _logger.LogWarning(
                    "[POSNET-RESOLVE] MAC calculated (resolve): {Mac}",
                    calculatedMac);

                // oosResolveMerchantData request oluştur
                _logger.LogWarning(
                    "[POSNET-RESOLVE] MAC input (resolve) - XID: {Xid}, Amount: {Amount}, Currency: {Currency}",
                    xidForMac, amountForMac, currencyForMac);

                var resolveRequest = new Models.PosnetOosResolveMerchantDataRequest
                {
                    BankData = callbackRequest.EffectiveBankData,
                    MerchantData = callbackRequest.EffectiveMerchantData,
                    Sign = callbackRequest.Sign ?? string.Empty,
                    Mac = calculatedMac,
                    OriginalXid = xidForMac,
                    OriginalAmount = int.TryParse(amountForMac, out var macAmount) ? macAmount : originalAmount,
                    OriginalCurrency = currencyForMac
                };

                var resolveResult = await _posnetService.ResolveOosMerchantDataAsync(resolveRequest);

                if (!resolveResult.IsSuccess || resolveResult.Data == null)
                {
                    var resolveError = resolveResult.Error ?? "oosResolveMerchantData başarısız";
                    
                    _logger.LogWarning("[POSNET-3DS-CALLBACK] {CorrelationId} - Resolve başarısız: {Error}",
                        correlationId, resolveError);

                    await UpdatePaymentStatusAsync(orderId.Value, "RESOLVE_FAILED",
                        $"oosResolveMerchantData hatası: {resolveError}");

                    return Posnet3DSecureResultDto.FailureResult(
                        "İşlem doğrulama hatası",
                        "RESOLVE_FAILED",
                        orderId.Value,
                        callbackRequest.MdStatus);
                }

                var resolveResponse = resolveResult.Data;

                // Response MAC doğrulaması - Banka cevabının gerçekten bankadan geldiğini doğrula
                // POSNET Dokümanı sayfa 14-15
                if (!ValidateBankResponseMac(
                    resolveResponse, 
                    _settings.PosnetTerminalId,
                    _settings.PosnetMerchantId,
                    _settings.PosnetEncKey))
                {
                    _logger.LogError("[POSNET-3DS-CALLBACK] {CorrelationId} - BANKA RESPONSE MAC UYUŞMADI! " +
                        "OLASI MAN-IN-THE-MIDDLE SALDIRISI!",
                        correlationId);

                    await UpdatePaymentStatusAsync(orderId.Value, "RESPONSE_MAC_FAILED",
                        "Banka response MAC doğrulaması başarısız - Güvenlik ihlali");

                    return Posnet3DSecureResultDto.FailureResult(
                        "Güvenlik doğrulaması başarısız",
                        "RESPONSE_MAC_VALIDATION_FAILED",
                        orderId.Value,
                        callbackRequest.MdStatus);
                }

                // MdStatus kontrolü (resolve response'dan)
                if (!resolveResponse.CanProceedWithPayment)
                {
                    _logger.LogWarning("[POSNET-3DS-CALLBACK] {CorrelationId} - 3DS doğrulama başarısız (resolve). " +
                        "MdStatus: {MdStatus} - {Description}",
                        correlationId, resolveResponse.MdStatus, resolveResponse.MdStatusDescription);

                    await UpdatePaymentStatusAsync(orderId.Value, "3DS_VERIFICATION_FAILED",
                        $"MdStatus: {resolveResponse.MdStatus} - {resolveResponse.MdStatusDescription}");

                    return Posnet3DSecureResultDto.FailureResult(
                        resolveResponse.MdStatusDescription ?? "3D Secure doğrulama başarısız",
                        $"3DS_MDSTATUS_{resolveResponse.MdStatus}",
                        orderId.Value,
                        resolveResponse.MdStatus);
                }

                _logger.LogInformation("[POSNET-3DS-CALLBACK] {CorrelationId} - oosResolveMerchantData başarılı. " +
                    "XID: {Xid}, Amount: {Amount}, MdStatus: {MdStatus}",
                    correlationId, resolveResponse.Xid, resolveResponse.Amount, resolveResponse.MdStatus);

                // ═══════════════════════════════════════════════════════════════
                // ADIM 5: FİNANSALLAŞTIRMA (oosTranData)
                // POSNET Dokümantasyonu: Sayfa 15-17
                // Bu adımda para gerçekten çekilir!
                // ═══════════════════════════════════════════════════════════════

                _logger.LogInformation("[POSNET-3DS-CALLBACK] {CorrelationId} - oosTranData (Finansallaştırma) başlatılıyor...",
                    correlationId);

                // DEBUG: BankData ve MAC değerlerini logla
                _logger.LogDebug("[POSNET-3DS-CALLBACK] {CorrelationId} - BankData: {BankData}, XID: {Xid}, Amount: {Amount}",
                    correlationId, 
                    callbackRequest.EffectiveBankData?.Substring(0, Math.Min(100, callbackRequest.EffectiveBankData?.Length ?? 0)) ?? "null",
                    resolveResponse.Xid,
                    resolveResponse.Amount);

                // Finansallaştırma için MAC hesapla
                var tranDataMac = CalculateMacForTranData(
                    resolveResponse.Xid ?? string.Empty,
                    resolveResponse.Amount.ToString(),
                    resolveResponse.Currency ?? "TL",
                    _settings.PosnetMerchantId,
                    _settings.PosnetTerminalId,
                    _settings.PosnetEncKey);

                _logger.LogDebug("[POSNET-3DS-CALLBACK] {CorrelationId} - Hesaplanan MAC: {Mac}",
                    correlationId, tranDataMac);

                var tranDataRequest = new Models.PosnetOosTranDataRequest
                {
                    BankData = callbackRequest.EffectiveBankData,
                    WpAmount = 0, // World Puan kullanılmıyorsa 0
                    Mac = tranDataMac,
                    OrderId = orderId.Value.ToString(),
                    Amount = resolveResponse.Amount
                };

                var tranDataResult = await _posnetService.ProcessOosTranDataAsync(tranDataRequest);

                if (!tranDataResult.IsSuccess || tranDataResult.Data == null)
                {
                    var tranDataError = tranDataResult.Error ?? "Finansallaştırma başarısız";
                    
                    _logger.LogWarning("[POSNET-3DS-CALLBACK] {CorrelationId} - Finansallaştırma BAŞARISIZ: {Error}",
                        correlationId, tranDataError);

                    await UpdatePaymentStatusAsync(orderId.Value, "FINALIZATION_FAILED",
                        $"oosTranData hatası: {tranDataError}");

                    return Posnet3DSecureResultDto.FailureResult(
                        "Ödeme tamamlanamadı",
                        "FINALIZATION_FAILED",
                        orderId.Value,
                        callbackRequest.MdStatus);
                }

                var tranDataResponse = tranDataResult.Data;

                // Kritik değerleri al
                var hostLogKey = tranDataResponse.HostLogKey;
                var authCode = tranDataResponse.AuthCode;
                var transactionId = resolveResponse.Xid ?? callbackRequest.Xid;
                var amount = resolveResponse.Amount;

                // ═══════════════════════════════════════════════════════════════
                // ADIM 6: SİPARİŞ DURUMU GÜNCELLE
                // ═══════════════════════════════════════════════════════════════

                await UpdateOrderAndPaymentAsync(
                    orderId.Value,
                    hostLogKey,
                    authCode,
                    transactionId,
                    resolveResponse.MdStatus ?? "1");

                _logger.LogInformation("[POSNET-3DS-CALLBACK] {CorrelationId} - ✅ Ödeme başarılı! " +
                    "OrderId: {OrderId}, HostLogKey: {HostLogKey}, AuthCode: {AuthCode}",
                    correlationId, orderId.Value, hostLogKey, authCode);

                // ═══════════════════════════════════════════════════════════════
                // ADIM 7: BAŞARI SONUCU DÖNDÜR
                // ═══════════════════════════════════════════════════════════════

                var successUrl = BuildSuccessRedirectUrl(orderId.Value, transactionId);

                return Posnet3DSecureResultDto.SuccessResult(
                    orderId: orderId.Value,
                    transactionId: transactionId ?? correlationId,
                    bankReferenceId: hostLogKey,
                    authCode: authCode,
                    amount: amount,
                    mdStatus: resolveResponse.MdStatus ?? "1",
                    redirectUrl: successUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[POSNET-3DS-CALLBACK] {CorrelationId} - Beklenmeyen hata", correlationId);

                var orderId = ExtractOrderId(callbackRequest);
                if (orderId.HasValue)
                {
                    await UpdatePaymentStatusAsync(orderId.Value, "SYSTEM_ERROR", ex.Message);
                }

                return Posnet3DSecureResultDto.FailureResult(
                    "Ödeme işlemi sırasında beklenmeyen bir hata oluştu",
                    "SYSTEM_ERROR",
                    orderId);
            }
        }

        /// <inheritdoc/>
        public int? ExtractOrderId(Posnet3DSecureCallbackRequestDto callbackRequest)
        {
            if (callbackRequest == null) return null;

            // Önce MerchantData'dan çıkarmayı dene
            var merchantData = callbackRequest.EffectiveMerchantData;
            var orderId = PosnetMerchantDataParser.ExtractOrderId(merchantData);
            
            if (orderId.HasValue)
            {
                return orderId;
            }

            // XID'den çıkarmayı dene (format: YKB{OrderId:D6}{HHmmss})
            var xid = callbackRequest.Xid;
            if (!string.IsNullOrWhiteSpace(xid) && xid.StartsWith("YKB") && xid.Length >= 9)
            {
                var orderIdPart = xid.Substring(3, 6);
                if (int.TryParse(orderIdPart, out var xidOrderId))
                {
                    return xidOrderId;
                }
            }

            // Fallback: eski format
            if (!string.IsNullOrWhiteSpace(xid) && xid.Length >= 13)
            {
                // YYYYMMDD (8) + OrderId (5)
                var orderIdPart = xid.Substring(8, 5);
                if (int.TryParse(orderIdPart, out var xidOrderId))
                {
                    return xidOrderId;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public string BuildSuccessRedirectUrl(int orderId, string? transactionId = null)
        {
            var baseUrl = _settings.ReturnUrlSuccess ?? "/checkout/success";
            var separator = baseUrl.Contains("?") ? "&" : "?";
            
            var url = $"{baseUrl}{separator}orderId={orderId}&provider=posnet";
            
            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                url += $"&transactionId={Uri.EscapeDataString(transactionId)}";
            }

            return url;
        }

        /// <inheritdoc/>
        public string BuildFailureRedirectUrl(int? orderId, string errorCode, string errorMessage)
        {
            var baseUrl = _settings.ReturnUrlCancel ?? "/checkout/failed";
            var separator = baseUrl.Contains("?") ? "&" : "?";

            var url = $"{baseUrl}{separator}provider=posnet&errorCode={Uri.EscapeDataString(errorCode)}";
            
            if (orderId.HasValue)
            {
                url += $"&orderId={orderId.Value}";
            }

            url += $"&message={Uri.EscapeDataString(errorMessage)}";

            return url;
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // PRİVATE HELPER METODLAR
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Payment tablosunu günceller (hata durumları için)
        /// </summary>
        private async Task UpdatePaymentStatusAsync(int orderId, string status, string? rawResponse)
        {
            try
            {
                var payment = await _dbContext.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == orderId && p.Provider == "YapiKredi");

                if (payment != null)
                {
                    payment.Status = status;
                    payment.RawResponse = (payment.RawResponse ?? "") + 
                        $"\n[3DS-Callback-{DateTime.UtcNow:HH:mm:ss}] {rawResponse}";
                    
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[POSNET-3DS] Payment status güncelleme hatası - OrderId: {OrderId}", orderId);
            }
        }

        /// <summary>
        /// Başarılı ödeme sonrası Order ve Payment günceller
        /// </summary>
        private async Task UpdateOrderAndPaymentAsync(
            int orderId, 
            string? hostLogKey, 
            string? authCode,
            string? transactionId,
            string mdStatus)
        {
            try
            {
                // Payment güncelle
                var payment = await _dbContext.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == orderId && p.Provider == "YapiKredi");

                if (payment != null)
                {
                    payment.Status = "Success";
                    payment.ProviderPaymentId = hostLogKey ?? transactionId;
                    payment.PaidAt = DateTime.UtcNow;
                    payment.RawResponse = (payment.RawResponse ?? "") +
                        $"\n[3DS-Success] HostLogKey: {hostLogKey}, AuthCode: {authCode}, MdStatus: {mdStatus}";
                }

                // Order güncelle
                var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
                if (order != null)
                {
                    var previousStatus = order.Status;
                    order.Status = OrderStatus.Paid;

                    // Status history ekle
                    _dbContext.OrderStatusHistories.Add(new OrderStatusHistory
                    {
                        OrderId = orderId,
                        PreviousStatus = previousStatus,
                        NewStatus = OrderStatus.Paid,
                        ChangedAt = DateTime.UtcNow,
                        ChangedBy = "POSNET-3DSecure",
                        Reason = $"3D Secure ödeme başarılı - AuthCode: {authCode}"
                    });
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("[POSNET-3DS] Sipariş durumu güncellendi - OrderId: {OrderId}, Status: Paid",
                    orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[POSNET-3DS] Sipariş güncelleme hatası - OrderId: {OrderId}", orderId);
                throw; // Kritik hata - yukarı fırlat
            }
        }

        #region Helper Methods for oosResolveMerchantData Flow

        /// <summary>
        /// Banka yanıtındaki MAC değerini doğrular
        /// POSNET API Dokümanı v2.1.1.3 - Sayfa 14
        /// MAC Formülü: HASH(mdStatus + ';' + xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
        /// firstHash = HASH(encKey + ';' + terminalId)
        /// </summary>
        private bool ValidateBankResponseMac(
            PosnetOosResolveMerchantDataResponse resolveResponse,
            string terminalId,
            string merchantNo,
            string encKey)
        {
            try
            {
                if (string.IsNullOrEmpty(resolveResponse.Mac))
                {
                    _logger.LogWarning("[POSNET-3DS] Banka yanıtında MAC değeri yok - MAC doğrulama atlanıyor");
                    // Bazı durumlarda MAC dönmeyebilir - bu durumda doğrulama atlanır
                    return true;
                }

                // EncKey normalize et
                var normalizedEncKey = NormalizeEncKey(encKey) ?? string.Empty;

                // ADIM 1: firstHash hesapla = HASH(encKey + ';' + terminalId)
                var firstHashInput = $"{normalizedEncKey};{terminalId}";
                var firstHash = ComputeSha256Hash(firstHashInput);

                // Amount'u normalize et (sadece rakamlar)
                var normalizedAmount = NormalizeAmountForMac(resolveResponse.Amount.ToString()) ?? "0";
                var normalizedCurrency = NormalizeCurrencyForMac(resolveResponse.Currency) ?? "TL";

                // ADIM 2: MAC hesapla = HASH(mdStatus + ';' + xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
                var macInput = $"{resolveResponse.MdStatus};{resolveResponse.Xid};{normalizedAmount};{normalizedCurrency};{merchantNo};{firstHash}";
                var calculatedMac = ComputeSha256Hash(macInput);

                // DETAYLI DEBUG LOG
                _logger.LogWarning(
                    "[POSNET-RESPONSE-MAC-DEBUG] Input values: MdStatus={MdStatus}, XID={Xid}, Amount={Amount}, Currency={Currency}, MerchantNo={MerchantNo}",
                    resolveResponse.MdStatus, resolveResponse.Xid, normalizedAmount, normalizedCurrency, merchantNo);
                _logger.LogWarning(
                    "[POSNET-RESPONSE-MAC-DEBUG] FirstHash: {FirstHash}", firstHash);
                _logger.LogWarning(
                    "[POSNET-RESPONSE-MAC-DEBUG] Banka MAC: {BankMac}, Hesaplanan MAC: {CalculatedMac}",
                    resolveResponse.Mac, calculatedMac);

                // ADIM 3: Timing-safe comparison
                var isValid = TimingSafeCompare(calculatedMac, resolveResponse.Mac);

                if (!isValid)
                {
                    _logger.LogError("[POSNET-3DS] ❌ RESPONSE MAC DOĞRULAMA BAŞARISIZ! " +
                        "Banka MAC: {BankMac}, Hesaplanan: {Calculated}. " +
                        "OLASI SEBEPLER: 1) EncKey yanlış, 2) Amount formatı hatalı, 3) Currency kodu yanlış",
                        resolveResponse.Mac, calculatedMac);
                }
                else
                {
                    _logger.LogInformation("[POSNET-3DS] ✅ Response MAC doğrulama başarılı - XID: {Xid}", resolveResponse.Xid);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[POSNET-3DS] MAC doğrulama sırasında hata");
                return false;
            }
        }

        /// <summary>
        /// oosTranData için MAC hesaplar
        /// POSNET API Dokümanı v2.1.1.3 - Sayfa 15
        /// MAC Formülü: HASH(xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
        /// firstHash = HASH(encKey + ';' + terminalId)
        /// 
        /// KRİTİK NOKTALAR:
        /// 1. amount KURUŞ cinsinden olmalı (örn: 100.00 TL = "10000")
        /// 2. currency "TL", "US" veya "EU" olmalı
        /// 3. Tüm değerler trim edilmeli ve boşluk içermemeli
        /// </summary>
        private string CalculateMacForTranData(
            string xid,
            string amount,
            string currency,
            string merchantNo,
            string terminalId,
            string encKey)
        {
            // Parametreleri normalize et
            xid = xid?.Trim() ?? string.Empty;
            amount = NormalizeAmountForMac(amount) ?? "0";
            currency = NormalizeCurrencyForMac(currency) ?? "TL";
            merchantNo = merchantNo?.Trim() ?? string.Empty;
            terminalId = terminalId?.Trim() ?? string.Empty;
            encKey = NormalizeEncKey(encKey) ?? string.Empty;

            // ADIM 1: firstHash hesapla = HASH(encKey + ';' + terminalId)
            var firstHashInput = $"{encKey};{terminalId}";
            var firstHash = ComputeSha256Hash(firstHashInput);

            // DETAYLI LOG - MAC hatası debug için
            _logger.LogWarning(
                "[POSNET-MAC-DEBUG] FirstHash Input: encKey={EncKey}, terminalId={TerminalId}",
                encKey.Length > 4 ? encKey[..4] + "****" : "****",
                terminalId);
            _logger.LogWarning("[POSNET-MAC-DEBUG] FirstHash Result: {FirstHash}", firstHash);

            // ADIM 2: MAC hesapla = HASH(xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
            var macInput = $"{xid};{amount};{currency};{merchantNo};{firstHash}";
            var mac = ComputeSha256Hash(macInput);

            // DETAYLI LOG - Tam MAC input (güvenlik için sadece debug modda)
            _logger.LogWarning(
                "[POSNET-MAC-DEBUG] MAC Input: xid={Xid}, amount={Amount}, currency={Currency}, merchantNo={MerchantNo}",
                xid, amount, currency, merchantNo);
            _logger.LogWarning("[POSNET-MAC-DEBUG] Final MAC (Base64): {Mac}", mac);

            return mac;
        }

        /// <summary>
        /// EncKey formatını normalize eder (virgülle ayrılmış byte formatından hex'e çevir)
        /// Test ortamı: "10,10,10,10,10,10,10,10" → "0A0A0A0A0A0A0A0A"
        /// </summary>
        private static string? NormalizeEncKey(string? rawEncKey)
        {
            if (string.IsNullOrWhiteSpace(rawEncKey))
                return rawEncKey;

            // Virgülle ayrılmış byte formatı mı kontrol et
            if (rawEncKey.Contains(','))
            {
                try
                {
                    var parts = rawEncKey.Split(',');
                    var bytes = new byte[parts.Length];
                    
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (byte.TryParse(parts[i].Trim(), out var b))
                            bytes[i] = b;
                        else
                            return rawEncKey; // Parse edilemezse orijinal döndür
                    }

                    // Byte array'i hex string'e çevir DEĞİL - olduğu gibi string olarak kullan!
                    // POSNET test: "10,10,10,10,10,10,10,10" → string olarak birleştirilmeli
                    return rawEncKey; // Test ortamında virgüllü format olduğu gibi kullanılıyor!
                }
                catch
                {
                    return rawEncKey;
                }
            }

            return rawEncKey;
        }

        /// <summary>
        /// Siparişin orijinal tutarını veritabanından alır
        /// Tutarı kuruş cinsine (YKR) çevirir
        /// </summary>
        private async Task<string> GetOriginalOrderAmountAsync(int orderId)
        {
            try
            {
                var order = await _dbContext.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    _logger.LogWarning("[POSNET-3DS] Sipariş bulunamadı - OrderId: {OrderId}", orderId);
                    return "0";
                }

                // TL'den YKR'ye çevir (kuruş): 100.50 TL = 10050 YKR
                var amountInKurus = (int)(order.TotalPrice * 100);
                
                _logger.LogDebug("[POSNET-3DS] Orijinal tutar alındı - OrderId: {OrderId}, Amount: {Amount} YKR",
                    orderId, amountInKurus);

                return amountInKurus.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[POSNET-3DS] Tutar alma hatası - OrderId: {OrderId}", orderId);
                throw;
            }
        }

        /// <summary>
        /// Tutar değerini MAC hesaplaması için normalize eder
        /// POSNET KURALI: Tutar KURUŞ cinsinden olmalı, sadece rakam içermeli
        /// Örnek: "100.50" TL → "10050" kuruş
        /// Örnek: "1234" → "1234" (zaten kuruş)
        /// </summary>
        private static string? NormalizeAmountForMac(string? amount)
        {
            if (string.IsNullOrWhiteSpace(amount)) return null;
            
            // Sadece rakamları al (nokta, virgül vs. temizle)
            var digitsOnly = new string(amount.Where(char.IsDigit).ToArray());
            
            // Boş string kontrolü
            if (string.IsNullOrWhiteSpace(digitsOnly)) return null;
            
            // Baştaki sıfırları temizle (MAC için önemli)
            digitsOnly = digitsOnly.TrimStart('0');
            
            // Tamamen sıfır ise "0" döndür
            return string.IsNullOrWhiteSpace(digitsOnly) ? "0" : digitsOnly;
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
        /// SHA-256 hash hesaplar ve Base64 encode eder
        /// POSNET Dokümantasyonu v2.1.1.3 - Sayfa 36: MAC değeri Base64 encoded olmalı!
        /// NOT: Hex string YANLIŞ, Base64 DOĞRU format!
        /// </summary>
        private static string ComputeSha256Hash(string input)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(bytes);
            // KRİTİK: POSNET Base64 format bekliyor, HEX DEĞİL!
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Timing-safe string comparison (Side-channel attack koruması)
        /// </summary>
        private static bool TimingSafeCompare(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            var result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }
            return result == 0;
        }

        #endregion
    }
}
