using System;
using System.Collections.Generic;
using ECommerce.Entities.Enums;

namespace ECommerce.Core.Helpers
{
    /// <summary>
    /// Müşteri iptal/iade kurallarının tek kaynağı.
    /// Kurye paketi teslim alana kadar (PickedUp öncesi) otomatik iptal + banka reverse/return;
    /// sonrasında WhatsApp + admin onayı.
    /// </summary>
    public static class OrderCancelPolicy
    {
        public const string CancelModeAuto = "auto";
        public const string CancelModeWhatsApp = "whatsapp";
        public const string CancelModeNone = "none";

        public static readonly HashSet<OrderStatus> AutoCancellableStatuses = new()
        {
            OrderStatus.New,
            OrderStatus.Pending,
            OrderStatus.Confirmed,
            OrderStatus.Paid,
            OrderStatus.Preparing,
            OrderStatus.Processing,
            OrderStatus.Ready,
            OrderStatus.ReadyForPickup,
            OrderStatus.Assigned,
            OrderStatus.PreAuthorized,
            OrderStatus.WeightPending
        };

        public static readonly HashSet<OrderStatus> WhatsAppRequiredStatuses = new()
        {
            OrderStatus.PickedUp,
            OrderStatus.InTransit,
            OrderStatus.OutForDelivery,
            OrderStatus.Shipped,
            OrderStatus.Delivered,
            OrderStatus.Completed,
            OrderStatus.DeliveryFailed,
            OrderStatus.DeliveryPaymentPending,
            OrderStatus.PartialRefund
        };

        public static readonly HashSet<OrderStatus> TerminalStatuses = new()
        {
            OrderStatus.Cancelled,
            OrderStatus.Refunded
        };

        public static string NormalizeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return string.Empty;
            }

            return status
                .Trim()
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }

        public static bool TryParseOrderStatus(string? status, out OrderStatus parsed)
        {
            parsed = default;

            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            if (Enum.TryParse<OrderStatus>(status, true, out parsed))
            {
                return true;
            }

            var normalized = NormalizeStatus(status);
            foreach (OrderStatus value in Enum.GetValues(typeof(OrderStatus)))
            {
                if (NormalizeStatus(value.ToString()) == normalized)
                {
                    parsed = value;
                    return true;
                }
            }

            return false;
        }

        public static string GetCancelMode(OrderStatus status, DateTime orderDateUtc, DateTime? turkeyNow = null)
        {
            if (TerminalStatuses.Contains(status))
            {
                return CancelModeNone;
            }

            // PickedUp öncesi: gün farkı olmadan otomatik iptal (aynı gün reverse, sonrası return)
            if (AutoCancellableStatuses.Contains(status))
            {
                return CancelModeAuto;
            }

            if (WhatsAppRequiredStatuses.Contains(status))
            {
                return CancelModeWhatsApp;
            }

            return CancelModeWhatsApp;
        }

        public static string GetCancelMode(string? status, DateTime orderDateUtc, DateTime? turkeyNow = null)
        {
            if (!TryParseOrderStatus(status, out var parsed))
            {
                return CancelModeWhatsApp;
            }

            return GetCancelMode(parsed, orderDateUtc, turkeyNow);
        }

        public static bool CanCustomerAutoCancel(OrderStatus status, DateTime orderDateUtc, DateTime? turkeyNow = null)
        {
            return GetCancelMode(status, orderDateUtc, turkeyNow) == CancelModeAuto;
        }

        public static bool IsSameBusinessDay(DateTime orderDateUtc, DateTime turkeyNow)
        {
            return ConvertUtcToTurkey(orderDateUtc).Date == turkeyNow.Date;
        }

        public static DateTime GetTurkeyNow()
        {
            return ConvertUtcToTurkey(DateTime.UtcNow);
        }

        public static DateTime ConvertUtcToTurkey(DateTime utcDateTime)
        {
            var normalizedUtc = utcDateTime.Kind == DateTimeKind.Utc
                ? utcDateTime
                : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

            foreach (var timeZoneId in new[] { "Europe/Istanbul", "Turkey Standard Time" })
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
    }
}
