using System;

namespace ECommerce.Entities.Concrete
{
    public class Favorite : BaseEntity
    {
        public Guid UserId { get; set; }
        public int ProductId { get; set; }

        // Navigation properties
        public User User { get; set; }
        public Product Product { get; set; }
    }
}
 