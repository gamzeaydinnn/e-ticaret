namespace ECommerce.Entities.Concrete
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; } = 0m;
        public int StockQuantity { get; set; } = 0;
        public string ImageUrl { get; set; } = string.Empty;

        public string SKU { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; } = null!;
        public int Id { get; set; }
        public string Currency { get; set; } = "TRY";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual ICollection<Category> Categories { get; set; } = new HashSet<Category>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new HashSet<OrderItem>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new HashSet<CartItem>();
    }
}
