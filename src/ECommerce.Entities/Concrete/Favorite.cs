using System;

namespace ECommerce.Entities.Concrete
{
    public class Favorite : BaseEntity
    {
        // Kayıtlı kullanıcılar için
        public int? UserId { get; set; }
        
        // Misafir kullanıcılar için (UUID token)
        public string? GuestToken { get; set; }
        
        public int ProductId { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Product Product { get; set; } = null!;
    }
}
 
