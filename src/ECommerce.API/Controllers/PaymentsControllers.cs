using ECommerce.Core.DTOs.Payment;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Stripe;
using ECommerce.Infrastructure.Config;
using Microsoft.Extensions.Options;
using ECommerce.Infrastructure.Services.Payment;
using ECommerce.Infrastructure.Services.Payment.Posnet;
using ECommerce.Infrastructure.Services.Payment.Posnet.Models;
using ECommerce.Infrastructure.Services.Payment.Posnet.Security;
using Polly;
using System;
using System.Text;
using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using ECommerce.Entities.Enums;
using ECommerce.Business.Services.Managers;
using Microsoft.AspNetCore.Http;
using ECommerce.Entities.Concrete;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly PaymentManager _paymentManager;
        private readonly IOptions<PaymentSettings> _paymentOptions;
        private readonly ECommerceDbContext _db;
        private readonly IPosnet3DSecureCallbackHandler? _posnet3DHandler;
        private readonly IPosnetPaymentService? _posnetService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            PaymentManager paymentManager, 
            IOptions<PaymentSettings> paymentOptions, 
            ECommerceDbContext db,
            ILogger<PaymentsController> logger,
            IPosnet3DSecureCallbackHandler? posnet3DHandler = null,
            IPosnetPaymentService? posnetService = null)
        {
            _paymentManager = paymentManager;
            _paymentOptions = paymentOptions;
            _db = db;
            _logger = logger;
            _posnet3DHandler = posnet3DHandler;
            _posnetService = posnetService;
        }

        /// <summary>
        /// Ödeme başlat
        /// </summary>
        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentCreateDto dto)
        {
            var result = await _paymentManager.ProcessPaymentAsync(dto.OrderId, dto.Amount);
            if (!result)
                return BadRequest(new { message = "Ödeme başarısız" });

            return Ok(new { message = "Ödeme başarılı", orderId = dto.OrderId, amount = dto.Amount });
        }

        /// <summary>
        /// Ödeme durumunu sorgula
        /// </summary>
        [HttpGet("status/{paymentId}")]
        public async Task<IActionResult> CheckPaymentStatus(string paymentId)
        {
            var status = await _paymentManager.CheckPaymentStatusAsync(paymentId);
            return Ok(new { paymentId, status });
        }

        /// <summary>
        /// Hosted checkout başlatır ve yönlendirme bilgisi döner (Stripe Checkout / Iyzico Form).
        /// Frontend bu endpoint'e PaymentMethod alanı ile birlikte istek atar.
        /// </summary>
        [HttpPost("initiate")]
        public async Task<IActionResult> Initiate([FromBody] PaymentCreateDto dto)
        {
            var init = await _paymentManager.InitiateAsync(dto);
            return Ok(init);
        }

        /// <summary>
        /// Geriye dönük uyumluluk için eski /api/payments/init endpoint'i.
        /// PaymentMethod boş ise config'teki varsayılan sağlayıcı kullanılır.
        /// </summary>
        [HttpPost("init")]
        public async Task<IActionResult> InitiateLegacy([FromBody] PaymentCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.PaymentMethod))
            {
                // Eski frontend'ler için varsayılan provider ile devam et
                var initDefault = await _paymentManager.InitiateAsync(dto.OrderId, dto.Amount, dto.Currency ?? "TRY");
                return Ok(initDefault);
            }

            return await Initiate(dto);
        }

        /// <summary>
        /// Iyzipay callback endpoint (POST). Iyzipay token'ı ile sonucu doğrular.
        /// </summary>
        [HttpPost("iyzico/callback")]
        [HttpPost("callback/iyzico")]
        public async Task<IActionResult> IyzicoCallback([FromForm] string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return BadRequest();

            var settings = _paymentOptions.Value;
            // Validate signature/header for iyzico
            var valid = IyzicoWebhookValidator.Validate(Request, settings);
            if (!valid) return BadRequest("Invalid signature");

            var policy = Policy.Handle<Exception>().WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

            var result = await policy.ExecuteAsync<IActionResult>(async () =>
            {
                var status = await _paymentManager.GetPaymentStatusAsync(token);
                if (status == PaymentStatus.Paid)
                {
                    var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Provider == "iyzico" && p.ProviderPaymentId == token);
                    if (payment != null)
                    {
                        payment.Status = "Success";
                        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId);
                        if (order != null)
                        {
                            var previous = order.Status;
                            order.Status = OrderStatus.Paid;
                            _db.OrderStatusHistories.Add(new OrderStatusHistory
                            {
                                OrderId = order.Id,
                                PreviousStatus = previous,
                                NewStatus = OrderStatus.Paid,
                                ChangedBy = HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? HttpContext?.User?.Identity?.Name,
                                ChangedAt = DateTime.UtcNow
                            });
                        }
                        payment.RawResponse = (payment.RawResponse ?? string.Empty) + "\n[iyzico-callback]";
                        await _db.SaveChangesAsync();

                        var successUrl = settings.ReturnUrlSuccess;
                        if (!string.IsNullOrWhiteSpace(successUrl))
                        {
                            var sep = successUrl.Contains("?") ? "&" : "?";
                            return (IActionResult)Redirect(successUrl + sep + $"orderId={payment.OrderId}");
                        }
                    }
                }
                return Ok(new { token });
            });

            return result;
        }

        /// <summary>
        /// İade işlemleri için placeholder endpoint.
        /// Şu an için yalnızca gelecekteki geliştirmeler için yer tutucu olarak tanımlı.
        /// </summary>
        [HttpPost("refund")]
        public IActionResult Refund([FromBody] PaymentRefundRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.PaymentId))
            {
                return BadRequest(new { message = "PaymentId zorunludur." });
            }

            // Şimdilik gerçek iade entegrasyonu yok, sadece endpoint hazır.
            return StatusCode(StatusCodes.Status501NotImplemented, new
            {
                message = "İade işlemleri bu ortamda henüz aktif değil.",
                paymentId = dto.PaymentId
            });
        }

        /// <summary>
        /// Stripe webhook endpoint (Checkout Session / PaymentIntent events)
        /// </summary>
        [HttpPost("stripe/webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new System.IO.StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var sigHeader = Request.Headers["Stripe-Signature"].ToString();
            var secret = _paymentOptions.Value.StripeWebhookSecret;
            if (string.IsNullOrWhiteSpace(secret)) return BadRequest("Webhook secret not configured");

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, sigHeader, secret);
            }
            catch
            {
                return BadRequest();
            }

            var policy = Policy.Handle<Exception>().WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

            await policy.ExecuteAsync(async () =>
            {
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = (Stripe.Checkout.Session)stripeEvent.Data.Object;
                    var orderIdMeta = session.Metadata != null && session.Metadata.ContainsKey("orderId") ? session.Metadata["orderId"] : null;
                    if (int.TryParse(orderIdMeta, out var orderId))
                    {
                        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
                        if (order != null)
                        {
                            var previous = order.Status;
                            order.Status = OrderStatus.Paid;
                            _db.OrderStatusHistories.Add(new OrderStatusHistory
                            {
                                OrderId = order.Id,
                                PreviousStatus = previous,
                                NewStatus = OrderStatus.Paid,
                                ChangedBy = HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? HttpContext?.User?.Identity?.Name,
                                ChangedAt = DateTime.UtcNow
                            });
                            await _db.SaveChangesAsync();
                        }
                        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.ProviderPaymentId == session.Id);
                        if (payment != null)
                        {
                            payment.Status = "Success";
                            payment.RawResponse = json;
                            await _db.SaveChangesAsync();
                        }
                    }
                }

                if (stripeEvent.Type == "charge.dispute.created" || stripeEvent.Type == "charge.dispute.updated")
                {
                    try
                    {
                        var charge = stripeEvent.Data.Object as Stripe.Charge;
                        string providerPaymentId = charge?.PaymentIntent?.ToString() ?? charge?.Id ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(providerPaymentId))
                        {
                            // Avoid complex expression tree by querying in two steps
                            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.ProviderPaymentId == providerPaymentId);
                            var chargeId = charge?.Id;
                            if (payment == null && !string.IsNullOrWhiteSpace(chargeId))
                            {
                                payment = await _db.Payments.FirstOrDefaultAsync(p => p.ProviderPaymentId == chargeId);
                            }

                            if (payment != null)
                            {
                                payment.Status = "Chargeback";
                                payment.RawResponse = json;
                                var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId);
                                if (order != null)
                                {
                                    var previous = order.Status;
                                    order.Status = OrderStatus.ChargebackPending;
                                    _db.OrderStatusHistories.Add(new OrderStatusHistory
                                    {
                                        OrderId = order.Id,
                                        PreviousStatus = previous,
                                        NewStatus = OrderStatus.ChargebackPending,
                                        ChangedBy = HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? HttpContext?.User?.Identity?.Name,
                                        Reason = "Chargeback/Dispute",
                                        ChangedAt = DateTime.UtcNow
                                    });
                                }
                                await _db.SaveChangesAsync();
                            }
                        }
                    }
                    catch { }
                }
            });

            return Ok();
        }

        /// <summary>
        /// PayTR callback endpoint - Ödeme sonucu bildirim
        /// </summary>
        [HttpPost("paytr/callback")]
        [HttpPost("callback/paytr")]
        public async Task<IActionResult> PayTRCallback(
            [FromForm] string merchant_oid,
            [FromForm] string status,
            [FromForm] string total_amount,
            [FromForm] string hash,
            [FromForm] string failed_reason_code = "",
            [FromForm] string failed_reason_msg = "")
        {
            try
            {
                var settings = _paymentOptions.Value;
                
                // Hash doğrulama
                var merchantKey = settings.PayTRSecretKey ?? "";
                var merchantSalt = Environment.GetEnvironmentVariable("PAYTR_MERCHANT_SALT") ?? "";
                
                var hashStr = string.Concat(merchant_oid, merchantSalt, status, total_amount);
                using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(merchantKey));
                var expectedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(hashStr)));
                
                if (!string.Equals(hash, expectedHash, StringComparison.Ordinal))
                {
                    return Content("PAYTR notification failed: invalid hash");
                }

                // Order ID'yi merchantOid'den çıkar (ORDER_{id}_{ticks})
                var parts = merchant_oid?.Split('_') ?? Array.Empty<string>();
                if (parts.Length < 2 || !int.TryParse(parts[1], out var orderId))
                {
                    return Content("PAYTR notification failed: invalid merchant_oid");
                }

                var payment = await _db.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == orderId && p.Provider == "PayTR");

                if (payment != null)
                {
                    if (status == "success")
                    {
                        payment.Status = "Success";
                        payment.PaidAt = DateTime.UtcNow;
                        payment.RawResponse = (payment.RawResponse ?? "") + $"\n[PayTR Callback Success] {DateTime.UtcNow}";

                        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
                        if (order != null)
                        {
                            var previousStatus = order.Status;
                            order.Status = OrderStatus.Paid;
                            
                            _db.OrderStatusHistories.Add(new OrderStatusHistory
                            {
                                OrderId = order.Id,
                                PreviousStatus = previousStatus,
                                NewStatus = OrderStatus.Paid,
                                ChangedAt = DateTime.UtcNow,
                                ChangedBy = "PayTR Callback"
                            });
                        }
                    }
                    else
                    {
                        payment.Status = "Failed";
                        payment.RawResponse = (payment.RawResponse ?? "") + 
                            $"\n[PayTR Callback Failed] {DateTime.UtcNow} - Code: {failed_reason_code}, Msg: {failed_reason_msg}";
                    }

                    await _db.SaveChangesAsync();
                }

                // PayTR'a başarılı yanıt
                return Content("OK");
            }
            catch (Exception ex)
            {
                return Content($"PAYTR notification failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Iyzipay notification endpoint for refund/chargeback callbacks (minimal)
        /// </summary>
        [HttpPost("iyzico/notification")]
        public async Task<IActionResult> IyzicoNotification()
        {
            var settings = _paymentOptions.Value;
            var valid = IyzicoWebhookValidator.Validate(Request, settings);
            if (!valid) return BadRequest("Invalid signature");

            // Read raw body
            Request.Body.Position = 0;
            using var sr = new System.IO.StreamReader(Request.Body, leaveOpen: true);
            var body = await sr.ReadToEndAsync();
            Request.Body.Position = 0;

            // Very small heuristic: if body contains 'refund' or 'chargeback', mark accordingly
            if (body.IndexOf("refund", StringComparison.OrdinalIgnoreCase) >= 0 || body.IndexOf("chargeback", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // attempt to extract token/payment id
                string token = string.Empty;
                if (Request.HasFormContentType && Request.Form.ContainsKey("token")) token = Request.Form["token"].ToString();

                if (!string.IsNullOrWhiteSpace(token))
                {
                    var payment = await _db.Payments.FirstOrDefaultAsync(p => p.ProviderPaymentId == token && p.Provider == "iyzico");
                    if (payment != null)
                    {
                        payment.Status = "Chargeback";
                        payment.RawResponse = (payment.RawResponse ?? string.Empty) + "\n[iyzico-notification] " + body;
                        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId);
                        if (order != null) order.Status = OrderStatus.ChargebackPending;
                        await _db.SaveChangesAsync();
                    }
                }
            }

            return Ok();
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // YAPI KREDİ POSNET 3D SECURE ENDPOINT'LERİ
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// POSNET 3D Secure ödeme başlatma
        /// Frontend kart bilgilerini gönderir, 3D Secure form verileri döner
        /// </summary>
        [HttpPost("posnet/3dsecure/initiate")]
        public async Task<IActionResult> PosnetInitiate3DSecure([FromBody] Posnet3DSecureInitiateRequestDto request)
        {
            if (_posnetService == null)
            {
                return StatusCode(503, new { message = "POSNET servisi yapılandırılmamış" });
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                _logger.LogError("[POSNET-3DS] Validation hatası: {Errors}", errors);
                return BadRequest(new { 
                    message = "Geçersiz kart bilgileri",
                    errors = ModelState 
                });
            }

            try
            {
                _logger.LogInformation("[POSNET-3DS] 3D Secure başlatılıyor - OrderId: {OrderId}, Amount: {Amount}",
                    request.OrderId, request.Amount);

                // ExpireDate formatı: YYMM
                var expireDate = $"{request.ExpireYear}{request.ExpireMonth}";

                // 3D Secure başlat
                var result = await _posnetService.Initiate3DSecureAsync(
                    orderId: request.OrderId,
                    cardNumber: request.CardNumber,
                    expireDate: expireDate,
                    cvv: request.Cvv,
                    installment: request.InstallmentCount);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("[POSNET-3DS] 3D Secure başlatma başarısız - OrderId: {OrderId}, Error: {Error}",
                        request.OrderId, result.Error);

                    return BadRequest(Posnet3DSecureInitiateResponseDto.FailureResponse(
                        result.Error ?? "3D Secure başlatılamadı",
                        result.ErrorCode.ToString()));
                }

                // Başarılı - Form verileri döndür
                var oosData = result.Data;
                _logger.LogInformation("[POSNET-3DS] 3D Secure başarıyla başlatıldı - OrderId: {OrderId}, XID: {Xid}, Data1: {Data1Exists}",
                    request.OrderId, oosData?.OrderId, !string.IsNullOrEmpty(oosData?.Data1));

                // Form data oluştur - POSNET OOS response verilerini dahil et
                var settings = _paymentOptions.Value;
                var formData = new Posnet3DSecureFormData
                {
                    ActionUrl = settings.Posnet3DServiceUrl,
                    MerchantId = settings.PosnetMerchantId,
                    PosnetId = settings.PosnetId,
                    Xid = oosData?.OrderId ?? request.OrderId.ToString(),
                    Amount = ((int)(request.Amount * 100)).ToString(), // YKr formatı
                    Currency = "TL", // 3D Secure dokümanı: TL/US/EU
                    InstallmentCount = request.InstallmentCount > 1 ? request.InstallmentCount.ToString("D2") : "00",
                    TranType = "Sale",
                    ReturnUrl = settings.PosnetCallbackUrl ?? $"{Request.Scheme}://{Request.Host}/api/payments/posnet/3dsecure/callback",
                    OpenNewWindow = "0",
                    // OOS response'dan gelen kritik veriler - Banka formu için gerekli
                    Data1 = oosData?.Data1,
                    Data2 = oosData?.Data2,
                    Sign = oosData?.Sign
                };

                // DEBUG: OOS response detaylarını logla
                var actionUrl = settings.Posnet3DServiceUrl ?? "https://setmpos.ykb.com/3DSWebService/YKBPaymentService";
                _logger.LogWarning(
                    "[POSNET-3DS-DEBUG] Form Action URL: {ActionUrl}, Data1 length: {Data1Len}, Data2 length: {Data2Len}",
                    actionUrl, oosData?.Data1?.Length ?? 0, oosData?.Data2?.Length ?? 0);

                // threeDSecureHtml oluştur (auto-submit form) - POSNET 3sapi formatı
                var threeDSecureHtml = oosData?.GenerateAutoSubmitForm(
                    actionUrl,
                    settings.PosnetMerchantId,
                    settings.PosnetId,
                    settings.PosnetCallbackUrl ?? $"{Request.Scheme}://{Request.Host}/api/payments/posnet/3dsecure/callback",
                    lang: "tr",
                    openANewWindow: "0",
                    url: "");

                // ÖNEMLI: redirectUrl kullanma - frontend bunu window.location.href yapar
                // Form POST yapması için sadece threeDSecureHtml kullan
                return Ok(new Posnet3DSecureInitiateResponseDto
                {
                    Success = true,
                    RedirectUrl = null, // NULL - form POST kullan, direk redirect YAPMA
                    FormData = formData,
                    TransactionId = oosData?.OrderId ?? request.OrderId.ToString(),
                    OrderId = request.OrderId,
                    RequiresRedirect = oosData?.RequiresRedirect ?? true,
                    ThreeDSecureHtml = threeDSecureHtml
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[POSNET-3DS] 3D Secure başlatma hatası - OrderId: {OrderId}", request.OrderId);
                return StatusCode(500, new { message = "Ödeme başlatılırken bir hata oluştu" });
            }
        }

        /// <summary>
        /// POSNET 3D Secure callback endpoint
        /// Banka, 3D Secure sonrasında bu endpoint'e POST veya GET yapar
        /// </summary>
        [HttpPost("posnet/3dsecure/callback")]
        [HttpPost("posnet/3d-callback")]
        [HttpPost("callback/posnet")]
        [HttpGet("posnet/3dsecure/callback")]
        [HttpGet("posnet/3d-callback")]
        [HttpGet("callback/posnet")]
        public async Task<IActionResult> Posnet3DSecureCallback()
        {
            if (_posnet3DHandler == null)
            {
                _logger.LogError("[POSNET-3DS-CALLBACK] Handler yapılandırılmamış!");
                return BadRequest("POSNET 3D Secure handler not configured");
            }

            // POST veya GET'ten parametreleri al
            Posnet3DSecureCallbackRequestDto callbackRequest;
            
            if (HttpContext.Request.Method == "POST" && HttpContext.Request.HasFormContentType)
            {
                // POST - Form verilerini oku
                var form = await HttpContext.Request.ReadFormAsync();
                
                // DEBUG: Tüm form alanlarını logla
                _logger.LogWarning("[POSNET-3DS-CALLBACK-FORM-DUMP] Tüm form alanları:");
                foreach (var key in form.Keys)
                {
                    var value = form[key].FirstOrDefault() ?? "";
                    _logger.LogWarning("[POSNET-3DS-CALLBACK-FORM] {Key} = {Value} (Length: {Length})", 
                        key, 
                        value.Length > 50 ? value.Substring(0, 50) + "..." : value, 
                        value.Length);
                }
                
                string? GetFormValue(string key)
                {
                    return form.TryGetValue(key, out var value) ? value.FirstOrDefault() : null;
                }

                string? GetFormValueAny(params string[] keys)
                {
                    foreach (var key in keys)
                    {
                        var value = GetFormValue(key);
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }

                    return null;
                }

                callbackRequest = new Posnet3DSecureCallbackRequestDto
                {
                    BankPacket = GetFormValueAny("BankPacket", "bankPacket"),
                    BankData = GetFormValueAny("BankData", "bankData"),
                    MerchantPacket = GetFormValueAny("MerchantPacket", "merchantPacket"),
                    MerchantData = GetFormValueAny("MerchantData", "merchantData"),
                    Sign = GetFormValueAny("Sign", "sign", "Digest", "digest"),
                    Mac = GetFormValueAny("Mac", "mac"),
                    MdStatus = GetFormValueAny("MdStatus", "mdStatus", "MDStatus"),
                    Xid = GetFormValueAny("Xid", "xid", "XID"),
                    Amount = GetFormValueAny("Amount", "amount"),
                    Currency = GetFormValueAny("Currency", "currency"),
                    InstallmentCount = GetFormValueAny("InstallmentCount", "installmentCount"),
                    Eci = GetFormValueAny("Eci", "eci"),
                    Cavv = GetFormValueAny("Cavv", "cavv")
                };
            }
            else
            {
                // GET - Query string'den oku
                var query = HttpContext.Request.Query;
                callbackRequest = new Posnet3DSecureCallbackRequestDto
                {
                    BankPacket = query["BankPacket"].FirstOrDefault(),
                    BankData = query["BankData"].FirstOrDefault(),
                    MerchantPacket = query["MerchantPacket"].FirstOrDefault(),
                    MerchantData = query["MerchantData"].FirstOrDefault(),
                    Sign = query["Sign"].FirstOrDefault(),
                    Mac = query["Mac"].FirstOrDefault(),
                    MdStatus = query["MdStatus"].FirstOrDefault(),
                    Xid = query["Xid"].FirstOrDefault()
                };
            }

            _logger.LogInformation("[POSNET-3DS-CALLBACK] Callback alındı - Method: {Method}, XID: {Xid}, MdStatus: {MdStatus}",
                HttpContext.Request.Method, callbackRequest?.Xid, callbackRequest?.MdStatus);

            // DEBUG: Callback parametrelerini logla
            _logger.LogWarning("[POSNET-3DS-CALLBACK-DATA] " +
                "BankPacket Length: {BankPacketLen}, BankData Length: {BankDataLen}, " +
                "MerchantPacket Length: {MerchantPacketLen}, MerchantData Length: {MerchantDataLen}, " +
                "EffectiveBankData Length: {EffectiveBankDataLen}",
                callbackRequest?.BankPacket?.Length ?? 0,
                callbackRequest?.BankData?.Length ?? 0,
                callbackRequest?.MerchantPacket?.Length ?? 0,
                callbackRequest?.MerchantData?.Length ?? 0,
                callbackRequest?.EffectiveBankData?.Length ?? 0);
            
            _logger.LogWarning("[POSNET-3DS-CALLBACK-DATA-FULL] " +
                "BankPacket: {BankPacket}, BankData: {BankData}",
                callbackRequest?.BankPacket ?? "NULL",
                callbackRequest?.BankData ?? "NULL");

            try
            {
                // Callback'i işle
                var result = await _posnet3DHandler.HandleCallbackAsync(callbackRequest!);

                if (result.Success)
                {
                    // Başarılı - Success sayfasına yönlendir
                    _logger.LogInformation("[POSNET-3DS-CALLBACK] ✅ Ödeme başarılı - OrderId: {OrderId}",
                        result.OrderId);

                    var successUrl = result.RedirectUrl ?? 
                        _posnet3DHandler.BuildSuccessRedirectUrl(result.OrderId ?? 0, result.TransactionId);

                    // HTML redirect sayfası döndür (Banka formdan geldiği için)
                    return Content(GenerateRedirectHtml(
                        isSuccess: true,
                        redirectUrl: successUrl,
                        message: "Ödemeniz başarıyla tamamlandı",
                        orderId: result.OrderId,
                        transactionId: result.TransactionId), "text/html");
                }
                else
                {
                    // Başarısız - Hata sayfasına yönlendir
                    _logger.LogWarning("[POSNET-3DS-CALLBACK] ❌ Ödeme başarısız - OrderId: {OrderId}, Error: {Error}",
                        result.OrderId, result.Message);

                    var failUrl = _posnet3DHandler.BuildFailureRedirectUrl(
                        result.OrderId, 
                        result.ErrorCode ?? "UNKNOWN", 
                        result.Message);

                    return Content(GenerateRedirectHtml(
                        isSuccess: false,
                        redirectUrl: failUrl,
                        message: result.Message,
                        orderId: result.OrderId,
                        errorCode: result.ErrorCode), "text/html");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[POSNET-3DS-CALLBACK] Beklenmeyen hata");

                var failUrl = _paymentOptions.Value.ReturnUrlCancel ?? "/checkout/failed";
                return Content(GenerateRedirectHtml(
                    isSuccess: false,
                    redirectUrl: failUrl + "?error=system",
                    message: "Ödeme işlemi sırasında beklenmeyen bir hata oluştu"), "text/html");
            }
        }

        /// <summary>
        /// POSNET 3D Secure başarı sayfası (opsiyonel - frontend'de de işlenebilir)
        /// </summary>
        [HttpGet("posnet/3dsecure/success")]
        public IActionResult Posnet3DSecureSuccess([FromQuery] int orderId, [FromQuery] string? transactionId)
        {
            _logger.LogInformation("[POSNET-3DS-SUCCESS] Success sayfası - OrderId: {OrderId}", orderId);

            var settings = _paymentOptions.Value;
            var redirectUrl = settings.ReturnUrlSuccess ?? "/checkout/success";
            var separator = redirectUrl.Contains("?") ? "&" : "?";

            return Redirect($"{redirectUrl}{separator}orderId={orderId}&provider=posnet&status=success");
        }

        /// <summary>
        /// POSNET 3D Secure hata sayfası (opsiyonel - frontend'de de işlenebilir)
        /// </summary>
        [HttpGet("posnet/3dsecure/fail")]
        public IActionResult Posnet3DSecureFail(
            [FromQuery] int? orderId, 
            [FromQuery] string? errorCode, 
            [FromQuery] string? message)
        {
            _logger.LogWarning("[POSNET-3DS-FAIL] Fail sayfası - OrderId: {OrderId}, Error: {Error}",
                orderId, errorCode);

            var settings = _paymentOptions.Value;
            var redirectUrl = settings.ReturnUrlCancel ?? "/checkout/failed";
            var separator = redirectUrl.Contains("?") ? "&" : "?";

            var url = $"{redirectUrl}{separator}provider=posnet&status=failed";
            if (orderId.HasValue) url += $"&orderId={orderId}";
            if (!string.IsNullOrEmpty(errorCode)) url += $"&errorCode={Uri.EscapeDataString(errorCode)}";
            if (!string.IsNullOrEmpty(message)) url += $"&message={Uri.EscapeDataString(message)}";

            return Redirect(url);
        }

        /// <summary>
        /// POSNET puan sorgulama (World Puan)
        /// </summary>
        [HttpPost("posnet/points/query")]
        public async Task<IActionResult> PosnetQueryPoints([FromBody] PosnetPointQueryRequestDto request)
        {
            if (_posnetService == null)
            {
                return StatusCode(503, new { message = "POSNET servisi yapılandırılmamış" });
            }

            try
            {
                // ExpireDate formatı: YYMM
                var expireDate = $"{request.ExpireYear}{request.ExpireMonth}";
                
                var result = await _posnetService.QueryPointsAsync(
                    request.CardNumber,
                    expireDate,
                    "000"); // CVV genellikle puan sorgulamada gerekli değil

                if (!result.IsSuccess)
                {
                    return BadRequest(new { message = result.Error, errorCode = result.ErrorCode.ToString() });
                }

                var pointData = result.Data;
                return Ok(new
                {
                    success = true,
                    totalPoints = pointData?.PointInfo?.TotalPoint ?? 0,
                    usablePoints = pointData?.PointInfo?.WorldPoint ?? 0,
                    pointsAsTL = pointData?.PointInfo?.PointAsTL ?? 0m
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[POSNET-POINTS] Puan sorgulama hatası");
                return StatusCode(500, new { message = "Puan sorgulanırken hata oluştu" });
            }
        }

        /// <summary>
        /// 3D Secure redirect HTML sayfası oluşturur
        /// Banka formdan döndüğünde gösterilir ve frontend'e yönlendirir
        /// </summary>
        private static string GenerateRedirectHtml(
            bool isSuccess,
            string redirectUrl,
            string message,
            int? orderId = null,
            string? transactionId = null,
            string? errorCode = null)
        {
            var iconColor = isSuccess ? "#28a745" : "#dc3545";
            var icon = isSuccess ? "✓" : "✗";
            var title = isSuccess ? "Ödeme Başarılı" : "Ödeme Başarısız";

            // CSP hatası olmaması için inline style/script yerine meta refresh kullan
            return $@"<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta http-equiv='refresh' content='3;url={redirectUrl}'>
    <title>{title}</title>
    <link rel='stylesheet' href='/css/payment-redirect.css'>
</head>
<body>
    <div class='container'>
        <div class='icon' style='background:{iconColor}'>{icon}</div>
        <h1>{title}</h1>
        <p>{message}</p>
        {(orderId.HasValue ? $"<p class='info'>Sipariş No: #{orderId}</p>" : "")}
        {(!string.IsNullOrEmpty(transactionId) ? $"<p class='info'>İşlem No: {transactionId}</p>" : "")}
        {(!string.IsNullOrEmpty(errorCode) ? $"<p class='info'>Hata Kodu: {errorCode}</p>" : "")}
        <div class='loader'></div>
        <p class='info'>Yönlendiriliyorsunuz...</p>
        <noscript>
            <p class='info'>JavaScript devre dışı. <a href='{redirectUrl}'>Buraya tıklayın</a>.</p>
        </noscript>
    </div>
</body>
</html>";
        }
    }
}

/// <summary>
/// Puan sorgulama request DTO
/// </summary>
public class PosnetPointQueryRequestDto
{
    public string CardNumber { get; set; } = string.Empty;
    public string ExpireMonth { get; set; } = string.Empty;
    public string ExpireYear { get; set; } = string.Empty;
}
