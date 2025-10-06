using ECommerce.Entities.Concrete;
namespace ECommerce.Entities.Concrete
{
public class ProductReview : BaseEntity
{
    public int ProductId { get; set; }
        public int UserId { get; set; } = 0;
        public int Rating { get; set; } // 1..5
        public string Comment { get; set; } = string.Empty;
        public bool IsApproved { get; set; } = false; // admin onayı
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Product? Product { get; set; }
        public virtual User? User { get; set; }
   }


}