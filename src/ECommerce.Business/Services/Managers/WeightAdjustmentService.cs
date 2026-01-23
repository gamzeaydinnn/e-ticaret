using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Core.DTOs;
using ECommerce.Core.DTOs.Weight;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// AĞIRLIK BAZLI DİNAMİK ÖDEME SİSTEMİ - ANA SERVİS
    /// 
    /// Bu servis ağırlık bazlı ürünlerin (kg/gram satılan) tüm iş mantığını yönetir.
    /// 
    /// ANA AKIŞ:
    /// 1. Sipariş oluşturma → InitializeWeightBasedOrderAsync (tahmini değerler set)
    /// 2. Kurye tartımı → RecordCourierWeightEntryAsync (gerçek değerler)
    /// 3. Fark hesaplama → CalculateOrderWeightDifferenceAsync
    /// 4. Ödeme finalizasyonu → FinalizeWeightBasedPaymentAsync
    /// 
    /// TOLERANS YOK - HER GRAM FARK HESAPLANIR!
    /// </summary>
    public class WeightAdjustmentService : IWeightAdjustmentService
    {
        // Dokümanla uyumlu eşikler: %20 veya 50 TL üzeri fark admin onayı gerektirir.
        private const decimal ADMIN_APPROVAL_THRESHOLD_PERCENT = 20m;
        private const decimal ADMIN_APPROVAL_THRESHOLD_AMOUNT = 50m;

        // Dokümanla uyumlu ön provizyon marjı: %20.
        private const decimal PRE_AUTH_MARGIN_PERCENT = 20m;

        private readonly IWeightAdjustmentRepository _adjustmentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPaymentService _paymentService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<WeightAdjustmentService>? _logger;

        public WeightAdjustmentService(
            IWeightAdjustmentRepository adjustmentRepository,
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            IUserRepository userRepository,
            IPaymentService paymentService,
            IUnitOfWork unitOfWork,
            ILogger<WeightAdjustmentService>? logger = null)
        {
            _adjustmentRepository = adjustmentRepository ?? throw new ArgumentNullException(nameof(adjustmentRepository));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger;
        }

        #region Kurye Ağırlık Girişi

        /// <inheritdoc />
        public async Task<WeightEntryResultDto> RecordCourierWeightEntryAsync(
            int orderId, int orderItemId, decimal actualWeight, int courierId)
        {
            _logger?.LogInformation("[WEIGHT-SVC] Kurye ağırlık girişi: Order={OrderId}, Item={ItemId}, Weight={Weight}g, Courier={CourierId}",
                orderId, orderItemId, actualWeight, courierId);

            var result = new WeightEntryResultDto
            {
                OrderItemId = orderItemId,
                IsSuccess = false
            };

            try
            {
                // Validasyon
                var validationResult = await ValidateWeightEntryAsync(orderItemId, actualWeight);
                if (!validationResult.IsValid)
                {
                    result.ErrorMessage = validationResult.ErrorMessage;
                    return result;
                }

                // Siparişi getir (tüm detaylarla)
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    result.ErrorMessage = "Sipariş bulunamadı";
                    return result;
                }

                // Sipariş kalemini bul
                var orderItem = order.OrderItems?.FirstOrDefault(oi => oi.Id == orderItemId);
                if (orderItem == null)
                {
                    result.ErrorMessage = "Sipariş kalemi bulunamadı";
                    return result;
                }

                // Ağırlık bazlı ürün mü kontrol et
                if (!orderItem.IsWeightBased)
                {
                    result.ErrorMessage = "Bu ürün ağırlık bazlı değil";
                    return result;
                }

                // Zaten tartılmış mı?
                if (orderItem.IsWeighed)
                {
                    _logger?.LogWarning("[WEIGHT-SVC] Ürün zaten tartılmış: Item={ItemId}", orderItemId);
                    result.ErrorMessage = "Bu ürün zaten tartılmış";
                    return result;
                }

                // Ürün bilgilerini al (PricePerUnit için)
                var product = await _productRepository.GetByIdAsync(orderItem.ProductId);
                if (product == null)
                {
                    result.ErrorMessage = "Ürün bulunamadı";
                    return result;
                }

                // Farkları hesapla
                var estimatedWeight = orderItem.EstimatedWeight;
                var weightDifference = actualWeight - estimatedWeight;
                var pricePerUnit = product.PricePerUnit > 0 ? product.PricePerUnit : product.Price;
                var priceDifference = weightDifference * pricePerUnit;
                var actualPrice = actualWeight * pricePerUnit;
                var differencePercent = estimatedWeight > 0 ? (weightDifference / estimatedWeight) * 100 : 0;

                // OrderItem güncelle
                orderItem.ActualWeight = actualWeight;
                orderItem.IsWeighed = true;
                orderItem.WeighedAt = DateTime.UtcNow;
                orderItem.WeighedByCourierId = courierId;
                orderItem.WeightDifference = weightDifference;
                orderItem.ActualPrice = actualPrice;
                orderItem.PriceDifference = priceDifference;

                // WeightAdjustment kaydı oluştur
                var adjustment = new WeightAdjustment
                {
                    OrderId = orderId,
                    OrderItemId = orderItemId,
                    ProductId = orderItem.ProductId,
                    ProductName = product.Name,
                    WeightUnit = product.WeightUnit,
                    EstimatedWeight = estimatedWeight,
                    ActualWeight = actualWeight,
                    WeightDifference = weightDifference,
                    DifferencePercent = differencePercent,
                    PricePerUnit = pricePerUnit,
                    EstimatedPrice = orderItem.EstimatedPrice > 0 ? orderItem.EstimatedPrice : (estimatedWeight * pricePerUnit),
                    ActualPrice = actualPrice,
                    PriceDifference = priceDifference,
                    Status = WeightAdjustmentStatus.Weighed,
                    WeighedByCourierId = courierId,
                    WeighedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                // Fark büyükse admin onayına gönder (>%20 veya >50 TL ise)
                var requiresAdminApproval =
                    Math.Abs(differencePercent) > ADMIN_APPROVAL_THRESHOLD_PERCENT ||
                    Math.Abs(priceDifference) > ADMIN_APPROVAL_THRESHOLD_AMOUNT;

                if (requiresAdminApproval)
                {
                    adjustment.Status = WeightAdjustmentStatus.PendingAdminApproval;
                    adjustment.RequiresAdminApproval = true;
                    _logger?.LogWarning("[WEIGHT-SVC] Yüksek fark - admin onayı gerekli: %{Percent:F1}", differencePercent);
                }
                else if (priceDifference > 0)
                {
                    adjustment.Status = WeightAdjustmentStatus.PendingAdditionalPayment;
                }
                else if (priceDifference < 0)
                {
                    adjustment.Status = WeightAdjustmentStatus.PendingRefund;
                }
                else
                {
                    adjustment.Status = WeightAdjustmentStatus.NoDifference;
                }

                await _adjustmentRepository.AddAsync(adjustment);

                // Order toplam değerlerini güncelle
                await UpdateOrderTotalsAsync(order);

                // Değişiklikleri kaydet
                await _unitOfWork.SaveChangesAsync();

                // Sonuç doldur
                result.IsSuccess = true;
                result.ProductName = product.Name;
                result.EstimatedWeight = estimatedWeight;
                result.ActualWeight = actualWeight;
                result.WeightDifference = weightDifference;
                result.PriceDifference = priceDifference;
                result.AdjustmentId = adjustment.Id;

                _logger?.LogInformation("[WEIGHT-SVC] Ağırlık kaydedildi: Item={ItemId}, Fark={WeightDiff}g, Tutar={PriceDiff:F2}TL",
                    orderItemId, weightDifference, priceDifference);

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-SVC] Ağırlık girişi hatası: Order={OrderId}, Item={ItemId}", orderId, orderItemId);
                result.ErrorMessage = "Beklenmeyen bir hata oluştu";
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<WeightEntryResultDto>> RecordBulkWeightEntriesAsync(
            int orderId, List<WeightEntryRequestDto> entries, int courierId)
        {
            _logger?.LogInformation("[WEIGHT-SVC] Toplu ağırlık girişi: Order={OrderId}, Count={Count}", orderId, entries.Count);

            var results = new List<WeightEntryResultDto>();

            foreach (var entry in entries)
            {
                var result = await RecordCourierWeightEntryAsync(orderId, entry.OrderItemId, entry.ActualWeight, courierId);
                results.Add(result);
            }

            return results;
        }

        #endregion

        #region Fark Hesaplama

        /// <inheritdoc />
        public async Task<WeightDifferenceCalculationDto> CalculateOrderWeightDifferenceAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new ArgumentException($"Sipariş bulunamadı: {orderId}");
            }

            var calculation = new WeightDifferenceCalculationDto
            {
                OrderId = orderId,
                Items = new List<ItemWeightDifferenceDto>()
            };

            var weightBasedItems = order.OrderItems?.Where(oi => oi.IsWeightBased) ?? Enumerable.Empty<OrderItem>();

            foreach (var item in weightBasedItems)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                var pricePerUnit = (product != null && product.PricePerUnit > 0) ? product.PricePerUnit : item.UnitPrice;

                var itemCalc = new ItemWeightDifferenceDto
                {
                    OrderItemId = item.Id,
                    ProductName = product?.Name ?? "Bilinmeyen Ürün",
                    EstimatedWeight = item.EstimatedWeight,
                    ActualWeight = item.ActualWeight ?? 0,
                    WeightDifference = (item.ActualWeight ?? 0) - item.EstimatedWeight,
                    PricePerUnit = pricePerUnit,
                    PriceDifference = item.PriceDifference ?? 0
                };

                calculation.Items.Add(itemCalc);

                // Toplamları güncelle
                calculation.TotalEstimatedWeight += itemCalc.EstimatedWeight;
                calculation.TotalActualWeight += itemCalc.ActualWeight;
                calculation.TotalWeightDifference += itemCalc.WeightDifference;
                calculation.TotalEstimatedPrice += item.EstimatedPrice > 0 ? item.EstimatedPrice : (itemCalc.EstimatedWeight * pricePerUnit);
                calculation.TotalActualPrice += (item.ActualPrice ?? 0) > 0 ? item.ActualPrice.Value : (itemCalc.ActualWeight * pricePerUnit);
                calculation.TotalPriceDifference += itemCalc.PriceDifference;
            }

            return calculation;
        }

        /// <inheritdoc />
        public async Task<WeightDifferenceCalculationDto> CalculateItemWeightDifferenceAsync(int orderItemId)
        {
            var adjustment = await _adjustmentRepository.GetByOrderItemIdAsync(orderItemId);
            if (adjustment == null)
            {
                throw new ArgumentException($"Sipariş kalemi için ayarlama kaydı bulunamadı: {orderItemId}");
            }

            return await CalculateOrderWeightDifferenceAsync(adjustment.OrderId);
        }

        #endregion

        #region Ödeme Finalizasyonu

        /// <inheritdoc />
        public async Task<PaymentSettlementResultDto> FinalizeWeightBasedPaymentAsync(
            int orderId, int courierId, string? courierNotes = null)
        {
            _logger?.LogInformation("[WEIGHT-SVC] Ödeme finalizasyonu başlıyor: Order={OrderId}", orderId);

            var result = new PaymentSettlementResultDto { IsSuccess = false };

            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    result.ErrorMessage = "Sipariş bulunamadı";
                    return result;
                }

                // Tüm kalemler tartılmış mı?
                var weightBasedItems = order.OrderItems?.Where(oi => oi.IsWeightBased) ?? Enumerable.Empty<OrderItem>();
                if (weightBasedItems.Any(oi => !oi.IsWeighed))
                {
                    result.ErrorMessage = "Henüz tartılmamış kalemler var";
                    return result;
                }

                // Admin onayı bekleyen var mı?
                var adjustments = await _adjustmentRepository.GetByOrderIdAsync(orderId);
                if (adjustments.Any(a => a.Status == WeightAdjustmentStatus.PendingAdminApproval))
                {
                    result.ErrorMessage = "Admin onayı bekleyen kalemler var";
                    return result;
                }

                // Fark hesapla
                var calculation = await CalculateOrderWeightDifferenceAsync(orderId);

                result.PreAuthAmount = order.PreAuthAmount > 0 ? order.PreAuthAmount : order.TotalPrice;
                result.FinalAmount = order.TotalPrice + calculation.TotalPriceDifference;
                result.DifferenceAmount = calculation.TotalPriceDifference;
                result.PaymentMethod = order.PaymentMethod ?? "Unknown";

                // Ödeme türüne göre işlem
                if (order.PaymentMethod == "Cash" || order.PaymentMethod == "Nakit")
                {
                    // Nakit ödeme - kurye farkı tahsil etti/iade etti olarak işaretle
                    result.IsSuccess = await RecordCashDifferenceSettlementAsync(orderId, courierId, 
                        calculation.TotalPriceDifference, courierNotes);
                    result.TransactionReference = $"CASH-{orderId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
                }
                else
                {
                    // Kart ödemesi - POSNET entegrasyonu gerekiyor
                    // TODO: POSNET PreAuth capture veya refund işlemi
                    _logger?.LogWarning("[WEIGHT-SVC] Kart ödeme finalizasyonu henüz implemente edilmedi");
                    
                    // Şimdilik sadece order'ı güncelle
                    order.FinalAmount = result.FinalAmount;
                    order.TotalWeightDifference = calculation.TotalWeightDifference;
                    order.TotalPriceDifference = calculation.TotalPriceDifference;
                    order.DifferenceSettled = true;
                    order.DifferenceSettledAt = DateTime.UtcNow;
                    order.WeightAdjustmentStatus = WeightAdjustmentStatus.Completed;

                    await _unitOfWork.SaveChangesAsync();

                    result.IsSuccess = true;
                    result.TransactionReference = $"CARD-{orderId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
                }

                _logger?.LogInformation("[WEIGHT-SVC] Ödeme finalize edildi: Order={OrderId}, Diff={Diff:F2}TL", 
                    orderId, calculation.TotalPriceDifference);

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-SVC] Ödeme finalizasyonu hatası: Order={OrderId}", orderId);
                result.ErrorMessage = "Ödeme işlemi sırasında hata oluştu";
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> RecordCashDifferenceSettlementAsync(
            int orderId, int courierId, decimal collectedAmount, string? notes = null)
        {
            _logger?.LogInformation("[WEIGHT-SVC] Nakit fark tahsilatı: Order={OrderId}, Amount={Amount:F2}TL", 
                orderId, collectedAmount);

            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null) return false;

                // WeightAdjustment kayıtlarını güncelle
                var adjustments = await _adjustmentRepository.GetByOrderIdAsync(orderId);
                foreach (var adjustment in adjustments.Where(a => 
                    a.Status == WeightAdjustmentStatus.Weighed ||
                    a.Status == WeightAdjustmentStatus.PendingAdditionalPayment ||
                    a.Status == WeightAdjustmentStatus.PendingRefund))
                {
                    adjustment.Status = WeightAdjustmentStatus.Completed;
                    adjustment.IsSettled = true;
                    adjustment.SettledAt = DateTime.UtcNow;
                    adjustment.UpdatedAt = DateTime.UtcNow;
                }

                // Order güncelle
                order.DifferenceSettled = true;
                order.DifferenceSettledAt = DateTime.UtcNow;
                order.TotalPriceDifference = collectedAmount;
                order.FinalAmount = order.TotalPrice + collectedAmount;
                order.WeightAdjustmentStatus = WeightAdjustmentStatus.Completed;

                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-SVC] Nakit tahsilat kaydı hatası: Order={OrderId}", orderId);
                throw;
            }
        }

        #endregion

        #region Admin Müdahalesi

        /// <inheritdoc />
        public async Task<bool> ProcessAdminDecisionAsync(
            int adjustmentId, int adminId, AdminDecisionType decision,
            decimal? overrideAmount = null, string? adminNotes = null)
        {
            _logger?.LogInformation("[WEIGHT-SVC] Admin kararı: Adj={AdjustmentId}, Decision={Decision}, Admin={AdminId}",
                adjustmentId, decision, adminId);

            try
            {
                var adjustment = await _adjustmentRepository.GetByIdAsync(adjustmentId);
                if (adjustment == null)
                {
                    _logger?.LogWarning("[WEIGHT-SVC] Ayarlama kaydı bulunamadı: {Id}", adjustmentId);
                    return false;
                }

                adjustment.AdminUserId = adminId;
                adjustment.AdminReviewedAt = DateTime.UtcNow;
                adjustment.AdminNote = adminNotes;
                adjustment.AdminReviewed = true;
                adjustment.UpdatedAt = DateTime.UtcNow;

                switch (decision)
                {
                    case AdminDecisionType.Approve:
                        adjustment.AdminApproved = true;
                        adjustment.Status = adjustment.PriceDifference > 0 
                            ? WeightAdjustmentStatus.PendingAdditionalPayment 
                            : WeightAdjustmentStatus.PendingRefund;
                        break;

                    case AdminDecisionType.Reject:
                        adjustment.AdminApproved = false;
                        adjustment.Status = WeightAdjustmentStatus.RejectedByAdmin;
                        // Farkı sıfırla
                        adjustment.PriceDifference = 0;
                        break;

                    case AdminDecisionType.Override:
                        adjustment.AdminApproved = true;
                        if (overrideAmount.HasValue)
                        {
                            adjustment.AdminAdjustedPrice = overrideAmount.Value;
                            adjustment.PriceDifference = overrideAmount.Value;
                        }
                        adjustment.Status = adjustment.PriceDifference > 0 
                            ? WeightAdjustmentStatus.PendingAdditionalPayment 
                            : WeightAdjustmentStatus.PendingRefund;
                        break;

                    case AdminDecisionType.WaiveForCustomer:
                        adjustment.AdminApproved = true;
                        adjustment.PriceDifference = 0;
                        adjustment.AdminNote = $"[MÜŞTERİ LEHİNE İPTAL] {adminNotes}";
                        adjustment.Status = WeightAdjustmentStatus.NoDifference;
                        break;
                }

                await _unitOfWork.SaveChangesAsync();

                // Order toplamlarını güncelle
                if (adjustment.OrderId > 0)
                {
                    var order = await _orderRepository.GetByIdAsync(adjustment.OrderId);
                    if (order != null)
                    {
                        await UpdateOrderTotalsAsync(order);
                    }
                }

                _logger?.LogInformation("[WEIGHT-SVC] Admin kararı uygulandı: Adj={AdjustmentId}, NewStatus={Status}",
                    adjustmentId, adjustment.Status);

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-SVC] Admin kararı uygulama hatası: Adj={AdjustmentId}", adjustmentId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> RequestAdminReviewAsync(int orderId, string reason)
        {
            _logger?.LogInformation("[WEIGHT-SVC] Admin onayı talep ediliyor: Order={OrderId}, Reason={Reason}", orderId, reason);

            try
            {
                var adjustments = await _adjustmentRepository.GetByOrderIdAsync(orderId);
                foreach (var adjustment in adjustments.Where(a => 
                    a.Status == WeightAdjustmentStatus.Weighed ||
                    a.Status == WeightAdjustmentStatus.PendingWeighing))
                {
                    adjustment.Status = WeightAdjustmentStatus.PendingAdminApproval;
                    adjustment.RequiresAdminApproval = true;
                    adjustment.UpdatedAt = DateTime.UtcNow;
                }

                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order != null)
                {
                    order.WeightAdjustmentStatus = WeightAdjustmentStatus.PendingAdminApproval;
                }

                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-SVC] Admin onay talebi hatası: Order={OrderId}", orderId);
                throw;
            }
        }

        #endregion

        #region Sipariş Oluşturma Entegrasyonu

        /// <inheritdoc />
        public async Task InitializeWeightBasedOrderAsync(Order order)
        {
            if (order?.OrderItems == null) return;

            var hasWeightBasedItems = false;
            decimal totalEstimatedPrice = 0;

            foreach (var item in order.OrderItems)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null) continue;

                if (product.IsWeightBased)
                {
                    hasWeightBasedItems = true;
                    item.IsWeightBased = true;
                    item.WeightUnit = product.WeightUnit;
                    item.PricePerUnit = product.PricePerUnit > 0 ? product.PricePerUnit : product.Price;

                    // Tahmini ağırlık (sepetten gelen miktar gram olarak)
                    // Eğer adet olarak geldiyse, varsayılan gramaj hesapla
                    if (item.EstimatedWeight == 0)
                    {
                        item.EstimatedWeight = item.Quantity * 1000; // 1 adet = 1kg varsayımı
                    }

                    item.EstimatedPrice = item.EstimatedWeight * item.PricePerUnit;
                    totalEstimatedPrice += item.EstimatedPrice;
                }
            }

            if (hasWeightBasedItems)
            {
                order.HasWeightBasedItems = true;
                order.WeightAdjustmentStatus = WeightAdjustmentStatus.PendingWeighing;
                order.PreAuthAmount = CalculatePreAuthAmountInternal(totalEstimatedPrice);
                order.AllItemsWeighed = false;
            }
        }

        /// <inheritdoc />
        public async Task<decimal> CalculatePreAuthAmountAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return 0;

            var calculation = await CalculateOrderWeightDifferenceAsync(orderId);
            return CalculatePreAuthAmountInternal(calculation.TotalEstimatedPrice);
        }

        private decimal CalculatePreAuthAmountInternal(decimal estimatedTotal)
        {
            // PreAuth = Tahmini toplam + %20 güvenlik marjı (dokümanla uyumlu)
            // %20'nin üzerindeki farklar admin onayına düşer.
            var preAuthAmount = estimatedTotal * (1 + (PRE_AUTH_MARGIN_PERCENT / 100m));
            return Math.Round(preAuthAmount, 2);
        }

        #endregion

        #region Sorgulama

        /// <inheritdoc />
        public async Task<OrderWeightSummaryDto?> GetOrderWeightSummaryAsync(int orderId)
        {
            var adjustments = await _adjustmentRepository.GetByOrderIdAsync(orderId);
            if (!adjustments.Any()) return null;

            var order = await _orderRepository.GetByIdAsync(orderId);

            return new OrderWeightSummaryDto
            {
                OrderId = orderId,
                OrderNumber = order?.OrderNumber ?? "",
                WeightBasedItemCount = adjustments.Count(),
                WeighedItemCount = adjustments.Count(a => a.WeighedAt != null),
                AllItemsWeighed = order?.AllItemsWeighed ?? false,
                EstimatedTotal = adjustments.Sum(a => a.EstimatedPrice),
                ActualTotal = adjustments.Sum(a => a.ActualPrice),
                TotalDifference = adjustments.Sum(a => a.PriceDifference),
                DifferencePercent = adjustments.Sum(a => a.EstimatedPrice) > 0 
                    ? (adjustments.Sum(a => a.PriceDifference) / adjustments.Sum(a => a.EstimatedPrice)) * 100 
                    : 0,
                OverallStatus = order?.WeightAdjustmentStatus ?? WeightAdjustmentStatus.PendingWeighing,
                HasPendingAdminApproval = adjustments.Any(a => a.Status == WeightAdjustmentStatus.PendingAdminApproval),
                Adjustments = adjustments.Select(MapToDto).ToList()
            };
        }

        /// <inheritdoc />
        public async Task<WeightAdjustmentDto?> GetWeightAdjustmentByIdAsync(int adjustmentId)
        {
            var adjustment = await _adjustmentRepository.GetByIdAsync(adjustmentId);
            return adjustment != null ? MapToDto(adjustment) : null;
        }

        /// <inheritdoc />
        public async Task<List<PendingWeightOrderDto>> GetPendingWeightEntriesForCourierAsync(int courierId)
        {
            _logger?.LogInformation("[WEIGHT-SVC] Kurye bekleyen tartım listesi: Courier={CourierId}", courierId);

            // TODO: Kuryeye atanmış siparişleri filtrele
            // Şimdilik tüm bekleyen ağırlık bazlı siparişleri getir
            var result = new List<PendingWeightOrderDto>();

            // Bu metod için özel bir repository sorgusu gerekiyor
            // Şimdilik boş liste dön
            return await Task.FromResult(result);
        }

        /// <inheritdoc />
        public async Task<PagedResult<WeightAdjustmentDto>> GetPendingAdminReviewsAsync(int page = 1, int pageSize = 20)
        {
            var filter = new WeightAdjustmentFilterDto
            {
                Status = WeightAdjustmentStatus.PendingAdminApproval,
                Page = page,
                PageSize = pageSize
            };

            return await GetFilteredAdjustmentsAsync(filter);
        }

        /// <inheritdoc />
        public async Task<PagedResult<WeightAdjustmentDto>> GetFilteredAdjustmentsAsync(WeightAdjustmentFilterDto filter)
        {
            var pagedResult = await _adjustmentRepository.GetFilteredAsync(filter);

            var dtos = pagedResult.Items.Select(MapToDto).ToList();

            return new PagedResult<WeightAdjustmentDto>(dtos, pagedResult.Total, pagedResult.Skip, pagedResult.Take);
        }

        #endregion

        #region İstatistik ve Raporlama

        /// <inheritdoc />
        public async Task<WeightAdjustmentStatsDto> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _adjustmentRepository.GetStatisticsAsync(startDate, endDate);
        }

        /// <inheritdoc />
        public async Task<CourierWeightPerformanceDto> GetCourierPerformanceAsync(
            int courierId, DateTime? startDate = null, DateTime? endDate = null)
        {
            // TODO: Kurye bazlı istatistik sorgusu
            var performance = new CourierWeightPerformanceDto
            {
                CourierId = courierId,
                TotalWeighings = 0,
                AverageWeightDifference = 0,
                AveragePriceDifference = 0,
                TotalCustomerFavorDifference = 0,
                TotalStoreFavorDifference = 0,
                SettlementSuccessRate = 0
            };

            // Kurye bilgisini al
            var courier = await _userRepository.GetByIdAsync(courierId);
            if (courier != null)
            {
                performance.CourierName = courier.FullName ?? courier.Email ?? "Bilinmeyen";
            }

            return await Task.FromResult(performance);
        }

        #endregion

        #region Validasyon

        /// <inheritdoc />
        public async Task<bool> CanProcessWeightAdjustmentAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return false;

            // Sipariş ağırlık bazlı kalem içermeli
            if (!order.HasWeightBasedItems) return false;

            // Sipariş durumu uygun olmalı (örn: Hazırlanıyor veya Kargoda)
            // TODO: Order status kontrolü

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> CanCourierEnterWeightAsync(int orderId, int courierId)
        {
            // TODO: Kuryenin bu siparişe atanıp atanmadığını kontrol et
            return await CanProcessWeightAdjustmentAsync(orderId);
        }

        /// <inheritdoc />
        public async Task<WeightValidationResultDto> ValidateWeightEntryAsync(int orderItemId, decimal weight)
        {
            var result = new WeightValidationResultDto { IsValid = true };

            if (weight <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "Ağırlık sıfır veya negatif olamaz";
                return result;
            }

            // TODO: OrderItem'dan ürün bilgisi al ve min/max kontrolü yap
            // Şimdilik makul bir aralık kontrolü
            if (weight > 100000) // 100kg üstü şüpheli
            {
                result.IsValid = false;
                result.ErrorMessage = "Ağırlık değeri çok yüksek görünüyor (max 100kg)";
                result.MaxAllowed = 100000;
            }

            return await Task.FromResult(result);
        }

        #endregion

        #region Private Yardımcı Metodlar

        private async Task UpdateOrderTotalsAsync(Order order)
        {
            if (order?.OrderItems == null) return;

            var weightBasedItems = order.OrderItems.Where(oi => oi.IsWeightBased);

            decimal totalWeightDiff = 0;
            decimal totalPriceDiff = 0;
            bool allWeighed = true;

            foreach (var item in weightBasedItems)
            {
                if (!item.IsWeighed)
                {
                    allWeighed = false;
                }
                else
                {
                    totalWeightDiff += item.WeightDifference ?? 0;
                    totalPriceDiff += item.PriceDifference ?? 0;
                }
            }

            order.TotalWeightDifference = totalWeightDiff;
            order.TotalPriceDifference = totalPriceDiff;
            order.AllItemsWeighed = allWeighed;
            order.FinalAmount = order.TotalPrice + totalPriceDiff;

            if (allWeighed && order.WeightAdjustmentStatus == WeightAdjustmentStatus.PendingWeighing)
            {
                order.WeightAdjustmentStatus = WeightAdjustmentStatus.Weighed;
            }

            await Task.CompletedTask;
        }

        private WeightAdjustmentDto MapToDto(WeightAdjustment adjustment)
        {
            var dto = new WeightAdjustmentDto
            {
                Id = adjustment.Id,
                OrderId = adjustment.OrderId,
                OrderNumber = adjustment.Order?.OrderNumber ?? "",
                OrderItemId = adjustment.OrderItemId,
                ProductId = adjustment.ProductId,
                ProductName = adjustment.ProductName,
                ProductImageUrl = adjustment.Product?.ImageUrl,
                
                // Ağırlık bilgileri
                WeightUnit = adjustment.WeightUnit,
                WeightUnitDisplay = GetWeightUnitDisplay(adjustment.WeightUnit),
                EstimatedWeight = adjustment.EstimatedWeight,
                ActualWeight = adjustment.ActualWeight,
                WeightDifference = adjustment.WeightDifference,
                DifferencePercent = adjustment.DifferencePercent,
                WeightDifferenceDisplay = $"{adjustment.WeightDifference:F2} {GetWeightUnitDisplay(adjustment.WeightUnit)}",
                
                // Fiyat bilgileri
                PricePerUnit = adjustment.PricePerUnit,
                EstimatedPrice = adjustment.EstimatedPrice,
                ActualPrice = adjustment.ActualPrice,
                PriceDifference = adjustment.PriceDifference,
                PriceDifferenceDisplay = $"{adjustment.PriceDifference:F2} TL",
                
                // Durum bilgileri
                Status = adjustment.Status,
                StatusDisplay = GetStatusDisplay(adjustment.Status),
                StatusColor = GetStatusColor(adjustment.Status),
                
                // Tartı bilgileri
                WeighedAt = adjustment.WeighedAt,
                WeighedByCourierId = adjustment.WeighedByCourierId,
                WeighedByCourierName = adjustment.WeighedByCourierName,
                
                // Ödeme bilgileri
                IsSettled = adjustment.IsSettled,
                SettledAt = adjustment.SettledAt,
                PaymentTransactionId = adjustment.PaymentTransactionId,
                
                // Admin bilgileri
                RequiresAdminApproval = adjustment.RequiresAdminApproval,
                AdminReviewed = adjustment.AdminReviewed,
                AdminApproved = adjustment.AdminApproved,
                AdminAdjustedPrice = adjustment.AdminAdjustedPrice,
                AdminNote = adjustment.AdminNote,
                AdminReviewedAt = adjustment.AdminReviewedAt,
                AdminUserName = adjustment.AdminUserName,
                
                // Müşteri bilgilendirme
                CustomerNotified = adjustment.CustomerNotified,
                CustomerNotifiedAt = adjustment.CustomerNotifiedAt,
                
                // Zaman damgaları
                CreatedAt = adjustment.CreatedAt,
                UpdatedAt = adjustment.UpdatedAt
            };

            return dto;
        }

        private string GetWeightUnitDisplay(WeightUnit unit) => unit switch
        {
            WeightUnit.Gram => "g",
            WeightUnit.Kilogram => "kg",
            WeightUnit.Liter => "L",
            WeightUnit.Milliliter => "mL",
            WeightUnit.Piece => "adet",
            _ => ""
        };

        private string GetStatusDisplay(WeightAdjustmentStatus status) => status switch
        {
            WeightAdjustmentStatus.NotApplicable => "Uygulanamaz",
            WeightAdjustmentStatus.PendingWeighing => "Tartı Bekliyor",
            WeightAdjustmentStatus.Weighed => "Tartıldı",
            WeightAdjustmentStatus.NoDifference => "Fark Yok",
            WeightAdjustmentStatus.PendingAdditionalPayment => "Ek Ödeme Bekliyor",
            WeightAdjustmentStatus.PendingRefund => "İade Bekliyor",
            WeightAdjustmentStatus.Completed => "Tamamlandı",
            WeightAdjustmentStatus.PendingAdminApproval => "Admin Onayı Bekliyor",
            WeightAdjustmentStatus.RejectedByAdmin => "Admin Reddetti",
            WeightAdjustmentStatus.Failed => "Hata Oluştu",
            _ => "Bilinmiyor"
        };

        private string GetStatusColor(WeightAdjustmentStatus status) => status switch
        {
            WeightAdjustmentStatus.NotApplicable => "gray",
            WeightAdjustmentStatus.PendingWeighing => "blue",
            WeightAdjustmentStatus.Weighed => "cyan",
            WeightAdjustmentStatus.NoDifference => "green",
            WeightAdjustmentStatus.PendingAdditionalPayment => "yellow",
            WeightAdjustmentStatus.PendingRefund => "purple",
            WeightAdjustmentStatus.Completed => "green",
            WeightAdjustmentStatus.PendingAdminApproval => "orange",
            WeightAdjustmentStatus.RejectedByAdmin => "red",
            WeightAdjustmentStatus.Failed => "red",
            _ => "gray"
        };

        #endregion
    }
}
