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
using ECommerce.Business.Helpers;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Payment;
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
    /// 1. Sipariş → sepet tutarı ile Pre-Auth (3DS Auth). %20 şişirme yok.
    /// 2. Market tartım → gerçek tutar hesaplanır
    /// 3. Teslimat → Capt gerçek tutar üzerinden (≤ Auth × 1.20)
    /// 4. Fark Auth tavanını aşarsa → kalan admin/manuel tahsilat
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
        private readonly IExtendedPaymentService? _extendedPaymentService;
        private readonly IPaymentCaptureService? _paymentCaptureService;
        private readonly ECommerceDbContext _db;
        private readonly ILogger<WeightBasedPaymentService>? _logger;

        // ═══════════════════════════════════════════════════════════════════════
        // CONSTANTS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Varsayılan güvenlik marjı parametresi geriye dönük imza için durur.
        /// NEDEN 0: Auth tutarı sepet toplamıdır. %20 yalnız Capt tavanıdır
        /// (<see cref="WeightBasedCapturePolicy.CaptureOveragePercent"/>).
        /// </summary>
        private const decimal DEFAULT_SECURITY_MARGIN_PERCENT = 0m;

        /// <summary>
        /// Provizyon geçerlilik süresi (saat) — tek doğruluk kaynağı: WeightBasedCapturePolicy.
        /// Banka süresi teyit edilince yalnız politika güncellenir, tüm akışlar uyumlanır.
        /// </summary>
        private const int PRE_AUTH_VALIDITY_HOURS = WeightBasedCapturePolicy.PreAuthValidityHours;

        // ═══════════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// WeightBasedPaymentService constructor
        /// POSNET servisi opsiyonel - olmadığında sadece nakit ödemeler desteklenir.
        /// IExtendedPaymentService ile birleşik iade yürütücüsüne bağlanır (Faz 4).
        /// </summary>
        public WeightBasedPaymentService(
            ECommerceDbContext db,
            IPosnetPaymentService? posnetService = null,
            IExtendedPaymentService? extendedPaymentService = null,
            IPaymentCaptureService? paymentCaptureService = null,
            ILogger<WeightBasedPaymentService>? logger = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _posnetService = posnetService;
            _extendedPaymentService = extendedPaymentService;
            _paymentCaptureService = paymentCaptureService;
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

                // Auth tutarı = tahmini/sepet tutarı. securityMarginPercent yok sayılır.
                // NEDEN: %20 banka Capt tavanıdır; 3DS tutarını şişirmek müşteriye yanlış tutar gösterir.
                var blockAmount = WeightBasedCapturePolicy.ResolveCheckoutAuthAmount(order, estimatedAmount);

                _logger?.LogInformation(
                    "[WEIGHT-PAYMENT] Provizyon tutarı (marjsız). Tahmini: {Estimated}, Auth: {Block}",
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

                // Auth tutarı marjsız — %20 Capt tavanı teslimatta uygulanır
                var blockAmount = WeightBasedCapturePolicy.ResolveCheckoutAuthAmount(order, estimatedAmount);

                // POSNET Pre-Auth çağır
                var authResult = await _posnetService.ProcessAuthAsync(
                    orderId,
                    cardNumber,
                    expireDate,
                    cvv,
                    blockAmount,
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

                // Faz C: Tek Capt orkestratörü — PaymentCaptureService.Clamp + POSNET.
                // NEDEN: Bu metot eskiden bankayı doğrudan çağırıyordu; MarkDelivered ile çift çekim
                // ve limit aşımında "hiç çekme" tutarsızlığı üretiyordu.
                if (_paymentCaptureService != null)
                {
                    var unified = await _paymentCaptureService.CapturePaymentAsync(orderId, actualAmount);
                    stopwatch.Stop();

                    if (!unified.Success)
                    {
                        return PostAuthorizationResult.Failure(
                            orderId,
                            unified.Message ?? "Kesin çekim başarısız",
                            unified.ErrorCode);
                    }

                    var authorized = WeightBasedCapturePolicy.ResolveAuthorizedAmount(order);
                    return new PostAuthorizationResult
                    {
                        IsSuccess = true,
                        OrderId = orderId,
                        OriginalBlockedAmount = authorized,
                        CapturedAmount = unified.CapturedAmount,
                        DifferenceAmount = authorized - unified.CapturedAmount,
                        HostLogKey = unified.CaptureReference ?? hostLogKey,
                        TransactionDate = unified.CapturedAt,
                        ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                    };
                }

                // ── İDEMPOTENCY: Çift çekim koruması (fallback yol) ───────────────────
                if (order.CaptureStatus == CaptureStatus.Success)
                {
                    _logger?.LogWarning(
                        "[WEIGHT-PAYMENT] Tekrar Post-Auth çağrısı yok sayıldı; sipariş zaten capture edilmiş. " +
                        "OrderId: {OrderId}, CapturedAmount: {Captured}",
                        orderId, order.CapturedAmount);

                    return new PostAuthorizationResult
                    {
                        IsSuccess = true,
                        OrderId = orderId,
                        OriginalBlockedAmount = WeightBasedCapturePolicy.ResolveAuthorizedAmount(order),
                        CapturedAmount = order.CapturedAmount,
                        DifferenceAmount = 0m,
                        TransactionDate = DateTime.UtcNow
                    };
                }

                // ── MADDE 7: PreAuthHostLogKey boş kontrolü ────────────────────────────
                // Her iki kaynak da boşsa Post-Auth başlatılamaz; bankaya boş key göndermek
                // hata üretir. Açıklayıcı mesajla erken çıkış sağlanıyor.
                var resolvedHostLogKey = !string.IsNullOrWhiteSpace(hostLogKey)
                    ? hostLogKey
                    : order.PreAuthHostLogKey;

                if (string.IsNullOrWhiteSpace(resolvedHostLogKey))
                {
                    _logger?.LogError(
                        "[WEIGHT-PAYMENT] Post-Auth başlatılamadı: PreAuthHostLogKey bulunamadı. " +
                        "3D Secure callback tamamlandı mı? OrderId: {OrderId}",
                        orderId);
                    return PostAuthorizationResult.Failure(
                        orderId,
                        "Ön provizyon referansı (PreAuthHostLogKey) bulunamadı. " +
                        "3D Secure callback başarıyla tamamlanmış olmalı.");
                }

                var preAuthHostLogKey = resolvedHostLogKey;
                // Provizyon tutarı için tek doğruluk kaynağı (AuthorizedAmount veya PreAuthAmount).
                var preAuthAmount = WeightBasedCapturePolicy.ResolveAuthorizedAmount(order);

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

                var maxCapturableAmount = CalculateMaxCapturableAmount(preAuthAmount);
                var captureDecision = WeightBasedCapturePolicy.ClampToCaptureLimit(preAuthAmount, actualAmount);
                var captureAmount = captureDecision.CaptureAmount;

                // Limit aşımında hiç çekmek yerine tavan kadar Capt (tek politika).
                if (captureDecision.ExceedsLimit)
                {
                    _logger?.LogWarning(
                        "[WEIGHT-PAYMENT] Final tutar Capt tavanını aşıyor; tavan kadar çekilecek. " +
                        "OrderId: {OrderId}, PreAuth: {PreAuth}, MaxCapturable: {MaxCapturable}, Actual: {Actual}, Capture: {Capture}",
                        orderId, preAuthAmount, maxCapturableAmount, actualAmount, captureAmount);
                }

                // POSNET Capture (finansallaştırma) çağır — kırpılmış tutar
                var captureResult = await _posnetService.ProcessCaptureAsync(
                    orderId,
                    preAuthHostLogKey,
                    captureAmount,
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

                // Siparişi güncelle — Capt kırpılmış tutar, FinalAmount gerçek tartı
                var differenceAmount = preAuthAmount - captureAmount;
                order.FinalAmount = actualAmount;
                order.CapturedAmount = captureAmount;
                order.WeightDifference = differenceAmount;
                order.TotalPrice = actualAmount;
                order.WeightAdjustmentStatus = differenceAmount == 0
                    ? WeightAdjustmentStatus.NoDifference
                    : WeightAdjustmentStatus.Completed;

                if (captureDecision.ExceedsLimit)
                {
                    order.DeliveryProblemReason =
                        $"Final tutar ({actualAmount:N2} TL) Capt tavanını ({captureAmount:N2} TL) aştı. Kalan manuel tahsilat.";
                }

                // ── MADDE 17: Post-Auth (Capt) başarılı → sipariş Paid olarak işaretle ─
                // WeightPending (tartım bitti, capt bekleniyor) → Paid (finansallaştırma tamam)
                if (order.Status == OrderStatus.WeightPending || order.Status == OrderStatus.PreAuthorized)
                {
                    order.Status = OrderStatus.Paid;
                }

                await _db.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "[WEIGHT-PAYMENT] Kesin çekim başarılı. OrderId: {OrderId}, " +
                    "Captured: {Captured}, Difference: {Diff}, ElapsedMs: {Elapsed}",
                    orderId, captureAmount, differenceAmount, stopwatch.ElapsedMilliseconds);

                return new PostAuthorizationResult
                {
                    IsSuccess = true,
                    OrderId = orderId,
                    OriginalBlockedAmount = preAuthAmount,
                    CapturedAmount = captureAmount,
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
                    // Ek tahsilat - bu akışta ek kart tahsilatı için ayrıca ödeme başlatmak gerekir.
                    _logger?.LogWarning(
                        "[WEIGHT-PAYMENT] Ek tahsilat gerekiyor. OrderId: {OrderId}, Amount: {Amount}",
                        orderId, Math.Abs(differenceAmount));
                    order.WeightAdjustmentStatus = WeightAdjustmentStatus.PendingAdditionalPayment;
                    await _db.SaveChangesAsync(cancellationToken);

                    stopwatch.Stop();
                    return new PostAuthorizationResult
                    {
                        IsSuccess = false,
                        OrderId = orderId,
                        OriginalBlockedAmount = order.PreAuthAmount,
                        DifferenceAmount = differenceAmount,
                        ErrorMessage = "Ek tahsilat gerekiyor. Mevcut provizyon akışı yalnızca blokeli tutarı çekebilir.",
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

                // Faz 4: Doğrudan ProcessRefundAsync yerine birleşik yürütücü — reverse/return/fallback tek noktada
                if (_extendedPaymentService != null)
                {
                    var payment = await _db.Payments
                        .Where(p => p.OrderId == orderId &&
                                    (p.TransactionType == "sale" || p.TransactionType == "capt" ||
                                     p.TransactionType == "auth"))
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (payment == null)
                    {
                        return PartialRefundResult.Failure(orderId, "İade için ödeme kaydı bulunamadı.");
                    }

                    var maxRefundable = order.CapturedAmount > 0
                        ? order.CapturedAmount
                        : (order.FinalPrice > 0 ? order.FinalPrice : order.TotalPrice);

                    var execResult = await _extendedPaymentService.ExecutePosnetRefundAsync(
                        new PosnetRefundExecutionRequest
                        {
                            OrderId = orderId,
                            PaymentId = payment.Id,
                            RefundAmount = refundAmount,
                            MaxRefundableAmount = maxRefundable,
                            IsAuthOnly = false,
                            PreAuthHostLogKey = order.PreAuthHostLogKey ?? hostLogKey,
                            Reason = reason ?? "Ağırlık farkı iadesi"
                        });

                    stopwatch.Stop();

                    if (!execResult.Success)
                    {
                        _logger?.LogWarning(
                            "[WEIGHT-PAYMENT] Birleşik POSNET iade başarısız. OrderId: {OrderId}, Reason: {Reason}",
                            orderId, execResult.FailureReason);

                        return PartialRefundResult.Failure(
                            orderId,
                            execResult.FailureReason ?? "İade başarısız",
                            execResult.BankResponseCode);
                    }

                    _logger?.LogInformation(
                        "[WEIGHT-PAYMENT] Birleşik kısmi iade başarılı. OrderId: {OrderId}, Type: {Type}, ElapsedMs: {Elapsed}",
                        orderId, execResult.TransactionType, stopwatch.ElapsedMilliseconds);

                    return new PartialRefundResult
                    {
                        IsSuccess = true,
                        OrderId = orderId,
                        RefundedAmount = refundAmount,
                        OriginalAmount = order.TotalPrice,
                        RefundHostLogKey = execResult.HostLogKey ?? hostLogKey,
                        TransactionDate = DateTime.UtcNow,
                        ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                    };
                }

                // Geriye dönük uyumluluk: extended service yoksa eski doğrudan return çağrısı
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
            decimal adminApprovalThresholdPercent = 20,
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

            var result = new CashPaymentDifferenceResult
            {
                IsSuccess = true,
                OrderId = orderId,
                EstimatedAmount = estimatedAmount,
                ActualAmount = actualAmount,
                DifferenceAmount = differenceAmount,
                Direction = direction,
                DifferencePercent = differencePercent,
                RequiresAdminApproval = false,
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
                    order.CaptureStatus = CaptureStatus.Voided;
                    order.PreAuthHostLogKey = null;
                    order.PreAuthDate = null;
                    order.PreAuthAmount = 0m;
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

        // Tek doğruluk kaynağı: banka aşım sınırı (Auth × 1.20) WeightBasedCapturePolicy'de hesaplanır.
        private static decimal CalculateMaxCapturableAmount(decimal preAuthAmount)
            => WeightBasedCapturePolicy.CalculateMaxCapturableAmount(preAuthAmount);
    }
}
