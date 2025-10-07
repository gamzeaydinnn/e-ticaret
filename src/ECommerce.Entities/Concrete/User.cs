using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace ECommerce.Entities.Concrete
{
    public class User : IdentityUser<int>
{
    // Id, Email, PasswordHash zaten IdentityUser<int>’den geliyor, kaldır
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    //public bool IsCourier { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? ResetTokenExpires { get; set; }

    // Navigation Properties
        public virtual ICollection<Order> Orders { get; set; } = new HashSet<Order>();
}

}
