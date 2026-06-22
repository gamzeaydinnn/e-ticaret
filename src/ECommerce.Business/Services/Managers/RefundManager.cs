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
using System.Text.Json;
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
            OrderStatus.Paid,
            OrderStatus.Preparing
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

        private static readonly string[] ReversiblePaymentStatuses =
        {
            "Success",
            "Paid",
            "Authorized",
            "PartiallyRefunded"
        };

        private const string RefundItemMetadataPrefix = "[REFUND_ITEMS_JSON]";

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

        private Task<Payments?> GetLatestReversiblePaymentAsync(int orderId)
        {
            return _db.Payments
                .Where(p => p.OrderId == orderId && ReversiblePaymentStatuses.Contains(p.Status))
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// İade işlemlerinde kullanılacak orijinal ödeme kaydını döndürür.
        ///
        /// BANKA DOKÜMANTASYONU (MADDE 10 DÜZELTMESİ):
        /// "İade işleminde iade edilecek orijinal işlemin bilgileriyle gelinmesi
        /// gerekmektedir; örneğin satış işlemi sonrası kısmi iade yapılması
        /// durumunda, ikinci yapılacak kısmi iade ilk kısmi iadeden dönen
        /// bilgilerle değil satıştan dönen bilgilerle yapılmalıdır."
        ///
        /// ÖNEM: Her zaman TransactionType = 'sale' veya 'capt' olan ilk başarılı
        /// ödeme kaydı döndürülür. 'return' veya 'reverse' kaydı hiçbir zaman
        /// döndürülmez.
        /// </summary>
        private Task<Payments?> GetOriginalSaleOrCaptPaymentAsync(int orderId)
        {
            // Önce capt (finansallaştırma) bak, sonra sale (direkt satış)
            return _db.Payments
                .Where(p => p.OrderId == orderId &&
                       (p.TransactionType == "capt" || p.TransactionType == "sale") &&
                       (p.Status == "Paid" || p.Status == "Success"))
                .OrderByDescending(p => p.CreatedAt)  // En son capt/sale (re-auth senaryosu için)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Siparişin ön provizyonda (Auth) olup olmadığını kontrol eder.
        /// Provizyon durumundaki siparişler için iade yerine reverse(auth) yapılır.
        /// </summary>
        private static bool IsPaymentInAuthOnlyState(Order order, Payments? payment)
        {
            // Sipariş hem provizyon alınmış hem de henüz capture edilmemiş
            return payment?.Status == "Authorized" ||
                   (order.PaymentStatus == PaymentStatus.Authorized &&
                    order.CaptureStatus != CaptureStatus.Success);
        }

        private static decimal CalculateRefundAmount(Order order, Payments? payment)
        {
            // Önce capture tutarını kullan (finansallaştırılmış gerçek tutar)
            if (order.CapturedAmount > 0)
            {
                return order.CapturedAmount;
            }

            if (order.FinalAmount > 0)
            {
                return order.FinalAmount;
            }

            if (payment?.CapturedAmount > 0)
            {
                return payment.CapturedAmount;
            }

            if (payment?.Amount > 0)
            {
                return payment.Amount;
            }

            return order.FinalPrice;
        }

        private static decimal CalculateOrderItemRefundAmount(OrderItem item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                return 0m;
            }

            if (item.IsWeightBased || item.ActualPrice.HasValue || item.EstimatedPrice > 0)
            {
                var lineTotal = item.ActualPrice ?? item.EstimatedPrice;
                if (lineTotal <= 0)
                {
                    lineTotal = item.UnitPrice * item.Quantity;
                }

                if (item.Quantity <= 0)
                {
                    return Math.Round(lineTotal, 2, MidpointRounding.AwayFromZero);
                }

                return Math.Round((lineTotal / item.Quantity) * quantity, 2, MidpointRounding.AwayFromZero);
            }

            return Math.Round(item.UnitPrice * quantity, 2, MidpointRounding.AwayFromZero);
        }

        private static bool IsCashOnDelivery(string? paymentMethod)
        {
            var normalized = paymentMethod?.Trim().ToLowerInvariant();
            return normalized == "cash_on_delivery" || normalized == "kapida_odeme";
        }

        private static void MarkWeightPaymentCancelled(Order order)
        {
            if (string.IsNullOrEmpty(order.PreAuthHostLogKey) && order.PreAuthAmount <= 0)
            {
                return;
            }

            order.PreAuthHostLogKey = null;
            order.PreAuthDate = null;
            order.PreAuthAmount = 0m;
            order.CaptureStatus = CaptureStatus.Voided;

            if (order.WeightAdjustmentStatus == WeightAdjustmentStatus.PendingWeighing ||
                order.WeightAdjustmentStatus == WeightAdjustmentStatus.PendingAdminApproval)
            {
                order.WeightAdjustmentStatus = WeightAdjustmentStatus.Failed;
            }
        }

        private static bool HasActiveWeightPreAuthorization(Order order)
        {
            return (!string.IsNullOrEmpty(order.PreAuthHostLogKey) || order.PreAuthAmount > 0)
                && (order.WeightAdjustmentStatus == WeightAdjustmentStatus.PendingWeighing
                    || order.WeightAdjustmentStatus == WeightAdjustmentStatus.PendingAdminApproval);
        }

        private static string BuildWeightCancellationMessage(string reason, bool hadActiveWeightPreAuthorization)
        {
            if (!hadActiveWeightPreAuthorization)
            {
                return reason;
            }

            return $"{reason} Provizyon kaldırma süresi bankaya göre değişebilir.";
        }

        private static string AttachRefundItemsToAdminNote(string? adminNote, IReadOnlyCollection<RefundRequestItemDto> items)
        {
            var visibleNote = string.IsNullOrWhiteSpace(adminNote) ? string.Empty : adminNote.Trim();
            var json = JsonSerializer.Serialize(items);
            return string.IsNullOrWhiteSpace(visibleNote)
                ? $"{RefundItemMetadataPrefix}{json}"
                : $"{visibleNote}\n{RefundItemMetadataPrefix}{json}";
        }

        private static string? ExtractVisibleAdminNote(string? adminNote)
        {
            if (string.IsNullOrWhiteSpace(adminNote))
            {
                return adminNote;
            }

            var markerIndex = adminNote.IndexOf(RefundItemMetadataPrefix, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return adminNote;
            }

            var visiblePart = adminNote[..markerIndex].Trim();
            return string.IsNullOrWhiteSpace(visiblePart) ? null : visiblePart;
        }

        private static List<RefundRequestItemDto> ExtractRefundItems(RefundRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AdminNote))
            {
                return new List<RefundRequestItemDto>();
            }

            var markerIndex = request.AdminNote.IndexOf(RefundItemMetadataPrefix, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return new List<RefundRequestItemDto>();
            }

            var json = request.AdminNote[(markerIndex + RefundItemMetadataPrefix.Length)..].Trim();
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<RefundRequestItemDto>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<RefundRequestItemDto>>(json) ?? new List<RefundRequestItemDto>();
            }
            catch
            {
                return new List<RefundRequestItemDto>();
            }
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

        private static bool IsSameBusinessDay(DateTime utcDateTime)
        {
            return ConvertUtcToTurkey(utcDateTime).Date == ConvertUtcToTurkey(DateTime.UtcNow).Date;
        }

        private static bool IsFullRemainingRefund(decimal refundAmount, decimal maxRefundableAmount)
        {
            return refundAmount >= maxRefundableAmount - 0.01m;
        }

        private static bool ShouldUseSameDayReverseForCapturedPayment(
            Payments paymentForRefund,
            decimal refundAmount,
            decimal maxRefundableAmount)
        {
            return (paymentForRefund.TransactionType == "capt" || paymentForRefund.TransactionType == "sale")
                && IsFullRemainingRefund(refundAmount, maxRefundableAmount)
                && IsSameBusinessDay(paymentForRefund.CreatedAt);
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
            var payment = await GetLatestReversiblePaymentAsync(order.Id);
            var refundAmount = CalculateRefundAmount(order, payment);
            refundRequest.RefundAmount = refundAmount;

            bool paymentRefunded = false;
            string transactionType = "none";

            // Krédi kartı ödemesi varsa para iadesini dene
            if (payment != null && order.PaymentMethod?.ToLower() != "cash_on_delivery"
                                && order.PaymentMethod?.ToLower() != "kapida_odeme")
            {
                // ═══════════════════════════════════════════════════════════════
                // MADDE 10 DÜZELTMESİ: İade için orijinal sale/capt kaydı kullan
                // Provizyon durumunda reverse(auth) yap, capture sonrası return yap
                // ═══════════════════════════════════════════════════════════════
                var originalPayment = await GetOriginalSaleOrCaptPaymentAsync(order.Id);
                var paymentForRefund = originalPayment ?? payment; // Fallback: herhangi bir reversible payment

                try
                {
                    if (IsPaymentInAuthOnlyState(order, payment))
                    {
                        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                        // Henüz capture yapılmamış provizyon → reverse(auth) yap
                        // BANKA DOK: Provizyon durumundaki işlem için return değil
                        // reverse kullanılmalıdır.
                        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                        _logger.LogInformation(
                            "[İADE] Sipariş Auth durumunda - reverse(auth) yapılıyor. " +
                            "OrderId={OrderId}, HostLogKey={Key}",
                            order.Id, order.PreAuthHostLogKey);

                        var cancelResult = await _paymentService.CancelPaymentAsync(
                            paymentForRefund.Id, "Otomatik iptal - kargo çıkmadan, provizyon iptali");
                        if (cancelResult)
                        {
                            paymentRefunded = true;
                            transactionType = "reverse";
                            refundRequest.PosnetHostLogKey = order.PreAuthHostLogKey ?? paymentForRefund.HostLogKey;
                            _logger.LogInformation(
                                "[İADE] POSNET reverse(auth) başarılı. OrderId={OrderId}", order.Id);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[İADE] POSNET reverse(auth) başarısız. OrderId={OrderId}", order.Id);
                        }
                    }
                    else
                    {
                        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                        // Capture edilmiş → önce reverse (aynı gün), sonra return (farklı gün)
                        // Her iki durumda da ORGINAL sale/capt ödeme ID kullanılır
                        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                        var cancelResult = await _paymentService.CancelPaymentAsync(
                            paymentForRefund.Id, "Otomatik iptal - kargo çıkmadan");
                        if (cancelResult)
                        {
                            paymentRefunded = true;
                            transactionType = "reverse";
                            refundRequest.PosnetHostLogKey = paymentForRefund.HostLogKey;
                            _logger.LogInformation(
                                "[İADE] POSNET reverse başarılı. OrderId={OrderId}, PaymentId={PaymentId}",
                                order.Id, paymentForRefund.Id);
                        }
                        else
                        {
                            // Reverse başarısız olduysa return (iade) dene
                            // NEDEN: Aynı gün geçmiş olabilir (batch kapanmış)
                            // ÖNEMLİ: Orijinal sale/capt ID'si kullanılır!
                            var refundResult = await _paymentService.PartialRefundAsync(paymentForRefund.Id, refundAmount);
                            if (refundResult)
                            {
                                paymentRefunded = true;
                                transactionType = "return";
                                refundRequest.PosnetHostLogKey = paymentForRefund.HostLogKey;
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
            var hadActiveWeightPreAuthorization = HasActiveWeightPreAuthorization(order);
            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.UtcNow;
            order.CancelReason = $"Müşteri iade talebi: {refundRequest.Reason}";
            if (paymentRefunded)
            {
                MarkWeightPaymentCancelled(order);
            }

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
                    BuildWeightCancellationMessage("Müşteri iade talebi - otomatik iptal", hadActiveWeightPreAuthorization),
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
            var payment = await GetLatestReversiblePaymentAsync(order.Id);
            var maxRefundableAmount = CalculateRefundAmount(order, payment);

            if (payment != null && refundAmount > maxRefundableAmount)
            {
                return RefundRequestResult.Failed(
                    $"İade tutarı en fazla {maxRefundableAmount:C} olabilir.",
                    "INVALID_AMOUNT");
            }

            bool refundSuccess = false;

            if (payment != null && order.PaymentMethod?.ToLower() != "cash_on_delivery"
                                && order.PaymentMethod?.ToLower() != "kapida_odeme")
            {
                // MADDE 10: Orijinal sale/capt ödeme kaydını al
                var originalPayment = await GetOriginalSaleOrCaptPaymentAsync(order.Id);
                var paymentForRefund = originalPayment ?? payment;

                try
                {
                    if (IsPaymentInAuthOnlyState(order, payment))
                    {
                        // Provizyon durumu → return yerine reverse(auth)
                        _logger.LogInformation(
                            "[İADE-ADMIN] Sipariş Auth durumunda - reverse(auth) yapılıyor. " +
                            "OrderId={OrderId}", order.Id);

                        var cancelResult = await _paymentService.CancelPaymentAsync(
                            paymentForRefund.Id, $"Admin iptal (provizyon reverse): {dto.AdminNote}");
                        if (cancelResult)
                        {
                            refundSuccess = true;
                            refundRequest.TransactionType = "reverse";
                            refundRequest.PosnetHostLogKey = order.PreAuthHostLogKey ?? paymentForRefund.HostLogKey;
                            refundRequest.RefundedAt = DateTime.UtcNow;
                            refundRequest.Status = RefundRequestStatus.Refunded;
                        }
                        else
                        {
                            refundRequest.Status = RefundRequestStatus.RefundFailed;
                            refundRequest.RefundFailureReason = "Provizyon iptali (reverse auth) başarısız.";
                        }
                    }
                    else
                    {
                        var useSameDayReverse = ShouldUseSameDayReverseForCapturedPayment(
                            paymentForRefund,
                            refundAmount,
                            maxRefundableAmount);

                        bool result;
                        if (useSameDayReverse)
                        {
                            result = await _paymentService.CancelPaymentAsync(
                                paymentForRefund.Id,
                                $"Admin iade (aynı gün reverse): {dto.AdminNote}");
                            refundRequest.TransactionType = result ? "reverse" : "none";
                        }
                        else
                        {
                            result = await _paymentService.PartialRefundAsync(paymentForRefund.Id, refundAmount);
                            refundRequest.TransactionType = result ? "return" : "none";
                        }

                        if (result)
                        {
                            refundSuccess = true;
                            refundRequest.PosnetHostLogKey = paymentForRefund.HostLogKey;
                            refundRequest.RefundedAt = DateTime.UtcNow;
                            refundRequest.Status = RefundRequestStatus.Refunded;

                            _logger.LogInformation(
                                useSameDayReverse
                                    ? "[İADE] POSNET reverse başarılı. OrderId={OrderId}, Amount={Amount}, OriginalPaymentId={PaymentId}"
                                    : "[İADE] POSNET return başarılı. OrderId={OrderId}, Amount={Amount}, OriginalPaymentId={PaymentId}",
                                order.Id, refundAmount, paymentForRefund.Id);
                        }
                        else
                        {
                            refundRequest.Status = RefundRequestStatus.RefundFailed;
                            refundRequest.RefundFailureReason = useSameDayReverse
                                ? "POSNET aynı gün reverse işlemi başarısız oldu."
                                : "POSNET iade işlemi başarısız oldu.";
                            _logger.LogWarning(
                                useSameDayReverse
                                    ? "[İADE] POSNET reverse başarısız. OrderId={OrderId}"
                                    : "[İADE] POSNET return başarısız. OrderId={OrderId}",
                                order.Id);
                        }
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
                order.CaptureStatus = CaptureStatus.PartialCapture;

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

            if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Completed)
            {
                _logger.LogInformation(
                    "[İADE-ADMIN] Teslim edilmiş sipariş için cancel isteği refund akışına yönlendiriliyor. OrderId={OrderId}, Status={Status}",
                    orderId,
                    order.Status);

                return await AdminRefundOrderAsync(orderId, adminUserId, reason);
            }

            _logger.LogInformation(
                "[İADE-ADMIN] Admin siparişi iptal ediyor. OrderId={OrderId}, AdminId={AdminId}, Status={Status}",
                orderId, adminUserId, order.Status);

            var previousStatus = order.Status;
            var hadActiveWeightPreAuthorization = HasActiveWeightPreAuthorization(order);

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

            var manualRefundRequired = IsCashOnDelivery(order.PaymentMethod);
            refundRequest.ManualRefundRequired = manualRefundRequired;
            if (manualRefundRequired)
            {
                refundRequest.AdminNote = $"Admin #{adminUserId} tarafından iptal edildi. Kapıda ödeme yapılmışsa müşteriye manuel iade yapılmalı.";
            }

            // Ödeme kaydını bul
            var payment = await GetLatestReversiblePaymentAsync(orderId);
            var refundAmount = CalculateRefundAmount(order, payment);
            refundRequest.RefundAmount = refundAmount;

            bool paymentRefunded = false;
            string transactionType = "none";

            // Kredi kartı ödemesi varsa para iadesini yap
            if (payment != null && !manualRefundRequired)
            {
                try
                {
                    var originalPayment = await GetOriginalSaleOrCaptPaymentAsync(orderId);
                    var paymentForRefund = originalPayment ?? payment;
                    var useSameDayReverse = ShouldUseSameDayReverseForCapturedPayment(
                        paymentForRefund,
                        refundAmount,
                        refundAmount);

                    if (useSameDayReverse)
                    {
                        var cancelResult = await _paymentService.CancelPaymentAsync(
                            paymentForRefund.Id, $"Admin iptal: {reason}");
                        if (cancelResult)
                        {
                            paymentRefunded = true;
                            transactionType = "reverse";
                            refundRequest.PosnetHostLogKey = paymentForRefund.HostLogKey;
                            _logger.LogInformation(
                                "[İADE-ADMIN] POSNET reverse başarılı. OrderId={OrderId}", orderId);
                        }
                        else
                        {
                            var refundResult = await _paymentService.PartialRefundAsync(
                                paymentForRefund.Id, refundAmount);
                            if (refundResult)
                            {
                                paymentRefunded = true;
                                transactionType = "return";
                                refundRequest.PosnetHostLogKey = paymentForRefund.HostLogKey;
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
                    else
                    {
                        var refundResult = await _paymentService.PartialRefundAsync(
                            paymentForRefund.Id, refundAmount);
                        if (refundResult)
                        {
                            paymentRefunded = true;
                            transactionType = "return";
                            refundRequest.PosnetHostLogKey = paymentForRefund.HostLogKey;
                            _logger.LogInformation(
                                "[İADE-ADMIN] POSNET return başarılı. OrderId={OrderId}", orderId);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[İADE-ADMIN] POSNET return başarısız. OrderId={OrderId}", orderId);
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
            if (paymentRefunded)
            {
                MarkWeightPaymentCancelled(order);
            }

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
                    BuildWeightCancellationMessage($"Admin tarafından iptal edildi: {reason}", hadActiveWeightPreAuthorization),
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
        public async Task<RefundRequestResult> AdminRefundOrderAsync(
            int orderId, int adminUserId, string reason)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return RefundRequestResult.Failed("Sipariş bulunamadı.", "ORDER_NOT_FOUND");

            if (order.Status == OrderStatus.Refunded)
                return RefundRequestResult.Failed("Bu sipariş için zaten iade yapılmış.", "ALREADY_REFUNDED");

            var payment = await GetLatestReversiblePaymentAsync(orderId);
            var refundAmount = CalculateRefundAmount(order, payment);

            if (refundAmount <= 0)
                return RefundRequestResult.Failed("İade edilebilir tutar bulunamadı.", "INVALID_AMOUNT");

            var previousStatus = order.Status;
            var refundRequest = new RefundRequest
            {
                OrderId = orderId,
                UserId = order.UserId,
                Reason = reason,
                RefundType = "full",
                RefundAmount = refundAmount,
                OrderStatusAtRequest = order.Status.ToString(),
                RequestedAt = DateTime.UtcNow,
                ProcessedByUserId = adminUserId,
                ProcessedAt = DateTime.UtcNow,
                AdminNote = $"Admin #{adminUserId} tarafından tam iade uygulandı.",
                Status = RefundRequestStatus.Approved
            };

            var manualRefundRequired = IsCashOnDelivery(order.PaymentMethod);
            refundRequest.ManualRefundRequired = manualRefundRequired;
            if (manualRefundRequired)
            {
                refundRequest.AdminNote = $"Admin #{adminUserId} tarafından tam iade uygulandı. Kapıda ödeme yapılmışsa müşteriye manuel iade yapılmalı.";
            }

            var refundSuccess = false;
            var transactionType = "none";
            var isCardPayment = !manualRefundRequired;

            if (payment != null && isCardPayment)
            {
                try
                {
                    var originalPayment = await GetOriginalSaleOrCaptPaymentAsync(orderId);
                    var paymentForRefund = originalPayment ?? payment;
                    var hasCapture = order.CapturedAmount > 0 || paymentForRefund.CapturedAmount > 0 || paymentForRefund.Status != "Authorized";

                    if (!hasCapture)
                    {
                        refundSuccess = await _paymentService.CancelPaymentAsync(
                            paymentForRefund.Id,
                            $"Admin refund: {reason}");
                        transactionType = refundSuccess ? "reverse" : "none";
                    }
                    else
                    {
                        var useSameDayReverse = ShouldUseSameDayReverseForCapturedPayment(
                            paymentForRefund,
                            refundAmount,
                            refundAmount);

                        if (useSameDayReverse)
                        {
                            refundSuccess = await _paymentService.CancelPaymentAsync(
                                paymentForRefund.Id,
                                $"Admin refund same-day reverse: {reason}");
                            transactionType = refundSuccess ? "reverse" : "none";
                        }
                        else
                        {
                            refundSuccess = await _paymentService.PartialRefundAsync(paymentForRefund.Id, refundAmount);
                            transactionType = refundSuccess ? "return" : "none";
                        }
                    }

                    if (refundSuccess)
                    {
                        refundRequest.PosnetHostLogKey = paymentForRefund.HostLogKey;
                        refundRequest.RefundedAt = DateTime.UtcNow;
                        refundRequest.Status = RefundRequestStatus.Refunded;
                    }
                    else
                    {
                        refundRequest.Status = RefundRequestStatus.RefundFailed;
                        refundRequest.RefundFailureReason = "POSNET iade işlemi başarısız oldu.";
                    }
                }
                catch (Exception ex)
                {
                    refundRequest.Status = RefundRequestStatus.RefundFailed;
                    refundRequest.RefundFailureReason = $"Hata: {ex.Message}";
                    _logger.LogError(ex,
                        "[İADE-ADMIN] Tam iade hatası. OrderId={OrderId}", orderId);
                }
            }
            else
            {
                refundSuccess = true;
                refundRequest.RefundedAt = DateTime.UtcNow;
                refundRequest.Status = RefundRequestStatus.Refunded;
            }

            refundRequest.TransactionType = transactionType;

            if (refundSuccess)
            {
                order.Status = OrderStatus.Refunded;
                order.RefundedAt = DateTime.UtcNow;

                if (payment?.Status == "Authorized" && order.CapturedAmount <= 0 && payment.CapturedAmount <= 0)
                {
                    MarkWeightPaymentCancelled(order);
                }

                await RestoreStockAsync(order);

                _db.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = orderId,
                    PreviousStatus = previousStatus,
                    NewStatus = OrderStatus.Refunded,
                    ChangedBy = $"Admin #{adminUserId}",
                    Reason = $"Admin tam iade: {reason}",
                    ChangedAt = DateTime.UtcNow
                });
            }

            _db.RefundRequests.Add(refundRequest);
            await _db.SaveChangesAsync();

            if (refundSuccess)
            {
                try
                {
                    await _notificationService.NotifyOrderStatusChangedAsync(
                        orderId,
                        order.OrderNumber ?? $"#{orderId}",
                        OrderStatus.Refunded.ToString(),
                        "İade işleminiz tamamlandı.",
                        null);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[İADE-ADMIN] Tam iade bildirim hatası. OrderId={OrderId}", orderId);
                }
            }

            var resultDto = MapToDto(refundRequest, order);
            var message = refundSuccess
                ? "Sipariş için para iadesi tamamlandı."
                : "Para iadesi başarısız oldu. Tekrar denenebilir.";

            return RefundRequestResult.Succeeded(resultDto, message);
        }

        /// <inheritdoc />
        public async Task<RefundRequestResult> AdminRefundOrderItemsAsync(
            int orderId, int adminUserId, AdminItemRefundRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Reason))
            {
                return RefundRequestResult.Failed("İade sebebi zorunludur.", "INVALID_REQUEST");
            }

            if (dto.Items == null || dto.Items.Count == 0)
            {
                return RefundRequestResult.Failed("İade edilecek en az bir ürün seçilmelidir.", "NO_ITEMS_SELECTED");
            }

            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return RefundRequestResult.Failed("Sipariş bulunamadı.", "ORDER_NOT_FOUND");
            }

            if (order.Status == OrderStatus.Refunded)
            {
                return RefundRequestResult.Failed("Sipariş zaten tamamen iade edilmiş.", "ALREADY_REFUNDED");
            }

            var refundableRequests = await _db.RefundRequests
                .Where(r => r.OrderId == orderId && r.Status == RefundRequestStatus.Refunded)
                .ToListAsync();

            var refundedQuantities = new Dictionary<int, int>();
            foreach (var request in refundableRequests)
            {
                foreach (var refundedItem in ExtractRefundItems(request))
                {
                    refundedQuantities[refundedItem.OrderItemId] =
                        refundedQuantities.GetValueOrDefault(refundedItem.OrderItemId) + refundedItem.Quantity;
                }
            }

            var selectedItems = new List<RefundRequestItemDto>();
            foreach (var requestedItem in dto.Items.Where(i => i.OrderItemId > 0 && i.Quantity > 0))
            {
                var orderItem = order.OrderItems.FirstOrDefault(oi => oi.Id == requestedItem.OrderItemId);
                if (orderItem == null)
                {
                    return RefundRequestResult.Failed($"Sipariş kalemi bulunamadı: {requestedItem.OrderItemId}", "ORDER_ITEM_NOT_FOUND");
                }

                var alreadyRefundedQty = refundedQuantities.GetValueOrDefault(orderItem.Id);
                var remainingQty = Math.Max(0, orderItem.Quantity - alreadyRefundedQty);
                if (requestedItem.Quantity > remainingQty)
                {
                    return RefundRequestResult.Failed(
                        $"{orderItem.Product?.Name ?? "Ürün"} için en fazla {remainingQty} adet iade edilebilir.",
                        "INVALID_ITEM_QUANTITY");
                }

                var lineAmount = CalculateOrderItemRefundAmount(orderItem, requestedItem.Quantity);
                selectedItems.Add(new RefundRequestItemDto
                {
                    OrderItemId = orderItem.Id,
                    Quantity = requestedItem.Quantity,
                    ProductName = orderItem.Product?.Name ?? requestedItem.ProductName,
                    VariantTitle = orderItem.VariantTitle,
                    UnitAmount = requestedItem.Quantity > 0
                        ? Math.Round(lineAmount / requestedItem.Quantity, 2, MidpointRounding.AwayFromZero)
                        : 0m,
                    LineAmount = lineAmount
                });
            }

            if (selectedItems.Count == 0)
            {
                return RefundRequestResult.Failed("Geçerli iade satırı bulunamadı.", "NO_VALID_ITEMS");
            }

            var payment = await GetLatestReversiblePaymentAsync(orderId);
            var remainingRefundableAmount = CalculateRefundAmount(order, payment) - (payment?.RefundedAmount ?? 0m);
            var refundAmount = Math.Round(selectedItems.Sum(x => x.LineAmount), 2, MidpointRounding.AwayFromZero);

            if (refundAmount <= 0)
            {
                return RefundRequestResult.Failed("Hesaplanan iade tutarı geçersiz.", "INVALID_AMOUNT");
            }

            if (refundAmount > remainingRefundableAmount + 0.01m)
            {
                return RefundRequestResult.Failed(
                    $"İade edilebilir kalan tutar {remainingRefundableAmount:C} ile sınırlıdır.",
                    "AMOUNT_EXCEEDS_REMAINING");
            }

            if (IsPaymentInAuthOnlyState(order, payment) &&
                refundAmount < remainingRefundableAmount - 0.01m)
            {
                return RefundRequestResult.Failed(
                    "Provizyon aşamasındaki siparişte ürün bazlı kısmi iade yerine teslim sonrası kesin tutar düzenlenmelidir.",
                    "AUTH_ONLY_PARTIAL_NOT_SUPPORTED");
            }

            var refundRequest = new RefundRequest
            {
                OrderId = orderId,
                UserId = order.UserId,
                Reason = dto.Reason.Trim(),
                RefundType = "partial",
                RefundAmount = refundAmount,
                OrderStatusAtRequest = order.Status.ToString(),
                RequestedAt = DateTime.UtcNow,
                ProcessedByUserId = adminUserId,
                ProcessedAt = DateTime.UtcNow,
                AdminNote = AttachRefundItemsToAdminNote(dto.AdminNote, selectedItems),
                Status = RefundRequestStatus.Approved
            };

            var refundSuccess = false;
            var transactionType = "none";

            if (payment != null && !IsCashOnDelivery(order.PaymentMethod))
            {
                try
                {
                    var originalPayment = await GetOriginalSaleOrCaptPaymentAsync(orderId);
                    var paymentForRefund = originalPayment ?? payment;

                    if (IsPaymentInAuthOnlyState(order, payment))
                    {
                        refundSuccess = await _paymentService.CancelPaymentAsync(
                            paymentForRefund.Id,
                            $"Admin ürün bazlı tam reverse: {dto.Reason}");
                        transactionType = refundSuccess ? "reverse" : "none";
                    }
                    else
                    {
                        refundSuccess = await _paymentService.PartialRefundAsync(paymentForRefund.Id, refundAmount);
                        transactionType = refundSuccess ? "return" : "none";
                    }

                    if (refundSuccess)
                    {
                        refundRequest.PosnetHostLogKey = paymentForRefund.HostLogKey;
                        refundRequest.RefundedAt = DateTime.UtcNow;
                        refundRequest.Status = RefundRequestStatus.Refunded;
                    }
                    else
                    {
                        refundRequest.Status = RefundRequestStatus.RefundFailed;
                        refundRequest.RefundFailureReason = "POSNET ürün bazlı iade işlemi başarısız oldu.";
                    }
                }
                catch (Exception ex)
                {
                    refundRequest.Status = RefundRequestStatus.RefundFailed;
                    refundRequest.RefundFailureReason = $"Hata: {ex.Message}";
                    _logger.LogError(ex, "[İADE-ITEM] Ürün bazlı iade hatası. OrderId={OrderId}", orderId);
                }
            }
            else
            {
                refundSuccess = true;
                refundRequest.RefundedAt = DateTime.UtcNow;
                refundRequest.Status = RefundRequestStatus.Refunded;
            }

            refundRequest.TransactionType = transactionType;

            if (refundSuccess)
            {
                await RestoreStockAsync(order, selectedItems);

                var previousStatus = order.Status;
                var totalRefundableAmount = CalculateRefundAmount(order, payment);
                var newRefundedTotal = (payment?.RefundedAmount ?? 0m) + refundAmount;

                if (newRefundedTotal >= totalRefundableAmount - 0.01m)
                {
                    order.Status = OrderStatus.Refunded;
                    order.RefundedAt = DateTime.UtcNow;
                }
                else
                {
                    order.Status = OrderStatus.PartialRefund;
                    order.CaptureStatus = CaptureStatus.PartialCapture;
                }

                _db.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = orderId,
                    PreviousStatus = previousStatus,
                    NewStatus = order.Status,
                    ChangedBy = $"Admin #{adminUserId}",
                    Reason = $"Ürün bazlı iade uygulandı - {refundAmount:C}",
                    ChangedAt = DateTime.UtcNow
                });
            }

            _db.RefundRequests.Add(refundRequest);
            await _db.SaveChangesAsync();

            var resultDto = MapToDto(refundRequest, order);
            var message = refundSuccess
                ? "Seçilen ürünler için kısmi iade tamamlandı."
                : "Ürün bazlı iade başarısız oldu. Tekrar denenebilir.";

            return RefundRequestResult.Succeeded(resultDto, message);
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

        private async Task RestoreStockAsync(Order order, IReadOnlyCollection<RefundRequestItemDto> refundedItems)
        {
            if (refundedItems == null || refundedItems.Count == 0)
            {
                return;
            }

            var orderItems = (order.OrderItems != null && order.OrderItems.Any())
                ? order.OrderItems.ToList()
                : await _db.OrderItems.Where(oi => oi.OrderId == order.Id).ToListAsync();

            foreach (var refundedItem in refundedItems)
            {
                var orderItem = orderItems.FirstOrDefault(oi => oi.Id == refundedItem.OrderItemId);
                if (orderItem == null || refundedItem.Quantity <= 0)
                {
                    continue;
                }

                if (orderItem.ProductVariantId.HasValue && orderItem.ProductVariantId > 0)
                {
                    var stock = await _db.Stocks.FirstOrDefaultAsync(
                        s => s.ProductVariantId == orderItem.ProductVariantId.Value);
                    if (stock != null)
                    {
                        stock.Quantity += refundedItem.Quantity;
                    }
                }
                else
                {
                    var variant = await _db.ProductVariants
                        .Where(v => v.ProductId == orderItem.ProductId)
                        .FirstOrDefaultAsync();

                    if (variant != null)
                    {
                        var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.ProductVariantId == variant.Id);
                        if (stock != null)
                        {
                            stock.Quantity += refundedItem.Quantity;
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
                AdminNote = ExtractVisibleAdminNote(request.AdminNote),
                PosnetHostLogKey = request.PosnetHostLogKey,
                TransactionType = request.TransactionType,
                RefundedAt = request.RefundedAt,
                RefundFailureReason = request.RefundFailureReason,
                OrderTotalPrice = order?.FinalPrice ?? 0,
                OrderStatus = order?.Status.ToString(),
                Items = ExtractRefundItems(request)
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
