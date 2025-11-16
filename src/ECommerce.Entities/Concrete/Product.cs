using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System;

namespace ECommerce.Entities.Concrete
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }

        // Brand artık string değil, entity ile ilişki olacak
        public int? BrandId { get; set; }         // Foreign key
        public virtual Brand? Brand { get; set; } // Navigation property

        public string Slug { get; set; } = string.Empty;
        public decimal Price { get; set; } = 0m;
        public decimal? SpecialPrice { get; set; }
        public int StockQuantity { get; set; } = 0;
        public string ImageUrl { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Currency { get; set; } = "TRY";

        // Navigation
        public virtual Category Category { get; set; } = null!;
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new HashSet<OrderItem>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new HashSet<CartItem>();
        public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new HashSet<ProductVariant>();
        public virtual ICollection<Discount> Discounts { get; set; } = new HashSet<Discount>();
        public virtual ICollection<ProductReview> ProductReviews { get; set; } = new HashSet<ProductReview>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new HashSet<Favorite>();
        public virtual ICollection<ProductImage> ProductImages { get; set; } = new HashSet<ProductImage>();
        public virtual ICollection<StockReservation> StockReservations { get; set; } = new HashSet<StockReservation>();
    }

}
