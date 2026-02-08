using ECommerce.Core.Constants;
using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
namespace ECommerce.Core.DTOs.User
{
    /// <summary>
    /// Yeni kullanıcı oluşturma DTO'su.
    /// PhoneNumber: Opsiyonel - boş bırakılabilir.
    /// Role: Varsayılan "User" - admin tarafından değiştirilebilir.
    /// </summary>
    public class UserCreateDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string Role { get; set; } = Roles.User;
    }
}
