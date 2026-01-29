using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Courier;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Kurye sipariş işlemlerini yöneten servis implementasyonu.
    /// 
    /// Güvenlik Özellikleri:
    /// - Ownership kontrolü: Her işlemde order.CourierId == courierId kontrolü
    /// - State machine entegrasyonu: Geçersiz durum geçişleri engellenir
    /// - Audit logging: Tüm aksiyonlar loglanır
    /// 
    /// Tartı Entegrasyonu:
    /// - IWeightService üzerinden tartı farkı hesaplaması
    /// - Final tutar = Order.TotalPrice + WeightDifference
    /// 
    /// Performans:
    /// - Eager loading ile N+1 sorgu engellenir
    /// - Gerekli alanlar için projection kullanılır
    /// </summary>
    public class CourierOrderManager : ICourierOrderService
    {
        private readonly ECommerceDbContext _context;
        private readonly IOrderStateMachine _orderStateMachine;
        private readonly IPaymentCaptureService _paymentCaptureService;
        private readonly IRealTimeNotificationService _notificationService;
        private readonly IWeightService _weightService;
        private readonly ILogger<CourierOrderManager> _logger;

        public CourierOrderManager(
            ECommerceDbContext context,
            IOrderStateMachine orderStateMachine,
            IPaymentCaptureService paymentCaptureService,
            IRealTimeNotificationService notificationService,
            IWeightService weightService,
            ILogger<CourierOrderManager> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _orderStateMachine = orderStateMachine ?? throw new ArgumentNullException(nameof(orderStateMachine));
            _paymentCaptureService = paymentCaptureService ?? throw new ArgumentNullException(nameof(paymentCaptureService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _weightService = weightService ?? throw new ArgumentNullException(nameof(weightService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Sipariş Listeleme

        /// <summary>
        /// Kuryeye atanan siparişleri listeler.
        /// </summary>
        public async Task<CourierOrderListResponseDto> GetAssignedOrdersAsync(int courierId, CourierOrderFilterDto? filter = null)
        {
            try
            {
                _logger.LogInformation("🔍 Kurye #{CourierId} için sipariş listesi istendi", courierId);

                filter ??= new CourierOrderFilterDto();

                // DEBUG: Tüm siparişlerde bu kuryeye atanan var mı kontrol et
                var allOrdersWithCourier = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.CourierId != null)
                    .Select(o => new { o.Id, o.CourierId, o.Status })
                    .ToListAsync();
                _logger.LogInformation("🔍 Kurye atanmış tüm siparişler: {@Orders}", allOrdersWithCourier);
                
                var ordersForThisCourier = allOrdersWithCourier.Where(o => o.CourierId == courierId).ToList();
                _logger.LogInformation("🔍 Bu kuryeye ({CourierId}) atanan siparişler: {@Orders}", courierId, ordersForThisCourier);

                // Temel sorgu - sadece bu kuryeye atanan siparişler
                var query = _context.Orders
                    .AsNoTracking()
                    .Include(o => o.OrderItems)
                    .Where(o => o.CourierId == courierId);

                // Durum filtresi
                if (!string.IsNullOrWhiteSpace(filter.Status))
                {
                    if (Enum.TryParse<OrderStatus>(filter.Status, true, out var statusEnum))
                    {
                        query = query.Where(o => o.Status == statusEnum);
                    }
                }

                // Öncelik filtresi
                if (!string.IsNullOrWhiteSpace(filter.Priority))
                {
                    query = query.Where(o => o.Priority == filter.Priority);
                }

                // Tarih filtresi
                if (filter.FromDate.HasValue)
                {
                    query = query.Where(o => o.AssignedAt >= filter.FromDate.Value);
                }
                if (filter.ToDate.HasValue)
                {
                    query = query.Where(o => o.AssignedAt <= filter.ToDate.Value);
                }

                // Toplam kayıt sayısı
                var totalCount = await query.CountAsync();

                // Sıralama: Öncelik (urgent önce), sonra atanma tarihi
                query = query.OrderByDescending(o => o.Priority == "urgent")
                             .ThenByDescending(o => o.AssignedAt);

                // Sayfalama
                var orders = await query
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                // DTO'ya dönüştür
                var orderDtos = orders.Select(MapToListDto).ToList();

                // Özet istatistikler
                var summary = await GetDailySummaryAsync(courierId);

                return new CourierOrderListResponseDto
                {
                    Orders = orderDtos,
                    TotalCount = totalCount,
                    CurrentPage = filter.Page,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize),
                    Summary = summary
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye #{CourierId} sipariş listesi alınırken hata", courierId);
                throw;
            }
        }

        /// <summary>
        /// Belirli bir siparişin detayını getirir.
        /// </summary>
        public async Task<CourierOrderDetailDto?> GetOrderDetailAsync(int orderId, int courierId)
        {
            try
            {
                _logger.LogInformation("Kurye #{CourierId} sipariş #{OrderId} detayı istedi", courierId, orderId);

                var order = await _context.Orders
                    .AsNoTracking()
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    _logger.LogWarning("Sipariş #{OrderId} bulunamadı", orderId);
                    return null;
                }

                // Ownership kontrolü
                if (order.CourierId != courierId)
                {
                    _logger.LogWarning("Yetkisiz erişim denemesi: Kurye #{CourierId} -> Sipariş #{OrderId}", courierId, orderId);
                    return null; // Yetkisiz erişimde null dön (güvenlik için detay verme)
                }

                return MapToDetailDto(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş #{OrderId} detayı alınırken hata", orderId);
                throw;
            }
        }

        #endregion

        #region Sipariş Aksiyonları

        /// <summary>
        /// Kurye teslimat için yola çıktığını bildirir.
        /// </summary>
        public async Task<CourierOrderActionResponseDto> StartDeliveryAsync(int orderId, int courierId, StartDeliveryDto dto)
        {
            try
            {
                _logger.LogInformation("Kurye #{CourierId} sipariş #{OrderId} için yola çıkıyor", courierId, orderId);

                // 1. Sipariş ve ownership kontrolü
                var order = await GetOrderWithOwnershipCheckAsync(orderId, courierId);
                if (order == null)
                {
                    return CreateFailResponse(orderId, "Sipariş bulunamadı veya bu siparişe erişim yetkiniz yok.");
                }

                var previousStatus = order.Status;

                // 2. Durum geçiş kontrolü (ASSIGNED → OUT_FOR_DELIVERY)
                if (!_orderStateMachine.CanTransition(order.Status, OrderStatus.OutForDelivery))
                {
                    _logger.LogWarning("Geçersiz durum geçişi: {CurrentStatus} → OUT_FOR_DELIVERY", order.Status);
                    return CreateFailResponse(orderId, $"Sipariş durumu '{_orderStateMachine.GetStatusDescription(order.Status)}' olduğu için yola çıkılamaz.");
                }

                // 3. State machine ile geçiş yap
                var transitionResult = await _orderStateMachine.TransitionAsync(
                    orderId, 
                    OrderStatus.OutForDelivery, 
                    courierId,
                    dto.Note ?? "Kurye yola çıktı",
                    new Dictionary<string, object>
                    {
                        { "Location", dto.CurrentLocation ?? "" },
                        { "ActionType", "StartDelivery" }
                    });

                if (!transitionResult.Success)
                {
                    return CreateFailResponse(orderId, transitionResult.Message ?? "Durum güncellenemedi.");
                }

                // 4. Ek alanları güncelle
                order.PickedUpAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(dto.Note))
                {
                    order.DeliveryNotes = (order.DeliveryNotes ?? "") + $"\n[Yola Çıkış] {dto.Note}";
                }
                await _context.SaveChangesAsync();

                // 5. Tüm taraflara bildirim gönder
                try
                {
                    await _notificationService.NotifyAllPartiesOrderStatusChangedAsync(
                        orderId,
                        order.OrderNumber ?? $"#{orderId}",
                        previousStatus.ToString(),
                        OrderStatus.OutForDelivery.ToString(),
                        $"Courier#{courierId}",
                        courierId);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogWarning(notifyEx, "Sipariş #{OrderId} için durum bildirimi gönderilemedi", orderId);
                }

                _logger.LogInformation("Kurye #{CourierId} sipariş #{OrderId} için yola çıktı", courierId, orderId);

                return new CourierOrderActionResponseDto
                {
                    Success = true,
                    Message = "Teslimat başlatıldı. İyi yolculuklar!",
                    OrderId = orderId,
                    NewStatus = OrderStatus.OutForDelivery.ToString(),
                    NewStatusText = _orderStateMachine.GetStatusDescription(OrderStatus.OutForDelivery),
                    ActionTime = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş #{OrderId} için yola çıkış işlemi başarısız", orderId);
                return CreateFailResponse(orderId, "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin.");
            }
        }

        /// <summary>
        /// Kurye siparişi teslim ettiğini bildirir.
        /// </summary>
        public async Task<CourierOrderActionResponseDto> MarkDeliveredAsync(int orderId, int courierId, MarkDeliveredDto dto)
        {
            try
            {
                _logger.LogInformation("Kurye #{CourierId} sipariş #{OrderId} teslim ediyor", courierId, orderId);

                // 1. Sipariş ve ownership kontrolü
                var order = await GetOrderWithOwnershipCheckAsync(orderId, courierId);
                if (order == null)
                {
                    return CreateFailResponse(orderId, "Sipariş bulunamadı veya bu siparişe erişim yetkiniz yok.");
                }

                var previousStatus = order.Status;

                // 2. Durum geçiş kontrolü (OUT_FOR_DELIVERY → DELIVERED)
                if (!_orderStateMachine.CanTransition(order.Status, OrderStatus.Delivered))
                {
                    _logger.LogWarning("Geçersiz durum geçişi: {CurrentStatus} → DELIVERED", order.Status);
                    return CreateFailResponse(orderId, $"Sipariş durumu '{_orderStateMachine.GetStatusDescription(order.Status)}' olduğu için teslim edilemez.");
                }

                // 3. Final tutarı hesapla (tartı farkı dahil)
                var finalAmount = await CalculateFinalAmountAsync(order, dto.WeightAdjustmentGrams);

                // 4. Payment capture işlemi (Kredi kartı ödemelerinde)
                PaymentCaptureInfo? paymentInfo = null;
                if (order.PaymentStatus == PaymentStatus.Paid || order.AuthorizedAmount > 0)
                {
                    paymentInfo = await ProcessPaymentCaptureAsync(order, finalAmount);

                    // Final > Authorized durumunda DELIVERY_PAYMENT_PENDING'e geç
                    if (paymentInfo.RequiresAdminAction)
                    {
                        _logger.LogWarning("Sipariş #{OrderId}: Final tutar ({FinalAmount}) > Authorize tutar ({AuthorizedAmount})", 
                            orderId, finalAmount, order.AuthorizedAmount);

                        // DELIVERY_PAYMENT_PENDING durumuna geç
                        await _orderStateMachine.TransitionAsync(orderId, OrderStatus.DeliveryPaymentPending, courierId,
                            $"Final tutar ({finalAmount:C}) authorize tutarını ({order.AuthorizedAmount:C}) aştı");

                        // Admin'e bildirim
                        await _notificationService.NotifyPaymentFailedAsync(
                            orderId, 
                            order.OrderNumber ?? $"#{orderId}", 
                            $"Final tutar ({finalAmount:C}) authorize tutarını ({order.AuthorizedAmount:C}) aştı",
                            "PaymentCapture");

                        try
                        {
                            await _notificationService.NotifyAllPartiesOrderStatusChangedAsync(
                                orderId,
                                order.OrderNumber ?? $"#{orderId}",
                                previousStatus.ToString(),
                                OrderStatus.DeliveryPaymentPending.ToString(),
                                $"Courier#{courierId}",
                                courierId);
                        }
                        catch (Exception notifyEx)
                        {
                            _logger.LogWarning(notifyEx, "Sipariş #{OrderId} için durum bildirimi gönderilemedi", orderId);
                        }

                        return new CourierOrderActionResponseDto
                        {
                            Success = true,
                            Message = "Teslimat kaydedildi ancak ödeme işlemi admin onayı bekliyor.",
                            OrderId = orderId,
                            NewStatus = OrderStatus.DeliveryPaymentPending.ToString(),
                            NewStatusText = _orderStateMachine.GetStatusDescription(OrderStatus.DeliveryPaymentPending),
                            ActionTime = DateTime.UtcNow,
                            PaymentInfo = paymentInfo
                        };
                    }
                }

                // 5. State machine ile DELIVERED geçişi yap
                var metadata = new Dictionary<string, object>
                {
                    { "ReceiverName", dto.ReceiverName ?? "" },
                    { "PhotoUrl", dto.PhotoUrl ?? "" },
                    { "CashCollected", dto.CashCollected ?? false },
                    { "CollectedAmount", dto.CollectedAmount ?? 0m },
                    { "ActionType", "MarkDelivered" }
                };

                var transitionResult = await _orderStateMachine.TransitionAsync(
                    orderId, 
                    OrderStatus.Delivered, 
                    courierId,
                    dto.Note ?? "Sipariş teslim edildi",
                    metadata);

                if (!transitionResult.Success)
                {
                    return CreateFailResponse(orderId, transitionResult.Message ?? "Durum güncellenemedi.");
                }

                // 6. Ek alanları güncelle
                order.DeliveredAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(dto.Note))
                {
                    order.DeliveryNotes = (order.DeliveryNotes ?? "") + $"\n[Teslim] {dto.Note}";
                }
                if (!string.IsNullOrWhiteSpace(dto.ReceiverName))
                {
                    order.DeliveryNotes = (order.DeliveryNotes ?? "") + $"\n[Teslim Alan] {dto.ReceiverName}";
                }
                await _context.SaveChangesAsync();

                // 7. Kurye istatistiklerini güncelle
                await UpdateCourierStatsAsync(courierId);

                // 8. Tüm taraflara bildirim gönder
                try
                {
                    await _notificationService.NotifyAllPartiesOrderStatusChangedAsync(
                        orderId,
                        order.OrderNumber ?? $"#{orderId}",
                        previousStatus.ToString(),
                        OrderStatus.Delivered.ToString(),
                        $"Courier#{courierId}",
                        courierId);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogWarning(notifyEx, "Sipariş #{OrderId} için durum bildirimi gönderilemedi", orderId);
                }

                _logger.LogInformation("Kurye #{CourierId} sipariş #{OrderId} teslim etti", courierId, orderId);

                return new CourierOrderActionResponseDto
                {
                    Success = true,
                    Message = "Sipariş başarıyla teslim edildi. Teşekkürler!",
                    OrderId = orderId,
                    NewStatus = OrderStatus.Delivered.ToString(),
                    NewStatusText = _orderStateMachine.GetStatusDescription(OrderStatus.Delivered),
                    ActionTime = DateTime.UtcNow,
                    PaymentInfo = paymentInfo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş #{OrderId} teslim işlemi başarısız", orderId);
                return CreateFailResponse(orderId, "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin.");
            }
        }

        /// <summary>
        /// Kurye teslimat problemi bildirir.
        /// </summary>
        public async Task<CourierOrderActionResponseDto> ReportProblemAsync(int orderId, int courierId, ReportProblemDto dto)
        {
            try
            {
                _logger.LogInformation("Kurye #{CourierId} sipariş #{OrderId} için problem bildiriyor: {Reason}", 
                    courierId, orderId, dto.Reason);

                // 1. "Other" sebebinde açıklama zorunlu
                if (dto.Reason == DeliveryProblemReason.Other && string.IsNullOrWhiteSpace(dto.Description))
                {
                    return CreateFailResponse(orderId, "Lütfen problem sebebini açıklayınız.");
                }

                // 2. Sipariş ve ownership kontrolü
                var order = await GetOrderWithOwnershipCheckAsync(orderId, courierId);
                if (order == null)
                {
                    return CreateFailResponse(orderId, "Sipariş bulunamadı veya bu siparişe erişim yetkiniz yok.");
                }

                // 3. Terminal durumlardan problem bildirilemez
                if (_orderStateMachine.IsTerminalState(order.Status))
                {
                    return CreateFailResponse(orderId, "Bu sipariş için artık problem bildirilemez.");
                }

                var previousStatus = order.Status;

                // 4. State machine ile DELIVERY_FAILED geçişi
                var reasonText = dto.Reason.GetDescription();
                var fullReason = string.IsNullOrWhiteSpace(dto.Description) 
                    ? reasonText 
                    : $"{reasonText}: {dto.Description}";

                var metadata = new Dictionary<string, object>
                {
                    { "ProblemReason", dto.Reason.ToString() },
                    { "ProblemDescription", dto.Description ?? "" },
                    { "PhotoUrl", dto.PhotoUrl ?? "" },
                    { "Location", dto.CurrentLocation ?? "" },
                    { "AttemptedContact", dto.AttemptedToContactCustomer },
                    { "CallAttempts", dto.CallAttempts ?? 0 },
                    { "ActionType", "ReportProblem" }
                };

                var transitionResult = await _orderStateMachine.TransitionAsync(
                    orderId, 
                    OrderStatus.DeliveryFailed, 
                    courierId,
                    fullReason,
                    metadata);

                if (!transitionResult.Success)
                {
                    return CreateFailResponse(orderId, transitionResult.Message ?? "Durum güncellenemedi.");
                }

                // 5. Ek alanları güncelle
                order.DeliveryNotes = (order.DeliveryNotes ?? "") + $"\n[PROBLEM - {DateTime.UtcNow:dd.MM.yyyy HH:mm}] {fullReason}";
                if (!string.IsNullOrWhiteSpace(dto.PhotoUrl))
                {
                    order.DeliveryNotes += $"\n[Fotoğraf] {dto.PhotoUrl}";
                }
                await _context.SaveChangesAsync();

                // 6. Admin'e bildirim gönder
                try
                {
                    // Kurye ID'sinden kurye adını bul (User ilişkisi üzerinden)
                    var courier = await _context.Couriers
                        .Include(c => c.User)
                        .FirstOrDefaultAsync(c => c.Id == courierId);
                    var courierName = courier?.User?.FullName ?? $"Kurye #{courierId}";
                    
                    await _notificationService.NotifyDeliveryProblemToAdminAsync(
                        orderId, 
                        order.OrderNumber ?? $"#{orderId}",
                        dto.Reason.ToString(),
                        courierName,
                        fullReason);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogWarning(notifyEx, "Sipariş #{OrderId} için admin bildirimi gönderilemedi", orderId);
                }

                // 7. Tüm taraflara bildirim gönder
                try
                {
                    await _notificationService.NotifyAllPartiesOrderStatusChangedAsync(
                        orderId,
                        order.OrderNumber ?? $"#{orderId}",
                        previousStatus.ToString(),
                        OrderStatus.DeliveryFailed.ToString(),
                        $"Courier#{courierId}",
                        courierId);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogWarning(notifyEx, "Sipariş #{OrderId} için durum bildirimi gönderilemedi", orderId);
                }

                _logger.LogInformation("Kurye #{CourierId} sipariş #{OrderId} için problem bildirdi: {Reason}", 
                    courierId, orderId, dto.Reason);

                // Yeniden deneme mümkün mü?
                var retryableMessage = dto.Reason.IsRetryable() 
                    ? " Admin yeni bir teslimat planlayacak." 
                    : " Admin sizinle iletişime geçecek.";

                return new CourierOrderActionResponseDto
                {
                    Success = true,
                    Message = $"Problem kaydedildi. {retryableMessage}",
                    OrderId = orderId,
                    NewStatus = OrderStatus.DeliveryFailed.ToString(),
                    NewStatusText = _orderStateMachine.GetStatusDescription(OrderStatus.DeliveryFailed),
                    ActionTime = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş #{OrderId} problem bildirimi başarısız", orderId);
                return CreateFailResponse(orderId, "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin.");
            }
        }

        #endregion

        #region Yardımcı Metotlar

        /// <summary>
        /// Ownership kontrolü ile sipariş getirir.
        /// </summary>
        public async Task<bool> ValidateOrderOwnershipAsync(int orderId, int courierId)
        {
            return await _context.Orders
                .AsNoTracking()
                .AnyAsync(o => o.Id == orderId && o.CourierId == courierId);
        }

        /// <summary>
        /// User ID'den Courier ID'yi bulur.
        /// </summary>
        public async Task<int?> GetCourierIdByUserIdAsync(int userId)
        {
            var courier = await _context.Couriers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            return courier?.Id;
        }

        /// <summary>
        /// Kurye'nin günlük istatistiklerini getirir.
        /// </summary>
        public async Task<CourierOrderSummaryDto> GetDailySummaryAsync(int courierId)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CourierId == courierId)
                .ToListAsync();

            return new CourierOrderSummaryDto
            {
                TodayDelivered = orders.Count(o => 
                    o.Status == OrderStatus.Delivered && 
                    o.DeliveredAt >= today && o.DeliveredAt < tomorrow),
                ActiveOrders = orders.Count(o => 
                    o.Status == OrderStatus.OutForDelivery),
                PendingOrders = orders.Count(o => 
                    o.Status == OrderStatus.Assigned),
                TodayFailed = orders.Count(o => 
                    o.Status == OrderStatus.DeliveryFailed && 
                    o.UpdatedAt >= today && o.UpdatedAt < tomorrow),
                TodayEarnings = orders
                    .Where(o => o.Status == OrderStatus.Delivered && 
                                o.DeliveredAt >= today && o.DeliveredAt < tomorrow)
                    .Sum(o => o.FinalPrice) * 0.05m // %5 kurye komisyonu (örnek)
            };
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Ownership kontrolü ile sipariş getirir.
        /// </summary>
        private async Task<Order?> GetOrderWithOwnershipCheckAsync(int orderId, int courierId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || order.CourierId != courierId)
            {
                return null;
            }

            return order;
        }

        /// <summary>
        /// Final tutarı hesaplar (tartı farkı dahil).
        /// 
        /// Hesaplama Akışı:
        /// 1. IWeightService üzerinden toplam tartı farkı tutarını al
        /// 2. Kurye'den gelen manuel fark (weightAdjustmentGrams) varsa ekle
        /// 3. Final = Order.FinalPrice + WeightDifferenceAmount + ManualAdjustment
        /// </summary>
        private async Task<decimal> CalculateFinalAmountAsync(Order order, int? weightAdjustmentGrams)
        {
            // 1. WeightService'den tartı bazlı final tutarı al
            var finalAmount = await _weightService.CalculateFinalAmountForOrderAsync(order.Id);
            
            // Eğer WeightService 0 döndüyse (rapor yoksa), order.FinalPrice kullan
            if (finalAmount == 0)
            {
                finalAmount = order.FinalPrice;
            }

            // 2. Kurye'den gelen manuel tartı farkı varsa ekle
            if (weightAdjustmentGrams.HasValue && weightAdjustmentGrams.Value != 0)
            {
                // Sipariş bazında gram başına fiyat hesapla
                var expectedWeight = order.OrderItems?.Sum(oi => oi.ExpectedWeightGrams) ?? 0;
                decimal pricePerGram;
                
                if (expectedWeight > 0)
                {
                    pricePerGram = order.TotalPrice / expectedWeight;
                }
                else
                {
                    // Fallback: Varsayılan fiyat (1 TL/gram)
                    pricePerGram = 0.001m;
                }
                
                var manualAdjustment = weightAdjustmentGrams.Value * pricePerGram;
                finalAmount += manualAdjustment;

                _logger.LogInformation("Sipariş #{OrderId}: Manuel tartı farkı {Grams}g = {Amount:C} (gram başı {PricePerGram:C4})", 
                    order.Id, weightAdjustmentGrams.Value, manualAdjustment, pricePerGram);
            }

            // 3. Bekleyen raporları logla (info amaçlı)
            var pendingReports = await _weightService.GetPendingReportsForOrderAsync(order.Id);
            if (pendingReports.Any())
            {
                _logger.LogWarning("Sipariş #{OrderId}: {Count} adet bekleyen tartı raporu var. Admin onayı bekleniyor.",
                    order.Id, pendingReports.Count());
            }

            _logger.LogInformation("Sipariş #{OrderId}: Final tutar hesaplandı. Base={Base:C}, Final={Final:C}",
                order.Id, order.FinalPrice, finalAmount);

            return Math.Max(0, finalAmount);
        }

        /// <summary>
        /// Payment capture işlemi yapar.
        /// </summary>
        private async Task<PaymentCaptureInfo> ProcessPaymentCaptureAsync(Order order, decimal finalAmount)
        {
            try
            {
                var captureResult = await _paymentCaptureService.CapturePaymentAsync(order.Id, finalAmount);

                return new PaymentCaptureInfo
                {
                    CaptureSuccess = captureResult.Success,
                    CapturedAmount = captureResult.CapturedAmount,
                    AdditionalAmount = finalAmount - order.FinalPrice,
                    CaptureMessage = captureResult.Message,
                    RequiresAdminAction = !captureResult.Success && 
                        captureResult.ErrorCode == "AMOUNT_EXCEEDS_AUTHORIZED"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş #{OrderId} payment capture hatası", order.Id);
                return new PaymentCaptureInfo
                {
                    CaptureSuccess = false,
                    CaptureMessage = "Ödeme işlemi sırasında hata oluştu",
                    RequiresAdminAction = true
                };
            }
        }

        /// <summary>
        /// Kurye istatistiklerini günceller.
        /// </summary>
        private async Task UpdateCourierStatsAsync(int courierId)
        {
            try
            {
                var courier = await _context.Couriers.FindAsync(courierId);
                if (courier != null)
                {
                    courier.CompletedToday++;
                    courier.ActiveOrders = Math.Max(0, courier.ActiveOrders - 1);
                    courier.LastActiveAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kurye #{CourierId} istatistikleri güncellenemedi", courierId);
            }
        }

        /// <summary>
        /// Order'ı liste DTO'suna dönüştürür.
        /// </summary>
        private CourierOrderListDto MapToListDto(Order order)
        {
            var (statusColor, statusText) = GetStatusDisplayInfo(order.Status);

            return new CourierOrderListDto
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber ?? $"#{order.Id}",
                CustomerName = MaskCustomerName(order.CustomerName),
                AddressSummary = TruncateAddress(order.ShippingAddress, 50),
                TotalAmount = order.FinalPrice,
                Currency = order.Currency,
                Status = order.Status.ToString(),
                StatusColor = statusColor,
                StatusText = statusText,
                PaymentMethod = order.PaymentStatus == PaymentStatus.Paid ? "Kredi Kartı" : "Kapıda Ödeme",
                PaymentStatus = order.PaymentStatus.ToString(),
                Priority = order.Priority,
                AssignedAt = order.AssignedAt,
                OrderDate = order.OrderDate,
                EstimatedDelivery = order.EstimatedDelivery,
                ItemCount = order.OrderItems?.Count ?? 0,
                HasCustomerNote = !string.IsNullOrWhiteSpace(order.DeliveryNotes)
            };
        }

        /// <summary>
        /// Order'ı detay DTO'suna dönüştürür.
        /// </summary>
        private CourierOrderDetailDto MapToDetailDto(Order order)
        {
            var (_, statusText) = GetStatusDisplayInfo(order.Status);

            var detail = new CourierOrderDetailDto
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber ?? $"#{order.Id}",
                Status = order.Status.ToString(),
                StatusText = statusText,
                CustomerName = order.CustomerName ?? "Müşteri",
                CustomerPhone = order.CustomerPhone ?? "",
                CustomerEmail = order.CustomerEmail,
                FullAddress = order.ShippingAddress,
                City = order.ShippingCity,
                GoogleMapsUrl = BuildGoogleMapsUrl(order.ShippingAddress, order.ShippingCity),
                TotalAmount = order.FinalPrice,
                Currency = order.Currency,
                PaymentMethod = order.PaymentStatus == PaymentStatus.Paid ? "Kredi Kartı" : "Kapıda Ödeme",
                PaymentStatus = order.PaymentStatus.ToString(),
                PaymentInfo = GetPaymentInfoText(order),
                CashOnDeliveryAmount = order.PaymentStatus != PaymentStatus.Paid ? order.FinalPrice : null,
                OrderDate = order.OrderDate,
                AssignedAt = order.AssignedAt,
                PickedUpAt = order.PickedUpAt,
                DeliveredAt = order.DeliveredAt,
                EstimatedDelivery = order.EstimatedDelivery,
                Priority = order.Priority,
                CustomerNote = order.DeliveryNotes,
                Items = order.OrderItems?.Select(MapToItemDto).ToList() ?? new List<CourierOrderItemDto>(),
                AllowedActions = new CourierAllowedActions
                {
                    CanStartDelivery = order.Status == OrderStatus.Assigned,
                    CanMarkDelivered = order.Status == OrderStatus.OutForDelivery,
                    CanReportProblem = !_orderStateMachine.IsTerminalState(order.Status),
                    CanCallCustomer = !string.IsNullOrWhiteSpace(order.CustomerPhone),
                    CanShowOnMap = !string.IsNullOrWhiteSpace(order.ShippingAddress)
                }
            };

            return detail;
        }

        /// <summary>
        /// OrderItem'ı DTO'ya dönüştürür.
        /// </summary>
        private CourierOrderItemDto MapToItemDto(OrderItem item)
        {
            // Total price hesaplama: ActualPrice varsa onu, yoksa EstimatedPrice veya UnitPrice * Quantity kullan
            var totalPrice = item.ActualPrice ?? item.EstimatedPrice;
            if (totalPrice == 0)
            {
                totalPrice = item.UnitPrice * item.Quantity;
            }

            // Birim: Ağırlık bazlı ise WeightUnit'ten al, değilse "adet"
            var unit = item.IsWeightBased 
                ? item.WeightUnit.ToString().ToLowerInvariant() 
                : "adet";

            return new CourierOrderItemDto
            {
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? "Ürün",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = totalPrice,
                Unit = unit,
                HasWeightDifference = item.ActualWeight.HasValue && item.WeightDifference.HasValue && item.WeightDifference.Value != 0,
                ExpectedWeightGrams = item.IsWeightBased ? (int?)item.EstimatedWeight : item.ExpectedWeightGrams,
                ActualWeightGrams = item.ActualWeight
            };
        }

        /// <summary>
        /// Durum için renk ve metin bilgisi döner.
        /// </summary>
        private (string color, string text) GetStatusDisplayInfo(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Assigned => ("yellow", "Atandı"),
                OrderStatus.OutForDelivery => ("blue", "Yolda"),
                OrderStatus.Delivered => ("green", "Teslim Edildi"),
                OrderStatus.DeliveryFailed => ("red", "Teslimat Başarısız"),
                OrderStatus.DeliveryPaymentPending => ("orange", "Ödeme Bekliyor"),
                _ => ("gray", _orderStateMachine.GetStatusDescription(status))
            };
        }

        /// <summary>
        /// Müşteri adını KVKK için maskeler.
        /// </summary>
        private string MaskCustomerName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Müşteri";
            if (name.Length <= 3) return name;
            return name[..2] + new string('*', name.Length - 3) + name[^1];
        }

        /// <summary>
        /// Adresi belirli uzunlukta kısaltır.
        /// </summary>
        private string TruncateAddress(string? address, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(address)) return "";
            if (address.Length <= maxLength) return address;
            return address[..maxLength] + "...";
        }

        /// <summary>
        /// Google Maps URL oluşturur.
        /// </summary>
        private string? BuildGoogleMapsUrl(string? address, string? city)
        {
            if (string.IsNullOrWhiteSpace(address)) return null;
            var fullAddress = $"{address}, {city ?? ""}".Trim().TrimEnd(',');
            var encoded = HttpUtility.UrlEncode(fullAddress);
            return $"https://www.google.com/maps/search/?api=1&query={encoded}";
        }

        /// <summary>
        /// Ödeme bilgisi metni oluşturur.
        /// </summary>
        private string GetPaymentInfoText(Order order)
        {
            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                return order.AuthorizedAmount > 0 
                    ? $"Kredi Kartı (Provizyon: {order.AuthorizedAmount:C})" 
                    : "Kredi Kartı (Ödendi)";
            }
            return $"Kapıda Ödeme ({order.FinalPrice:C})";
        }

        /// <summary>
        /// Başarısız yanıt oluşturur.
        /// </summary>
        private CourierOrderActionResponseDto CreateFailResponse(int orderId, string message)
        {
            return new CourierOrderActionResponseDto
            {
                Success = false,
                Message = message,
                OrderId = orderId,
                ActionTime = DateTime.UtcNow
            };
        }

        #endregion
    }
}
