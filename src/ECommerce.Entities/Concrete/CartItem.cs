namespace ECommerce.Entities.Concrete
{
    public class CartItem : BaseEntity
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int Id { get; set; }
        public string? CartToken { get; set; } // guest identifier
        public System.DateTime CreatedAt { get; set; } = System.DateTime.UtcNow;
        public System.DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual User? User { get; set; }
        public virtual Product? Product { get; set; }
    }
}
