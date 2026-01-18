// =============================================================================
// CouponManager - Kupon Yönetim Servisi
// =============================================================================
// Bu servis, kupon CRUD operasyonları ve gelişmiş doğrulama/uygulama işlemlerini
// yönetir. SOLID prensiplerine uygun, test edilebilir ve güvenli bir yapıdadır.
// 
// Desteklenen Kupon Türleri:
// - Percentage: Yüzde bazlı indirim
// - FixedAmount: Sabit tutar indirimi
// - FirstOrder: İlk sipariş indirimi
// - BuyXGetY: X al Y öde
// - FreeShipping: Ücretsiz kargo
//
// Kontrol Edilen Durumlar:
// - Kupon aktiflik ve tarih geçerliliği
// - Toplam ve kullanıcı bazlı kullanım limitleri
// - Minimum sipariş tutarı
// - Kategori ve ürün bazlı kısıtlamalar
// - İlk sipariş kontrolü
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Coupon;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Kupon yönetim servisi implementasyonu.
    /// Tüm kupon işlemlerini merkezi olarak yönetir.
    /// </summary>
    public class CouponManager : ICouponService
    {
        // =============================================================================
        // Bağımlılıklar
        // =============================================================================

        private readonly ICouponRepository _couponRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<CouponManager> _logger;

        // =============================================================================
        // Hata Kodları - Frontend i18n ve tutarlı hata yönetimi için
        // =============================================================================

        private static class ErrorCodes
        {
            public const string CouponNotFound = "COUPON_NOT_FOUND";
            public const string CouponInactive = "COUPON_INACTIVE";
            public const string CouponExpired = "COUPON_EXPIRED";
            public const string CouponNotStarted = "COUPON_NOT_STARTED";
            public const string UsageLimitExceeded = "USAGE_LIMIT_EXCEEDED";
            public const string UserUsageLimitExceeded = "USER_USAGE_LIMIT_EXCEEDED";
            public const string MinOrderAmountNotMet = "MIN_ORDER_AMOUNT_NOT_MET";
            public const string CategoryMismatch = "CATEGORY_MISMATCH";
            public const string ProductMismatch = "PRODUCT_MISMATCH";
            public const string NotFirstOrder = "NOT_FIRST_ORDER";
            public const string GuestNotAllowed = "GUEST_NOT_ALLOWED";
            public const string InternalError = "INTERNAL_ERROR";
        }

        // =============================================================================
        // Constructor - Dependency Injection
        // =============================================================================

        public CouponManager(
            ICouponRepository couponRepository,
            IOrderRepository orderRepository,
            ILogger<CouponManager> logger)
        {
            _couponRepository = couponRepository ?? throw new ArgumentNullException(nameof(couponRepository));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // =============================================================================
        // CRUD Operasyonları
        // =============================================================================

        /// <inheritdoc />
        public async Task<IEnumerable<Coupon>> GetAllAsync()
        {
            try
            {
                return await _couponRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kuponlar getirilirken hata oluştu");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Coupon?> GetByIdAsync(int id)
        {
            try
            {
                return await _couponRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kupon getirilirken hata oluştu. CouponId: {CouponId}", id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task AddAsync(Coupon coupon)
        {
            try
            {
                // Kupon kodu unique kontrolü
                var existing = await _couponRepository.GetByCodeAsync(coupon.Code);
                if (existing != null)
                {
                    throw new InvalidOperationException($"'{coupon.Code}' kupon kodu zaten mevcut.");
                }

                // Kod normalizasyonu - büyük harfe çevir
                coupon.Code = coupon.Code.Trim().ToUpperInvariant();
                
                await _couponRepository.AddAsync(coupon);
                _logger.LogInformation("Yeni kupon oluşturuldu. Code: {Code}, Type: {Type}", coupon.Code, coupon.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kupon eklenirken hata oluştu. Code: {Code}", coupon.Code);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task UpdateAsync(Coupon coupon)
        {
            try
            {
                // Kod değiştirildiyse unique kontrolü
                var existing = await _couponRepository.GetByIdAsync(coupon.Id);
                if (existing == null)
                {
                    throw new InvalidOperationException($"Kupon bulunamadı. Id: {coupon.Id}");
                }

                if (!existing.Code.Equals(coupon.Code, StringComparison.OrdinalIgnoreCase))
                {
                    var codeExists = await _couponRepository.GetByCodeAsync(coupon.Code);
                    if (codeExists != null)
                    {
                        throw new InvalidOperationException($"'{coupon.Code}' kupon kodu zaten mevcut.");
                    }
                }

                // Kod normalizasyonu
                coupon.Code = coupon.Code.Trim().ToUpperInvariant();
                coupon.UpdatedAt = DateTime.UtcNow;

                await _couponRepository.UpdateAsync(coupon);
                _logger.LogInformation("Kupon güncellendi. CouponId: {CouponId}", coupon.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kupon güncellenirken hata oluştu. CouponId: {CouponId}", coupon.Id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteAsync(int id)
        {
            try
            {
                var existing = await _couponRepository.GetByIdAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("Silinecek kupon bulunamadı. CouponId: {CouponId}", id);
                    return;
                }

                // Soft delete - IsActive = false
                existing.IsActive = false;
                existing.UpdatedAt = DateTime.UtcNow;
                await _couponRepository.UpdateAsync(existing);

                _logger.LogInformation("Kupon silindi (soft delete). CouponId: {CouponId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kupon silinirken hata oluştu. CouponId: {CouponId}", id);
                throw;
            }
        }

        // =============================================================================
        // Kupon Kodu İşlemleri
        // =============================================================================

        /// <inheritdoc />
        public async Task<Coupon?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            try
            {
                // Kod normalizasyonu - trim ve büyük harf
                var normalizedCode = code.Trim().ToUpperInvariant();
                return await _couponRepository.GetByCodeAsync(normalizedCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kupon koda göre getirilirken hata oluştu. Code: {Code}", code);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ValidateCouponAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            try
            {
                var normalizedCode = code.Trim().ToUpperInvariant();
                var coupon = await _couponRepository.GetByCodeAsync(normalizedCode);

                if (coupon == null)
                    return false;

                var now = DateTime.UtcNow;

                // Temel kontroller
                return coupon.IsActive 
                    && coupon.ExpirationDate > now
                    && (coupon.StartDate == null || coupon.StartDate <= now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kupon basit doğrulama hatası. Code: {Code}", code);
                return false;
            }
        }

        // =============================================================================
        // Gelişmiş Kupon Doğrulama ve Hesaplama
        // =============================================================================

        /// <inheritdoc />
        public async Task<CouponValidationResult> ValidateAndCalculateAsync(
            string code,
            int? userId,
            CouponValidateRequestDto request)
        {
            // Girdi validasyonu
            if (string.IsNullOrWhiteSpace(code))
            {
                return CouponValidationResult.Failure(ErrorCodes.CouponNotFound, "Kupon kodu boş olamaz.");
            }

            if (request == null)
            {
                return CouponValidationResult.Failure(ErrorCodes.InternalError, "Geçersiz istek.");
            }

            try
            {
                var normalizedCode = code.Trim().ToUpperInvariant();
                
                // Kuponu ilişkili verilerle birlikte getir
                var coupon = await _couponRepository.GetByCodeWithDetailsAsync(normalizedCode);

                // 1. Kupon var mı?
                if (coupon == null)
                {
                    _logger.LogWarning("Kupon bulunamadı. Code: {Code}", normalizedCode);
                    return CouponValidationResult.Failure(ErrorCodes.CouponNotFound, "Kupon bulunamadı.");
                }

                var now = DateTime.UtcNow;

                // 2. Kupon aktif mi?
                if (!coupon.IsActive)
                {
                    return CouponValidationResult.Failure(ErrorCodes.CouponInactive, "Bu kupon artık geçerli değil.");
                }

                // 3. Başlangıç tarihi kontrolü
                if (coupon.StartDate.HasValue && coupon.StartDate.Value > now)
                {
                    return CouponValidationResult.Failure(
                        ErrorCodes.CouponNotStarted,
                        $"Bu kupon {coupon.StartDate.Value:dd.MM.yyyy} tarihinden itibaren geçerli olacak.");
                }

                // 4. Bitiş tarihi kontrolü
                if (coupon.ExpirationDate <= now)
                {
                    return CouponValidationResult.Failure(ErrorCodes.CouponExpired, "Bu kuponun süresi dolmuş.");
                }

                // 5. Toplam kullanım limiti kontrolü
                if (coupon.UsageLimit > 0 && coupon.UsageCount >= coupon.UsageLimit)
                {
                    return CouponValidationResult.Failure(
                        ErrorCodes.UsageLimitExceeded,
                        "Bu kuponun kullanım limiti dolmuş.");
                }

                // 6. Tek kullanımlık kupon kontrolü
                if (coupon.IsSingleUse && coupon.UsageCount > 0)
                {
                    return CouponValidationResult.Failure(
                        ErrorCodes.UsageLimitExceeded,
                        "Bu kupon daha önce kullanılmış.");
                }

                // 7. Kullanıcı bazlı limit kontrolü
                if (coupon.MaxUsagePerUser.HasValue && coupon.MaxUsagePerUser.Value > 0)
                {
                    if (!userId.HasValue)
                    {
                        // Misafir kullanıcılar için kullanıcı bazlı limit uygulanamaz
                        // Ama izin verebiliriz - sadece uyarı log'layalım
                        _logger.LogDebug("Misafir kullanıcı için kullanıcı bazlı limit kontrolü atlandı. Code: {Code}", normalizedCode);
                    }
                    else
                    {
                        var userUsageCount = await _couponRepository.GetUserUsageCountAsync(coupon.Id, userId);
                        if (userUsageCount >= coupon.MaxUsagePerUser.Value)
                        {
                            return CouponValidationResult.Failure(
                                ErrorCodes.UserUsageLimitExceeded,
                                $"Bu kuponu en fazla {coupon.MaxUsagePerUser.Value} kez kullanabilirsiniz.");
                        }
                    }
                }

                // 8. Minimum sipariş tutarı kontrolü
                if (coupon.MinOrderAmount.HasValue && request.CartTotal < coupon.MinOrderAmount.Value)
                {
                    return CouponValidationResult.Failure(
                        ErrorCodes.MinOrderAmountNotMet,
                        $"Bu kupon için minimum sipariş tutarı {coupon.MinOrderAmount.Value:N2} TL'dir.");
                }

                // 9. Kategori bazlı kontrol
                if (coupon.CategoryId.HasValue && request.CategoryIds != null && request.CategoryIds.Any())
                {
                    bool categoryMatch = request.CategoryIds.Contains(coupon.CategoryId.Value);
                    
                    // Alt kategorileri de kontrol et (IncludeSubCategories = true ise)
                    // NOT: Alt kategori hiyerarşisi için ayrı bir servis gerekebilir
                    // Şimdilik sadece direkt eşleşme kontrolü yapıyoruz
                    
                    if (!categoryMatch)
                    {
                        return CouponValidationResult.Failure(
                            ErrorCodes.CategoryMismatch,
                            "Bu kupon sepetinizdeki ürün kategorileri için geçerli değil.");
                    }
                }

                // 10. Ürün bazlı kontrol
                if (coupon.CouponProducts != null && coupon.CouponProducts.Any())
                {
                    var couponProductIds = coupon.CouponProducts.Select(cp => cp.ProductId).ToList();
                    
                    // Sepetteki ürünlerden en az biri kupon ürünlerinde olmalı
                    bool productMatch = request.ProductIds?.Any(pid => couponProductIds.Contains(pid)) ?? false;
                    
                    if (!productMatch)
                    {
                        return CouponValidationResult.Failure(
                            ErrorCodes.ProductMismatch,
                            "Bu kupon sepetinizdeki ürünler için geçerli değil.");
                    }
                }

                // 11. İlk sipariş kontrolü (FirstOrder türü için)
                if (coupon.Type == CouponType.FirstOrder)
                {
                    if (!userId.HasValue)
                    {
                        return CouponValidationResult.Failure(
                            ErrorCodes.GuestNotAllowed,
                            "Bu kupon sadece üye müşteriler için geçerlidir.");
                    }

                    var isFirstOrder = await IsFirstOrderAsync(userId);
                    if (!isFirstOrder)
                    {
                        return CouponValidationResult.Failure(
                            ErrorCodes.NotFirstOrder,
                            "Bu kupon sadece ilk siparişinizde geçerlidir.");
                    }
                }

                // =============================================================================
                // İndirim Hesaplama
                // =============================================================================

                decimal calculatedDiscount = 0;
                bool freeShippingApplied = false;
                decimal originalTotal = request.CartTotal + request.ShippingCost;
                decimal finalTotal = originalTotal;

                switch (coupon.Type)
                {
                    case CouponType.Percentage:
                    case CouponType.FirstOrder: // FirstOrder da yüzde bazlı çalışır
                        // Yüzde indirimi hesapla
                        calculatedDiscount = request.CartTotal * (coupon.Value / 100m);
                        
                        // Maksimum indirim tutarı kontrolü
                        if (coupon.MaxDiscountAmount.HasValue && calculatedDiscount > coupon.MaxDiscountAmount.Value)
                        {
                            calculatedDiscount = coupon.MaxDiscountAmount.Value;
                        }
                        break;

                    case CouponType.FixedAmount:
                        // Sabit tutar indirimi
                        calculatedDiscount = coupon.Value;
                        
                        // İndirim sepet tutarını aşamaz
                        if (calculatedDiscount > request.CartTotal)
                        {
                            calculatedDiscount = request.CartTotal;
                        }
                        break;

                    case CouponType.FreeShipping:
                        // Ücretsiz kargo
                        calculatedDiscount = request.ShippingCost;
                        freeShippingApplied = true;
                        break;

                    case CouponType.BuyXGetY:
                        // X al Y öde hesaplaması
                        // Bu daha kompleks bir hesaplama gerektirir
                        // request.ProductQuantities kullanılarak hesaplanabilir
                        // Şimdilik basit bir yüzde indirimi uygulayalım
                        if (coupon.BuyXPayY.HasValue && coupon.Value > 0)
                        {
                            // Örnek: 3 al 2 öde = Value: 3, BuyXPayY: 2
                            // İndirim oranı = (3-2)/3 = %33.33
                            decimal discountRate = (coupon.Value - coupon.BuyXPayY.Value) / coupon.Value;
                            calculatedDiscount = request.CartTotal * discountRate;
                        }
                        break;

                    default:
                        // Bilinmeyen tür - yüzde olarak değerlendir
                        calculatedDiscount = request.CartTotal * (coupon.Value / 100m);
                        break;
                }

                // İndirim tutarını yuvarla (2 ondalık)
                calculatedDiscount = Math.Round(calculatedDiscount, 2);
                finalTotal = originalTotal - calculatedDiscount;

                // Final total negatif olamaz
                if (finalTotal < 0)
                {
                    finalTotal = 0;
                }

                // Kalan kullanım hakkını hesapla
                int? remainingUsage = null;
                if (coupon.UsageLimit > 0)
                {
                    remainingUsage = coupon.UsageLimit - coupon.UsageCount;
                }

                _logger.LogInformation(
                    "Kupon doğrulandı. Code: {Code}, UserId: {UserId}, Discount: {Discount}, OriginalTotal: {Original}, FinalTotal: {Final}",
                    normalizedCode, userId, calculatedDiscount, originalTotal, finalTotal);

                return CouponValidationResult.Success(
                    couponId: coupon.Id,
                    couponCode: coupon.Code,
                    couponTitle: coupon.Title,
                    couponType: coupon.Type,
                    discountValue: coupon.Value,
                    isPercentage: coupon.Type == CouponType.Percentage || coupon.Type == CouponType.FirstOrder,
                    calculatedDiscount: calculatedDiscount,
                    originalTotal: originalTotal,
                    finalTotal: finalTotal,
                    freeShippingApplied: freeShippingApplied,
                    remainingUsage: remainingUsage,
                    expirationDate: coupon.ExpirationDate
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kupon doğrulama hatası. Code: {Code}, UserId: {UserId}", code, userId);
                return CouponValidationResult.Failure(ErrorCodes.InternalError, "Kupon doğrulanırken bir hata oluştu.");
            }
        }

        /// <inheritdoc />
        public async Task<bool> IncrementUsageAsync(CouponUsageCreateDto usageDto)
        {
            if (usageDto == null)
                throw new ArgumentNullException(nameof(usageDto));

            try
            {
                // Kuponu getir
                var coupon = await _couponRepository.GetByIdAsync(usageDto.CouponId);
                if (coupon == null)
                {
                    _logger.LogWarning("Kullanım kaydı için kupon bulunamadı. CouponId: {CouponId}", usageDto.CouponId);
                    return false;
                }

                // Kullanım sayısını artır
                coupon.UsageCount++;
                coupon.UpdatedAt = DateTime.UtcNow;
                await _couponRepository.UpdateAsync(coupon);

                // Kullanım kaydı oluştur
                var usage = new CouponUsage
                {
                    CouponId = usageDto.CouponId,
                    UserId = usageDto.UserId,
                    OrderId = usageDto.OrderId,
                    DiscountApplied = usageDto.DiscountApplied,
                    OrderTotalBeforeDiscount = usageDto.OrderTotalBeforeDiscount,
                    OrderTotalAfterDiscount = usageDto.OrderTotalAfterDiscount,
                    CouponCode = coupon.Code,
                    CouponType = coupon.Type.ToString(),
                    UsedAt = DateTime.UtcNow,
                    IpAddress = usageDto.IpAddress,
                    UserAgent = usageDto.UserAgent,
                    SessionId = usageDto.SessionId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _couponRepository.AddCouponUsageAsync(usage);

                _logger.LogInformation(
                    "Kupon kullanımı kaydedildi. CouponId: {CouponId}, OrderId: {OrderId}, UserId: {UserId}, Discount: {Discount}",
                    usageDto.CouponId, usageDto.OrderId, usageDto.UserId, usageDto.DiscountApplied);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kupon kullanım kaydı hatası. CouponId: {CouponId}, OrderId: {OrderId}", 
                    usageDto.CouponId, usageDto.OrderId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> CanUserUseCouponAsync(int couponId, int? userId)
        {
            try
            {
                var coupon = await _couponRepository.GetByIdAsync(couponId);
                if (coupon == null || !coupon.IsActive)
                    return false;

                // Temel kontroller
                var now = DateTime.UtcNow;
                if (coupon.ExpirationDate <= now)
                    return false;

                if (coupon.StartDate.HasValue && coupon.StartDate.Value > now)
                    return false;

                // Toplam limit
                if (coupon.UsageLimit > 0 && coupon.UsageCount >= coupon.UsageLimit)
                    return false;

                // Tek kullanımlık
                if (coupon.IsSingleUse && coupon.UsageCount > 0)
                    return false;

                // Kullanıcı bazlı limit
                if (coupon.MaxUsagePerUser.HasValue && userId.HasValue)
                {
                    var userUsageCount = await _couponRepository.GetUserUsageCountAsync(couponId, userId);
                    if (userUsageCount >= coupon.MaxUsagePerUser.Value)
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı kupon kontrolü hatası. CouponId: {CouponId}, UserId: {UserId}", couponId, userId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<int> GetUserUsageCountAsync(int couponId, int? userId)
        {
            if (!userId.HasValue)
                return 0;

            try
            {
                return await _couponRepository.GetUserUsageCountAsync(couponId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı kullanım sayısı getirme hatası. CouponId: {CouponId}, UserId: {UserId}", couponId, userId);
                return 0;
            }
        }

        // =============================================================================
        // Listeleme ve Filtreleme
        // =============================================================================

        /// <inheritdoc />
        public async Task<IEnumerable<CouponSummaryDto>> GetActiveCouponsAsync()
        {
            try
            {
                var coupons = await _couponRepository.GetActiveCouponsAsync();
                return coupons.Select(MapToSummaryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Aktif kuponlar getirilirken hata oluştu");
                return Enumerable.Empty<CouponSummaryDto>();
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<CouponSummaryDto>> GetCouponsByCategoryAsync(int categoryId)
        {
            try
            {
                var coupons = await _couponRepository.GetByCategoryIdAsync(categoryId);
                return coupons.Select(MapToSummaryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategori kuponları getirilirken hata oluştu. CategoryId: {CategoryId}", categoryId);
                return Enumerable.Empty<CouponSummaryDto>();
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<CouponSummaryDto>> GetCouponsForProductAsync(int productId)
        {
            try
            {
                var coupons = await _couponRepository.GetByProductIdAsync(productId);
                return coupons.Select(MapToSummaryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün kuponları getirilirken hata oluştu. ProductId: {ProductId}", productId);
                return Enumerable.Empty<CouponSummaryDto>();
            }
        }

        /// <inheritdoc />
        public async Task<CouponDetailDto?> GetCouponDetailAsync(int id)
        {
            try
            {
                var coupon = await _couponRepository.GetByIdAsync(id);
                if (coupon == null)
                    return null;

                return new CouponDetailDto
                {
                    Id = coupon.Id,
                    Code = coupon.Code,
                    Title = coupon.Title,
                    Description = coupon.Description,
                    Type = coupon.Type,
                    Value = coupon.Value,
                    IsActive = coupon.IsActive,
                    ExpirationDate = coupon.ExpirationDate,
                    StartDate = coupon.StartDate,
                    UsageCount = coupon.UsageCount,
                    UsageLimit = coupon.UsageLimit,
                    MaxUsagePerUser = coupon.MaxUsagePerUser,
                    IsSingleUse = coupon.IsSingleUse,
                    MinOrderAmount = coupon.MinOrderAmount,
                    MaxDiscountAmount = coupon.MaxDiscountAmount,
                    CategoryId = coupon.CategoryId,
                    CategoryName = coupon.Category?.Name,
                    IncludeSubCategories = coupon.IncludeSubCategories,
                    IsPrivate = coupon.IsPrivate,
                    BuyXPayY = coupon.BuyXPayY,
                    ProductIds = coupon.CouponProducts?.Select(cp => cp.ProductId).ToList(),
                    ProductNames = coupon.CouponProducts?.Select(cp => cp.Product?.Name ?? "").ToList(),
                    CreatedAt = coupon.CreatedAt,
                    UpdatedAt = coupon.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kupon detayı getirilirken hata oluştu. CouponId: {CouponId}", id);
                return null;
            }
        }

        // =============================================================================
        // İlk Sipariş Kontrolü
        // =============================================================================

        /// <inheritdoc />
        public async Task<bool> IsFirstOrderAsync(int? userId)
        {
            // Misafir kullanıcılar için ilk sipariş kontrolü yapılamaz
            if (!userId.HasValue)
                return false;

            try
            {
                var orders = await _orderRepository.GetByUserIdAsync(userId.Value);
                
                // Tamamlanmış veya işlemde olan siparişleri say
                var completedOrderCount = orders.Count(o => 
                    o.Status != OrderStatus.Cancelled && 
                    o.PaymentStatus != PaymentStatus.Failed);

                return completedOrderCount == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İlk sipariş kontrolü hatası. UserId: {UserId}", userId);
                return false;
            }
        }

        // =============================================================================
        // Yardımcı Metodlar
        // =============================================================================

        /// <summary>
        /// Coupon entity'sini CouponSummaryDto'ya dönüştürür
        /// </summary>
        private static CouponSummaryDto MapToSummaryDto(Coupon coupon)
        {
            return new CouponSummaryDto
            {
                Id = coupon.Id,
                Code = coupon.Code,
                Title = coupon.Title,
                Type = coupon.Type,
                Value = coupon.Value,
                IsActive = coupon.IsActive,
                ExpirationDate = coupon.ExpirationDate,
                UsageCount = coupon.UsageCount,
                UsageLimit = coupon.UsageLimit,
                MinOrderAmount = coupon.MinOrderAmount
            };
        }
    }
}
