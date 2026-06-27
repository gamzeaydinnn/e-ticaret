using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Constants;
using ECommerce.Core.DTOs.Order;
using ECommerce.Core.Extensions;
using ECommerce.API.Infrastructure;
using ECommerce.Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ECommerce.Entities.Enums;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IRefundService _refundService;
        private readonly IRealTimeNotificationService _notificationService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IOrderService orderService,
            IRefundService refundService,
            IRealTimeNotificationService notificationService,
            ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _refundService = refundService;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Kullanıcının siparişlerini listeler.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] int? userId = null)
        {
            var effectiveUserId = ResolveUserId(userId);
            if (!effectiveUserId.HasValue)
                return BadRequest(new { message = "userId bulunamadı. Giriş yapın veya userId parametresi gönderin." });

            var orders = await _orderService.GetOrdersAsync(effectiveUserId);
            return Ok(orders);
        }

        /// <summary>
        /// Tek bir siparişin detayını döner.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOrder(int id, [FromQuery] int? userId = null)
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order == null)
                return NotFound();

            var effectiveUserId = ResolveUserId(userId);
            if (effectiveUserId.HasValue && order.UserId != effectiveUserId.Value)
                return Forbid();

            return Ok(order);
        }

        /// <summary>
        /// Admin panelinden manuel sipariş oluşturma senaryoları için basit endpoint.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Geçersiz istek gövdesi" });

            if (dto.ClientOrderId.HasValue)
            {
                var existing = await _orderService.GetByClientOrderIdAsync(dto.ClientOrderId.Value);
                if (existing != null)
                {
                    return Conflict(new
                    {
                        success = true,
                        message = "Bu işlem için zaten bir sipariş oluşturulmuş.",
                        orderId = existing.Id,
                        orderNumber = existing.OrderNumber,
                        status = existing.Status,
                        vatAmount = existing.VatAmount,
                        totalPrice = existing.TotalPrice,
                        finalPrice = existing.FinalPrice,
                        discountAmount = existing.DiscountAmount,
                        couponDiscountAmount = existing.CouponDiscountAmount,
                        campaignDiscountAmount = existing.CampaignDiscountAmount,
                        couponCode = existing.CouponCode,
                        clientOrderId = dto.ClientOrderId,
                        order = existing
                    });
                }
            }

            var authenticatedUserId = User.GetUserId();
            dto.UserId = authenticatedUserId > 0 ? authenticatedUserId : null;

            var order = await _orderService.CreateAsync(dto);
            await NotifyNewOrderAsync(order);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }

        /// <summary>
        /// Üye olmadan veya üye olarak checkout işlemi
        /// </summary>
        [HttpPost("checkout")]
        [AllowAnonymous]
        public async Task<IActionResult> Checkout()
        {
            var dto = await ReadCheckoutPayloadAsync();
            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "Geçersiz istek gövdesi",
                    errors = new { dto = new[] { "The dto field is required." } }
                });
            }

            var validator = HttpContext.RequestServices.GetService(typeof(IValidator<OrderCreateDto>)) as IValidator<OrderCreateDto>;
            if (validator != null)
            {
                var validationResult = await validator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(e => e.ErrorMessage).ToArray());

                    var errorMessage = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogError("[CHECKOUT] Validation hatası: {Errors}", errorMessage);

                    return BadRequest(new
                    {
                        message = "Validation hatası",
                        errors
                    });
                }
            }

            if (dto.ClientOrderId.HasValue)
            {
                var existing = await _orderService.GetByClientOrderIdAsync(dto.ClientOrderId.Value);
                if (existing != null)
                {
                    return Conflict(new
                    {
                        success = true,
                        message = "Bu işlem için zaten bir sipariş oluşturulmuş.",
                        orderId = existing.Id,
                        orderNumber = existing.OrderNumber,
                        status = existing.Status,
                        vatAmount = existing.VatAmount,
                        totalPrice = existing.TotalPrice,
                        finalPrice = existing.FinalPrice,
                        discountAmount = existing.DiscountAmount,
                        couponDiscountAmount = existing.CouponDiscountAmount,
                        campaignDiscountAmount = existing.CampaignDiscountAmount,
                        couponCode = existing.CouponCode,
                        clientOrderId = dto.ClientOrderId,
                        order = existing
                    });
                }
            }

            var authenticatedUserId = User.GetUserId();
            dto.UserId = authenticatedUserId > 0 ? authenticatedUserId : null;

            // ══════════════════════════════════════════════════════════════════════════
            // MİNİMUM SEPET TUTARI KONTROLÜ
            // Sipariş oluşturmadan önce sepet toplamı minimum tutarı karşılıyor mu?
            // ══════════════════════════════════════════════════════════════════════════
            try
            {
                var cartSettingsService = HttpContext.RequestServices.GetRequiredService<ICartSettingsService>();
                var subtotal = dto.OrderItems?.Sum(i => i.UnitPrice * i.Quantity) ?? 0;
                var isCartValid = await cartSettingsService.ValidateMinimumCartAmountAsync(subtotal);

                if (!isCartValid)
                {
                    var cartSettings = await cartSettingsService.GetActiveSettingsAsync();
                    var message = (cartSettings.MinimumCartAmountMessage ?? "Minimum sepet tutarına ulaşılamadı.")
                        .Replace("{amount}", cartSettings.MinimumCartAmount.ToString("N2"));

                    _logger.LogWarning(
                        "[CHECKOUT] Minimum sepet tutarı kontrolü başarısız. Subtotal: {Subtotal}, MinAmount: {MinAmount}",
                        subtotal, cartSettings.MinimumCartAmount);

                    return BadRequest(new
                    {
                        message = message,
                        errorCode = "MINIMUM_CART_AMOUNT",
                        minimumAmount = cartSettings.MinimumCartAmount,
                        currentAmount = subtotal,
                        remainingAmount = cartSettings.MinimumCartAmount - subtotal
                    });
                }
            }
            catch (Exception ex)
            {
                // Minimum tutar servisi hata verirse siparişi engelleme (güvenli varsayılan)
                _logger.LogWarning(ex, "[CHECKOUT] Minimum sepet tutarı kontrolü sırasında hata, sipariş devam ediyor");
            }

            try
            {
                var result = await _orderService.CheckoutAsync(dto);
                await NotifyNewOrderAsync(result);

                return Ok(new
                {
                    success = true,
                    orderId = result.Id,
                    orderNumber = result.OrderNumber,
                    status = result.Status,
                    vatAmount = result.VatAmount,
                    totalPrice = result.TotalPrice,
                    finalPrice = result.FinalPrice,
                    discountAmount = result.DiscountAmount,
                    couponDiscountAmount = result.CouponDiscountAmount,
                    campaignDiscountAmount = result.CampaignDiscountAmount,
                    couponCode = result.CouponCode
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "[CHECKOUT] Sipariş oluşturma hatası. PaymentMethod={PaymentMethod}, ItemCount={ItemCount}",
                    dto.PaymentMethod, dto.OrderItems?.Count ?? 0);
                // Sadece business/validation exception mesajlarını kullanıcıya göster
                var userMessage = (ex is ECommerce.Core.Exceptions.BusinessException
                                || ex is ECommerce.Core.Exceptions.ValidationException)
                    ? ex.Message
                    : ex.Message;
                return BadRequest(new { message = userMessage });
            }
        }

        private async Task<OrderCreateDto?> ReadCheckoutPayloadAsync()
        {
            Request.EnableBuffering();
            Request.Body.Position = 0;

            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(rawBody))
            {
                _logger.LogWarning("[CHECKOUT] Boş request body alındı.");
                return null;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using var document = JsonDocument.Parse(rawBody);
                var root = document.RootElement;
                var normalizedBody = NormalizeCheckoutPayload(root).GetRawText();

                var dto = JsonSerializer.Deserialize<OrderCreateDto>(normalizedBody, options);
                if (dto == null)
                {
                    return null;
                }

                if (dto.OrderItems != null && dto.OrderItems.Count > 0)
                {
                    return dto;
                }

                // Legacy checkout payload still posts `items`; map it for compatibility.
                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("items", out var itemsElement) &&
                    itemsElement.ValueKind == JsonValueKind.Array)
                {
                    var normalizedItems = NormalizeOrderItems(itemsElement);
                    dto.OrderItems = JsonSerializer.Deserialize<System.Collections.Generic.List<OrderItemDto>>(
                        normalizedItems.GetRawText(),
                        options) ?? new System.Collections.Generic.List<OrderItemDto>();
                }

                return dto;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[CHECKOUT] Request body parse edilemedi.");
                return null;
            }
        }

        private static JsonElement NormalizeCheckoutPayload(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object)
            {
                return root.Clone();
            }

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var property in root.EnumerateObject())
                {
                    if ((property.NameEquals("orderItems") || property.NameEquals("items")) &&
                        property.Value.ValueKind == JsonValueKind.Array)
                    {
                        writer.WritePropertyName(property.Name);
                        NormalizeOrderItems(property.Value, writer);
                        continue;
                    }

                    property.WriteTo(writer);
                }

                writer.WriteEndObject();
            }

            return JsonDocument.Parse(stream.ToArray()).RootElement.Clone();
        }

        private static JsonElement NormalizeOrderItems(JsonElement itemsElement)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                NormalizeOrderItems(itemsElement, writer);
            }

            return JsonDocument.Parse(stream.ToArray()).RootElement.Clone();
        }

        private static void NormalizeOrderItems(JsonElement itemsElement, Utf8JsonWriter writer)
        {
            writer.WriteStartArray();

            foreach (var item in itemsElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    item.WriteTo(writer);
                    continue;
                }

                var estimatedWeight = item.TryGetProperty("estimatedWeight", out var estimatedWeightElement) &&
                    estimatedWeightElement.ValueKind == JsonValueKind.Number &&
                    estimatedWeightElement.TryGetDecimal(out var estimatedWeightValue)
                        ? estimatedWeightValue
                        : 0m;

                var rawQuantity = item.TryGetProperty("quantity", out var quantityElement) &&
                    quantityElement.ValueKind == JsonValueKind.Number &&
                    quantityElement.TryGetDecimal(out var quantityValue)
                        ? quantityValue
                        : 0m;

                var normalizedQuantity = estimatedWeight > 0m
                    ? 1
                    : Math.Max(
                        1,
                        (int)Math.Round(rawQuantity <= 0m ? 1m : rawQuantity, MidpointRounding.AwayFromZero));

                writer.WriteStartObject();
                foreach (var property in item.EnumerateObject())
                {
                    if (property.NameEquals("quantity"))
                    {
                        writer.WriteNumber("quantity", normalizedQuantity);
                        continue;
                    }

                    property.WriteTo(writer);
                }
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        private async Task NotifyNewOrderAsync(OrderListDto order)
        {
            try
            {
                var orderNumber = string.IsNullOrWhiteSpace(order.OrderNumber)
                    ? $"#{order.Id}"
                    : order.OrderNumber;
                var customerName = string.IsNullOrWhiteSpace(order.CustomerName)
                    ? "Müşteri"
                    : order.CustomerName;

                await _notificationService.NotifyNewOrderAsync(
                    order.Id,
                    orderNumber,
                    customerName,
                    order.FinalPrice,
                    order.TotalItems);

                await _notificationService.NotifyStoreAttendantNewOrderAsync(
                    order.Id,
                    orderNumber,
                    customerName,
                    order.TotalItems,
                    order.FinalPrice,
                    order.OrderDate);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Yeni sipariş bildirimleri gönderilemedi. OrderId={OrderId}", order.Id);
            }
        }
        /// <summary>
        /// Sipariş iptali (kullanıcı kendi siparişini iptal eder)
        /// MARKET KURALLARI:
        /// - Sadece aynı gün içinde iptal edilebilir
        /// - Sadece hazırlanıyor aşaması ve öncesinde iptal edilebilir
        /// - Aksi halde müşteri hizmetleriyle iletişime geçilmeli
        /// </summary>
        [HttpPost("{orderId}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = User.GetUserId(); // extension ile alınıyor
            if (userId <= 0)
            {
                return Unauthorized(new { success = false, message = "Giriş yapmanız gerekiyor." });
            }

            var order = await _orderService.GetByIdAsync(orderId);
            if (order == null)
            {
                return NotFound(new { success = false, message = "Sipariş bulunamadı." });
            }

            if (order.UserId != userId)
            {
                return Forbid();
            }

            var currentBusinessDate = GetTurkeyNow().Date;
            if (ConvertUtcToTurkey(order.OrderDate).Date != currentBusinessDate)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Sipariş iptali yalnızca siparişin verildiği gün yapılabilir.",
                    errorCode = "SAME_DAY_CANCEL_ONLY"
                });
            }

            if (!Enum.TryParse<OrderStatus>(order.Status, true, out var orderStatus) ||
                (orderStatus != OrderStatus.New &&
                 orderStatus != OrderStatus.Pending &&
                 orderStatus != OrderStatus.Confirmed &&
                 orderStatus != OrderStatus.Paid &&
                 orderStatus != OrderStatus.Preparing))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Bu sipariş mevcut durumunda doğrudan iptal edilemez. Müşteri hizmetleriyle iletişime geçin.",
                    errorCode = "ORDER_NOT_CANCELLABLE"
                });
            }

            var result = await _refundService.CreateRefundRequestAsync(
                orderId,
                userId,
                new CreateRefundRequestDto
                {
                    Reason = "Müşteri tarafından aynı gün iptal edildi",
                    RefundType = "full"
                });

            if (!result.Success)
            {
                return BadRequest(new {
                    success = false,
                    message = result.Message ?? "Sipariş iptal edilemedi.",
                    errorCode = result.ErrorCode,
                    contactInfo = new {
                        whatsapp = "+905334783072",
                        phone = "+90 533 478 30 72",
                        email = "golturkbuku@golkoygurme.com.tr"
                    }
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message ?? "Sipariş iptal edildi ve iade süreci başlatıldı.",
                autoCancelled = result.AutoCancelled
            });
        }

        private static DateTime GetTurkeyNow()
        {
            var utcNow = DateTime.UtcNow;
            return ConvertUtcToTurkey(utcNow);
        }

        private static DateTime ConvertUtcToTurkey(DateTime utcDateTime)
        {
            var normalizedUtc = utcDateTime.Kind == DateTimeKind.Utc
                ? utcDateTime
                : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

            foreach (var timeZoneId in new[] { "Turkey Standard Time", "Europe/Istanbul" })
            {
                try
                {
                    var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                    return TimeZoneInfo.ConvertTimeFromUtc(normalizedUtc, timeZone);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return normalizedUtc;
        }

        // ============================================================================
        // İADE TALEBİ ENDPOINTLERİ
        // Müşteri sipariş durumuna göre iade talebi oluşturabilir.
        // Kargo çıkmadan → Otomatik iptal + para iadesi
        // Kargo çıktıktan sonra → Admin onayı bekleyen iade talebi
        // ============================================================================

        /// <summary>
        /// Müşteri iade talebi oluşturur.
        /// Sipariş durumuna göre otomatik iptal veya admin onaylı iade akışı başlatır.
        /// </summary>
        [HttpPost("{orderId}/refund-request")]
        [Authorize]
        public async Task<IActionResult> CreateRefundRequest(
            int orderId, [FromBody] CreateRefundRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { success = false, message = "İade sebebi zorunludur." });

            var userId = User.GetUserId();
            if (userId <= 0)
                return Unauthorized(new { success = false, message = "Giriş yapmanız gerekiyor." });

            var result = await _refundService.CreateRefundRequestAsync(orderId, userId, dto);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    errorCode = result.ErrorCode,
                    contactInfo = new
                    {
                        whatsapp = "+905334783072",
                        phone = "+90 533 478 30 72",
                        email = "golturkbuku@golkoygurme.com.tr"
                    }
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                autoCancelled = result.AutoCancelled,
                refundRequest = result.RefundRequest,
                contactInfo = result.ContactInfo
            });
        }

        /// <summary>
        /// Kullanıcının iade taleplerini listeler.
        /// </summary>
        [HttpGet("refund-requests")]
        [Authorize]
        public async Task<IActionResult> GetMyRefundRequests()
        {
            var userId = User.GetUserId();
            if (userId <= 0)
                return Unauthorized(new { success = false, message = "Giriş yapmanız gerekiyor." });

            var requests = await _refundService.GetUserRefundRequestsAsync(userId);
            return Ok(new { success = true, data = requests });
        }

        /// <summary>
        /// Belirli bir siparişin iade taleplerini getirir.
        /// </summary>
        [HttpGet("{orderId}/refund-requests")]
        [Authorize]
        public async Task<IActionResult> GetOrderRefundRequests(int orderId)
        {
            // Sipariş sahiplik kontrolü
            var order = await _orderService.GetByIdAsync(orderId);
            if (order == null)
                return NotFound(new { success = false, message = "Sipariş bulunamadı." });

            var userId = User.GetUserId();
            var isAdminLike = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.SuperAdmin);
            if (!isAdminLike && order.UserId != userId)
                return Forbid();

            var requests = await _refundService.GetRefundRequestsByOrderAsync(orderId);
            return Ok(new { success = true, data = requests });
        }

        // ============================================================================
        // MİSAFİR SİPARİŞ SORGULAMA
        // Giriş yapmamış kullanıcılar email + sipariş numarası ile sipariş sorgulayabilir
        // Güvenlik: Sadece kendi siparişini görebilir (email doğrulaması ile)
        // ============================================================================
        /// <summary>
        /// Misafir kullanıcılar için sipariş sorgulama.
        /// Email ve sipariş numarası ile eşleşen siparişi döner.
        /// </summary>
        [HttpGet("guest-lookup")]
        [AllowAnonymous]
        public async Task<IActionResult> GuestLookup(
            [FromQuery] string email, 
            [FromQuery] string orderNumber)
        {
            // Parametre validasyonu
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(orderNumber))
            {
                return BadRequest(new { 
                    message = "E-posta ve sipariş numarası zorunludur.",
                    success = false 
                });
            }

            // Email formatı kontrolü
            var emailTrimmed = email.Trim().ToLowerInvariant();
            if (!IsValidEmail(emailTrimmed))
            {
                return BadRequest(new { 
                    message = "Geçerli bir e-posta adresi girin.",
                    success = false 
                });
            }

            try
            {
                _logger.LogInformation(
                    "[GUEST-LOOKUP] Misafir sipariş sorgusu: Email={Email}, OrderNumber={OrderNumber}", 
                    emailTrimmed, 
                    orderNumber.Trim());

                // Sipariş numarasına göre sipariş bul
                var order = await _orderService.GetByOrderNumberAsync(orderNumber.Trim());
                
                if (order == null)
                {
                    _logger.LogWarning(
                        "[GUEST-LOOKUP] Sipariş bulunamadı: OrderNumber={OrderNumber}", 
                        orderNumber.Trim());
                    return NotFound(new { 
                        message = "Bu sipariş numarasıyla eşleşen sipariş bulunamadı.",
                        success = false 
                    });
                }

                // Email kontrolü - güvenlik için email eşleşmeli
                // Büyük/küçük harf duyarsız karşılaştırma
                if (!string.Equals(order.CustomerEmail?.Trim(), emailTrimmed, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "[GUEST-LOOKUP] Email eşleşmedi: Beklenen={Expected}, Gelen={Received}", 
                        order.CustomerEmail, 
                        emailTrimmed);
                    return NotFound(new { 
                        message = "Bu bilgilerle eşleşen sipariş bulunamadı.",
                        success = false 
                    });
                }

                _logger.LogInformation(
                    "[GUEST-LOOKUP] Sipariş bulundu: OrderId={OrderId}, Status={Status}", 
                    order.Id, 
                    order.Status);

                return Ok(new
                {
                    success = true,
                    order = order
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GUEST-LOOKUP] Sipariş sorgulama hatası");
                return StatusCode(500, new { 
                    message = "Sipariş sorgulama sırasında bir hata oluştu.",
                    success = false 
                });
            }
        }

        // ============================================================================
        // MİSAFİR SİPARİŞ SORGULAMA - TELEFON NUMARASI VEYA SİPARİŞ NUMARASI
        // Giriş yapmamış misafir kullanıcılar telefon numarası veya sipariş numarası
        // ile kendi siparişlerini sorgulayabilir
        // ============================================================================
        /// <summary>
        /// Misafir kullanıcılar için telefon numarası veya sipariş numarası ile sipariş sorgulama.
        /// Telefon numarası verildiğinde eşleşen tüm misafir siparişlerini döner.
        /// Sipariş numarası verildiğinde sadece ilgili siparişi döner.
        /// </summary>
        [HttpGet("guest-track")]
        [AllowAnonymous]
        public async Task<IActionResult> GuestTrack(
            [FromQuery] string? phone,
            [FromQuery] string? orderNumber)
        {
            // En az bir parametre gerekli
            if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(orderNumber))
            {
                return BadRequest(new
                {
                    message = "Telefon numarası veya sipariş numarası zorunludur.",
                    success = false
                });
            }

            try
            {
                // Sipariş numarası ile sorgulama (tek sipariş döner)
                if (!string.IsNullOrWhiteSpace(orderNumber))
                {
                    _logger.LogInformation(
                        "[GUEST-TRACK] Sipariş numarası ile sorgu: OrderNumber={OrderNumber}",
                        orderNumber.Trim());

                    var order = await _orderService.GetByOrderNumberAsync(orderNumber.Trim());

                    if (order == null)
                    {
                        return NotFound(new
                        {
                            message = "Bu sipariş numarasıyla eşleşen sipariş bulunamadı.",
                            success = false
                        });
                    }

                    // Telefon numarası da gönderildiyse doğrulama yap
                    if (!string.IsNullOrWhiteSpace(phone))
                    {
                        var normalizedInput = new string(phone.Where(char.IsDigit).ToArray());
                        var normalizedOrder = new string((order.CustomerPhone ?? "").Where(char.IsDigit).ToArray());

                        var inputSuffix = normalizedInput.Length >= 10
                            ? normalizedInput.Substring(normalizedInput.Length - 10)
                            : normalizedInput;
                        var orderSuffix = normalizedOrder.Length >= 10
                            ? normalizedOrder.Substring(normalizedOrder.Length - 10)
                            : normalizedOrder;

                        if (inputSuffix != orderSuffix)
                        {
                            return NotFound(new
                            {
                                message = "Bu bilgilerle eşleşen sipariş bulunamadı.",
                                success = false
                            });
                        }
                    }

                    return Ok(new { success = true, orders = new[] { order } });
                }

                // Telefon numarası ile sorgulama (birden fazla sipariş dönebilir)
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    var normalizedPhone = new string(phone.Where(char.IsDigit).ToArray());
                    if (normalizedPhone.Length < 10)
                    {
                        return BadRequest(new
                        {
                            message = "Geçerli bir telefon numarası girin (en az 10 hane).",
                            success = false
                        });
                    }

                    _logger.LogInformation(
                        "[GUEST-TRACK] Telefon numarası ile sorgu: Phone=***{PhoneLast4}",
                        normalizedPhone.Substring(normalizedPhone.Length - 4));

                    var orders = await _orderService.GetGuestOrdersByPhoneAsync(phone.Trim());
                    var orderList = orders?.ToList() ?? new List<ECommerce.Core.DTOs.Order.OrderListDto>();

                    if (orderList.Count == 0)
                    {
                        return NotFound(new
                        {
                            message = "Bu telefon numarasıyla eşleşen misafir siparişi bulunamadı.",
                            success = false
                        });
                    }

                    _logger.LogInformation(
                        "[GUEST-TRACK] {Count} sipariş bulundu", orderList.Count);

                    return Ok(new { success = true, orders = orderList });
                }

                return BadRequest(new
                {
                    message = "Geçersiz istek.",
                    success = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GUEST-TRACK] Sipariş sorgulama hatası");
                return StatusCode(500, new
                {
                    message = "Sipariş sorgulama sırasında bir hata oluştu.",
                    success = false
                });
            }
        }

        /// <summary>
        /// Basit email format doğrulama yardımcı metodu
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Basit regex kontrolü
                var emailRegex = new System.Text.RegularExpressions.Regex(
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ödeme sağlayıcısından veya frontend'den tetiklenen ödeme başarısız senaryosu.
        /// Sipariş durumu PaymentFailed olarak işaretlenir.
        /// </summary>
        [HttpPost("{orderId:int}/payment-failed")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentFailed(int orderId)
        {
            var ok = await _orderService.MarkPaymentFailedAsync(orderId);
            if (!ok)
            {
                return NotFound(new { message = "Sipariş bulunamadı.", orderId });
            }

            return BadRequest(new { message = "Ödeme işlemi başarısız oldu.", orderId });
        }
        /// <summary>
        /// Sipariş için PDF fatura indir
        /// </summary>
        [HttpGet("{orderId}/invoice")]
        [Authorize]
        public async Task<IActionResult> DownloadInvoice(int orderId)
        {
            var orderDetail = await _orderService.GetDetailByIdAsync(orderId);
            if (orderDetail == null)
                return NotFound();

            // Sadece sipariş sahibi veya admin/super admin fatura indirebilsin
            var currentUserId = User.GetUserId();
            var isAdminLike = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.SuperAdmin);
            if (!isAdminLike && orderDetail.UserId != currentUserId)
            {
                return Forbid();
            }

            var pdfBytes = InvoiceGenerator.Generate(orderDetail);
            return File(pdfBytes, "application/pdf", $"invoice-{orderId}.pdf");
        }

        /// <summary>
        /// Sipariş durumunu günceller (sadece adminler).
        /// </summary>
        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] OrderStatusUpdateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest(new { message = "Status alanı zorunludur." });

            var adminUserId = User.GetUserId();
            var normalizedStatus = dto.Status.Trim().Replace("_", string.Empty, StringComparison.Ordinal);

            if (string.Equals(normalizedStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                var cancelResult = await _refundService.AdminCancelOrderWithRefundAsync(
                    id,
                    adminUserId,
                    "Yetkili kullanıcı tarafından durum güncellemesi ile iptal edildi");

                if (!cancelResult.Success)
                    return BadRequest(new { message = cancelResult.Message, errorCode = cancelResult.ErrorCode });
            }
            else if (string.Equals(normalizedStatus, "Refunded", StringComparison.OrdinalIgnoreCase))
            {
                var refundResult = await _refundService.AdminRefundOrderAsync(
                    id,
                    adminUserId,
                    "Yetkili kullanıcı tarafından durum güncellemesi ile iade edildi");

                if (!refundResult.Success)
                    return BadRequest(new { message = refundResult.Message, errorCode = refundResult.ErrorCode });
            }
            else
            {
                await _orderService.UpdateOrderStatusAsync(id, dto.Status);
            }

            return NoContent();
        }

        private int? ResolveUserId(int? incomingUserId)
        {
            if (incomingUserId.HasValue && incomingUserId.Value > 0)
                return incomingUserId;

            var claimUserId = User.GetUserId();
            if (claimUserId > 0)
                return claimUserId;

            return null;
        }
    }
}
