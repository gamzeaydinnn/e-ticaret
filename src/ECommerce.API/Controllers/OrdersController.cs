using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Constants;
using ECommerce.Core.DTOs.Order;
using ECommerce.Core.Extensions;
using ECommerce.API.Infrastructure;
using ECommerce.Core.Interfaces;
using Microsoft.Extensions.Logging;

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
        public async Task<IActionResult> Checkout([FromBody] OrderCreateDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Geçersiz istek gövdesi" });

            // Validation handled by FluentValidation
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                var errorMessage = string.Join("; ", errors);
                
                // Log validation errors
                var logger = HttpContext.RequestServices.GetService(typeof(ILogger<OrdersController>)) as ILogger<OrdersController>;
                logger?.LogError("[CHECKOUT] Validation hatası: {Errors}", errorMessage);
                
                return BadRequest(new 
                { 
                    message = "Validation hatası",
                    errors = ModelState.ToDictionary(
                        x => x.Key, 
                        x => x.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    )
                });
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
                _logger.LogError(ex, "[CHECKOUT] Sipariş oluşturma hatası");
                // Sadece business/validation exception mesajlarını kullanıcıya göster
                var userMessage = (ex is ECommerce.Core.Exceptions.BusinessException
                                || ex is ECommerce.Core.Exceptions.ValidationException)
                    ? ex.Message
                    : "Sipariş oluşturulurken bir hata oluştu. Lütfen tekrar deneyin.";
                return BadRequest(new { message = userMessage });
            }
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
        /// - Sadece hazırlanmadan önce (New, Pending, Confirmed) iptal edilebilir
        /// - Aksi halde müşteri hizmetleriyle iletişime geçilmeli
        /// </summary>
        [HttpPost("{orderId}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = User.GetUserId(); // extension ile alınıyor
            var (success, errorMessage) = await _orderService.CancelOrderAsync(orderId, userId);
            
            if (!success)
            {
                return BadRequest(new { 
                    success = false, 
                    message = errorMessage ?? "Sipariş iptal edilemedi.",
                    // Müşteri hizmetleri iletişim bilgileri
                    contactInfo = new {
                        whatsapp = "+905334783072",
                        phone = "+90 533 478 30 72",
                        email = "golturkbuku@golkoygurme.com.tr"
                    }
                });
            }
            
            return Ok(new { success = true, message = "Sipariş başarıyla iptal edildi." });
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

            await _orderService.UpdateOrderStatusAsync(id, dto.Status);
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
