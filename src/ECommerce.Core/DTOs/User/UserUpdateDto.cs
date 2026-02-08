using ECommerce.Core.Constants;
using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.User
{
    /// <summary>
    /// Kullanıcı güncelleme DTO'su.
    /// IsActive: nullable → null gönderilirse mevcut değer korunur, true/false ise güncellenir.
    /// PhoneNumber: nullable → null gönderilirse güncellenmez, boş string sıfırlar.
    /// </summary>
    public class UserUpdateDto
    {
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
    }
}
