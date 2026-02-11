// ==========================================================================
// OrderStateMachine.cs - Sipariş Durum Geçiş Yöneticisi
// ==========================================================================
// Sipariş durumları arasında geçişleri yöneten state machine implementasyonu.
// Guard koşulları, geçiş validasyonları ve bildirim tetikleme içerir.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Sipariş durumları arasında geçişleri yöneten state machine.
    /// </summary>
    public class OrderStateMachine : IOrderStateMachine
    {
        private readonly ECommerceDbContext _context;
        private readonly IRealTimeNotificationService _notificationService;
        private readonly ILogger<OrderStateMachine> _logger;

        // Durum geçiş matrisi: hangi durumdan hangi durumlara geçilebilir
        // TUTARLI AKIŞ: Pending → Confirmed → Preparing → Ready → Assigned → OutForDelivery → Delivered
        private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> AllowedTransitions = new()
        {
            // ═══════════════════════════════════════════════════════════════════════════
            // SİPARİŞ OLUŞTURMA VE ONAY AŞAMASI
            // ═══════════════════════════════════════════════════════════════════════════
            
            // Yeni sipariş (New = -1)
            // Sipariş yeni oluşturuldu, ödeme bekliyor
            [OrderStatus.New] = new HashSet<OrderStatus>
            {
                OrderStatus.Pending,       // Ödeme başlatıldı
                OrderStatus.Confirmed,     // Kapıda ödeme onaylandı
                OrderStatus.Cancelled      // İptal edildi
            },

            // Beklemede (Pending = 0)
            // Ödeme bekleniyor veya işleniyor
            [OrderStatus.Pending] = new HashSet<OrderStatus>
            {
                OrderStatus.Confirmed,     // Ödeme başarılı / Admin onayı
                OrderStatus.Cancelled,     // Ödeme başarısız veya iptal
                OrderStatus.New            // Ödeme yeniden deneme
            },

            // Ödendi (Paid)
            // Ödeme alındı, onay bekliyor
            [OrderStatus.Paid] = new HashSet<OrderStatus>
            {
                OrderStatus.Confirmed,     // Admin onayı
                OrderStatus.Cancelled,     // İptal (iade)
                OrderStatus.Refunded       // Tam iade
            },

            // ═══════════════════════════════════════════════════════════════════════════
            // HAZIRLAMA AŞAMASI (Store Attendant)
            // ═══════════════════════════════════════════════════════════════════════════

            // Onaylandı (Confirmed = -2)
            // Admin onayladı, hazırlanmayı bekliyor
            [OrderStatus.Confirmed] = new HashSet<OrderStatus>
            {
                OrderStatus.Preparing,     // Store Attendant hazırlamaya başladı ✅
                OrderStatus.Processing,    // Eski uyumluluk için
                OrderStatus.Cancelled,     // Admin tarafından iptal
                OrderStatus.Refunded       // Tam iade
            },

            // Hazırlanıyor (Preparing = 1)
            // Store Attendant sipariş hazırlıyor
            [OrderStatus.Preparing] = new HashSet<OrderStatus>
            {
                OrderStatus.Ready,            // Hazır, kurye atama bekliyor ✅
                OrderStatus.ReadyForPickup,   // Eski uyumluluk için
                OrderStatus.Cancelled,        // Admin tarafından iptal
                OrderStatus.Refunded          // Tam iade
            },

            // Processing - Eski uyumluluk için (Preparing ile aynı)
            [OrderStatus.Processing] = new HashSet<OrderStatus>
            {
                OrderStatus.Ready,            // Hazır ✅
                OrderStatus.ReadyForPickup,   // Eski uyumluluk
                OrderStatus.Shipped,          // Kargoya verildi (harici kargo)
                OrderStatus.Cancelled,        
                OrderStatus.Refunded          
            },

            // ═══════════════════════════════════════════════════════════════════════════
            // KURYE ATAMA AŞAMASI (Dispatcher)
            // ═══════════════════════════════════════════════════════════════════════════

            // Hazır (Ready = 2)
            // Store Attendant hazırladı, Dispatcher kurye atacak
            [OrderStatus.Ready] = new HashSet<OrderStatus>
            {
                OrderStatus.Assigned,         // Dispatcher kurye atadı ✅
                OrderStatus.Preparing,        // Geri alındı (sorun)
                OrderStatus.Cancelled,        
                OrderStatus.Refunded          
            },

            // ReadyForPickup - Eski uyumluluk için (Ready ile aynı)
            [OrderStatus.ReadyForPickup] = new HashSet<OrderStatus>
            {
                OrderStatus.Assigned,         // Kurye atandı ✅
                OrderStatus.Shipped,          // Eski uyumluluk
                OrderStatus.Processing,       // Geri alındı
                OrderStatus.Cancelled,        
                OrderStatus.Refunded          
            },

            // ═══════════════════════════════════════════════════════════════════════════
            // TESLİMAT AŞAMASI (Kurye)
            // ═══════════════════════════════════════════════════════════════════════════

            // Kuryeye Atandı (Assigned = 3)
            // Dispatcher kurye atadı, kurye teslim alacak ve yola çıkacak
            [OrderStatus.Assigned] = new HashSet<OrderStatus>
            {
                OrderStatus.PickedUp,          // Kurye teslim aldı
                OrderStatus.OutForDelivery,    // Kurye yola çıktı ✅ KRİTİK DÜZELTME
                OrderStatus.Shipped,           // Eski uyumluluk
                OrderStatus.Ready,             // Kurye iptal etti, yeniden atama
                OrderStatus.DeliveryFailed,    // Teslimat başarısız
                OrderStatus.Cancelled          
            },

            // Kurye Teslim Aldı (PickedUp = 4)
            // Kurye siparişi depoda teslim aldı
            [OrderStatus.PickedUp] = new HashSet<OrderStatus>
            {
                OrderStatus.OutForDelivery,    // Kurye yola çıktı ✅
                OrderStatus.InTransit,         // Yolda
                OrderStatus.Shipped,           // Eski uyumluluk
                OrderStatus.DeliveryFailed,    
                OrderStatus.Assigned           // Geri döndü
            },

            // Yolda (InTransit = 5)
            // Kurye teslimat yolunda
            [OrderStatus.InTransit] = new HashSet<OrderStatus>
            {
                OrderStatus.OutForDelivery,    // Son aşama
                OrderStatus.Delivered,         // Teslim edildi
                OrderStatus.DeliveryFailed,    
                OrderStatus.PickedUp           // Geri döndü
            },

            // Kargoda/Yolda (Shipped) - Eski uyumluluk
            [OrderStatus.Shipped] = new HashSet<OrderStatus>
            {
                OrderStatus.OutForDelivery,   
                OrderStatus.Delivered,        
                OrderStatus.DeliveryFailed,   
                OrderStatus.ReadyForPickup,   
                OrderStatus.DeliveryPaymentPending 
            },

            // Teslimat Yolunda (OutForDelivery = 6)
            // Kurye müşteriye yaklaştı
            [OrderStatus.OutForDelivery] = new HashSet<OrderStatus>
            {
                OrderStatus.Delivered,             // Teslim edildi ✅
                OrderStatus.DeliveryFailed,        // Teslimat başarısız
                OrderStatus.DeliveryPaymentPending // Kapıda ödeme bekliyor
            },

            // ═══════════════════════════════════════════════════════════════════════════
            // TESLİMAT SONRASI DURUMLAR
            // ═══════════════════════════════════════════════════════════════════════════

            // Teslimat Ödemesi Bekliyor (DeliveryPaymentPending = -4)
            [OrderStatus.DeliveryPaymentPending] = new HashSet<OrderStatus>
            {
                OrderStatus.Delivered,        // Ödeme alındı
                OrderStatus.DeliveryFailed,   // Ödeme alınamadı
                OrderStatus.Refunded          
            },

            // Teslimat Başarısız (DeliveryFailed = -3)
            [OrderStatus.DeliveryFailed] = new HashSet<OrderStatus>
            {
                OrderStatus.Ready,            // Yeniden hazırla (Dispatcher yeniden atayacak) ✅
                OrderStatus.Assigned,         // Yeniden atandı
                OrderStatus.ReadyForPickup,   // Eski uyumluluk
                OrderStatus.Shipped,          
                OrderStatus.Refunded,         
                OrderStatus.Cancelled         
            },

            // Teslim Edildi (Delivered) - Başarılı son durum
            [OrderStatus.Delivered] = new HashSet<OrderStatus>
            {
                OrderStatus.Refunded,        // İade
                OrderStatus.PartialRefund    // Kısmi iade
            },

            // Tamamlandı (Completed) - Delivered ile aynı
            [OrderStatus.Completed] = new HashSet<OrderStatus>
            {
                OrderStatus.Refunded,        
                OrderStatus.PartialRefund    
            },

            // Kısmi İade
            [OrderStatus.PartialRefund] = new HashSet<OrderStatus>
            {
                OrderStatus.Refunded         
            },

            // Terminal durumlar
            [OrderStatus.Refunded] = new HashSet<OrderStatus>(),
            [OrderStatus.Cancelled] = new HashSet<OrderStatus>(),
            [OrderStatus.PaymentFailed] = new HashSet<OrderStatus>
            {
                OrderStatus.Pending,
                OrderStatus.Cancelled
            },
            [OrderStatus.ChargebackPending] = new HashSet<OrderStatus>
            {
                OrderStatus.Refunded,
                OrderStatus.Delivered
            }
        };

        // Terminal (son) durumlar - geçiş yapılamaz
        private static readonly HashSet<OrderStatus> TerminalStates = new()
        {
            OrderStatus.Refunded,
            OrderStatus.Cancelled
        };

        // İptal edilebilir durumlar
        // MARKET KURALI: Sadece sipariş hazırlanmaya başlamadan önce iptal edilebilir
        // Hazırlanıyor, Hazır, Yolda, Teslim Edildi durumlarında müşteri hizmetleriyle iletişime geçilmeli
        private static readonly HashSet<OrderStatus> CancellableStates = new()
        {
            OrderStatus.New,       // Yeni sipariş - henüz işleme alınmadı
            OrderStatus.Pending,   // Ödeme bekleniyor
            OrderStatus.Paid,      // Ödendi ama henüz onaylanmadı (POSNET reverse ile iade)
            OrderStatus.Confirmed  // Onaylandı ama henüz hazırlanmaya başlamadı
            // NOT: Preparing, Processing, Ready, ReadyForPickup, DeliveryFailed çıkarıldı
            // Bu durumlarda müşteri hizmetleriyle iletişime geçilmeli
        };

        // İade edilebilir durumlar
        private static readonly HashSet<OrderStatus> RefundableStates = new()
        {
            OrderStatus.Paid,
            OrderStatus.Confirmed,
            OrderStatus.Preparing,
            OrderStatus.Processing,
            OrderStatus.Ready,
            OrderStatus.ReadyForPickup,
            OrderStatus.Assigned,
            OrderStatus.PickedUp,
            OrderStatus.Shipped,
            OrderStatus.InTransit,
            OrderStatus.OutForDelivery,
            OrderStatus.Delivered,
            OrderStatus.DeliveryFailed,
            OrderStatus.DeliveryPaymentPending,
            OrderStatus.PartialRefund
        };

        // Kurye atanabilir durumlar (Dispatcher için)
        private static readonly HashSet<OrderStatus> CourierAssignableStates = new()
        {
            OrderStatus.Ready,            // Ana akış: Ready → Assigned ✅
            OrderStatus.ReadyForPickup,   // Eski uyumluluk
            OrderStatus.DeliveryFailed    // Yeniden atama
        };

        // Türkçe durum açıklamaları
        private static readonly Dictionary<OrderStatus, string> StatusDescriptions = new()
        {
            [OrderStatus.New] = "Yeni Sipariş",
            [OrderStatus.Pending] = "Ödeme Bekleniyor",
            [OrderStatus.Paid] = "Ödendi",
            [OrderStatus.Confirmed] = "Onaylandı",
            [OrderStatus.Preparing] = "Hazırlanıyor",
            [OrderStatus.Processing] = "İşleniyor",
            [OrderStatus.Ready] = "Hazır",
            [OrderStatus.ReadyForPickup] = "Teslime Hazır",
            [OrderStatus.Assigned] = "Kuryeye Atandı",
            [OrderStatus.PickedUp] = "Kurye Teslim Aldı",
            [OrderStatus.InTransit] = "Yolda",
            [OrderStatus.Shipped] = "Kargoya Verildi",
            [OrderStatus.OutForDelivery] = "Teslimat Yolunda",
            [OrderStatus.Delivered] = "Teslim Edildi",
            [OrderStatus.Completed] = "Tamamlandı",
            [OrderStatus.DeliveryFailed] = "Teslimat Başarısız",
            [OrderStatus.DeliveryPaymentPending] = "Kapıda Ödeme Bekleniyor",
            [OrderStatus.Refunded] = "İade Edildi",
            [OrderStatus.PartialRefund] = "Kısmi İade Yapıldı",
            [OrderStatus.Cancelled] = "İptal Edildi",
            [OrderStatus.PaymentFailed] = "Ödeme Başarısız",
            [OrderStatus.ChargebackPending] = "Chargeback Bekleniyor"
        };

        public OrderStateMachine(
            ECommerceDbContext context,
            IRealTimeNotificationService notificationService,
            ILogger<OrderStateMachine> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public bool CanTransition(OrderStatus currentStatus, OrderStatus targetStatus)
        {
            if (currentStatus == targetStatus)
                return false;

            if (!AllowedTransitions.TryGetValue(currentStatus, out var allowedTargets))
                return false;

            return allowedTargets.Contains(targetStatus);
        }

        /// <inheritdoc />
        public async Task<OrderTransitionResult> TransitionAsync(int orderId, OrderStatus targetStatus, 
            int? actorId, string? reason = null, Dictionary<string, object>? metadata = null)
        {
            try
            {
                // Siparişi getir
                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    _logger.LogWarning("Sipariş bulunamadı. OrderId={OrderId}", orderId);
                    return OrderTransitionResult.Failed("Sipariş bulunamadı.", "ORDER_NOT_FOUND");
                }

                var currentStatus = order.Status;

                // Geçiş yapılabilir mi kontrol et
                if (!CanTransition(currentStatus, targetStatus))
                {
                    _logger.LogWarning(
                        "Geçersiz durum geçişi. OrderId={OrderId}, Current={Current}, Target={Target}", 
                        orderId, currentStatus, targetStatus);
                    return OrderTransitionResult.Failed(
                        $"'{GetStatusDescription(currentStatus)}' durumundan '{GetStatusDescription(targetStatus)}' durumuna geçiş yapılamaz.", 
                        "INVALID_TRANSITION");
                }

                // Guard koşullarını kontrol et
                var guardResult = await CheckGuardConditionsAsync(order, currentStatus, targetStatus, metadata);
                if (!guardResult.Success)
                {
                    _logger.LogWarning(
                        "Guard koşulu başarısız. OrderId={OrderId}, Error={Error}", 
                        orderId, guardResult.Message);
                    return guardResult;
                }

                // Durumu güncelle
                order.Status = targetStatus;
                order.UpdatedAt = DateTime.UtcNow;

                // Durum bazlı ek güncellemeler
                await ApplyStatusSpecificUpdatesAsync(order, currentStatus, targetStatus, metadata);

                // Kaydet
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Sipariş durumu değiştirildi. OrderId={OrderId}, From={From}, To={To}, Actor={Actor}, Reason={Reason}", 
                    orderId, currentStatus, targetStatus, actorId, reason);

                // Bildirimleri gönder
                await SendStatusChangeNotificationsAsync(order, currentStatus, targetStatus, reason);

                return OrderTransitionResult.Succeeded(
                    currentStatus, 
                    targetStatus, 
                    $"Sipariş durumu '{GetStatusDescription(targetStatus)}' olarak güncellendi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş durumu değiştirme hatası. OrderId={OrderId}", orderId);
                return OrderTransitionResult.Failed("Sipariş durumu güncellenirken bir hata oluştu.", "INTERNAL_ERROR");
            }
        }

        /// <inheritdoc />
        public IEnumerable<OrderStatus> GetAllowedTransitions(OrderStatus currentStatus)
        {
            if (AllowedTransitions.TryGetValue(currentStatus, out var allowedTargets))
                return allowedTargets;

            return Enumerable.Empty<OrderStatus>();
        }

        /// <inheritdoc />
        public bool IsTerminalState(OrderStatus status)
        {
            return TerminalStates.Contains(status);
        }

        /// <inheritdoc />
        public bool CanCancel(OrderStatus currentStatus)
        {
            return CancellableStates.Contains(currentStatus);
        }

        /// <inheritdoc />
        public bool CanRefund(OrderStatus currentStatus)
        {
            return RefundableStates.Contains(currentStatus);
        }

        /// <inheritdoc />
        public bool CanAssignCourier(OrderStatus currentStatus)
        {
            return CourierAssignableStates.Contains(currentStatus);
        }

        /// <inheritdoc />
        public string GetStatusDescription(OrderStatus status)
        {
            return StatusDescriptions.TryGetValue(status, out var description) 
                ? description 
                : status.ToString();
        }

        #region Private Helper Methods

        /// <summary>
        /// Guard koşullarını kontrol eder.
        /// Bazı durum geçişleri ek koşullar gerektirir.
        /// </summary>
        private async Task<OrderTransitionResult> CheckGuardConditionsAsync(
            ECommerce.Entities.Concrete.Order order, 
            OrderStatus currentStatus, 
            OrderStatus targetStatus,
            Dictionary<string, object>? metadata)
        {
            // Delivered'a geçiş için kapıda ödeme kontrolü
            if (targetStatus == OrderStatus.Delivered)
            {
                // Kapıda ödeme ise ve ödeme alındı mı kontrol et
                if (order.PaymentMethod?.ToLower() == "cash_on_delivery" || 
                    order.PaymentMethod?.ToLower() == "kapida_odeme")
                {
                    if (currentStatus == OrderStatus.DeliveryPaymentPending)
                    {
                        // Ödeme alındı işareti olmalı
                        if (metadata == null || !metadata.ContainsKey("payment_collected") || 
                            !(bool)metadata["payment_collected"])
                        {
                            return OrderTransitionResult.Failed(
                                "Kapıda ödeme alınmadan teslimat tamamlanamaz.", 
                                "COD_PAYMENT_REQUIRED");
                        }
                    }
                }
            }

            // Shipped'a geçiş için kurye atama kontrolü
            if (targetStatus == OrderStatus.Shipped)
            {
                if (!order.CourierId.HasValue)
                {
                    return OrderTransitionResult.Failed(
                        "Sipariş kargoya verilemez, kurye atanmamış.", 
                        "COURIER_NOT_ASSIGNED");
                }
            }

            // Refunded'a geçiş için ödeme kontrolü
            if (targetStatus == OrderStatus.Refunded)
            {
                // Status string olarak "Success" veya "Paid" olabilir
                var payment = await _context.Payments
                    .Where(p => p.OrderId == order.Id && 
                           (p.Status == "Success" || p.Status == "Paid"))
                    .FirstOrDefaultAsync();

                // Kapıda ödeme değilse ve ödeme yoksa iade yapılamaz
                if (payment == null && 
                    order.PaymentMethod?.ToLower() != "cash_on_delivery" &&
                    order.PaymentMethod?.ToLower() != "kapida_odeme")
                {
                    return OrderTransitionResult.Failed(
                        "İade yapılacak ödeme bulunamadı.", 
                        "NO_PAYMENT_FOUND");
                }
            }

            return OrderTransitionResult.Succeeded(currentStatus, targetStatus);
        }

        /// <summary>
        /// Durum değişikliğine göre ek güncellemeler yapar.
        /// </summary>
        private async Task ApplyStatusSpecificUpdatesAsync(
            ECommerce.Entities.Concrete.Order order,
            OrderStatus currentStatus,
            OrderStatus targetStatus,
            Dictionary<string, object>? metadata)
        {
            switch (targetStatus)
            {
                case OrderStatus.Confirmed:
                    order.ConfirmedAt = DateTime.UtcNow;
                    break;

                case OrderStatus.Processing:
                    order.ProcessingStartedAt = DateTime.UtcNow;
                    break;

                case OrderStatus.Shipped:
                    order.ShippedAt = DateTime.UtcNow;
                    break;

                case OrderStatus.OutForDelivery:
                    order.OutForDeliveryAt = DateTime.UtcNow;
                    break;

                case OrderStatus.Delivered:
                    order.DeliveredAt = DateTime.UtcNow;
                    // Capture status'u güncelle
                    if (order.CaptureStatus == CaptureStatus.Pending)
                    {
                        order.CaptureStatus = CaptureStatus.NotRequired; // Kapıda ödeme
                    }
                    break;

                case OrderStatus.DeliveryFailed:
                    if (metadata?.ContainsKey("problem_reason") == true)
                    {
                        order.DeliveryProblemReason = metadata["problem_reason"]?.ToString();
                    }
                    break;

                case OrderStatus.Cancelled:
                    order.CancelledAt = DateTime.UtcNow;
                    if (metadata?.ContainsKey("cancel_reason") == true)
                    {
                        order.CancelReason = metadata["cancel_reason"]?.ToString();
                    }
                    // Capture'ı void et (eğer varsa)
                    if (order.CaptureStatus == CaptureStatus.Success)
                    {
                        order.CaptureStatus = CaptureStatus.Voided;
                    }

                    // Kupon geri yükleme: İptal edilen siparişte kupon kullanılmışsa geri al
                    await RestoreCouponIfUsedAsync(order,
                        metadata?.ContainsKey("cancel_reason") == true
                            ? $"Order Cancelled: {metadata["cancel_reason"]}"
                            : "Order Cancelled");
                    break;

                case OrderStatus.Refunded:
                    order.RefundedAt = DateTime.UtcNow;
                    order.CaptureStatus = CaptureStatus.Voided;

                    // Kupon geri yükleme: İade edilen siparişte kupon kullanılmışsa geri al
                    await RestoreCouponIfUsedAsync(order, "Order Refunded");
                    break;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Durum değişikliği bildirimlerini gönderir.
        /// </summary>
        private async Task SendStatusChangeNotificationsAsync(
            ECommerce.Entities.Concrete.Order order,
            OrderStatus previousStatus,
            OrderStatus newStatus,
            string? reason)
        {
            try
            {
                var statusText = GetStatusDescription(newStatus);
                var orderNumber = order.OrderNumber ?? $"#{order.Id}";

                // Müşteriye bildirim gönder
                await _notificationService.NotifyOrderStatusChangedAsync(
                    order.Id,
                    orderNumber,
                    newStatus.ToString(),
                    statusText,
                    order.EstimatedDeliveryDate);

                // Admin'e özel bildirimler
                switch (newStatus)
                {
                    case OrderStatus.DeliveryFailed:
                        var courierName = order.Courier?.User?.FullName ?? "Bilinmiyor";
                        await _notificationService.NotifyDeliveryProblemToAdminAsync(
                            order.Id,
                            orderNumber,
                            "delivery_failed",
                            courierName,
                            reason);
                        break;

                    case OrderStatus.Cancelled:
                        await _notificationService.NotifyOrderCancelledAsync(
                            order.Id,
                            orderNumber,
                            reason ?? "Belirtilmedi",
                            "system");
                        break;

                    case OrderStatus.Refunded:
                        await _notificationService.NotifyRefundRequestedAsync(
                            order.Id,
                            orderNumber,
                            order.FinalPrice, // FinalPrice kullanılıyor (TotalAmount yerine)
                            reason ?? "İade talebi onaylandı");
                        break;
                }

                // Kuryeye bildirim (eğer atanmışsa ve ilgili durumsa)
                if (order.CourierId.HasValue)
                {
                    if (newStatus == OrderStatus.Cancelled)
                    {
                        await _notificationService.NotifyOrderUnassignedFromCourierAsync(
                            order.CourierId.Value,
                            order.Id,
                            orderNumber,
                            reason ?? "Sipariş iptal edildi");
                    }
                }
            }
            catch (Exception ex)
            {
                // Bildirim hatası ana işlemi etkilememeli
                _logger.LogWarning(ex, "Durum değişikliği bildirimi gönderilemedi. OrderId={OrderId}", order.Id);
            }
        }

        /// <summary>
        /// Sipariş iptal veya iade edildiğinde kullanılan kuponu geri yükler.
        ///
        /// Yapılan işlemler:
        /// 1. CouponUsage kaydını devre dışı bırak (IsActive = false)
        /// 2. Coupon.UsageCount'u azalt
        /// 3. İlgili bilgileri loglama (audit trail)
        ///
        /// NOT: Transaction içinde çalışır - ApplyStatusSpecificUpdatesAsync zaten transaction içindedir.
        /// </summary>
        /// <param name="order">İptal/iade edilen sipariş</param>
        /// <param name="statusReason">İptal/iade nedeni (logların için)</param>
        private async Task RestoreCouponIfUsedAsync(
            ECommerce.Entities.Concrete.Order order,
            string statusReason)
        {
            // Siparişte kupon kullanılmadıysa işlem yapma
            if (string.IsNullOrWhiteSpace(order.AppliedCouponCode))
            {
                return;
            }

            try
            {
                // Kupon kodunu normalize et
                var normalizedCode = order.AppliedCouponCode.Trim().ToUpperInvariant();

                // Kuponu bul
                var coupon = await _context.Set<ECommerce.Entities.Concrete.Coupon>()
                    .FirstOrDefaultAsync(c => c.Code.ToUpper() == normalizedCode);

                if (coupon == null)
                {
                    _logger.LogWarning(
                        "Kupon geri yüklenemedi - kupon bulunamadı. " +
                        "OrderId: {OrderId}, CouponCode: {CouponCode}",
                        order.Id, order.AppliedCouponCode);
                    return;
                }

                // CouponUsage kaydını bul ve devre dışı bırak
                var couponUsage = await _context.Set<ECommerce.Entities.Concrete.CouponUsage>()
                    .FirstOrDefaultAsync(cu =>
                        cu.OrderId == order.Id &&
                        cu.CouponId == coupon.Id &&
                        cu.IsActive);

                if (couponUsage != null)
                {
                    // Kupon kullanımını devre dışı bırak (soft delete)
                    couponUsage.IsActive = false;
                    couponUsage.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation(
                        "CouponUsage kaydı devre dışı bırakıldı. " +
                        "OrderId: {OrderId}, CouponCode: {CouponCode}, " +
                        "UsageId: {UsageId}, Reason: {Reason}",
                        order.Id, coupon.Code, couponUsage.Id, statusReason);
                }
                else
                {
                    _logger.LogWarning(
                        "CouponUsage kaydı bulunamadı ama kupon kodu siparişte kayıtlı. " +
                        "OrderId: {OrderId}, CouponCode: {CouponCode}",
                        order.Id, order.AppliedCouponCode);
                }

                // Kupon UsageCount'u azalt (en az 0 olmalı)
                if (coupon.UsageCount > 0)
                {
                    coupon.UsageCount--;

                    _logger.LogInformation(
                        "Kupon UsageCount azaltıldı. " +
                        "CouponId: {CouponId}, Code: {Code}, " +
                        "OldCount: {OldCount}, NewCount: {NewCount}, " +
                        "OrderId: {OrderId}, Reason: {Reason}",
                        coupon.Id, coupon.Code,
                        coupon.UsageCount + 1, coupon.UsageCount,
                        order.Id, statusReason);
                }
                else
                {
                    _logger.LogWarning(
                        "Kupon UsageCount zaten 0, azaltılamadı. " +
                        "CouponId: {CouponId}, Code: {Code}, OrderId: {OrderId}",
                        coupon.Id, coupon.Code, order.Id);
                }

                // SaveChanges YOK - transaction dışarıda handle ediyor (ApplyStatusSpecificUpdatesAsync caller'ı)
            }
            catch (Exception ex)
            {
                // Kupon geri yükleme hatası sipariş durumunu etkilememeli
                // Ancak loglanmalı ve takip edilmeli
                _logger.LogError(ex,
                    "Kupon geri yükleme işlemi başarısız. " +
                    "OrderId: {OrderId}, CouponCode: {CouponCode}, Reason: {Reason}",
                    order.Id, order.AppliedCouponCode, statusReason);

                // Exception fırlatmıyoruz - main transaction abort olmasın
                // Manual müdahale gerekebilir, bu yüzden ERROR seviyesinde log
            }
        }

        #endregion
    }
}
