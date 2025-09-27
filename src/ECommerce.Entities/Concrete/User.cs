using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace ECommerce.Entities.Concrete
{
    public class User : IdentityUser<int>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }

        //bu iki satırı sonradan ekledim
        public string PasswordHash { get; set; }
        public string Role { get; set; } = "User";

        // Navigation Properties
        public virtual ICollection<Order> Orders { get; set; } = new HashSet<Order>();
        
    }
}
