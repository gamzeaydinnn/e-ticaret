using ECommerce.Entities.Concrete;
namespace ECommerce.Entities.Concrete
{
public class Discount : BaseEntity
{
    public string Title { get; set; } = string.Empty;
        public bool IsPercentage { get; set; } = true;
        public decimal Value { get; set; } // percent if IsPercentage, otherwise fixed amount
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ConditionsJson { get; set; } // optional rules

        // Navigation
        public virtual ICollection<Product> Products { get; set; } = new HashSet<Product>();
   }
}