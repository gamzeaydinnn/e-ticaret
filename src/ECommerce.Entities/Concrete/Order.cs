using System;
using System.Collections.Generic;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

//	• Orders (Id GUID, UserId, TotalAmount, ShippingAmount, PaymentStatus, OrderStatus, CreatedAt, ReservationId (Guid?), AddressId)
/*    public int Id { get; set; }
    public int? UserId { get; set; } // guest ise null
    public User User { get; set; }
    public string CustomerName { get; set; }
    public string CustomerPhone { get; set; }
    public string CustomerEmail { get; set; }
    public string Address { get; set; }
    public string PaymentMethod { get; set; }
    public string Status { get; set; } // e.g. Pending, Preparing, OutForDelivery, Delivered, Cancelled
    public int? CourierId { get; set; }
    public Courier Courier { get; set; }
*/
namespace ECommerce.Entities.Concrete
{
    public class Order : BaseEntity
    {
        public string OrderNumber { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal TotalPrice { get; set; } = 0m;
        public string Currency { get; set; } = "TRY";
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        
        // Kargo bilgileri
        public string ShippingMethod { get; set; } = "car"; // car veya motorcycle
        public decimal ShippingCost { get; set; } = 30m; // Kargo ücreti
        
        // Kurye bilgileri
        public int? CourierId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? PickedUpAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? DeliveryNotes { get; set; }
        public string Priority { get; set; } = "normal"; // normal, urgent, low

        public ICollection<OrderItem> Items { get; set; } = new HashSet<OrderItem>();

        // Navigation Properties
        public virtual User? User { get; set; }
        public virtual Courier? Courier { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new HashSet<OrderItem>();
        public virtual ICollection<WeightReport> WeightReports { get; set; } = new HashSet<WeightReport>();
    }
}
