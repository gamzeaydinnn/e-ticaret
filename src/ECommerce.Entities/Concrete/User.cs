using Microsoft.AspNetCore.Identity;

namespace ECommerce.Entities.Concrete
{
    public class User : IdentityUser<int>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<Order> Orders { get; set; } = new HashSet<Order>();
    }
}