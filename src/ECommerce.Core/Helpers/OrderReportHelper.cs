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

            foreach (var timeZoneId in new[] { "Turkey Standard Time", "Europe/Istanbul" })
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

            return local;
        }

        public static (DateTime FromUtc, DateTime ToUtcExclusive) GetPeriodRangeUtc(string period)
        {
            var turkeyToday = OrderCancelPolicy.GetTurkeyNow().Date;
            var fromTurkey = period.Equals("weekly", StringComparison.OrdinalIgnoreCase)
                ? turkeyToday.AddDays(-7)
                : period.Equals("monthly", StringComparison.OrdinalIgnoreCase)
                    ? turkeyToday.AddDays(-30)
                    : turkeyToday;

            return (
                TurkeyToUtc(fromTurkey),
                TurkeyToUtc(turkeyToday.AddDays(1))
            );
        }

        public static (DateTime FromUtc, DateTime ToUtcExclusive) GetDateRangeUtc(DateTime? from, DateTime? to)
        {
            var turkeyNow = OrderCancelPolicy.GetTurkeyNow();
            var startTurkey = (from ?? turkeyNow.Date.AddDays(-7)).Date;
            var endTurkeyExclusive = (to ?? turkeyNow.Date).Date.AddDays(1);

            return (TurkeyToUtc(startTurkey), TurkeyToUtc(endTurkeyExclusive));
        }
    }
}
