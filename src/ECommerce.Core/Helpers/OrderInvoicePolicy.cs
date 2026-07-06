using System.Collections.Generic;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

namespace ECommerce.Core.Helpers
{
    /// <summary>
    /// Müşteri fatura indirme kuralları.
    /// PDF fatura: ödeme alınmış ve iptal edilmemiş siparişler için.
    /// Mikro e-fatura (FaturaSyncService) ayrı ERP entegrasyonudur.
    /// </summary>
    public static class OrderInvoicePolicy
    {
        private static readonly HashSet<OrderStatus> BlockedStatuses = new()
        {
            OrderStatus.New,
            OrderStatus.Pending,
            OrderStatus.Cancelled,
            OrderStatus.PaymentFailed
        };

        public static bool CanDownloadInvoice(OrderStatus status, PaymentStatus paymentStatus)
        {
            if (BlockedStatuses.Contains(status))
            {
                return false;
            }

            return paymentStatus == PaymentStatus.Paid
                   || paymentStatus == PaymentStatus.Authorized;
        }

        public static bool CanDownloadInvoice(Order order)
        {
            return CanDownloadInvoice(order.Status, order.PaymentStatus);
        }
    }
}
