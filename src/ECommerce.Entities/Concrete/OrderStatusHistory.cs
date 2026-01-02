using System;
using ECommerce.Entities.Enums;

namespace ECommerce.Entities.Concrete
{
    public class OrderStatusHistory : BaseEntity
    {
        public int OrderId { get; set; }
        public virtual Order? Order { get; set; }

        public OrderStatus PreviousStatus { get; set; }
        public OrderStatus NewStatus { get; set; }

        // kimin değiştirdiği (kullanıcı id veya kullanıcı adı, sistem işlemleri için null olabilir)
        public string? ChangedBy { get; set; }

        // isteğe bağlı açıklama / iptal nedeni vs.
        public string? Reason { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
