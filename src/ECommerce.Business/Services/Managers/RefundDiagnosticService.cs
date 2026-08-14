// ==========================================================================
// RefundDiagnosticService.cs - Faz 0 iade teşhis servisi
// ==========================================================================
// Başarısız iade taleplerinde HostLogKey, tutar uyumu ve önerilen banka
// operasyonunu (reverse vs return) raporlar.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Order;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    public class RefundDiagnosticService : IRefundDiagnosticService
    {
        private static readonly string[] ReversiblePaymentStatuses =
        {
            "Success", "Paid", "Authorized", "PartiallyRefunded"
        };

        private readonly ECommerceDbContext _db;
        private readonly ILogger<RefundDiagnosticService> _logger;

        public RefundDiagnosticService(
            ECommerceDbContext db,
            ILogger<RefundDiagnosticService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<RefundFailedSummaryDto> GetFailedRefundsDiagnosticAsync()
        {
            var failedOrderIds = await _db.RefundRequests
                .AsNoTracking()
                .Where(r => r.Status == RefundRequestStatus.RefundFailed)
                .Select(r => r.OrderId)
                .Distinct()
                .ToListAsync();

            var items = new List<RefundOrderDiagnosticDto>();
            foreach (var orderId in failedOrderIds)
            {
                var diagnostic = await BuildOrderDiagnosticAsync(orderId);
                if (diagnostic != null)
                {
                    items.Add(diagnostic);
                }
            }

            _logger.LogInformation(
                "[İADE-TEŞHİS] {Count} başarısız iade siparişi analiz edildi.",
                items.Count);

            return new RefundFailedSummaryDto
            {
                FailedCount = items.Count,
                Items = items.OrderByDescending(i => i.RefundRequests
                    .Max(r => (DateTime?)r.ProcessedAt) ?? DateTime.MinValue).ToList()
            };
        }

        /// <inheritdoc />
        public async Task<RefundOrderDiagnosticDto?> GetOrderDiagnosticAsync(int orderId)
        {
            var exists = await _db.Orders.AsNoTracking().AnyAsync(o => o.Id == orderId);
            if (!exists)
            {
                return null;
            }

            return await BuildOrderDiagnosticAsync(orderId);
        }

        private async Task<RefundOrderDiagnosticDto?> BuildOrderDiagnosticAsync(int orderId)
        {
            var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                return null;
            }

            var payments = await _db.Payments
                .AsNoTracking()
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var refundRequests = await _db.RefundRequests
                .AsNoTracking()
                .Where(r => r.OrderId == orderId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            var latestReversible = payments
                .FirstOrDefault(p => ReversiblePaymentStatuses.Contains(p.Status));

            var originalSaleOrCapt = payments
                .FirstOrDefault(p =>
                    (p.TransactionType == "capt" || p.TransactionType == "sale") &&
                    (p.Status == "Paid" || p.Status == "Success"));

            var paymentForAnalysis = originalSaleOrCapt ?? latestReversible;

            var maxRefundable = CalculateMaxRefundableAmount(order, paymentForAnalysis);
            var alreadyRefunded = paymentForAnalysis?.RefundedAmount ?? 0m;
            var remaining = Math.Max(0m, maxRefundable - alreadyRefunded);

            var isAuthOnly = IsAuthOnly(order, paymentForAnalysis);
            var isSameDay = paymentForAnalysis != null &&
                            IsSameBusinessDay(paymentForAnalysis.CreatedAt);

            var recommended = ResolveRecommendedOperation(
                isAuthOnly,
                isSameDay,
                remaining,
                maxRefundable);

            var issues = DetectIssues(order, paymentForAnalysis, payments, maxRefundable, remaining);

            return new RefundOrderDiagnosticDto
            {
                OrderId = orderId,
                OrderNumber = order.OrderNumber,
                OrderStatus = order.Status.ToString(),
                PaymentMethod = order.PaymentMethod,
                FinalPrice = order.FinalPrice,
                CapturedAmount = order.CapturedAmount,
                MaxRefundableAmount = maxRefundable,
                AlreadyRefundedAmount = alreadyRefunded,
                RemainingRefundableAmount = remaining,
                IsAuthOnly = isAuthOnly,
                IsSameBusinessDayAsPayment = isSameDay,
                RecommendedOperation = recommended,
                Issues = issues,
                Payments = payments.Select(MapPaymentDiagnostic).ToList(),
                RefundRequests = refundRequests.Select(MapRefundRequestDiagnostic).ToList()
            };
        }

        private static List<string> DetectIssues(
            Order order,
            Payments? paymentForAnalysis,
            IReadOnlyList<Payments> allPayments,
            decimal maxRefundable,
            decimal remaining)
        {
            var issues = new List<string>();

            if (paymentForAnalysis == null)
            {
                issues.Add("İade edilebilir ödeme kaydı bulunamadı.");
                return issues;
            }

            if (string.IsNullOrWhiteSpace(paymentForAnalysis.HostLogKey) &&
                string.IsNullOrWhiteSpace(paymentForAnalysis.ProviderPaymentId))
            {
                issues.Add("HostLogKey eksik — POSNET iade/iptal referansı yok.");
            }

            if (order.CapturedAmount > 0 &&
                paymentForAnalysis.Amount > 0 &&
                Math.Abs(order.CapturedAmount - paymentForAnalysis.Amount) > 0.02m)
            {
                issues.Add(
                    $"Tutar uyumsuzluğu: sipariş CapturedAmount={order.CapturedAmount:N2} TL, " +
                    $"payment.Amount={paymentForAnalysis.Amount:N2} TL. KG siparişlerde iade limiti yanlış hesaplanabilir.");
            }

            var hasPartialReturnRecords = allPayments.Any(p =>
                p.TransactionType == "return" &&
                string.Equals(p.Status, "Refunded", StringComparison.OrdinalIgnoreCase));

            if (hasPartialReturnRecords && remaining >= maxRefundable - 0.01m)
            {
                issues.Add("Önceki kısmi iade kayıtları var; tam iptal (reverse) bankaca reddedilebilir (0218).");
            }

            if (remaining <= 0)
            {
                issues.Add("İade edilebilir kalan tutar sıfır — muhtemelen zaten iade edilmiş.");
            }

            return issues;
        }

        private static string ResolveRecommendedOperation(
            bool isAuthOnly,
            bool isSameDay,
            decimal remaining,
            decimal maxRefundable)
        {
            if (isAuthOnly)
            {
                return "reverse(auth) — provizyon iptali";
            }

            var isFullRemaining = remaining >= maxRefundable - 0.01m;
            if (isSameDay && isFullRemaining)
            {
                return "reverse(sale/capt) — aynı gün tam iptal; 0211 alınırsa return fallback";
            }

            return "return — kısmi veya ertesi gün iade";
        }

        private static decimal CalculateMaxRefundableAmount(Order order, Payments? payment)
        {
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

        private static bool IsAuthOnly(Order order, Payments? payment)
        {
            return payment?.Status == "Authorized" ||
                   (order.PaymentStatus == PaymentStatus.Authorized &&
                    order.CaptureStatus != CaptureStatus.Success);
        }

        private static bool IsSameBusinessDay(DateTime utcDateTime)
        {
            return ConvertUtcToTurkey(utcDateTime).Date == ConvertUtcToTurkey(DateTime.UtcNow).Date;
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
                    return TimeZoneInfo.ConvertTimeFromUtc(
                        normalizedUtc,
                        TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
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

        private static RefundPaymentDiagnosticDto MapPaymentDiagnostic(Payments payment)
        {
            var hostLogKey = payment.HostLogKey ?? payment.ProviderPaymentId;
            return new RefundPaymentDiagnosticDto
            {
                PaymentId = payment.Id,
                TransactionType = payment.TransactionType,
                Status = payment.Status,
                Amount = payment.Amount,
                RefundedAmount = payment.RefundedAmount,
                CapturedAmount = payment.CapturedAmount,
                HasHostLogKey = !string.IsNullOrWhiteSpace(hostLogKey),
                HostLogKeyMasked = MaskHostLogKey(hostLogKey),
                CreatedAt = payment.CreatedAt
            };
        }

        private static RefundRequestDiagnosticDto MapRefundRequestDiagnostic(RefundRequest request)
        {
            return new RefundRequestDiagnosticDto
            {
                RefundRequestId = request.Id,
                Status = request.Status.ToString(),
                RefundAmount = request.RefundAmount,
                TransactionType = request.TransactionType,
                RefundFailureReason = request.RefundFailureReason,
                ProcessedAt = request.ProcessedAt
            };
        }

        private static string? MaskHostLogKey(string? hostLogKey)
        {
            if (string.IsNullOrWhiteSpace(hostLogKey) || hostLogKey.Length <= 6)
            {
                return hostLogKey;
            }

            return $"{hostLogKey[..4]}...{hostLogKey[^4..]}";
        }
    }
}
