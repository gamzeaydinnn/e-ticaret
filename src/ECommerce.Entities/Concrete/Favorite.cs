using System;

namespace ECommerce.Entities.Concrete
{
    public class Favorite : BaseEntity
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
 
