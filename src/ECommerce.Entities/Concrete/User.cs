using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace ECommerce.Entities.Concrete
{
    public class User : IdentityUser<int>
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Password { get; set; } = null!;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; } = "User";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation Properties
        public virtual ICollection<Order> Orders { get; set; } = new HashSet<Order>();
        
    }
}
