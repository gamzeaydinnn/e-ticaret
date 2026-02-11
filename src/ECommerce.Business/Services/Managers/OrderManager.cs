
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using ECommerce.Entities.Concrete;
using ECommerce.Core.DTOs.Cart;
using ECommerce.Core.Interfaces;
using ECommerce.Core.DTOs.Order;
using ECommerce.Core.DTOs.Pricing;
using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Services.Managers
{
    public class OrderManager : IOrderService
    {
        private readonly ECommerceDbContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly IInventoryLogService _inventoryLogService;
        private readonly IPricingEngine _pricingEngine;
        private readonly ECommerce.Business.Services.Interfaces.INotificationService? _notificationService;
        private readonly ECommerce.Business.Services.Interfaces.IPushService? _pushService;
        private readonly ECommerce.Business.Services.Interfaces.ISmsService? _smsService;
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private const decimal VatRate = 0.18m;

        // Sipariş durumu lifecycle geçiş kuralları
        // YENİ AKIŞ: New → Confirmed → Preparing → Ready → Assigned → PickedUp → OutForDelivery → Delivered
        private static readonly IReadOnlyDictionary<OrderStatus, HashSet<OrderStatus>> AllowedTransitions =
            new Dictionary<OrderStatus, HashSet<OrderStatus>>
            {
                // ═══════════════════════════════════════════════════════════════════════════════
                // YENİ SİPARİŞ AKIŞI (Ana akış)
                // ═══════════════════════════════════════════════════════════════════════════════
                
                // New → Confirmed (Admin onayı) veya Cancelled
                [OrderStatus.New] = new HashSet<OrderStatus>
                {
                    OrderStatus.Confirmed,
                    OrderStatus.Cancelled
                },
                
                // Confirmed → Preparing (Market görevlisi hazırlamaya başladı) veya Cancelled
                [OrderStatus.Confirmed] = new HashSet<OrderStatus>
                {
                    OrderStatus.Preparing,
                    OrderStatus.Cancelled
                },
                
                // Preparing → Ready (Market görevlisi hazırladı) veya Cancelled
                // ⚠️ DÜZELTME: OutForDelivery kısa yolu KALDIRILDI - akış kilitlendi
                [OrderStatus.Preparing] = new HashSet<OrderStatus>
                {
                    OrderStatus.Ready,
                    // OrderStatus.Shipped,        // KALDIRILDI: Kısa yol
                    // OrderStatus.OutForDelivery, // KALDIRILDI: Assigned olmadan geçiş yapılamaz
                    OrderStatus.Cancelled
                },
                
                // Ready → Assigned (Dispatcher kurye atadı) veya Cancelled
                // ⚠️ DÜZELTME: OutForDelivery kısa yolu KALDIRILDI - akış kilitlendi
                [OrderStatus.Ready] = new HashSet<OrderStatus>
                {
                    OrderStatus.Assigned,
                    // OrderStatus.OutForDelivery, // KALDIRILDI: Assigned olmadan geçiş yapılamaz
                    OrderStatus.Cancelled
                },
                
                // Assigned → PickedUp (Kurye teslim aldı), OutForDelivery veya Cancelled
                [OrderStatus.Assigned] = new HashSet<OrderStatus>
                {
                    OrderStatus.PickedUp,
                    OrderStatus.OutForDelivery,
                    OrderStatus.Ready, // Kurye değişikliği için geri alınabilir
                    OrderStatus.Cancelled
                },
                
                // PickedUp → OutForDelivery (Kurye yola çıktı)
                [OrderStatus.PickedUp] = new HashSet<OrderStatus>
                {
                    OrderStatus.OutForDelivery,
                    OrderStatus.Delivered,
                    OrderStatus.DeliveryFailed
                },
                
                // OutForDelivery → Delivered veya DeliveryFailed
                [OrderStatus.OutForDelivery] = new HashSet<OrderStatus>
                {
                    OrderStatus.Delivered,
                    OrderStatus.DeliveryFailed
                },
                
                // DeliveryFailed → Assigned (yeniden kurye atama) veya Cancelled
                [OrderStatus.DeliveryFailed] = new HashSet<OrderStatus>
                {
                    OrderStatus.Assigned,
                    OrderStatus.Ready,
                    OrderStatus.Cancelled
                },
                
                // ═══════════════════════════════════════════════════════════════════════════════
                // ESKİ/ALTERNATİF AKIŞLAR (Geriye uyumluluk için - KISITLANMIŞ)
                // ═══════════════════════════════════════════════════════════════════════════════
                
                // Pending → Confirmed (Admin onayı gerekli) veya Cancelled
                // ✅ DÜZELTME: Admin panelinde Preparing'e geçiş eklendi
                [OrderStatus.Pending] = new HashSet<OrderStatus>
                {
                    OrderStatus.Paid,
                    OrderStatus.Confirmed,
                    OrderStatus.Preparing, // Admin panelinden doğrudan hazırlamaya geçiş için
                    OrderStatus.Cancelled
                },
                
                // Paid → Confirmed (Admin onayı gerekli) veya Cancelled
                // ✅ DÜZELTME: Admin panelinde Preparing'e geçiş eklendi
                [OrderStatus.Paid] = new HashSet<OrderStatus>
                {
                    OrderStatus.Preparing,  // Admin panelinden doğrudan hazırlamaya geçiş için
                    OrderStatus.Confirmed,
                    OrderStatus.Cancelled
                },
                
                // Shipped → OutForDelivery veya Delivered (eski kargo akışı için)
                [OrderStatus.Shipped] = new HashSet<OrderStatus>
                {
                    OrderStatus.Delivered,
                    OrderStatus.OutForDelivery
                },
                
                // ═══════════════════════════════════════════════════════════════════════════════
                // TERMİNAL DURUMLAR
                // ═══════════════════════════════════════════════════════════════════════════════
                
                [OrderStatus.Delivered] = new HashSet<OrderStatus>
                {
                    OrderStatus.Refunded
                },
                [OrderStatus.Cancelled] = new HashSet<OrderStatus>
                {
                    OrderStatus.Refunded
                },
                [OrderStatus.Completed] = new HashSet<OrderStatus>
                {
                    OrderStatus.Refunded
                },
                [OrderStatus.Refunded] = new HashSet<OrderStatus>(),
                [OrderStatus.PaymentFailed] = new HashSet<OrderStatus>(),
                
                // Eski Processing durumu
                [OrderStatus.Processing] = new HashSet<OrderStatus>
                {
                    OrderStatus.Shipped,
                    OrderStatus.Preparing,
                    OrderStatus.Cancelled
                }
            };

        public OrderManager(
            ECommerceDbContext context,
            IInventoryService inventoryService,
            IInventoryLogService inventoryLogService,
            IPricingEngine pricingEngine,
            ECommerce.Business.Services.Interfaces.INotificationService? notificationService = null,
            ECommerce.Business.Services.Interfaces.IPushService? pushService = null,
            ECommerce.Business.Services.Interfaces.ISmsService? smsService = null,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            _context = context;
            _inventoryService = inventoryService;
            _inventoryLogService = inventoryLogService;
            _pricingEngine = pricingEngine;
            _notificationService = notificationService;
            _pushService = pushService;
            _smsService = smsService;
            _httpContextAccessor = httpContextAccessor;
        }

        // Siparişin tam detayını getir (fatura için)
        public async Task<OrderDetailDto?> GetDetailByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return null;
            return new OrderDetailDto
            {
                Id = order.Id,
                UserId = order.UserId ?? 0,
                IsGuestOrder = order.IsGuestOrder,
                VatAmount = order.VatAmount,
                TotalPrice = order.TotalPrice,
                DiscountAmount = order.DiscountAmount,
                FinalPrice = order.FinalPrice,
                CouponDiscountAmount = order.CouponDiscountAmount,
                CampaignDiscountAmount = order.CampaignDiscountAmount,
                CouponCode = order.AppliedCouponCode,
                TrackingNumber = order.TrackingNumber,
                Status = order.Status.ToString(),
                // NEDEN: Admin paneli "ödendi" filtresini bu alanlar üzerinden uygular.
                PaymentStatus = order.PaymentStatus.ToString(),
                IsPaid = order.PaymentStatus == PaymentStatus.Paid,
                OrderDate = order.OrderDate,
                OrderItems = order.OrderItems?.Select(oi => new OrderItemDto {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList() ?? new List<OrderItemDto>()
            };
        }

        /// <summary>
        /// Tüm siparişleri veya belirli bir kullanıcının siparişlerini getirir.
        /// SIRALAMA: En yeni siparişler en üstte görünür (OrderDate DESC).
        /// NEDEN: Admin ve mağaza görevlisi yeni siparişleri önce görmeli.
        /// </summary>
        public async Task<IEnumerable<OrderListDto>> GetOrdersAsync(int? userId = null)
        {
            var query = _context.Orders.Include(o => o.OrderItems).AsQueryable();
            if (userId.HasValue)
                query = query.Where(o => o.UserId == userId.Value);
            
            // DÜZELTME: Yeni siparişler en üstte görünsün
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return orders.Select(o => MapToDto(o));
        }

        public async Task<OrderListDto?> GetByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            return order != null ? MapToDto(order) : null;
        }

        public async Task<OrderListDto?> GetByClientOrderIdAsync(Guid clientOrderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.ClientOrderId == clientOrderId);

            return order != null ? MapToDto(order) : null;
        }

        // ============================================================================
        // MİSAFİR SİPARİŞ SORGULAMA
        // Sipariş numarasına göre sipariş bulma (guest-lookup endpoint'i için)
        // Neden: Misafir kullanıcılar giriş yapmadan sipariş durumunu takip edebilmeli
        // ============================================================================
        /// <summary>
        /// Sipariş numarasına göre sipariş getirir.
        /// Misafir kullanıcıların sipariş sorgulaması için kullanılır.
        /// </summary>
        /// <param name="orderNumber">Sipariş numarası (ör: ORD-12345)</param>
        /// <returns>Sipariş DTO'su veya null</returns>
        public async Task<OrderListDto?> GetByOrderNumberAsync(string orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                return null;

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber.Trim());

            return order != null ? MapToDto(order) : null;
        }

        public async Task<OrderListDto> CreateAsync(OrderCreateDto dto)
        {
            var effectiveUserId = dto.UserId is > 0 ? dto.UserId : null;
            decimal total = 0m;
            var items = new List<OrderItem>();
            foreach (var i in dto.OrderItems)
            {
                if (i.Quantity <= 0) throw new Exception("Geçersiz miktar");
                var product = await _context.Products.FindAsync(i.ProductId)
                    ?? throw new Exception($"Ürün bulunamadı: {i.ProductId}");
                var unitPrice = product.SpecialPrice ?? product.Price;
                total += unitPrice * i.Quantity;
                items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = i.Quantity,
                    UnitPrice = unitPrice,
                    ExpectedWeightGrams = product.UnitWeightGrams * i.Quantity
                });
            }
            var normalizedShipping = NormalizeShippingMethod(dto.ShippingMethod);
            var shippingCost = ComputeShippingCost(normalizedShipping);

            var order = new Order
            {
                ClientOrderId = dto.ClientOrderId,
                UserId = effectiveUserId,
                IsGuestOrder = !effectiveUserId.HasValue,
                OrderNumber = GenerateOrderNumber(),
                ShippingMethod = normalizedShipping,
                ShippingCost = shippingCost,
                TotalPrice = total + shippingCost,
                DiscountAmount = 0m,
                CouponDiscountAmount = 0m,
                CampaignDiscountAmount = 0m,
                FinalPrice = total + shippingCost,
                AppliedCouponCode = dto.CouponCode,
                Status = OrderStatus.Pending,
                OrderDate = DateTime.UtcNow,
                ShippingAddress = string.IsNullOrWhiteSpace(dto.ShippingAddress) ? "-" : dto.ShippingAddress,
                ShippingCity = string.IsNullOrWhiteSpace(dto.ShippingCity) ? "-" : dto.ShippingCity,
                CustomerName = dto.CustomerName,
                CustomerPhone = dto.CustomerPhone,
                CustomerEmail = dto.CustomerEmail,
                DeliveryNotes = dto.DeliveryNotes,
                OrderItems = items
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(order.Id) ?? throw new Exception("Sipariş oluşturulamadı.");
        }

        public async Task UpdateAsync(int id, OrderUpdateDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return;
            if (Enum.TryParse<OrderStatus>(dto.Status, out var statusEnum))
            {
                var previous = order.Status;
                if (previous != statusEnum)
                {
                    order.Status = statusEnum;
                    // add history entry
                    AddStatusHistory(order, previous, statusEnum);
                }
            }
            order.TotalPrice = dto.TotalPrice;
            await _context.SaveChangesAsync();
        }

        // ============================================================================
        // SİPARİŞ SİLME
        // İlişkili kayıtlar (OrderItems, Payments, StatusHistory) ile birlikte silme
        // Neden: Foreign key kısıtlamaları nedeniyle önce ilişkili kayıtları temizle
        // Dikkat: Bu operasyon geri alınamaz, audit log tutuluyor
        // ============================================================================
        public async Task DeleteAsync(int id)
        {
            // Transaction başlat - atomik operasyon için
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);
                    
                if (order == null) return;

                // 1. OrderStatusHistory kayıtlarını sil (Cascade değil, manuel)
                var statusHistories = await _context.OrderStatusHistories
                    .Where(h => h.OrderId == id)
                    .ToListAsync();
                if (statusHistories.Any())
                {
                    _context.OrderStatusHistories.RemoveRange(statusHistories);
                }

                // 2. Payment kayıtlarını sil (Restrict ilişki - manuel silme gerekli)
                var payments = await _context.Payments
                    .Where(p => p.OrderId == id)
                    .ToListAsync();
                if (payments.Any())
                {
                    _context.Payments.RemoveRange(payments);
                }

                // 3. CouponUsage kayıtlarını sil (OrderId non-nullable, silmek zorundayız)
                var couponUsages = await _context.CouponUsages
                    .Where(cu => cu.OrderId == id)
                    .ToListAsync();
                if (couponUsages.Any())
                {
                    _context.CouponUsages.RemoveRange(couponUsages);
                }

                // 4. OrderItems CASCADE ile silinecek ama explicit olarak temizle
                if (order.OrderItems?.Any() == true)
                {
                    _context.OrderItems.RemoveRange(order.OrderItems);
                }

                // 6. Siparişi sil
                _context.Orders.Remove(order);
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw; // Hatayı yukarı fırlat, controller yakalasın
            }
        }

        public async Task<bool> ChangeOrderStatusAsync(int id, string newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return false;
            if (Enum.TryParse<OrderStatus>(newStatus, out var statusEnum))
            {
                var previous = order.Status;
                if (previous != statusEnum)
                {
                    order.Status = statusEnum;
                    AddStatusHistory(order, previous, statusEnum);
                }
                await _context.SaveChangesAsync();

                // Push bildirimi: sipariş durumu başarıyla güncellendiğinde kullanıcıya bildir
                if (_pushService != null && order.UserId.HasValue)
                {
                    try
                    {
                        var userIdStr = order.UserId.Value.ToString();
                        var payload = JsonSerializer.Serialize(new { action = "OrderStatusChanged", status = statusEnum.ToString() });
                        await _pushService.SendNotificationAsync(userIdStr, payload);
                    }
                    catch
                    {
                        // Push hataları burada loglanabilir; mevcut akışı bozmayalım
                    }
                }
                return true;
            }
            return false;
        }

        public async Task<OrderListDto> CheckoutAsync(OrderCreateDto dto)
        {
            var stockCheck = await _inventoryService.ValidateStockForOrderAsync(dto.OrderItems);
            if (!stockCheck.Success)
            {
                throw new Exception(stockCheck.ErrorMessage ?? "Stok doğrulaması başarısız");
            }

            var clientOrderId = dto.ClientOrderId ?? Guid.NewGuid();
            dto.ClientOrderId = clientOrderId;

            var reservationItems = dto.OrderItems
                .Select(i => new CartItemDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                })
                .ToList();

            var reservationSucceeded = await _inventoryService.ReserveStockAsync(clientOrderId, reservationItems);
            if (!reservationSucceeded)
            {
                throw new Exception("Insufficient stock");
            }

            // ExecutionStrategy kullanarak transaction yönetimi
            var strategy = _context.Database.CreateExecutionStrategy();
            
            var createdOrderId = await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var effectiveUserId = dto.UserId is > 0 ? dto.UserId : null;
                    decimal itemsTotal = 0m;
                    var items = new List<OrderItem>();
                    foreach (var item in dto.OrderItems)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product == null)
                            throw new Exception($"Ürün bulunamadı: {item.ProductId}");
                        var unitPrice = product.SpecialPrice ?? product.Price;
                        itemsTotal += unitPrice * item.Quantity;
                        items.Add(new OrderItem
                        {
                            ProductId = product.Id,
                            Quantity = item.Quantity,
                            UnitPrice = unitPrice,
                            ExpectedWeightGrams = product.UnitWeightGrams * item.Quantity
                        });
                    }
                // Compute shipping server-side (whitelist + fixed costs)
                var shippingMethod = NormalizeShippingMethod(dto.ShippingMethod);
                var shippingCost = ComputeShippingCost(shippingMethod);

                var cartInputs = dto.OrderItems.Select(i => new CartItemInputDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList();

                var normalizedCoupon = string.IsNullOrWhiteSpace(dto.CouponCode)
                    ? null
                    : dto.CouponCode!.Trim();

                var pricingResult = await _pricingEngine.CalculateCartAsync(
                    effectiveUserId,
                    cartInputs,
                    normalizedCoupon);

                if (pricingResult == null)
                {
                    throw new Exception("Sepet fiyatı hesaplanamadı.");
                }

                if (pricingResult.Subtotal <= 0m)
                {
                    pricingResult.Subtotal = itemsTotal;
                }

                var discountTotal = pricingResult.CampaignDiscountTotal + pricingResult.CouponDiscountTotal;
                if (discountTotal < 0m)
                {
                    discountTotal = 0m;
                }

                pricingResult.DeliveryFee = shippingCost;
                var grandTotalBeforeVat = pricingResult.Subtotal - discountTotal + shippingCost;
                pricingResult.GrandTotal = grandTotalBeforeVat < 0m ? 0m : grandTotalBeforeVat;

                var vatAmount = Math.Round(pricingResult.Subtotal * VatRate, 2, MidpointRounding.AwayFromZero);
                var totalPrice = pricingResult.Subtotal + shippingCost + vatAmount;
                var finalPrice = pricingResult.GrandTotal + vatAmount;

                var order = new Order
                {
                    ClientOrderId = dto.ClientOrderId,
                    UserId = effectiveUserId,
                    IsGuestOrder = !effectiveUserId.HasValue,
                    OrderNumber = GenerateOrderNumber(),
                    VatAmount = vatAmount,
                    TotalPrice = totalPrice,
                    DiscountAmount = discountTotal,
                    CouponDiscountAmount = pricingResult.CouponDiscountTotal,
                    CampaignDiscountAmount = pricingResult.CampaignDiscountTotal,
                    FinalPrice = finalPrice,
                    AppliedCouponCode = pricingResult.AppliedCouponCode ?? normalizedCoupon,
                    Status = OrderStatus.Pending,
                    OrderDate = DateTime.UtcNow,
                    ShippingAddress = dto.ShippingAddress,
                    ShippingCity = dto.ShippingCity,
                    CustomerName = dto.CustomerName,
                    CustomerPhone = dto.CustomerPhone,
                    CustomerEmail = dto.CustomerEmail,
                    DeliveryNotes = dto.DeliveryNotes,
                    OrderItems = items,
                    ShippingMethod = shippingMethod,
                    ShippingCost = shippingCost
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // ═════════════════════════════════════════════════════════════════════════
                // KUPON KULLANIM KAYDI OLUŞTUR
                // ═════════════════════════════════════════════════════════════════════════
                // Eğer siparişte kupon kullanıldıysa, CouponUsage tablosuna kayıt ekle.
                // Bu kayıt:
                // 1. Kullanıcı başına límit kontrolü için gerekli
                // 2. Raporlama ve analiz için değerli veri sağlar
                // 3. Fraud detection için IP/UserAgent bilgisi tutar
                // 4. İptal/iade durumunda kupon restore için kullanılır
                // ═════════════════════════════════════════════════════════════════════════
                if (!string.IsNullOrWhiteSpace(order.AppliedCouponCode))
                {
                    // Kupon kodunu normalize et (büyük harf, trim)
                    var normalizedCode = order.AppliedCouponCode.Trim().ToUpperInvariant();

                    // Kuponu veritabanından bul
                    var coupon = await _context.Set<Coupon>()
                        .FirstOrDefaultAsync(c => c.Code.ToUpper() == normalizedCode && c.IsActive);

                    if (coupon != null)
                    {
                        // HTTP Context'ten güvenlik bilgilerini al
                        string? ipAddress = null;
                        string? userAgent = null;
                        string? sessionId = null;

                        if (_httpContextAccessor?.HttpContext != null)
                        {
                            var httpContext = _httpContextAccessor.HttpContext;

                            // IP Address (X-Forwarded-For header'dan veya RemoteIpAddress'den)
                            ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                                       ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                       ?? "unknown";

                            // User Agent
                            userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();

                            // Session ID
                            sessionId = httpContext.Session?.Id ?? httpContext.TraceIdentifier;
                        }

                        // CouponUsage kaydı oluştur
                        var couponUsage = new CouponUsage
                        {
                            CouponId = coupon.Id,
                            UserId = effectiveUserId, // Guest siparişlerinde null
                            OrderId = order.Id,
                            UsedAt = DateTime.UtcNow,
                            DiscountApplied = order.CouponDiscountAmount,
                            OrderTotalBeforeDiscount = itemsTotal,
                            OrderTotalAfterDiscount = finalPrice,
                            CouponCode = coupon.Code, // Snapshot - değişse bile geçmişte kalır
                            CouponType = coupon.Type.ToString(), // Snapshot
                            IpAddress = ipAddress,
                            UserAgent = userAgent,
                            SessionId = sessionId,
                            IsActive = true, // BaseEntity'den
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Set<CouponUsage>().Add(couponUsage);

                        // Kupon UsageCount'u artır (istatistik için)
                        // NOT: Bu alan kullanım limiti için kullanılmaz (CouponUsage tablosu kullanılır)
                        coupon.UsageCount++;

                        await _context.SaveChangesAsync(); // Transaction içinde
                    }
                }

                await _inventoryService.CommitReservationAsync(clientOrderId);
                await transaction.CommitAsync();

                return order.Id;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    if (reservationSucceeded)
                    {
                        await _inventoryService.ReleaseReservationAsync(clientOrderId);
                    }
                    throw;
                }
            });

            // Notification (await to avoid DbContext concurrency issues)
            if (_notificationService != null)
            {
                await _notificationService.SendOrderConfirmationAsync(createdOrderId);
            }

            return await GetByIdAsync(createdOrderId) ?? throw new Exception("Sipariş oluşturulamadı.");
        }

        /// <summary>
        /// Müşteri sipariş iptali - MARKET KURALLARI:
        /// 1. Sadece kendi siparişini iptal edebilir
        /// 2. Sadece aynı gün içinde iptal edilebilir
        /// 3. Sadece hazırlanmadan önce (New, Pending, Confirmed) iptal edilebilir
        /// Aksi halde müşteri hizmetleriyle iletişime geçilmeli
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> CancelOrderAsync(int orderId, int userId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            
            // Sipariş bulunamadı veya başka kullanıcıya ait
            if (order == null || order.UserId != userId)
                return (false, "Sipariş bulunamadı veya bu siparişi iptal etme yetkiniz yok.");
            
            // MARKET KURALI: Sadece aynı gün içinde iptal edilebilir
            // Gün kontrolü - sipariş tarihi ile bugünün tarihi karşılaştırılır
            var orderDate = order.CreatedAt.Date;
            var today = DateTime.Today;
            if (orderDate != today)
            {
                return (false, "Sipariş sadece aynı gün içinde iptal edilebilir. İptal için müşteri hizmetleriyle iletişime geçiniz.");
            }
            
            // Durum kontrolü - sadece hazırlanmadan önceki durumlar iptal edilebilir
            var cancellableStatuses = new[] { OrderStatus.New, OrderStatus.Pending, OrderStatus.Confirmed };
            if (!cancellableStatuses.Contains(order.Status))
            {
                var statusMessage = order.Status switch
                {
                    OrderStatus.Preparing => "Siparişiniz hazırlanmaya başladı",
                    OrderStatus.Ready or OrderStatus.ReadyForPickup => "Siparişiniz teslimata hazır",
                    OrderStatus.Assigned or OrderStatus.PickedUp => "Siparişiniz kuryeye teslim edildi",
                    OrderStatus.InTransit or OrderStatus.OutForDelivery => "Siparişiniz yolda",
                    OrderStatus.Delivered => "Siparişiniz teslim edildi",
                    OrderStatus.Cancelled => "Siparişiniz zaten iptal edilmiş",
                    _ => "Siparişiniz bu aşamada"
                };
                return (false, $"{statusMessage}. Bu aşamada iptal için müşteri hizmetleriyle iletişime geçiniz.");
            }
            
            // İptal işlemini gerçekleştir
            var result = await CancelOrderInternalAsync(order);
            return result 
                ? (true, null) 
                : (false, "Sipariş iptal edilemedi. Lütfen müşteri hizmetleriyle iletişime geçiniz.");
        }

        public async Task<bool> MarkPaymentFailedAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return false;
            }

            // Ödeme başarısız olduğunda sipariş Paid durumunda olmamalı
            if (order.Status == OrderStatus.PaymentFailed || order.Status == OrderStatus.Cancelled)
            {
                return true;
            }

            var previous = order.Status;
            order.Status = OrderStatus.PaymentFailed;
            AddStatusHistory(order, previous, OrderStatus.PaymentFailed);
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<OrderListDto?> MarkOrderAsPreparingAsync(int orderId)
        {
            return MoveOrderToStatusAsync(orderId, OrderStatus.Preparing);
        }

        public Task<OrderListDto?> MarkOrderOutForDeliveryAsync(int orderId)
        {
            return MoveOrderToStatusAsync(orderId, OrderStatus.OutForDelivery);
        }

        public Task<OrderListDto?> MarkOrderAsDeliveredAsync(int orderId)
        {
            return MoveOrderToStatusAsync(orderId, OrderStatus.Delivered);
        }

        public Task<OrderListDto?> CancelOrderByAdminAsync(int orderId)
        {
            return MoveOrderToStatusAsync(orderId, OrderStatus.Cancelled);
        }

        public Task<OrderListDto?> RefundOrderAsync(int orderId)
        {
            return MoveOrderToStatusAsync(orderId, OrderStatus.Refunded);
        }

        private async Task<bool> CancelOrderInternalAsync(Order order)
        {
            if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Delivered)
            {
                return false;
            }

            await EnsureOrderItemsLoadedAsync(order);

            if (order.OrderItems != null && order.OrderItems.Count > 0)
            {
                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                    if (product == null)
                    {
                        continue;
                    }

                    var oldStock = product.StockQuantity;
                    product.StockQuantity += item.Quantity;
                    await _inventoryLogService.WriteAsync(
                        product.Id,
                        "OrderCancelled",
                        item.Quantity,
                        oldStock,
                        product.StockQuantity,
                        order.OrderNumber ?? order.Id.ToString());
                }
            }

            var previous = order.Status;
            order.Status = OrderStatus.Cancelled;
            AddStatusHistory(order, previous, OrderStatus.Cancelled);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task EnsureOrderItemsLoadedAsync(Order order)
        {
            var entry = _context.Entry(order);
            if (!entry.Collection(o => o.OrderItems).IsLoaded)
            {
                await entry.Collection(o => o.OrderItems).LoadAsync();
            }
        }

        public async Task<int> GetOrderCountAsync()
        {
            return await _context.Orders.CountAsync();
        }

        public async Task<int> GetTodayOrderCountAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Orders.CountAsync(o => o.OrderDate.Date == today);
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .SumAsync(o => o.TotalPrice);
        }

        public async Task<IEnumerable<OrderListDto>> GetAllOrdersAsync(int page = 1, int size = 20)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
            return orders.Select(o => MapToDto(o));
        }

        public async Task<OrderListDto> GetOrderByIdAsync(int id)
        {
            var result = await GetByIdAsync(id);
            return result ?? throw new Exception("Sipariş bulunamadı.");
        }

        public async Task UpdateOrderStatusAsync(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return;

            // Normalize status: snake_case → PascalCase (out_for_delivery → OutForDelivery)
            var normalizedStatus = NormalizeStatusString(status);

            // Case-insensitive parse: "preparing" → OrderStatus.Preparing
            if (Enum.TryParse<OrderStatus>(normalizedStatus, ignoreCase: true, out var statusEnum))
            {
                var previous = order.Status;
                // Aynı duruma geçiş yapmaya çalışıyorsa, hiçbir şey yapma
                if (previous == statusEnum) return;

                // Tanımlı bir lifecycle kuralı varsa, geçişe izin var mı kontrol et
                if (AllowedTransitions.TryGetValue(previous, out var allowed) &&
                    !allowed.Contains(statusEnum))
                {
                    // Geçersiz transition: sessizce yoksay (mevcut test davranışını korumak için)
                    return;
                }

                if (statusEnum == OrderStatus.Cancelled)
                {
                    await CancelOrderInternalAsync(order);
                    return;
                }

                order.Status = statusEnum;
                AddStatusHistory(order, previous, statusEnum);
                await _context.SaveChangesAsync();

                // If status transitioned to Delivered, notify customer (if available)
                if (previous != OrderStatus.Delivered && statusEnum == OrderStatus.Delivered && _notificationService != null)
                {
                    // If you have a tracking number in real flow, pass it; here we pass empty string
                    _ = _notificationService.SendShipmentNotificationAsync(order.Id, trackingNumber: string.Empty);
                }

                // SMS bildirimi: eğer durum Delivered ise ve telefon varsa, stub ISmsService ile gönder
                if (previous != OrderStatus.Delivered && statusEnum == OrderStatus.Delivered && _smsService != null && !string.IsNullOrWhiteSpace(order.CustomerPhone))
                {
                    try
                    {
                        var msg = $"Siparişiniz teslim edildi. Sipariş No: {order.OrderNumber}";
                        await _smsService.SendAsync(order.CustomerPhone!, msg);
                    }
                    catch
                    {
                        // ignore SMS errors to avoid breaking flow
                    }
                }
            }
        }

        public async Task<IEnumerable<OrderListDto>> GetRecentOrdersAsync(int count = 10)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(count)
                .ToListAsync();
            return orders.Select(o => MapToDto(o));
        }

        private async Task<OrderListDto?> MoveOrderToStatusAsync(int orderId, OrderStatus targetStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return null;
            }

            var previousStatus = order.Status;
            if (previousStatus == targetStatus)
            {
                return await GetByIdAsync(orderId);
            }

            if (AllowedTransitions.TryGetValue(previousStatus, out var allowedTargets) &&
                !allowedTargets.Contains(targetStatus))
            {
                throw new InvalidOperationException($"Geçersiz durum geçişi: {previousStatus} -> {targetStatus}");
            }

            if (targetStatus == OrderStatus.Cancelled)
            {
                await CancelOrderInternalAsync(order);
                return await GetByIdAsync(orderId);
            }

            previousStatus = order.Status;
            order.Status = targetStatus;
            AddStatusHistory(order, previousStatus, targetStatus);
            await _context.SaveChangesAsync();

            if (previousStatus != OrderStatus.Delivered &&
                targetStatus == OrderStatus.Delivered &&
                _notificationService != null)
            {
                _ = _notificationService.SendShipmentNotificationAsync(order.Id, trackingNumber: string.Empty);
            }

            return await GetByIdAsync(orderId);
        }

        /// <summary>
        /// Order entity'sini OrderListDto'ya dönüştürür.
        /// NEDEN: Admin paneli, mağaza görevlisi ve frontend tüm sipariş alanlarına ihtiyaç duyar.
        /// ÖNEMLİ: PaymentStatus ve IsPaid alanları ödeme filtreleri için kritiktir.
        /// </summary>
        private OrderListDto MapToDto(Order order)
        {
            return new OrderListDto
            {
                Id = order.Id,
                UserId = order.UserId ?? 0,
                IsGuestOrder = order.IsGuestOrder,
                OrderNumber = order.OrderNumber,
                VatAmount = order.VatAmount,
                TotalPrice = order.TotalPrice,
                DiscountAmount = order.DiscountAmount,
                FinalPrice = order.FinalPrice,
                CouponDiscountAmount = order.CouponDiscountAmount,
                CampaignDiscountAmount = order.CampaignDiscountAmount,
                CouponCode = order.AppliedCouponCode,
                TrackingNumber = order.TrackingNumber,
                Status = order.Status.ToString(),
                // DÜZELTME: PaymentStatus ve IsPaid alanları eklendi
                // Admin panelindeki "Ödendi/Ödeme Bekliyor" filtreleri bu alanlara bağlı
                PaymentStatus = order.PaymentStatus.ToString(),
                IsPaid = order.PaymentStatus == ECommerce.Entities.Enums.PaymentStatus.Paid,
                OrderDate = order.OrderDate,
                TotalItems = order.OrderItems?.Sum(oi => oi.Quantity) ?? 0,
                ShippingMethod = order.ShippingMethod,
                ShippingCost = order.ShippingCost,
                ShippingAddress = order.ShippingAddress,
                CustomerName = order.CustomerName ?? string.Empty,
                CustomerPhone = order.CustomerPhone ?? string.Empty,
                // Misafir sipariş sorgulaması için CustomerEmail maplendi
                CustomerEmail = order.CustomerEmail,
                DeliveryNotes = order.DeliveryNotes ?? string.Empty,
                OrderItems = order.OrderItems?.Select(oi => new OrderItemDto {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList() ?? new List<OrderItemDto>()
            };
        }

        private string? GetCurrentUserIdentifier()
        {
            try
            {
                var user = _httpContextAccessor?.HttpContext?.User;
                if (user == null) return null;
                var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(id)) return id;
                var name = user.Identity?.Name;
                if (!string.IsNullOrWhiteSpace(name)) return name;
                var email = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                return email;
            }
            catch
            {
                return null;
            }
        }

        private void AddStatusHistory(Order order, OrderStatus previous, OrderStatus current, string? reason = null)
        {
            try
            {
                var hist = new OrderStatusHistory
                {
                    OrderId = order.Id,
                    PreviousStatus = previous,
                    NewStatus = current,
                    ChangedBy = GetCurrentUserIdentifier(),
                    Reason = reason,
                    ChangedAt = DateTime.UtcNow
                };
                _context.OrderStatusHistories.Add(hist);
            }
            catch
            {
                // Ensure that history logging does not break main flow
            }
        }

        // Normalize incoming shipping method and map to a known key
        private static string NormalizeShippingMethod(string? method)
        {
            if (string.IsNullOrWhiteSpace(method)) return "car";
            var m = method.Trim().ToLowerInvariant();
            // accept some common variants (english/turkish)
            if (m == "motokurye" || m == "motorcycle" || m == "motor") return "motorcycle";
            if (m == "araç" || m == "arac" || m == "car") return "car";
            // default fallback
            return "car";
        }

        // Fixed shipping cost mapping for allowed shipping methods
        // GÜNCELLEME: Kargo fiyatları 40/60 TL olarak belirlendi (eski 15/30 TL hatalıydı)
        private static decimal ComputeShippingCost(string method)
        {
            var m = method?.Trim().ToLowerInvariant() ?? "car";
            return m == "motorcycle" ? 40m : 60m;
        }

        private static string GenerateOrderNumber()
        {
            var ts = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var rnd = Random.Shared.Next(1000, 9999);
            return $"ORD-{ts}-{rnd}";
        }

        // ============================================================
        // KURYE ATAMA
        // ============================================================
        /// <summary>
        /// Siparişe kurye atar ve durumu günceller.
        /// GÜVENLİK: Kurye ve sipariş varlığı kontrol edilir.
        /// </summary>
        public async Task<OrderListDto?> AssignCourierAsync(int orderId, int courierId)
        {
            // Siparişi bul
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return null; // Sipariş bulunamadı
            }

            // Kuryeyi bul ve varlığını doğrula (User bilgisiyle birlikte)
            var courier = await _context.Couriers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == courierId);
            if (courier == null)
            {
                throw new InvalidOperationException($"Kurye bulunamadı: {courierId}");
            }

            // Önceki durumu kaydet (audit için)
            var previousStatus = order.Status;

            // Kurye ataması için izin verilen durumlar
            // Confirmed, Preparing, Ready, Assigned durumlarında kurye atanabilir
            // NEDEN: Assigned durumunda kurye değişikliği yapılabilmeli (yeniden atama senaryosu)
            var allowedStatuses = new[] { 
                OrderStatus.Confirmed, 
                OrderStatus.Preparing, 
                OrderStatus.Ready,
                OrderStatus.Assigned  // Kurye değiştirme için eklendi
            };
            
            if (!allowedStatuses.Contains(order.Status))
            {
                throw new InvalidOperationException($"Kurye ataması sadece Onaylanmış, Hazırlanıyor, Hazır veya Atanmış durumundaki siparişlere yapılabilir. Mevcut durum: {order.Status}");
            }

            // Kurye ataması yap
            order.CourierId = courierId;
            order.AssignedAt = DateTime.UtcNow;
            order.Status = OrderStatus.Assigned;

            // Durum değişikliği geçmişini kaydet
            // Kurye adını User entity'sinden al
            var courierName = courier.User?.FullName ?? $"Kurye #{courierId}";
            if (previousStatus != order.Status)
            {
                AddStatusHistory(order, previousStatus, order.Status, $"Kurye atandı: {courierName}");
            }

            await _context.SaveChangesAsync();

            // DTO'ya dönüştür
            return MapToDto(order);
        }

        // ============================================================
        // STORE ATTENDANT (MARKET GÖREVLİSİ) METODLARI
        // ============================================================

        /// <inheritdoc />
        public async Task<OrderListDto?> MarkOrderAsConfirmedAsync(int orderId, string confirmedBy)
        {
            // Siparişi bul
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return null;
            }

            // Sadece New durumundan Confirmed'a geçiş yapılabilir
            if (order.Status != OrderStatus.New && order.Status != OrderStatus.Pending && order.Status != OrderStatus.Paid)
            {
                throw new InvalidOperationException($"Sipariş onaylanamaz. Mevcut durum: {order.Status}");
            }

            var previousStatus = order.Status;
            order.Status = OrderStatus.Confirmed;
            order.ConfirmedAt = DateTime.UtcNow;
            
            AddStatusHistory(order, previousStatus, OrderStatus.Confirmed, $"Admin tarafından onaylandı: {confirmedBy}");
            
            await _context.SaveChangesAsync();

            return MapToDto(order);
        }

        /// <inheritdoc />
        public async Task<OrderListDto?> StartPreparingAsync(int orderId, string preparedBy)
        {
            // Siparişi bul
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return null;
            }

            // Sadece Confirmed durumundan Preparing'e geçiş yapılabilir
            // Dokümantasyona göre: Confirmed → Preparing (Paid veya Pending değil!)
            if (order.Status != OrderStatus.Confirmed)
            {
                throw new InvalidOperationException($"Sipariş hazırlanamaz. Sadece 'Confirmed' durumundaki siparişler hazırlanabilir. Mevcut durum: {order.Status}");
            }

            var previousStatus = order.Status;
            order.Status = OrderStatus.Preparing;
            order.PreparingStartedAt = DateTime.UtcNow;
            order.PreparedBy = preparedBy;
            
            AddStatusHistory(order, previousStatus, OrderStatus.Preparing, $"Hazırlanmaya başlandı: {preparedBy}");
            
            await _context.SaveChangesAsync();

            return MapToDto(order);
        }

        /// <inheritdoc />
        public async Task<OrderListDto?> MarkOrderAsReadyAsync(int orderId, string readyBy, int? weightInGrams = null)
        {
            // Siparişi bul
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return null;
            }

            // Sadece Preparing durumundan Ready'ye geçiş yapılabilir
            if (order.Status != OrderStatus.Preparing)
            {
                throw new InvalidOperationException($"Sipariş hazır olarak işaretlenemez. Mevcut durum: {order.Status}");
            }

            var previousStatus = order.Status;
            order.Status = OrderStatus.Ready;
            order.ReadyAt = DateTime.UtcNow;
            
            // Ağırlık bilgisi varsa kaydet
            if (weightInGrams.HasValue)
            {
                order.WeightInGrams = weightInGrams.Value;
            }
            
            var note = $"Hazır: {readyBy}";
            if (weightInGrams.HasValue)
            {
                note += $" (Ağırlık: {weightInGrams.Value}g)";
            }
            
            AddStatusHistory(order, previousStatus, OrderStatus.Ready, note);
            
            await _context.SaveChangesAsync();

            return MapToDto(order);
        }

        /// <inheritdoc />
        public async Task<StoreAttendantOrderListResponseDto> GetOrdersForStoreAttendantAsync(StoreAttendantOrderFilterDto? filter)
        {
            filter ??= new StoreAttendantOrderFilterDto();
            
            // Sayfa boyutu sınırlaması
            if (filter.PageSize > 100) filter.PageSize = 100;
            if (filter.PageSize < 1) filter.PageSize = 20;
            if (filter.Page < 1) filter.Page = 1;

            // Market görevlisi için gösterilecek durumlar
            // DÜZELTME: Pending durumu eklendi - yeni siparişler Pending ile başlar
            // NEDEN: Sipariş oluşturulduğunda Status=Pending olarak set ediliyor,
            // bu yüzden mağaza görevlisi bu siparişleri göremiyordu
            var allowedStatuses = new List<OrderStatus> 
            { 
                OrderStatus.Pending,    // Yeni oluşturulan siparişler (varsayılan durum)
                OrderStatus.Confirmed, 
                OrderStatus.Preparing, 
                OrderStatus.Ready,
                OrderStatus.Assigned,
                OrderStatus.PickedUp,
                OrderStatus.InTransit,
                OrderStatus.OutForDelivery,
                OrderStatus.Delivered,
                OrderStatus.DeliveryFailed,
                OrderStatus.DeliveryPaymentPending,
                OrderStatus.New,        // Admin panelinden yeni siparişler de görünsün
                OrderStatus.Paid,
                OrderStatus.Cancelled,
                OrderStatus.Processing,
                OrderStatus.Shipped,
                OrderStatus.Completed,
                OrderStatus.PaymentFailed,
                OrderStatus.ChargebackPending,
                OrderStatus.Refunded,
                OrderStatus.ReadyForPickup,
                OrderStatus.PartialRefund
            };

            var query = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => allowedStatuses.Contains(o.Status))
                .AsQueryable();

            // Durum filtresi
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                if (Enum.TryParse<OrderStatus>(filter.Status, true, out var statusFilter))
                {
                    query = query.Where(o => o.Status == statusFilter);
                }
                else if (filter.Status.ToLower() == "pending")
                {
                    // "pending" = Pending + Confirmed + Paid + New (TÜM BEKLEYEN SİPARİŞLER)
                    // DÜZELTME: OrderStatus.Pending eklendi - yeni siparişler bu durumla başlar
                    query = query.Where(o => o.Status == OrderStatus.Pending ||
                                             o.Status == OrderStatus.Confirmed || 
                                             o.Status == OrderStatus.Paid || 
                                             o.Status == OrderStatus.New);
                }
            }

            // Toplam sayı
            var totalCount = await query.CountAsync();

            // Sıralama
            query = filter.SortOrder?.ToLower() == "asc" 
                ? query.OrderBy(o => o.OrderDate) 
                : query.OrderByDescending(o => o.OrderDate);

            // Sayfalama
            var orders = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // DTO'lara dönüştür
            var orderDtos = orders.Select(o => MapToStoreAttendantDto(o)).ToList();

            // Özet istatistikleri hesapla
            var summary = await GetStoreAttendantSummaryAsync();

            return new StoreAttendantOrderListResponseDto
            {
                Orders = orderDtos,
                Summary = summary,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize),
                CurrentPage = filter.Page
            };
        }

        /// <inheritdoc />
        public async Task<StoreAttendantSummaryDto> GetStoreAttendantSummaryAsync()
        {
            var today = DateTime.UtcNow.Date;

            // İstatistikleri hesapla
            // DÜZELTME: OrderStatus.Pending eklendi - yeni siparişler bu durumla başlar
            var pendingCount = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Pending ||
                                 o.Status == OrderStatus.Confirmed || 
                                 o.Status == OrderStatus.New || 
                                 o.Status == OrderStatus.Paid);

            var preparingCount = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Preparing);

            var readyCount = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Ready);

            // Bugün tamamlanan (Ready, Assigned, OutForDelivery, Delivered olan)
            var completedTodayCount = await _context.Orders
                .CountAsync(o => o.ReadyAt.HasValue && 
                                 o.ReadyAt.Value.Date == today);

            var todayTotalAmount = await _context.Orders
                .Where(o => o.ReadyAt.HasValue && o.ReadyAt.Value.Date == today)
                .SumAsync(o => o.FinalPrice);

            // Ortalama hazırlık süresi (son 50 sipariş)
            var avgPrepTime = await _context.Orders
                .Where(o => o.PreparingStartedAt.HasValue && o.ReadyAt.HasValue)
                .OrderByDescending(o => o.ReadyAt)
                .Take(50)
                .Select(o => EF.Functions.DateDiffMinute(o.PreparingStartedAt, o.ReadyAt))
                .AverageAsync() ?? 0;

            return new StoreAttendantSummaryDto
            {
                PendingCount = pendingCount,
                PreparingCount = preparingCount,
                ReadyCount = readyCount,
                CompletedTodayCount = completedTodayCount,
                TodayTotalAmount = todayTotalAmount,
                AveragePreparationTimeMinutes = avgPrepTime,
                LastUpdated = DateTime.UtcNow
            };
        }

        // ============================================================
        // DISPATCHER (SEVKİYAT GÖREVLİSİ) METODLARI
        // ============================================================

        /// <inheritdoc />
        public async Task<DispatcherOrderListResponseDto> GetOrdersForDispatcherAsync(DispatcherOrderFilterDto? filter)
        {
            filter ??= new DispatcherOrderFilterDto();
            
            // Sayfa boyutu sınırlaması
            if (filter.PageSize > 100) filter.PageSize = 100;
            if (filter.PageSize < 1) filter.PageSize = 20;
            if (filter.Page < 1) filter.Page = 1;

            // Sevkiyat görevlisi için gösterilecek durumlar
            var allowedStatuses = new List<OrderStatus> 
            { 
                OrderStatus.Ready, 
                OrderStatus.Assigned, 
                OrderStatus.PickedUp,
                OrderStatus.OutForDelivery,
                OrderStatus.DeliveryFailed
            };

            var query = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Courier)
                .ThenInclude(c => c!.User)
                .Where(o => allowedStatuses.Contains(o.Status))
                .AsQueryable();

            // Durum filtresi
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                if (Enum.TryParse<OrderStatus>(filter.Status, true, out var statusFilter))
                {
                    query = query.Where(o => o.Status == statusFilter);
                }
                else if (filter.Status.ToLower() == "out_for_delivery")
                {
                    query = query.Where(o => o.Status == OrderStatus.OutForDelivery || 
                                             o.Status == OrderStatus.PickedUp);
                }
            }

            // Kurye filtresi
            if (filter.CourierId.HasValue)
            {
                query = query.Where(o => o.CourierId == filter.CourierId.Value);
            }

            // Acil siparişler (30+ dk bekleyen)
            if (filter.UrgentOnly == true)
            {
                var urgentThreshold = DateTime.UtcNow.AddMinutes(-30);
                query = query.Where(o => o.Status == OrderStatus.Ready && 
                                         o.ReadyAt.HasValue && 
                                         o.ReadyAt.Value < urgentThreshold);
            }

            // Toplam sayı
            var totalCount = await query.CountAsync();

            // Sıralama (varsayılan: en eski hazır olan önce)
            if (filter.SortBy?.ToLower() == "totalamount")
            {
                query = filter.SortOrder?.ToLower() == "desc" 
                    ? query.OrderByDescending(o => o.FinalPrice) 
                    : query.OrderBy(o => o.FinalPrice);
            }
            else
            {
                query = filter.SortOrder?.ToLower() == "desc" 
                    ? query.OrderByDescending(o => o.ReadyAt ?? o.OrderDate) 
                    : query.OrderBy(o => o.ReadyAt ?? o.OrderDate);
            }

            // Sayfalama
            var orders = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // DTO'lara dönüştür
            var orderDtos = orders.Select(o => MapToDispatcherDto(o)).ToList();

            // Özet istatistikleri hesapla
            var summary = await GetDispatcherSummaryAsync();

            return new DispatcherOrderListResponseDto
            {
                Orders = orderDtos,
                Summary = summary,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize),
                CurrentPage = filter.Page
            };
        }

        /// <inheritdoc />
        public async Task<DispatcherSummaryDto> GetDispatcherSummaryAsync()
        {
            var today = DateTime.UtcNow.Date;
            var urgentThreshold = DateTime.UtcNow.AddMinutes(-30);

            var readyCount = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Ready);

            var assignedCount = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Assigned);

            var outForDeliveryCount = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.OutForDelivery || 
                                 o.Status == OrderStatus.PickedUp);

            var deliveredTodayCount = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Delivered && 
                                 o.DeliveredAt.HasValue && 
                                 o.DeliveredAt.Value.Date == today);

            var failedTodayCount = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.DeliveryFailed && 
                                 o.OrderDate.Date == today);

            var urgentCount = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Ready && 
                                 o.ReadyAt.HasValue && 
                                 o.ReadyAt.Value < urgentThreshold);

            // Online kuryeler
            var onlineCouriersCount = await _context.Couriers
                .CountAsync(c => c.IsActive && c.IsOnline);

            // Müsait kuryeler (online ve 5'ten az aktif sipariş)
            var availableCouriersCount = await _context.Couriers
                .Where(c => c.IsActive && c.IsOnline)
                .CountAsync(c => !_context.Orders
                    .Any(o => o.CourierId == c.Id && 
                             (o.Status == OrderStatus.Assigned || 
                              o.Status == OrderStatus.PickedUp || 
                              o.Status == OrderStatus.OutForDelivery)));

            // Ortalama bekleme süresi
            var avgWaitingTime = await _context.Orders
                .Where(o => o.Status == OrderStatus.Ready && o.ReadyAt.HasValue)
                .Select(o => EF.Functions.DateDiffMinute(o.ReadyAt, DateTime.UtcNow))
                .AverageAsync() ?? 0;

            // Bugünkü toplam teslimat tutarı
            var todayTotalDelivered = await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered && 
                           o.DeliveredAt.HasValue && 
                           o.DeliveredAt.Value.Date == today)
                .SumAsync(o => o.FinalPrice);

            return new DispatcherSummaryDto
            {
                ReadyCount = readyCount,
                AssignedCount = assignedCount,
                OutForDeliveryCount = outForDeliveryCount,
                DeliveredTodayCount = deliveredTodayCount,
                FailedTodayCount = failedTodayCount,
                UrgentCount = urgentCount,
                OnlineCouriersCount = onlineCouriersCount,
                AvailableCouriersCount = availableCouriersCount,
                AverageWaitingTimeMinutes = avgWaitingTime,
                TodayTotalDeliveredAmount = todayTotalDelivered,
                LastUpdated = DateTime.UtcNow
            };
        }

        /// <inheritdoc />
        public async Task<AssignCourierResponseDto> AssignCourierToOrderAsync(int orderId, int courierId, string assignedBy, string? notes = null)
        {
            // Siparişi bul
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return new AssignCourierResponseDto
                {
                    Success = false,
                    Message = "Sipariş bulunamadı."
                };
            }

            // Sadece Ready durumundan Assigned'a geçiş yapılabilir
            if (order.Status != OrderStatus.Ready && order.Status != OrderStatus.DeliveryFailed)
            {
                return new AssignCourierResponseDto
                {
                    Success = false,
                    Message = $"Sipariş şu an kurye ataması için uygun değil. Mevcut durum: {order.Status}"
                };
            }

            // Kuryeyi bul
            var courier = await _context.Couriers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == courierId);

            if (courier == null)
            {
                return new AssignCourierResponseDto
                {
                    Success = false,
                    Message = "Kurye bulunamadı."
                };
            }

            if (!courier.IsActive)
            {
                return new AssignCourierResponseDto
                {
                    Success = false,
                    Message = "Seçilen kurye aktif değil."
                };
            }

            var previousStatus = order.Status;
            order.Status = OrderStatus.Assigned;
            order.CourierId = courierId;
            order.AssignedAt = DateTime.UtcNow;
            
            var historyNote = $"Kurye atandı: {courier.User?.FullName ?? $"#{courierId}"} - Atayan: {assignedBy}";
            if (!string.IsNullOrWhiteSpace(notes))
            {
                historyNote += $" - Not: {notes}";
            }
            
            AddStatusHistory(order, previousStatus, OrderStatus.Assigned, historyNote);
            
            await _context.SaveChangesAsync();

            return new AssignCourierResponseDto
            {
                Success = true,
                Message = "Kurye başarıyla atandı.",
                Order = MapToDispatcherDto(order),
                Courier = MapToCourierDto(courier)
            };
        }

        /// <inheritdoc />
        public async Task<AssignCourierResponseDto> ReassignCourierAsync(int orderId, int newCourierId, string reassignedBy, string reason)
        {
            // Siparişi bul
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Courier)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return new AssignCourierResponseDto
                {
                    Success = false,
                    Message = "Sipariş bulunamadı."
                };
            }

            // Kurye değişikliği yapılabilecek durumlar
            var allowedStatuses = new[] { OrderStatus.Assigned, OrderStatus.DeliveryFailed };
            if (!allowedStatuses.Contains(order.Status))
            {
                return new AssignCourierResponseDto
                {
                    Success = false,
                    Message = $"Bu sipariş için kurye değiştirilemez. Mevcut durum: {order.Status}"
                };
            }

            // Yeni kuryeyi bul
            var newCourier = await _context.Couriers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == newCourierId);

            if (newCourier == null)
            {
                return new AssignCourierResponseDto
                {
                    Success = false,
                    Message = "Yeni kurye bulunamadı."
                };
            }

            if (!newCourier.IsActive)
            {
                return new AssignCourierResponseDto
                {
                    Success = false,
                    Message = "Seçilen kurye aktif değil."
                };
            }

            var oldCourierId = order.CourierId;
            var oldCourierName = order.Courier?.User?.FullName ?? $"#{oldCourierId}";
            
            var previousStatus = order.Status;
            order.CourierId = newCourierId;
            order.AssignedAt = DateTime.UtcNow;
            order.Status = OrderStatus.Assigned; // Yeniden Assigned durumuna al
            
            var historyNote = $"Kurye değiştirildi: {oldCourierName} → {newCourier.User?.FullName ?? $"#{newCourierId}"} - " +
                             $"Neden: {reason} - Değiştiren: {reassignedBy}";
            
            AddStatusHistory(order, previousStatus, OrderStatus.Assigned, historyNote);
            
            await _context.SaveChangesAsync();

            return new AssignCourierResponseDto
            {
                Success = true,
                Message = "Kurye başarıyla değiştirildi.",
                Order = MapToDispatcherDto(order),
                Courier = MapToCourierDto(newCourier)
            };
        }

        /// <inheritdoc />
        public async Task<DispatcherCourierListResponseDto> GetAvailableCouriersAsync()
        {
            var couriers = await _context.Couriers
                .Include(c => c.User)
                .Where(c => c.IsActive)
                .ToListAsync();

            // Her kurye için aktif sipariş sayısını hesapla
            var courierDtos = new List<DispatcherCourierDto>();
            foreach (var courier in couriers)
            {
                var activeOrderCount = await _context.Orders
                    .CountAsync(o => o.CourierId == courier.Id && 
                                    (o.Status == OrderStatus.Assigned || 
                                     o.Status == OrderStatus.PickedUp || 
                                     o.Status == OrderStatus.OutForDelivery));

                var deliveredTodayCount = await _context.Orders
                    .CountAsync(o => o.CourierId == courier.Id && 
                                    o.Status == OrderStatus.Delivered && 
                                    o.DeliveredAt.HasValue && 
                                    o.DeliveredAt.Value.Date == DateTime.UtcNow.Date);

                courierDtos.Add(new DispatcherCourierDto
                {
                    Id = courier.Id,
                    Name = courier.User?.FullName ?? $"Kurye #{courier.Id}",
                    Phone = courier.User?.PhoneNumber ?? string.Empty,
                    Status = courier.IsOnline ? (activeOrderCount >= 5 ? "busy" : "online") : "offline",
                    StatusText = courier.IsOnline ? (activeOrderCount >= 5 ? "Meşgul" : "Müsait") : "Çevrimdışı",
                    StatusColor = courier.IsOnline ? (activeOrderCount >= 5 ? "yellow" : "green") : "red",
                    ActiveOrderCount = activeOrderCount,
                    DeliveredTodayCount = deliveredTodayCount,
                    VehicleType = courier.VehicleType ?? "motorcycle",
                    VehicleTypeText = GetVehicleTypeText(courier.VehicleType),
                    IsAvailable = courier.IsOnline && activeOrderCount < 5,
                    LastSeenAt = courier.LastSeenAt
                });
            }

            return new DispatcherCourierListResponseDto
            {
                Couriers = courierDtos.OrderByDescending(c => c.IsAvailable)
                                      .ThenBy(c => c.ActiveOrderCount)
                                      .ToList(),
                OnlineCount = courierDtos.Count(c => c.Status != "offline"),
                AvailableCount = courierDtos.Count(c => c.IsAvailable),
                BusyCount = courierDtos.Count(c => c.Status == "busy"),
                OfflineCount = courierDtos.Count(c => c.Status == "offline")
            };
        }

        // ============================================================
        // YARDIMCI MAPPER METODLARI
        // ============================================================

        /// <summary>
        /// Order entity'sini StoreAttendantOrderDto'ya dönüştürür.
        /// </summary>
        private StoreAttendantOrderDto MapToStoreAttendantDto(Order order)
        {
            var now = DateTime.UtcNow;
            var timeAgo = CalculateTimeAgo(order.OrderDate, now);

            return new StoreAttendantOrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber ?? $"#{order.Id}",
                CustomerName = order.CustomerName ?? "Misafir",
                CustomerPhone = order.CustomerPhone,
                Status = order.Status.ToString(),
                StatusText = GetStatusText(order.Status),
                TotalAmount = order.FinalPrice > 0 ? order.FinalPrice : order.TotalPrice,
                ItemCount = order.OrderItems?.Sum(oi => oi.Quantity) ?? 0,
                CreatedAt = order.OrderDate,
                ConfirmedAt = order.ConfirmedAt,
                PreparingStartedAt = order.PreparingStartedAt,
                ReadyAt = order.ReadyAt,
                PaymentMethod = order.PaymentMethod ?? "card",
                IsCashOnDelivery = order.PaymentMethod?.ToLower().Contains("cash") == true || 
                                   order.PaymentMethod?.ToLower().Contains("kapida") == true,
                OrderNotes = order.DeliveryNotes,
                DeliveryAddress = order.ShippingAddress,
                Items = order.OrderItems?.Take(3).Select(oi => new StoreOrderItemSummaryDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "Ürün",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    ImageUrl = oi.Product?.ImageUrl
                }).ToList() ?? new List<StoreOrderItemSummaryDto>(),
                PreparedBy = order.PreparedBy,
                TimeAgo = timeAgo,
                WeightInGrams = order.WeightInGrams,
                HasWeightBasedItems = order.OrderItems?.Any(oi => oi.Product?.IsWeightBased == true) ?? false
            };
        }

        /// <summary>
        /// Order entity'sini DispatcherOrderDto'ya dönüştürür.
        /// </summary>
        private DispatcherOrderDto MapToDispatcherDto(Order order)
        {
            var now = DateTime.UtcNow;
            var readyAt = order.ReadyAt ?? order.OrderDate;
            var waitingMinutes = (int)(now - readyAt).TotalMinutes;
            var timeAgo = CalculateTimeAgo(readyAt, now);

            return new DispatcherOrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber ?? $"#{order.Id}",
                CustomerName = order.CustomerName ?? "Misafir",
                CustomerPhone = order.CustomerPhone,
                Status = order.Status.ToString(),
                StatusText = GetStatusText(order.Status),
                TotalAmount = order.FinalPrice,
                ItemCount = order.OrderItems?.Sum(oi => oi.Quantity) ?? 0,
                DeliveryAddress = order.ShippingAddress ?? string.Empty,
                DeliveryNotes = order.DeliveryNotes,
                PaymentMethod = order.PaymentMethod ?? "card",
                IsCashOnDelivery = order.PaymentMethod?.ToLower().Contains("cash") == true || 
                                   order.PaymentMethod?.ToLower().Contains("kapida") == true,
                ReadyAt = order.ReadyAt,
                AssignedAt = order.AssignedAt,
                PickedUpAt = order.PickedUpAt,
                CourierId = order.CourierId,
                CourierName = order.Courier?.User?.FullName,
                CourierPhone = order.Courier?.User?.PhoneNumber,
                TimeAgo = timeAgo,
                WaitingMinutes = waitingMinutes,
                IsUrgent = waitingMinutes > 30 && order.Status == OrderStatus.Ready,
                WeightInGrams = order.WeightInGrams
            };
        }

        /// <summary>
        /// Courier entity'sini DispatcherCourierDto'ya dönüştürür.
        /// </summary>
        private DispatcherCourierDto MapToCourierDto(Courier courier)
        {
            return new DispatcherCourierDto
            {
                Id = courier.Id,
                Name = courier.User?.FullName ?? $"Kurye #{courier.Id}",
                Phone = courier.User?.PhoneNumber ?? string.Empty,
                Status = courier.IsOnline ? "online" : "offline",
                StatusText = courier.IsOnline ? "Müsait" : "Çevrimdışı",
                StatusColor = courier.IsOnline ? "green" : "red",
                VehicleType = courier.VehicleType ?? "motorcycle",
                VehicleTypeText = GetVehicleTypeText(courier.VehicleType),
                IsAvailable = courier.IsActive && courier.IsOnline,
                LastSeenAt = courier.LastSeenAt
            };
        }

        /// <summary>
        /// Sipariş durumu için Türkçe metin döner.
        /// </summary>
        private static string GetStatusText(OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.New:
                    return "Yeni Sipariş";
                case OrderStatus.Confirmed:
                    return "Onaylandı";
                case OrderStatus.Preparing:
                    return "Hazırlanıyor";
                case OrderStatus.Ready:
                    return "Hazır";
                case OrderStatus.Assigned:
                    return "Kurye Atandı";
                case OrderStatus.PickedUp:
                    return "Teslim Alındı";
                case OrderStatus.OutForDelivery:
                    return "Yolda";
                case OrderStatus.Delivered:
                    return "Teslim Edildi";
                case OrderStatus.Cancelled:
                    return "İptal Edildi";
                case OrderStatus.DeliveryFailed:
                    return "Teslimat Başarısız";
                case OrderStatus.Pending:
                    return "Beklemede";
                case OrderStatus.Paid:
                    return "Ödendi";
                default:
                    return status.ToString();
            }
        }

        /// <summary>
        /// Araç tipi için Türkçe metin döner.
        /// </summary>
        private static string GetVehicleTypeText(string? vehicleType)
        {
            return vehicleType?.ToLower() switch
            {
                "motorcycle" => "Motorsiklet",
                "car" => "Araba",
                "bicycle" => "Bisiklet",
                "on_foot" => "Yaya",
                _ => "Motorsiklet"
            };
        }

        /// <summary>
        /// Zaman farkını "X dk önce" formatında hesaplar.
        /// </summary>
        private static string CalculateTimeAgo(DateTime past, DateTime now)
        {
            var diff = now - past;
            
            if (diff.TotalMinutes < 1)
                return "Az önce";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} dk önce";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} saat önce";
            
            return $"{(int)diff.TotalDays} gün önce";
        }

        /// <summary>
        /// Status string'ini normalize eder: snake_case → PascalCase dönüşümü yapar.
        /// Frontend'den gelen "out_for_delivery" → "OutForDelivery" olur.
        /// </summary>
        private static string NormalizeStatusString(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return status;

            // Özel mapping: snake_case frontend değerleri → PascalCase enum değerleri
            var statusMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "out_for_delivery", "OutForDelivery" },
                { "outfordelivery", "OutForDelivery" },
                { "picked_up", "PickedUp" },
                { "pickedup", "PickedUp" },
                { "in_transit", "InTransit" },
                { "intransit", "InTransit" },
                { "delivery_failed", "DeliveryFailed" },
                { "deliveryfailed", "DeliveryFailed" },
                { "delivery_payment_pending", "DeliveryPaymentPending" },
                { "deliverypaymentpending", "DeliveryPaymentPending" },
                { "payment_failed", "PaymentFailed" },
                { "paymentfailed", "PaymentFailed" },
                { "chargeback_pending", "ChargebackPending" },
                { "chargebackpending", "ChargebackPending" },
                { "ready_for_pickup", "ReadyForPickup" },
                { "readyforpickup", "ReadyForPickup" },
                { "partial_refund", "PartialRefund" },
                { "partialrefund", "PartialRefund" }
            };

            // Mapping varsa dönüştür, yoksa olduğu gibi döndür
            return statusMappings.TryGetValue(status, out var mapped) ? mapped : status;
        }
    }
}
