namespace ECommerce.Entities.Concrete
{
    public class Product : BaseEntity
{
    // Id, CreatedAt, UpdatedAt, IsActive BaseEntity'den geliyor, yeniden tanımlamaya gerek yok

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public decimal Price { get; set; } = 0m;
    public decimal? SpecialPrice { get; set; }
    //public int Stock { get; set; }
    public int StockQuantity { get; set; } = 0;
    public string ImageUrl { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    

    //o	Price, DiscountPrice, IsActive, Slug, Images (n-n), Variants ilişkisi.

        public virtual Category Category { get; set; } = null!;
    
    // Navigation
    public virtual ICollection<Category> Categories { get; set; } = new HashSet<Category>();
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new HashSet<OrderItem>();
    public virtual ICollection<CartItem> CartItems { get; set; } = new HashSet<CartItem>();
}

}
