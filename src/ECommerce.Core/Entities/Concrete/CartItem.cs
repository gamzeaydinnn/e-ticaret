namespace ECommerce.Core.Entities.Concrete
{
    public class CartItem : IBaseEntity
    {
        public int Id { get; set; }
        public int? UserId { get; set; } // nullable for guest
        public string? CartToken { get; set; } // guest identifier
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public System.DateTime CreatedAt { get; set; } = System.DateTime.UtcNow;
        public System.DateTime? UpdatedAt { get; set; }
    }
}
