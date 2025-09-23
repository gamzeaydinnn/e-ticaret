namespace ECommerce.Core.Entities.Concrete
{
    public class OrderItem : IBaseEntity
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public System.DateTime CreatedAt { get; set; } = System.DateTime.UtcNow;
        public System.DateTime? UpdatedAt { get; set; }
    }
}
