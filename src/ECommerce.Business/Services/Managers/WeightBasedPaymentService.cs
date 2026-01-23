// ═══════════════════════════════════════════════════════════════════════════════════════════════
// AĞIRLIK BAZLI ÖDEME SERVİSİ IMPLEMENTASYONU
// Ağırlık bazlı ürünler için dinamik ödeme işlemlerini yöneten servis
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// NEDEN BU YAPIYI SEÇTİK?
// 1. POSNET API entegrasyonu üzerine abstraction layer
// 2. Ağırlık farkı hesaplama ve ödeme mantığı tek yerde
// 3. Kart ve nakit ödemeler için unified interface
// 4. Detaylı loglama ve hata yönetimi
// 5. Admin müdahalesi için workflow desteği
// ═══════════════════════════════════════════════════════════════════════════════════════════════

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using ECommerce.Infrastructure.Services.Payment.Posnet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Ağırlık Bazlı Dinamik Ödeme Servisi Implementasyonu
    /// 
    /// Bu servis, ağırlık bazlı satılan ürünler için Pre-Auth → Post-Auth → Refund
    /// akışını yönetir. POSNET API ile entegre çalışır.
    /// 
    /// AKIŞ DETAYI:
    /// 
    /// KART ÖDEMELERİ:
    /// 1. Sipariş → Tahmini tutar + %15 güvenlik marjı ile Pre-Auth
    /// 2. Kurye tartım → Gerçek tutar hesaplanır
    /// 3. Teslimat → Post-Auth (kesin çekim) gerçek tutar üzerinden
    /// 4. Fark varsa → Kısmi iade veya admin onaylı ek tahsilat
    /// 
    /// NAKİT ÖDEMELERİ:
    /// 1. Sipariş → Tahmini tutar kaydedilir
    /// 2. Kurye tartım → Gerçek tutar hesaplanır
    /// 3. Teslimat → Fark kurye tarafından tahsil/verilir
    /// 4. Yüksek fark → Admin onayı gerekir
    /// </summary>
    public class WeightBasedPaymentService : IWeightBasedPaymentService
    {
        // ═══════════════════════════════════════════════════════════════════════
        // DEPENDENCIES
        // ═══════════════════════════════════════════════════════════════════════

        private readonly IPosnetPaymentService? _posnetService;
        private readonly ECommerceDbContext _db;
        private readonly ILogger<WeightBasedPaymentService>? _logger;

        // ═══════════════════════════════════════════════════════════════════════
        // CONSTANTS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Varsayılan güvenlik marjı yüzdesi (dokümanla uyumlu)</summary>
        private const decimal DEFAULT_SECURITY_MARGIN_PERCENT = 20m;

        /// <summary>Provizyon geçerlilik süresi (saat)</summary>
        private const int PRE_AUTH_VALIDITY_HOURS = 48;

        /// <summary>Admin onayı gerektiren fark eşiği (%)</summary>
        private const decimal ADMIN_APPROVAL_THRESHOLD_PERCENT = 20m;

        /// <summary>Admin onayı gerektiren minimum fark tutarı (TL)</summary>
        private const decimal ADMIN_APPROVAL_THRESHOLD_AMOUNT = 50m;

        // ═══════════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// WeightBasedPaymentService constructor
        /// POSNET servisi opsiyonel - olmadığında sadece nakit ödemeler desteklenir
        /// </summary>
        public WeightBasedPaymentService(
            ECommerceDbContext db,
            IPosnetPaymentService? posnetService = null,
            ILogger<WeightBasedPaymentService>? logger = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _posnetService = posnetService;
            _logger = logger;

            if (_posnetService == null)
            {
                _logger?.LogWarning(
                    "[WEIGHT-PAYMENT] POSNET servisi inject edilmedi. " +
                    "Sadece nakit ödemeler desteklenecek.");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PRE-AUTHORIZATION METODLARI
        // Tahmini tutar üzerinden kart bloke etme
        // ═══════════════════════════════════════════════════════════════════════

        /// <inheritdoc />
        public async Task<PreAuthorizationResult> ProcessPreAuthorizationAsync(
            int orderId,
            decimal estimatedAmount,
            decimal securityMarginPercent = DEFAULT_SECURITY_MARGIN_PERCENT,
            string? hostLogKey = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger?.LogInformation(
                "[WEIGHT-PAYMENT] Ön provizyon başlatılıyor. OrderId: {OrderId}, " +
                "EstimatedAmount: {Amount}, SecurityMargin: {Margin}%",
                orderId, estimatedAmount, securityMarginPercent);

            try
            {
                // Sipariş kontrolü
                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

                if (order == null)
                {
                    _logger?.LogWarning("[WEIGHT-PAYMENT] Sipariş bulunamadı. OrderId: {OrderId}", orderId);
                    return PreAuthorizationResult.Failure(orderId, "Sipariş bulunamadı");
                }

                // Ağırlık bazlı ürün var mı?
                var hasWeightBasedItems = order.OrderItems?.Any(oi => oi.IsWeightBased) ?? false;
                if (!hasWeightBasedItems)
                {
                    _logger?.LogWarning(
                        "[WEIGHT-PAYMENT] Siparişte ağırlık bazlı ürün yok. OrderId: {OrderId}",
                        orderId);
                    return PreAuthorizationResult.Failure(orderId, "Siparişte ağırlık bazlı ürün bulunmuyor");
                }

                // Güvenlik marjı ile bloke tutarı hesapla
                // Örnek: 100 TL tahmini + %15 margin = 115 TL bloke
                var marginMultiplier = 1 + (securityMarginPercent / 100);
                var blockAmount = Math.Round(estimatedAmount * marginMultiplier, 2);

                _logger?.LogInformation(
                    "[WEIGHT-PAYMENT] Bloke tutarı hesaplandı. Tahmini: {Estimated}, Bloke: {Block}",
                    estimatedAmount, blockAmount);

                // POSNET servisi yoksa sadece kaydı oluştur (nakit ödemeler için)
                if (_posnetService == null)
                {
                    _logger?.LogWarning(
                        "[WEIGHT-PAYMENT] POSNET servisi yok, provizyon simüle ediliyor. OrderId: {OrderId}",
                        orderId);

                    // Siparişi güncelle
                    order.PreAuthAmount = blockAmount;
                    order.WeightAdjustmentStatus = WeightAdjustmentStatus.PendingWeighing;
                    await _db.SaveChangesAsync(cancellationToken);

                    stopwatch.Stop();
                    return new PreAuthorizationResult
                    {
                        IsSuccess = true,
                        OrderId = orderId,
                        BlockedAmount = blockAmount,
                        HostLogKey = $"SIMULATED_{orderId}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                        TransactionDate = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddHours(PRE_AUTH_VALIDITY_HOURS),
                        ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                    };
                }

                // Zaten hostLogKey varsa (3D Secure sonrası) direkt kaydet
                if (!string.IsNullOrEmpty(hostLogKey))
                {
                    order.PreAuthAmount = blockAmount;
                    order.PreAuthHostLogKey = hostLogKey;
                    order.PreAuthDate = DateTime.UtcNow;
                    order.WeightAdjustmentStatus = WeightAdjustmentStatus.PendingWeighing;
                    await _db.SaveChangesAsync(cancellationToken);

                    stopwatch.Stop();
                    return PreAuthorizationResult.Success(orderId, blockAmount, hostLogKey);
                }

                // Kart bilgileri olmadan Pre-Auth yapılamaz
                // Bu durumda sipariş kaydı güncellenir, kart bilgileri frontend'den gelecek
                _logger?.LogInformation(
                    "[WEIGHT-PAYMENT] Kart bilgileri bekleniyor. OrderId: {OrderId}",
                    orderId);

                order.PreAuthAmount = blockAmount;
                order.WeightAdjustmentStatus = WeightAdjustmentStatus.NotApplicable;
                await _db.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();
                return new PreAuthorizationResult
                {
                    IsSuccess = true,
                    OrderId = orderId,
                    BlockedAmount = blockAmount,
                    TransactionDate = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(PRE_AUTH_VALIDITY_HOURS),
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    ErrorMessage = "Kart bilgileri ile provizyon başlatılmalı"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger?.LogError(ex,
                    "[WEIGHT-PAYMENT] Ön provizyon hatası. OrderId: {OrderId}, ElapsedMs: {Elapsed}",
                    orderId, stopwatch.ElapsedMilliseconds);

                return PreAuthorizationResult.Failure(orderId, $"Sistem hatası: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<PreAuthorizationResult> InitiatePreAuthorizationWith3DSecureAsync(
            int orderId,
            decimal estimatedAmount,
            string cardNumber,
            string expireDate,
            string cvv,
            decimal securityMarginPercent = DEFAULT_SECURITY_MARGIN_PERCENT,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger?.LogInformation(
                "[WEIGHT-PAYMENT] 3D Secure ile ön provizyon başlatılıyor. OrderId: {OrderId}",
                orderId);

            try
            {
                if (_posnetService == null)
                {
                    return PreAuthorizationResult.Failure(orderId, "POSNET servisi yapılandırılmamış");
                }

                // Sipariş kontrolü
                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

                if (order == null)
                {
                    return PreAuthorizationResult.Failure(orderId, "Sipariş bulunamadı");
                }

                // Güvenlik marjı ile bloke tutarı hesapla
                var marginMultiplier = 1 + (securityMarginPercent / 100);
                var blockAmount = Math.Round(estimatedAmount * marginMultiplier, 2);

                // POSNET Pre-Auth çağır
                var authResult = await _posnetService.ProcessAuthAsync(
                    orderId,
                    cardNumber,
                    expireDate,
                    cvv,
                    0, // Taksit yok
                    cancellationToken);

                stopwatch.Stop();

                if (!authResult.IsSuccess || authResult.Data == null)
                {
                    _logger?.LogWarning(
                        "[WEIGHT-PAYMENT] POSNET Pre-Auth başarısız. OrderId: {OrderId}, Error: {Error}",
                        orderId, authResult.Error);

                    return PreAuthorizationResult.Failure(
                        orderId,
                        authResult.Error ?? "Pre-Auth başarısız",
                        authResult.ErrorCode.ToString());
                }

                // Siparişi güncelle
                order.PreAuthAmount = blockAmount;
                order.PreAuthHostLogKey = authResult.Data.HostLogKey;
                order.PreAuthDate = DateTime.UtcNow;
                order.WeightAdjustmentStatus = WeightAdjustmentStatus.PendingWeighing;
                await _db.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "[WEIGHT-PAYMENT] Pre-Auth başarılı. OrderId: {OrderId}, HostLogKey: {HostLogKey}, " +
                    "BlockAmount: {Amount}, ElapsedMs: {Elapsed}",
                    orderId, authResult.Data.HostLogKey, blockAmount, stopwatch.ElapsedMilliseconds);

                return new PreAuthorizationResult
                {
                    IsSuccess = true,
                    OrderId = orderId,
                    BlockedAmount = blockAmount,
                    HostLogKey = authResult.Data.HostLogKey,
                    AuthCode = authResult.Data.AuthCode,
                    TransactionDate = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(PRE_AUTH_VALIDITY_HOURS),
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger?.LogError(ex,
                    "[WEIGHT-PAYMENT] 3D Secure Pre-Auth hatası. OrderId: {OrderId}",
                    orderId);

                return PreAuthorizationResult.Failure(orderId, $"Sistem hatası: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // POST-AUTHORIZATION METODLARI
        // Gerçek tutar üzerinden finansallaştırma
        // ═══════════════════════════════════════════════════════════════════════

        /// <inheritdoc />
        public async Task<PostAuthorizationResult> ProcessPostAuthorizationAsync(
            int orderId,
            decimal actualAmount,
            string hostLogKey,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger?.LogInformation(
                "[WEIGHT-PAYMENT] Kesin çekim başlatılıyor. OrderId: {OrderId}, " +
                "ActualAmount: {Amount}, HostLogKey: {HostLogKey}",
                orderId, actualAmount, hostLogKey);

            try
            {
                // Sipariş kontrolü
                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

                if (order == null)
                {
                    return PostAuthorizationResult.Failure(orderId, "Sipariş bulunamadı");
                }

                // Pre-Auth kontrolü
                if (string.IsNullOrEmpty(order.PreAuthHostLogKey) && string.IsNullOrEmpty(hostLogKey))
                {
                    return PostAuthorizationResult.Failure(orderId, "Ön provizyon kaydı bulunamadı");
                }

                var preAuthHostLogKey = hostLogKey ?? order.PreAuthHostLogKey!;
                var preAuthAmount = order.PreAuthAmount;

                // POSNET servisi yoksa simüle et
                if (_posnetService == null)
                {
                    _logger?.LogWarning(
                        "[WEIGHT-PAYMENT] POSNET servisi yok, kesin çekim simüle ediliyor. OrderId: {OrderId}",
                        orderId);

                    // Farkı hesapla ve kaydet
                    var difference = preAuthAmount - actualAmount;
                    
                    order.FinalAmount = actualAmount;
                    order.WeightDifference = difference;
                    order.WeightAdjustmentStatus = WeightAdjustmentStatus.Completed;
                    order.TotalPrice = actualAmount;
                    await _db.SaveChangesAsync(cancellationToken);

                    stopwatch.Stop();
                    return PostAuthorizationResult.Success(orderId, preAuthAmount, actualAmount);
                }

                // Gerçek tutar, bloke tutarından büyükse ek işlem gerekir
                if (actualAmount > preAuthAmount)
                {
                    _logger?.LogWarning(
                        "[WEIGHT-PAYMENT] Gerçek tutar bloke tutarı aşıyor! " +
                        "OrderId: {OrderId}, PreAuth: {PreAuth}, Actual: {Actual}",
                        orderId, preAuthAmount, actualAmount);

                    // Bu durumda admin onayı gerekir
                    order.WeightAdjustmentStatus = WeightAdjustmentStatus.PendingAdminApproval;
                    order.FinalAmount = actualAmount;
                    order.WeightDifference = preAuthAmount - actualAmount;
                    await _db.SaveChangesAsync(cancellationToken);

                    stopwatch.Stop();
                    return new PostAuthorizationResult
                    {
                        IsSuccess = false,
                        OrderId = orderId,
                        OriginalBlockedAmount = preAuthAmount,
                        CapturedAmount = 0,
                        DifferenceAmount = actualAmount - preAuthAmount,
                        ErrorMessage = "Gerçek tutar bloke tutarı aşıyor. Admin onayı gerekiyor.",
                        ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                    };
                }

                // POSNET Capture (finansallaştırma) çağır
                var captureResult = await _posnetService.ProcessCaptureAsync(
                    orderId,
                    preAuthHostLogKey,
                    actualAmount,
                    cancellationToken);

                stopwatch.Stop();

                if (!captureResult.IsSuccess)
                {
                    _logger?.LogWarning(
                        "[WEIGHT-PAYMENT] POSNET Capture başarısız. OrderId: {OrderId}, Error: {Error}",
                        orderId, captureResult.Error);

                    return PostAuthorizationResult.Failure(
                        orderId,
                        captureResult.Error ?? "Kesin çekim başarısız",
                        captureResult.ErrorCode.ToString());
                }

                // Siparişi güncelle
                var differenceAmount = preAuthAmount - actualAmount;
                order.FinalAmount = actualAmount;
                order.WeightDifference = differenceAmount;
                order.TotalPrice = actualAmount;
                order.WeightAdjustmentStatus = differenceAmount == 0 
                    ? WeightAdjustmentStatus.NoDifference 
                    : WeightAdjustmentStatus.Completed;

                await _db.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "[WEIGHT-PAYMENT] Kesin çekim başarılı. OrderId: {OrderId}, " +
                    "Captured: {Captured}, Difference: {Diff}, ElapsedMs: {Elapsed}",
                    orderId, actualAmount, differenceAmount, stopwatch.ElapsedMilliseconds);

                return new PostAuthorizationResult
                {
                    IsSuccess = true,
                    OrderId = orderId,
                    OriginalBlockedAmount = preAuthAmount,
                    CapturedAmount = actualAmount,
                    DifferenceAmount = differenceAmount,
                    HostLogKey = captureResult.Data?.HostLogKey,
                    TransactionDate = DateTime.UtcNow,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger?.LogError(ex,
                    "[WEIGHT-PAYMENT] Kesin çekim hatası. OrderId: {OrderId}",
                    orderId);

                return PostAuthorizationResult.Failure(orderId, $"Sistem hatası: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<PostAuthorizationResult> ProcessDifferencePaymentAsync(
            int orderId,
            decimal differenceAmount,
            string hostLogKey,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger?.LogInformation(
                "[WEIGHT-PAYMENT] Fark ödemesi işleniyor. OrderId: {OrderId}, " +
                "Difference: {Amount}, HostLogKey: {HostLogKey}",
                orderId, differenceAmount, hostLogKey);

            try
            {
                var order = await _db.Orders.FindAsync(new object[] { orderId }, cancellationToken);
                if (order == null)
                {
                    return PostAuthorizationResult.Failure(orderId, "Sipariş bulunamadı");
                }

                // Dokümanla uyumlu kontrol: %20 veya 50 TL üzeri fark admin onayı gerektirir.
                var baseAmount = order.TotalPrice > 0 ? order.TotalPrice : order.PreAuthAmount;
                var differencePercent = baseAmount > 0
                    ? Math.Abs(differenceAmount / baseAmount * 100)
                    : 0;

                if (Math.Abs(differenceAmount) > ADMIN_APPROVAL_THRESHOLD_AMOUNT ||
                    differencePercent > ADMIN_APPROVAL_THRESHOLD_PERCENT)
                {
                    order.WeightAdjustmentStatus = WeightAdjustmentStatus.PendingAdminApproval;
                    await _db.SaveChangesAsync(cancellationToken);

                    stopwatch.Stop();
                    return new PostAuthorizationResult
                    {
                        IsSuccess = false,
                        OrderId = orderId,
                        OriginalBlockedAmount = order.PreAuthAmount,
                        DifferenceAmount = differenceAmount,
                        ErrorMessage = "Fark tutarı/yüzdesi eşikleri aştı. Admin onayı gerekiyor.",
                        ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                    };
                }

                // Fark pozitif = Müşteriye iade, Negatif = Müşteriden tahsilat
                if (differenceAmount > 0)
                {
                    // Kısmi iade yap
                    var refundResult = await ProcessPartialRefundAsync(
                        orderId,
                        differenceAmount,
                        hostLogKey,
                        "Ağırlık farkı iadesi",
                        cancellationToken);

                    if (refundResult.IsSuccess)
                    {
                        order.WeightAdjustmentStatus = WeightAdjustmentStatus.Completed;
                        await _db.SaveChangesAsync(cancellationToken);
                    }

                    stopwatch.Stop();
                    return new PostAuthorizationResult
                    {
                        IsSuccess = refundResult.IsSuccess,
                        OrderId = orderId,
                        OriginalBlockedAmount = order.PreAuthAmount,
                        CapturedAmount = order.FinalAmount,
                        DifferenceAmount = differenceAmount,
                        RefundProcessed = refundResult.IsSuccess,
                        RefundedAmount = refundResult.RefundedAmount,
                        ErrorMessage = refundResult.ErrorMessage,
                        ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                    };
                }
                else if (differenceAmount < 0)
                {
                    // Ek tahsilat - bu durumda admin onayı gerekir
                    _logger?.LogWarning(
                        "[WEIGHT-PAYMENT] Ek tahsilat gerekiyor. OrderId: {OrderId}, Amount: {Amount}",
                        orderId, Math.Abs(differenceAmount));

                    order.WeightAdjustmentStatus = WeightAdjustmentStatus.PendingAdminApproval;
                    await _db.SaveChangesAsync(cancellationToken);

                    stopwatch.Stop();
                    return new PostAuthorizationResult
                    {
                        IsSuccess = false,
                        OrderId = orderId,
                        OriginalBlockedAmount = order.PreAuthAmount,
                        DifferenceAmount = differenceAmount,
                        ErrorMessage = "Ek tahsilat için admin onayı gerekiyor",
                        ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                    };
                }

                // Fark yok
                stopwatch.Stop();
                order.WeightAdjustmentStatus = WeightAdjustmentStatus.NoDifference;
                await _db.SaveChangesAsync(cancellationToken);

                return PostAuthorizationResult.Success(orderId, order.PreAuthAmount, order.FinalAmount);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger?.LogError(ex,
                    "[WEIGHT-PAYMENT] Fark ödemesi hatası. OrderId: {OrderId}",
                    orderId);

                return PostAuthorizationResult.Failure(orderId, $"Sistem hatası: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PARTIAL REFUND METODLARI
        // Fazla çekilen tutarın iadesi
        // ═══════════════════════════════════════════════════════════════════════

        /// <inheritdoc />
        public async Task<PartialRefundResult> ProcessPartialRefundAsync(
            int orderId,
            decimal refundAmount,
            string hostLogKey,
            string? reason = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger?.LogInformation(
                "[WEIGHT-PAYMENT] Kısmi iade başlatılıyor. OrderId: {OrderId}, " +
                "RefundAmount: {Amount}, Reason: {Reason}",
                orderId, refundAmount, reason ?? "Ağırlık farkı");

            try
            {
                var order = await _db.Orders.FindAsync(new object[] { orderId }, cancellationToken);
                if (order == null)
                {
                    return PartialRefundResult.Failure(orderId, "Sipariş bulunamadı");
                }

                // POSNET servisi yoksa simüle et
                if (_posnetService == null)
                {
                    _logger?.LogWarning(
                        "[WEIGHT-PAYMENT] POSNET servisi yok, iade simüle ediliyor. OrderId: {OrderId}",
                        orderId);

                    stopwatch.Stop();
                    return PartialRefundResult.Success(
                        orderId, 
                        refundAmount, 
                        order.TotalPrice,
                        $"SIMULATED_REFUND_{orderId}_{DateTime.UtcNow:yyyyMMddHHmmss}");
                }

                // POSNET Refund çağır
                var refundResult = await _posnetService.ProcessRefundAsync(
                    orderId,
                    hostLogKey,
                    refundAmount,
                    cancellationToken);

                stopwatch.Stop();

                if (!refundResult.IsSuccess)
                {
                    _logger?.LogWarning(
                        "[WEIGHT-PAYMENT] POSNET Refund başarısız. OrderId: {OrderId}, Error: {Error}",
                        orderId, refundResult.Error);

                    return PartialRefundResult.Failure(
                        orderId,
                        refundResult.Error ?? "İade başarısız",
                        refundResult.ErrorCode.ToString());
                }

                _logger?.LogInformation(
                    "[WEIGHT-PAYMENT] Kısmi iade başarılı. OrderId: {OrderId}, " +
                    "RefundedAmount: {Amount}, ElapsedMs: {Elapsed}",
                    orderId, refundAmount, stopwatch.ElapsedMilliseconds);

                return new PartialRefundResult
                {
                    IsSuccess = true,
                    OrderId = orderId,
                    RefundedAmount = refundAmount,
                    OriginalAmount = order.TotalPrice,
                    RefundHostLogKey = refundResult.Data?.HostLogKey,
                    TransactionDate = DateTime.UtcNow,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger?.LogError(ex,
                    "[WEIGHT-PAYMENT] Kısmi iade hatası. OrderId: {OrderId}",
                    orderId);

                return PartialRefundResult.Failure(orderId, $"Sistem hatası: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // KAPIDA ÖDEME (NAKİT) METODLARI
        // ═══════════════════════════════════════════════════════════════════════

        /// <inheritdoc />
        public Task<CashPaymentDifferenceResult> CalculateCashPaymentDifferenceAsync(
            int orderId,
            decimal estimatedAmount,
            decimal actualAmount,
            decimal adminApprovalThresholdPercent = ADMIN_APPROVAL_THRESHOLD_PERCENT,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation(
                "[WEIGHT-PAYMENT] Nakit ödeme farkı hesaplanıyor. OrderId: {OrderId}, " +
                "Estimated: {Est}, Actual: {Act}",
                orderId, estimatedAmount, actualAmount);

            // Fark hesapla
            var differenceAmount = actualAmount - estimatedAmount;
            var differencePercent = estimatedAmount > 0 
                ? Math.Abs(differenceAmount / estimatedAmount * 100) 
                : 0;

            // Yön belirle
            PaymentDifferenceDirection direction;
            string description;

            if (differenceAmount == 0)
            {
                direction = PaymentDifferenceDirection.NoDifference;
                description = "Ağırlık farkı yok. Tutar değişmedi.";
            }
            else if (differenceAmount > 0)
            {
                direction = PaymentDifferenceDirection.ChargeFromCustomer;
                description = $"Müşteriden {differenceAmount:C2} ek tahsilat yapılacak.";
            }
            else
            {
                direction = PaymentDifferenceDirection.RefundToCustomer;
                description = $"Müşteriye {Math.Abs(differenceAmount):C2} para üstü verilecek.";
            }

            // Admin onayı gerekiyor mu?
            var requiresApproval = differencePercent > adminApprovalThresholdPercent 
                                   || Math.Abs(differenceAmount) > ADMIN_APPROVAL_THRESHOLD_AMOUNT;

            if (requiresApproval)
            {
                description += " (Yüksek fark - Admin onayı gerekiyor)";
                _logger?.LogWarning(
                    "[WEIGHT-PAYMENT] Yüksek fark tespit edildi. OrderId: {OrderId}, " +
                    "DiffPercent: {Percent}%, DiffAmount: {Amount}",
                    orderId, differencePercent, differenceAmount);
            }

            var result = new CashPaymentDifferenceResult
            {
                IsSuccess = true,
                OrderId = orderId,
                EstimatedAmount = estimatedAmount,
                ActualAmount = actualAmount,
                DifferenceAmount = differenceAmount,
                Direction = direction,
                DifferencePercent = differencePercent,
                RequiresAdminApproval = requiresApproval,
                DifferenceDescription = description
            };

            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public async Task<bool> CompleteCashPaymentDifferenceAsync(
            int orderId,
            decimal differenceAmount,
            PaymentDifferenceDirection direction,
            string? courierNotes = null,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation(
                "[WEIGHT-PAYMENT] Nakit fark ödemesi tamamlanıyor. OrderId: {OrderId}, " +
                "Amount: {Amount}, Direction: {Dir}",
                orderId, differenceAmount, direction);

            try
            {
                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

                if (order == null)
                {
                    _logger?.LogWarning("[WEIGHT-PAYMENT] Sipariş bulunamadı. OrderId: {OrderId}", orderId);
                    return false;
                }

                // Siparişi güncelle
                order.WeightDifference = differenceAmount;
                order.FinalAmount = order.TotalPrice + differenceAmount;
                order.TotalPrice = order.FinalAmount;
                order.WeightAdjustmentStatus = direction == PaymentDifferenceDirection.NoDifference
                    ? WeightAdjustmentStatus.NoDifference
                    : WeightAdjustmentStatus.Completed;

                // Not ekle (varsa)
                if (!string.IsNullOrEmpty(courierNotes))
                {
                    order.DeliveryNotes = string.IsNullOrEmpty(order.DeliveryNotes)
                        ? $"[Kurye Notu] {courierNotes}"
                        : $"{order.DeliveryNotes}\n[Kurye Notu] {courierNotes}";
                }

                await _db.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "[WEIGHT-PAYMENT] Nakit fark ödemesi tamamlandı. OrderId: {OrderId}, " +
                    "FinalAmount: {Final}",
                    orderId, order.FinalAmount);

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "[WEIGHT-PAYMENT] Nakit fark ödemesi hatası. OrderId: {OrderId}",
                    orderId);

                return false;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // YARDIMCI METODLAR
        // ═══════════════════════════════════════════════════════════════════════

        /// <inheritdoc />
        public async Task<WeightBasedPaymentStatus> GetPaymentStatusAsync(
            int orderId,
            CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order == null)
            {
                return new WeightBasedPaymentStatus
                {
                    OrderId = orderId,
                    StatusDescription = "Sipariş bulunamadı"
                };
            }

            var isCardPayment = order.PaymentMethod?.Contains("Card", StringComparison.OrdinalIgnoreCase) ?? false;
            var preAuthExpired = order.PreAuthDate.HasValue && 
                                 DateTime.UtcNow > order.PreAuthDate.Value.AddHours(PRE_AUTH_VALIDITY_HOURS);

            // Durum açıklaması oluştur
            string statusDesc = order.WeightAdjustmentStatus switch
            {
                WeightAdjustmentStatus.NotApplicable => "Ağırlık bazlı ödeme uygulanabilir değil",
                WeightAdjustmentStatus.PendingWeighing => "Tartım bekleniyor",
                WeightAdjustmentStatus.Weighed => "Tartıldı, ödeme bekleniyor",
                WeightAdjustmentStatus.NoDifference => "Fark yok, tamamlandı",
                WeightAdjustmentStatus.PendingAdditionalPayment => "Ek ödeme bekleniyor",
                WeightAdjustmentStatus.PendingRefund => "İade bekleniyor",
                WeightAdjustmentStatus.Completed => "Tamamlandı",
                WeightAdjustmentStatus.PendingAdminApproval => "Admin onayı bekleniyor",
                WeightAdjustmentStatus.RejectedByAdmin => "Admin tarafından reddedildi",
                WeightAdjustmentStatus.Failed => "Başarısız",
                _ => "Bilinmeyen durum"
            };

            return new WeightBasedPaymentStatus
            {
                OrderId = orderId,
                PaymentMethod = order.PaymentMethod ?? "Bilinmiyor",
                IsCardPayment = isCardPayment,
                PreAuthorizationCompleted = !string.IsNullOrEmpty(order.PreAuthHostLogKey),
                PreAuthorizationAmount = order.PreAuthAmount,
                PreAuthorizationDate = order.PreAuthDate,
                PreAuthorizationHostLogKey = order.PreAuthHostLogKey,
                PreAuthorizationExpired = preAuthExpired,
                PostAuthorizationCompleted = order.FinalAmount > 0,
                PostAuthorizationAmount = order.FinalAmount,
                DifferenceProcessed = order.WeightAdjustmentStatus == WeightAdjustmentStatus.Completed,
                DifferenceAmount = order.WeightDifference,
                DifferenceDirection = order.WeightDifference > 0 
                    ? PaymentDifferenceDirection.RefundToCustomer
                    : order.WeightDifference < 0 
                        ? PaymentDifferenceDirection.ChargeFromCustomer 
                        : PaymentDifferenceDirection.NoDifference,
                PendingAdminApproval = order.WeightAdjustmentStatus == WeightAdjustmentStatus.PendingAdminApproval,
                IsCompleted = order.WeightAdjustmentStatus == WeightAdjustmentStatus.Completed 
                              || order.WeightAdjustmentStatus == WeightAdjustmentStatus.NoDifference,
                StatusDescription = statusDesc
            };
        }

        /// <inheritdoc />
        public async Task<bool> IsPreAuthorizationValidAsync(
            int orderId,
            CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order == null || !order.PreAuthDate.HasValue)
            {
                return false;
            }

            var expiryTime = order.PreAuthDate.Value.AddHours(PRE_AUTH_VALIDITY_HOURS);
            return DateTime.UtcNow <= expiryTime;
        }

        /// <inheritdoc />
        public async Task<int> CancelExpiredPreAuthorizationsAsync(
            CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("[WEIGHT-PAYMENT] Süresi dolan provizyonlar iptal ediliyor...");

            var expiryThreshold = DateTime.UtcNow.AddHours(-PRE_AUTH_VALIDITY_HOURS);

            // Süresi dolan ve henüz tamamlanmamış provizyonları bul
            var expiredOrders = await _db.Orders
                .Where(o => o.PreAuthDate.HasValue 
                            && o.PreAuthDate < expiryThreshold
                            && !string.IsNullOrEmpty(o.PreAuthHostLogKey)
                            && o.WeightAdjustmentStatus == WeightAdjustmentStatus.PendingWeighing)
                .ToListAsync(cancellationToken);

            var cancelledCount = 0;

            foreach (var order in expiredOrders)
            {
                try
                {
                    // POSNET Reverse (iptal) çağır
                    if (_posnetService != null && !string.IsNullOrEmpty(order.PreAuthHostLogKey))
                    {
                        var reverseResult = await _posnetService.ProcessReverseAsync(
                            order.Id,
                            order.PreAuthHostLogKey,
                            cancellationToken);

                        if (!reverseResult.IsSuccess)
                        {
                            _logger?.LogWarning(
                                "[WEIGHT-PAYMENT] Provizyon iptali başarısız. OrderId: {OrderId}, Error: {Error}",
                                order.Id, reverseResult.Error);
                            continue;
                        }
                    }

                    // Siparişi güncelle
                    order.WeightAdjustmentStatus = WeightAdjustmentStatus.Failed;
                    order.DeliveryNotes = string.IsNullOrEmpty(order.DeliveryNotes)
                        ? "[SİSTEM] Provizyon süresi doldu, otomatik iptal edildi."
                        : $"{order.DeliveryNotes}\n[SİSTEM] Provizyon süresi doldu, otomatik iptal edildi.";

                    cancelledCount++;

                    _logger?.LogInformation(
                        "[WEIGHT-PAYMENT] Provizyon iptal edildi. OrderId: {OrderId}",
                        order.Id);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex,
                        "[WEIGHT-PAYMENT] Provizyon iptali hatası. OrderId: {OrderId}",
                        order.Id);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            _logger?.LogInformation(
                "[WEIGHT-PAYMENT] Provizyon iptali tamamlandı. İptal edilen: {Count}",
                cancelledCount);

            return cancelledCount;
        }
    }
}
