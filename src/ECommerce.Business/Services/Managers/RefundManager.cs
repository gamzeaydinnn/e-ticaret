// ==========================================================================
// RefundManager.cs - İade Talebi Yönetim Servisi
// ==========================================================================
// İade taleplerinin tüm yaşam döngüsünü yöneten servis.
// Kargo durumuna göre akıllı karar verme mekanizması:
//   - Kargo yola çıkmamış → Otomatik POSNET reverse + stok iadesi
//   - Kargo yola çıkmış  → Admin onaylı POSNET return
//
// NEDEN PaymentManager'a DI ile bağlı:
//   Para iadesi (reverse/return) işlemleri PaymentManager üzerinden yapılır.
//   RefundManager iş kurallarını yönetir, PaymentManager ödeme altyapısını.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Order;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// İade talebi iş mantığı servisi.
    /// Tüm iade akışlarını kontrol eder ve koordinasyon sağlar.
    /// </summary>
    public class RefundManager : IRefundService
    {
        private readonly ECommerceDbContext _db;
        private readonly IExtendedPaymentService _paymentService;
        private readonly IRealTimeNotificationService _notificationService;
        private readonly ILogger<RefundManager> _logger;

        // Kargo yola çıkmamış durumlar → Otomatik iptal + reverse yapılabilir
        // NEDEN bu kümede Preparing ve Ready var: Ürün henüz depoda, fiziksel teslimat başlamadı
        private static readonly HashSet<OrderStatus> AutoCancellableStatuses = new()
        {
            OrderStatus.New,
            OrderStatus.Pending,
            OrderStatus.Confirmed,
            OrderStatus.Paid
        };

        // Kargo yola çıkmış durumlar → Admin onaylı iade gerekli
        // NEDEN: Fiziksel teslimat başlamış, kargo/ürün müşteride veya yolda
        private static readonly HashSet<OrderStatus> RequiresAdminApprovalStatuses = new()
        {
            OrderStatus.Preparing,
            OrderStatus.Ready,
            OrderStatus.Assigned,
            OrderStatus.PickedUp,
            OrderStatus.InTransit,
            OrderStatus.OutForDelivery,
            OrderStatus.Shipped,
            OrderStatus.Delivered,
            OrderStatus.DeliveryFailed,
            OrderStatus.DeliveryPaymentPending,
            OrderStatus.Completed
        };

        public RefundManager(
            ECommerceDbContext db,
            IExtendedPaymentService paymentService,
            IRealTimeNotificationService notificationService,
            ILogger<RefundManager> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // MÜŞTERİ İADE TALEBİ OLUŞTURMA
        // Sipariş durumuna göre otomatik iptal veya admin onayı akışı
        // ═══════════════════════════════════════════════════════════════════════════

        /// <inheritdoc />
        public async Task<RefundRequestResult> CreateRefundRequestAsync(
            int orderId, int userId, CreateRefundRequestDto dto)
        {
            // Sipariş validasyonu
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return RefundRequestResult.Failed("Sipariş bulunamadı.", "ORDER_NOT_FOUND");

            // Yetki kontrolü: Kullanıcı sadece kendi siparişini iade edebilir
            if (order.UserId != userId)
                return RefundRequestResult.Failed("Bu sipariş size ait değil.", "UNAUTHORIZED");

            // Sipariş zaten iptal/iade edilmiş mi?
            if (order.Status == OrderStatus.Cancelled)
                return RefundRequestResult.Failed("Bu sipariş zaten iptal edilmiş.", "ALREADY_CANCELLED");

            if (order.Status == OrderStatus.Refunded)
                return RefundRequestResult.Failed("Bu sipariş için zaten iade yapılmış.", "ALREADY_REFUNDED");

            // Aynı sipariş için bekleyen iade talebi var mı?
            var existingPending = await _db.RefundRequests
                .AnyAsync(r => r.OrderId == orderId && r.Status == RefundRequestStatus.Pending);

            if (existingPending)
                return RefundRequestResult.Failed(
                    "Bu sipariş için zaten bekleyen bir iade talebi mevcut.", "PENDING_REQUEST_EXISTS");

            // İade tutarını belirle
            var refundAmount = dto.RefundType == "partial" && dto.RefundAmount.HasValue
                ? dto.RefundAmount.Value
                : order.FinalPrice;

            // Tutar validasyonu
            if (refundAmount <= 0 || refundAmount > order.FinalPrice)
                return RefundRequestResult.Failed(
                    $"İade tutarı 0 ile {order.FinalPrice:C} arasında olmalıdır.", "INVALID_AMOUNT");

            // İade talebi kaydını oluştur
            var refundRequest = new RefundRequest
            {
                OrderId = orderId,
                UserId = userId,
                Reason = dto.Reason,
                RefundType = dto.RefundType,
                RefundAmount = refundAmount,
                OrderStatusAtRequest = order.Status.ToString(),
                RequestedAt = DateTime.UtcNow,
                Status = RefundRequestStatus.Pending
            };

            // ═══════════════════════════════════════════════════════════════════════
            // KARAR MEKANİZMASI: Kargo durumuna göre akış belirleme
            // ═══════════════════════════════════════════════════════════════════════

            if (AutoCancellableStatuses.Contains(order.Status))
            {
                // AKIŞ 1: Kargo yola çıkmamış → Otomatik iptal + para iadesi
                return await HandleAutoCancelAsync(order, refundRequest);
            }
            else if (RequiresAdminApprovalStatuses.Contains(order.Status))
            {
                // AKIŞ 2: Kargo yola çıkmış veya hazırlanıyor → Admin onayı gerekli
                return await HandleAdminApprovalRequestAsync(order, refundRequest);
            }
            else
            {
                return RefundRequestResult.Failed(
                    "Bu sipariş durumunda iade talebi oluşturulamaz.", "INVALID_ORDER_STATUS");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // AKIŞ 1: OTOMATİK İPTAL + POSNET REVERSE
        // Kargo yola çıkmamış siparişler için
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Kargo yola çıkmamış siparişlerde otomatik iptal + para iadesi yapar.
        /// POSNET reverse (aynı gün) veya return (farklı gün) otomatik seçilir.
        /// </summary>
        private async Task<RefundRequestResult> HandleAutoCancelAsync(
            Order order, RefundRequest refundRequest)
        {
            _logger.LogInformation(
                "[İADE] Otomatik iptal başlatıldı. OrderId={OrderId}, Status={Status}",
                order.Id, order.Status);

            // Ödeme kaydını bul
            var payment = await _db.Payments
                .Where(p => p.OrderId == order.Id &&
                       (p.Status == "Success" || p.Status == "Paid"))
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            bool paymentRefunded = false;
            string transactionType = "none";

            // Kredi kartı ödemesi varsa para iadesini dene
            if (payment != null && order.PaymentMethod?.ToLower() != "cash_on_delivery"
                                && order.PaymentMethod?.ToLower() != "kapida_odeme")
            {
                try
                {
                    // Önce reverse dene (aynı gün, ekstre'ye yansımaz)
                    var cancelResult = await _paymentService.CancelPaymentAsync(payment.Id, "Otomatik iptal - kargo çıkmadan");
                    if (cancelResult)
                    {
                        paymentRefunded = true;
                        transactionType = "reverse";
                        refundRequest.PosnetHostLogKey = payment.HostLogKey;
                        _logger.LogInformation(
                            "[İADE] POSNET reverse başarılı. OrderId={OrderId}, PaymentId={PaymentId}",
                            order.Id, payment.Id);
                    }
                    else
                    {
                        // Reverse başarısız olduysa return (iade) dene
                        // NEDEN: Aynı gün geçmiş olabilir (batch kapanmış)
                        var refundResult = await _paymentService.PartialRefundAsync(payment.Id, refundRequest.RefundAmount);
                        if (refundResult)
                        {
                            paymentRefunded = true;
                            transactionType = "return";
                            refundRequest.PosnetHostLogKey = payment.HostLogKey;
                            _logger.LogInformation(
                                "[İADE] POSNET return başarılı (reverse fallback). OrderId={OrderId}",
                                order.Id);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[İADE] POSNET reverse ve return başarısız. OrderId={OrderId}",
                                order.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[İADE] Para iadesi hatası. OrderId={OrderId}", order.Id);
                }
            }
            else
            {
                // Kapıda ödeme veya ödeme kaydı yoksa direkt iptal
                paymentRefunded = true; // Fiziksel ödeme henüz alınmadı
                transactionType = "none";
            }

            // Sipariş durumunu güncelle
            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.UtcNow;
            order.CancelReason = $"Müşteri iade talebi: {refundRequest.Reason}";

            // Stok iadesi - sipariş kalemlerini geri ekle
            await RestoreStockAsync(order);

            // İade talebi kaydını güncelle
            refundRequest.Status = paymentRefunded
                ? RefundRequestStatus.AutoCancelled
                : RefundRequestStatus.RefundFailed;
            refundRequest.TransactionType = transactionType;
            refundRequest.ProcessedAt = DateTime.UtcNow;
            refundRequest.AdminNote = paymentRefunded
                ? "Sistem tarafından otomatik iptal ve para iadesi yapıldı."
                : "Otomatik iptal yapıldı ancak para iadesi başarısız. Admin müdahalesi gerekli.";

            if (paymentRefunded)
                refundRequest.RefundedAt = DateTime.UtcNow;

            _db.RefundRequests.Add(refundRequest);

            // Durum geçmişi ekle
            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                PreviousStatus = Enum.Parse<OrderStatus>(refundRequest.OrderStatusAtRequest),
                NewStatus = OrderStatus.Cancelled,
                ChangedBy = "Sistem",
                Reason = $"Otomatik iptal - İade talebi #{refundRequest.Id}",
                ChangedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            // Bildirim gönder
            try
            {
                await _notificationService.NotifyOrderCancelledAsync(
                    order.Id,
                    order.OrderNumber ?? $"#{order.Id}",
                    "Müşteri iade talebi - otomatik iptal",
                    "system");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[İADE] Bildirim hatası. OrderId={OrderId}", order.Id);
            }

            var resultDto = MapToDto(refundRequest, order);
            var message = paymentRefunded
                ? "Siparişiniz iptal edildi ve para iadeniz başlatıldı. Kartınıza yansıma süresi bankanıza göre değişiklik gösterebilir."
                : "Siparişiniz iptal edildi ancak para iadesi işleminde bir sorun oluştu. Müşteri hizmetlerimiz sizinle iletişime geçecek.";

            return RefundRequestResult.Succeeded(resultDto, message, autoCancelled: true);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // AKIŞ 2: ADMİN ONAYI BEKLEYEN İADE TALEBİ
        // Kargo yola çıkmış veya hazırlanıyor durumundaki siparişler için
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Kargo yola çıkmış siparişlerde iade talebi kayıt altına alır.
        /// Admin / müşteri hizmetleri onay sürecini başlatır.
        /// </summary>
        private async Task<RefundRequestResult> HandleAdminApprovalRequestAsync(
            Order order, RefundRequest refundRequest)
        {
            _logger.LogInformation(
                "[İADE] Admin onaylı iade talebi oluşturuluyor. OrderId={OrderId}, Status={Status}",
                order.Id, order.Status);

            refundRequest.Status = RefundRequestStatus.Pending;
            _db.RefundRequests.Add(refundRequest);
            await _db.SaveChangesAsync();

            // Admin'e bildirim gönder
            try
            {
                await _notificationService.NotifyRefundRequestedAsync(
                    order.Id,
                    order.OrderNumber ?? $"#{order.Id}",
                    refundRequest.RefundAmount,
                    refundRequest.Reason);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[İADE] Admin bildirimi gönderilemedi. OrderId={OrderId}", order.Id);
            }

            var resultDto = MapToDto(refundRequest, order);

            return new RefundRequestResult
            {
                Success = true,
                Message = "İade talebiniz alınmıştır. Müşteri hizmetlerimiz en kısa sürede inceleyecek ve size dönüş yapacaktır.",
                RefundRequest = resultDto,
                AutoCancelled = false,
                ContactInfo = new
                {
                    whatsapp = "+905334783072",
                    phone = "+90 533 478 30 72",
                    email = "golturkbuku@golkoygurme.com.tr"
                }
            };
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // ADMİN İADE TALEBİ İŞLEME (ONAY/RET)
        // Admin veya müşteri hizmetleri iade talebini değerlendirir
        // ═══════════════════════════════════════════════════════════════════════════

        /// <inheritdoc />
        public async Task<RefundRequestResult> ProcessRefundRequestAsync(
            int refundRequestId, int adminUserId, ProcessRefundDto dto)
        {
            var refundRequest = await _db.RefundRequests
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.Id == refundRequestId);

            if (refundRequest == null)
                return RefundRequestResult.Failed("İade talebi bulunamadı.", "REQUEST_NOT_FOUND");

            if (refundRequest.Status != RefundRequestStatus.Pending)
                return RefundRequestResult.Failed(
                    "Bu iade talebi zaten işlenmiş.", "ALREADY_PROCESSED");

            var order = refundRequest.Order;
            if (order == null)
                return RefundRequestResult.Failed("Sipariş bulunamadı.", "ORDER_NOT_FOUND");

            // Admin notu kaydet
            refundRequest.ProcessedByUserId = adminUserId;
            refundRequest.ProcessedAt = DateTime.UtcNow;
            refundRequest.AdminNote = dto.AdminNote;

            if (!dto.Approve)
            {
                // ═══════════════════════════════════════════════════════════════
                // RED: İade talebi reddedildi
                // ═══════════════════════════════════════════════════════════════
                refundRequest.Status = RefundRequestStatus.Rejected;
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "[İADE] İade talebi reddedildi. RefundRequestId={Id}, AdminId={AdminId}",
                    refundRequestId, adminUserId);

                var rejectedDto = MapToDto(refundRequest, order);
                return RefundRequestResult.Succeeded(rejectedDto, "İade talebi reddedildi.");
            }

            // ═══════════════════════════════════════════════════════════════════
            // ONAY: Para iadesi yap (POSNET return)
            // ═══════════════════════════════════════════════════════════════════

            // Admin farklı bir tutar belirleyebilir
            var refundAmount = dto.RefundAmount ?? refundRequest.RefundAmount;
            refundRequest.RefundAmount = refundAmount;
            refundRequest.Status = RefundRequestStatus.Approved;

            // Ödeme kaydını bul
            var payment = await _db.Payments
                .Where(p => p.OrderId == order.Id &&
                       (p.Status == "Success" || p.Status == "Paid" || p.Status == "PartiallyRefunded"))
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            bool refundSuccess = false;

            if (payment != null && order.PaymentMethod?.ToLower() != "cash_on_delivery"
                                && order.PaymentMethod?.ToLower() != "kapida_odeme")
            {
                try
                {
                    // POSNET return (iade) işlemi
                    var result = await _paymentService.PartialRefundAsync(payment.Id, refundAmount);
                    if (result)
                    {
                        refundSuccess = true;
                        refundRequest.TransactionType = "return";
                        refundRequest.PosnetHostLogKey = payment.HostLogKey;
                        refundRequest.RefundedAt = DateTime.UtcNow;
                        refundRequest.Status = RefundRequestStatus.Refunded;

                        _logger.LogInformation(
                            "[İADE] POSNET return başarılı. OrderId={OrderId}, Amount={Amount}",
                            order.Id, refundAmount);
                    }
                    else
                    {
                        refundRequest.Status = RefundRequestStatus.RefundFailed;
                        refundRequest.RefundFailureReason = "POSNET iade işlemi başarısız oldu.";
                        _logger.LogWarning(
                            "[İADE] POSNET return başarısız. OrderId={OrderId}", order.Id);
                    }
                }
                catch (Exception ex)
                {
                    refundRequest.Status = RefundRequestStatus.RefundFailed;
                    refundRequest.RefundFailureReason = $"Hata: {ex.Message}";
                    _logger.LogError(ex,
                        "[İADE] Para iadesi hatası. OrderId={OrderId}", order.Id);
                }
            }
            else
            {
                // Kapıda ödeme veya ödeme kaydı yok → sadece sipariş durumu güncelle
                refundSuccess = true;
                refundRequest.TransactionType = "none";
                refundRequest.RefundedAt = DateTime.UtcNow;
                refundRequest.Status = RefundRequestStatus.Refunded;
            }

            // Sipariş durumunu güncelle (tam iade ise)
            if (refundSuccess && refundAmount >= order.FinalPrice)
            {
                var previousStatus = order.Status;
                order.Status = OrderStatus.Refunded;
                order.RefundedAt = DateTime.UtcNow;

                // Stok iadesi
                await RestoreStockAsync(order);

                // Durum geçmişi
                _db.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    PreviousStatus = previousStatus,
                    NewStatus = OrderStatus.Refunded,
                    ChangedBy = $"Admin #{adminUserId}",
                    Reason = $"İade talebi onaylandı - #{refundRequestId}",
                    ChangedAt = DateTime.UtcNow
                });
            }
            else if (refundSuccess && refundAmount < order.FinalPrice)
            {
                // Kısmi iade
                var previousStatus = order.Status;
                order.Status = OrderStatus.PartialRefund;

                _db.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    PreviousStatus = previousStatus,
                    NewStatus = OrderStatus.PartialRefund,
                    ChangedBy = $"Admin #{adminUserId}",
                    Reason = $"Kısmi iade onaylandı - #{refundRequestId} - {refundAmount:C}",
                    ChangedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();

            // Bildirim gönder
            try
            {
                if (refundSuccess)
                {
                    await _notificationService.NotifyOrderStatusChangedAsync(
                        order.Id,
                        order.OrderNumber ?? $"#{order.Id}",
                        order.Status.ToString(),
                        "İade işleminiz onaylanmıştır.",
                        null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[İADE] Müşteri bildirimi gönderilemedi. OrderId={OrderId}", order.Id);
            }

            var resultDto = MapToDto(refundRequest, order);
            var message = refundSuccess
                ? "İade onaylandı ve para iadesi yapıldı."
                : "İade onaylandı ancak para iadesi başarısız oldu. Lütfen tekrar deneyin.";

            return RefundRequestResult.Succeeded(resultDto, message);
        }

        /// <inheritdoc />
        public async Task<RefundRequestResult> RetryRefundAsync(int refundRequestId, int adminUserId)
        {
            var refundRequest = await _db.RefundRequests
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.Id == refundRequestId);

            if (refundRequest == null)
                return RefundRequestResult.Failed("İade talebi bulunamadı.", "REQUEST_NOT_FOUND");

            if (refundRequest.Status != RefundRequestStatus.RefundFailed)
                return RefundRequestResult.Failed(
                    "Sadece başarısız iade talepleri yeniden denenebilir.", "INVALID_STATUS");

            // Yeniden iade denemesi için ProcessRefundRequestAsync'i kullan
            refundRequest.Status = RefundRequestStatus.Pending;
            refundRequest.RefundFailureReason = null;
            await _db.SaveChangesAsync();

            return await ProcessRefundRequestAsync(
                refundRequestId,
                adminUserId,
                new ProcessRefundDto { Approve = true, AdminNote = "Yeniden deneme" });
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // LİSTELEME METODlARI
        // ═══════════════════════════════════════════════════════════════════════════

        /// <inheritdoc />
        public async Task<RefundRequestResult> AdminCancelOrderWithRefundAsync(
            int orderId, int adminUserId, string reason)
        {
            // Sipariş validasyonu
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return RefundRequestResult.Failed("Sipariş bulunamadı.", "ORDER_NOT_FOUND");

            if (order.Status == OrderStatus.Cancelled)
                return RefundRequestResult.Failed("Bu sipariş zaten iptal edilmiş.", "ALREADY_CANCELLED");

            if (order.Status == OrderStatus.Refunded)
                return RefundRequestResult.Failed("Bu sipariş için zaten iade yapılmış.", "ALREADY_REFUNDED");

            _logger.LogInformation(
                "[İADE-ADMIN] Admin siparişi iptal ediyor. OrderId={OrderId}, AdminId={AdminId}, Status={Status}",
                orderId, adminUserId, order.Status);

            var previousStatus = order.Status;

            // İade talebi kaydı oluştur (audit trail için)
            var refundRequest = new RefundRequest
            {
                OrderId = orderId,
                UserId = order.UserId,
                Reason = reason,
                RefundType = "cancel",
                RefundAmount = order.FinalPrice,
                OrderStatusAtRequest = order.Status.ToString(),
                RequestedAt = DateTime.UtcNow,
                ProcessedByUserId = adminUserId,
                ProcessedAt = DateTime.UtcNow,
                AdminNote = $"Admin #{adminUserId} tarafından iptal edildi."
            };

            // Ödeme kaydını bul
            var payment = await _db.Payments
                .Where(p => p.OrderId == orderId &&
                       (p.Status == "Success" || p.Status == "Paid"))
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            bool paymentRefunded = false;
            string transactionType = "none";

            // Kredi kartı ödemesi varsa para iadesini yap
            if (payment != null && order.PaymentMethod?.ToLower() != "cash_on_delivery"
                                && order.PaymentMethod?.ToLower() != "kapida_odeme")
            {
                try
                {
                    // Önce reverse dene (aynı gün, ekstre'ye yansımaz)
                    var cancelResult = await _paymentService.CancelPaymentAsync(
                        payment.Id, $"Admin iptal: {reason}");
                    if (cancelResult)
                    {
                        paymentRefunded = true;
                        transactionType = "reverse";
                        refundRequest.PosnetHostLogKey = payment.HostLogKey;
                        _logger.LogInformation(
                            "[İADE-ADMIN] POSNET reverse başarılı. OrderId={OrderId}", orderId);
                    }
                    else
                    {
                        // Reverse başarısız → return (iade) dene
                        var refundResult = await _paymentService.PartialRefundAsync(
                            payment.Id, order.FinalPrice);
                        if (refundResult)
                        {
                            paymentRefunded = true;
                            transactionType = "return";
                            refundRequest.PosnetHostLogKey = payment.HostLogKey;
                            _logger.LogInformation(
                                "[İADE-ADMIN] POSNET return başarılı. OrderId={OrderId}", orderId);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[İADE-ADMIN] POSNET reverse ve return başarısız. OrderId={OrderId}", orderId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[İADE-ADMIN] Para iadesi hatası. OrderId={OrderId}", orderId);
                }
            }
            else
            {
                // Kapıda ödeme veya ödeme kaydı yok → direkt iptal
                paymentRefunded = true;
                transactionType = "none";
            }

            // Sipariş durumunu güncelle
            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.UtcNow;
            order.CancelReason = $"Admin/Görevli iptal: {reason}";

            // Stok iadesi
            await RestoreStockAsync(order);

            // İade talebi kaydını güncelle
            refundRequest.Status = paymentRefunded
                ? RefundRequestStatus.AutoCancelled
                : RefundRequestStatus.RefundFailed;
            refundRequest.TransactionType = transactionType;
            if (paymentRefunded)
                refundRequest.RefundedAt = DateTime.UtcNow;
            else
                refundRequest.RefundFailureReason = "POSNET işlemi başarısız. Manuel müdahale gerekli.";

            _db.RefundRequests.Add(refundRequest);

            // Durum geçmişi
            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                PreviousStatus = previousStatus,
                NewStatus = OrderStatus.Cancelled,
                ChangedBy = $"Admin #{adminUserId}",
                Reason = $"Admin iptal + para iadesi: {reason}",
                ChangedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            // Bildirim: Müşteriye + Admin + StoreAttendant
            try
            {
                await _notificationService.NotifyOrderCancelledAsync(
                    orderId,
                    order.OrderNumber ?? $"#{orderId}",
                    $"Admin tarafından iptal edildi: {reason}",
                    $"admin-{adminUserId}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[İADE-ADMIN] Bildirim hatası. OrderId={OrderId}", orderId);
            }

            var resultDto = MapToDto(refundRequest, order);
            var message = paymentRefunded
                ? "Sipariş iptal edildi ve para iadesi yapıldı."
                : "Sipariş iptal edildi ancak para iadesi başarısız. Tekrar denenebilir.";

            return RefundRequestResult.Succeeded(resultDto, message, autoCancelled: true);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RefundRequestListDto>> GetUserRefundRequestsAsync(int userId)
        {
            var requests = await _db.RefundRequests
                .Include(r => r.Order)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return requests.Select(r => MapToDto(r, r.Order));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RefundRequestListDto>> GetRefundRequestsByOrderAsync(int orderId)
        {
            var requests = await _db.RefundRequests
                .Include(r => r.Order)
                .Where(r => r.OrderId == orderId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return requests.Select(r => MapToDto(r, r.Order));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RefundRequestListDto>> GetAllRefundRequestsAsync(
            RefundRequestStatus? status = null)
        {
            var query = _db.RefundRequests
                .Include(r => r.Order)
                .Include(r => r.User)
                .Include(r => r.ProcessedByUser)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            var requests = await query
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return requests.Select(r => MapToDto(r, r.Order));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RefundRequestListDto>> GetPendingRefundRequestsAsync()
        {
            return await GetAllRefundRequestsAsync(RefundRequestStatus.Pending);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // YARDIMCI METODLAR
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Sipariş kalemlerinin stoklarını geri yükler.
        /// NEDEN ayrı metod: Hem otomatik iptal hem admin iadesi kullanıyor.
        /// Stocks tablosu ProductVariantId ile çalışır, ProductId yok.
        /// </summary>
        private async Task RestoreStockAsync(Order order)
        {
            // OrderItems yüklenmemişse yeniden çek
            var items = (order.OrderItems != null && order.OrderItems.Any())
                ? order.OrderItems.ToList()
                : await _db.OrderItems.Where(oi => oi.OrderId == order.Id).ToListAsync();

            foreach (var item in items)
            {
                // ProductVariantId üzerinden stok bul
                if (item.ProductVariantId.HasValue && item.ProductVariantId > 0)
                {
                    var stock = await _db.Stocks.FirstOrDefaultAsync(
                        s => s.ProductVariantId == item.ProductVariantId.Value);

                    if (stock != null)
                    {
                        stock.Quantity += item.Quantity;
                        _logger.LogInformation(
                            "[İADE-STOK] Stok geri yüklendi. VariantId={VariantId}, Qty={Qty}",
                            item.ProductVariantId, item.Quantity);
                    }
                }
                else
                {
                    // Varyant ID yoksa ürün ID ile en uygun stoku bul
                    var variant = await _db.ProductVariants
                        .Where(v => v.ProductId == item.ProductId)
                        .FirstOrDefaultAsync();

                    if (variant != null)
                    {
                        var stock = await _db.Stocks.FirstOrDefaultAsync(
                            s => s.ProductVariantId == variant.Id);

                        if (stock != null)
                        {
                            stock.Quantity += item.Quantity;
                            _logger.LogInformation(
                                "[İADE-STOK] Stok geri yüklendi (variant fallback). ProductId={ProductId}, Qty={Qty}",
                                item.ProductId, item.Quantity);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// RefundRequest entity'sini DTO'ya dönüştürür.
        /// </summary>
        private RefundRequestListDto MapToDto(RefundRequest request, Order? order)
        {
            return new RefundRequestListDto
            {
                Id = request.Id,
                OrderId = request.OrderId,
                OrderNumber = order?.OrderNumber,
                UserId = request.UserId,
                CustomerName = order?.CustomerName,
                CustomerEmail = order?.CustomerEmail,
                CustomerPhone = order?.CustomerPhone,
                Status = request.Status,
                StatusText = GetStatusText(request.Status),
                Reason = request.Reason,
                RefundAmount = request.RefundAmount,
                RefundType = request.RefundType,
                OrderStatusAtRequest = request.OrderStatusAtRequest,
                RequestedAt = request.RequestedAt,
                ProcessedByUserId = request.ProcessedByUserId,
                ProcessedByName = request.ProcessedByUser?.FullName,
                ProcessedAt = request.ProcessedAt,
                AdminNote = request.AdminNote,
                PosnetHostLogKey = request.PosnetHostLogKey,
                TransactionType = request.TransactionType,
                RefundedAt = request.RefundedAt,
                RefundFailureReason = request.RefundFailureReason,
                OrderTotalPrice = order?.FinalPrice ?? 0,
                OrderStatus = order?.Status.ToString()
            };
        }

        /// <summary>
        /// İade talebi durum açıklaması (Türkçe).
        /// </summary>
        private static string GetStatusText(RefundRequestStatus status)
        {
            return status switch
            {
                RefundRequestStatus.Pending => "Beklemede",
                RefundRequestStatus.Approved => "Onaylandı",
                RefundRequestStatus.Rejected => "Reddedildi",
                RefundRequestStatus.Refunded => "İade Edildi",
                RefundRequestStatus.AutoCancelled => "Otomatik İptal Edildi",
                RefundRequestStatus.RefundFailed => "İade Başarısız",
                _ => status.ToString()
            };
        }
    }
}
