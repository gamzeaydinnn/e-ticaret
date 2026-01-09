using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using ECommerce.Entities.Concrete;
using System;

namespace ECommerce.Entities.Concrete
{
    public class User : IdentityUser<int>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string Role { get; set; } = "User";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? Password { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }

        // SMS Doğrulama İlgili Alanlar
        /// <summary>
        /// Telefon numarası doğrulandı mı?
        /// Not: IdentityUser'daki PhoneNumberConfirmed özelliği kullanılır.
        /// </summary>
        // public bool PhoneNumberConfirmed { get; set; } = false; // IdentityUser'da var

        /// <summary>
        /// Telefon numarası doğrulama tarihi
        /// </summary>
        public DateTime? PhoneNumberConfirmedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<Order> Orders { get; set; } = new HashSet<Order>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new HashSet<CartItem>();
        public virtual ICollection<ProductReview> ProductReviews { get; set; } = new HashSet<ProductReview>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new HashSet<Favorite>();
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new HashSet<RefreshToken>();
        public virtual ICollection<Address> Addresses { get; set; } = new HashSet<Address>();
    
    }
}
