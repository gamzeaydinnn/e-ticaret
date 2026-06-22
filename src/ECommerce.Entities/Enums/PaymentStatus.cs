using System;

namespace ECommerce.Entities.Enums
{
    public enum PaymentStatus
    {
        Pending = 0,
        Authorized = 1,
        Paid = 2,
        Failed = 3,
        Refunded = 4,
        Cancelled = 5
    }
}
