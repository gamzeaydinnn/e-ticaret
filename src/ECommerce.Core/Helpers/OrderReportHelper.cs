using System;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

namespace ECommerce.Core.Helpers
{
    /// <summary>
    /// Admin raporları ve dashboard için ortak sipariş/ciro hesaplama kuralları.
    /// </summary>
    public static class OrderReportHelper
    {
        public static bool IsCountableSaleOrder(OrderStatus status, PaymentStatus paymentStatus)
        {
            if (status is OrderStatus.Cancelled or OrderStatus.Refunded or OrderStatus.PaymentFailed)
            {
                return false;
            }

            if (paymentStatus is PaymentStatus.Failed or PaymentStatus.Cancelled or PaymentStatus.Refunded)
            {
                return false;
            }

            return true;
        }

        public static decimal GetSaleAmount(Order order)
        {
            if (order == null) return 0m;

            if (order.CapturedAmount > 0) return order.CapturedAmount;
            if (order.FinalAmount > 0) return order.FinalAmount;
            if (order.FinalPrice > 0) return order.FinalPrice;
            return order.TotalPrice;
        }

        public static DateTime TurkeyToUtc(DateTime turkeyLocalDateTime)
        {
            var local = DateTime.SpecifyKind(turkeyLocalDateTime, DateTimeKind.Unspecified);

            // Linux Docker'da "Turkey Standard Time" yok; Europe/Istanbul önce denenir.
            foreach (var timeZoneId in new[] { "Europe/Istanbul", "Turkey Standard Time" })
            {
                try
                {
                    var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                    return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            // Türkiye 2016'dan beri sabit UTC+3
            return DateTime.SpecifyKind(local.AddHours(-3), DateTimeKind.Utc);
        }

        /// <summary>
        /// Sipariş listesi (GetOrdersAsync) ile aynı mantıkta UTC aralığı.
        /// </summary>
        public static (DateTime FromUtcInclusive, DateTime ToUtcInclusive) GetOrdersCompatibleRangeUtc(
            DateTime turkeyStartDate,
            DateTime turkeyEndDate)
        {
            var fromUtc = TurkeyToUtc(turkeyStartDate.Date);
            var toUtcExclusive = TurkeyToUtc(turkeyEndDate.Date.AddDays(1));
            return (fromUtc, toUtcExclusive.AddTicks(-1));
        }

        public static (DateTime FromUtc, DateTime ToUtcExclusive) GetPeriodRangeUtc(string period)
        {
            var turkeyToday = OrderCancelPolicy.GetTurkeyNow().Date;
            var turkeyStart = period.Equals("weekly", StringComparison.OrdinalIgnoreCase)
                ? turkeyToday.AddDays(-6)
                : period.Equals("monthly", StringComparison.OrdinalIgnoreCase)
                    ? turkeyToday.AddDays(-29)
                    : turkeyToday;

            return (
                TurkeyToUtc(turkeyStart),
                TurkeyToUtc(turkeyToday.AddDays(1))
            );
        }

        public static (DateTime FromUtc, DateTime ToUtcExclusive) GetDateRangeUtc(DateTime? from, DateTime? to)
        {
            var turkeyNow = OrderCancelPolicy.GetTurkeyNow();
            var startTurkey = (from ?? turkeyNow.Date.AddDays(-6)).Date;
            var endTurkey = (to ?? turkeyNow.Date).Date;

            return (
                TurkeyToUtc(startTurkey),
                TurkeyToUtc(endTurkey.AddDays(1))
            );
        }
    }
}
