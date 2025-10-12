using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
namespace ECommerce.Core.DTOs.User
{
    public class UserLoginDto
    {
        public int Id { get; set; }           // Guid yerine int olmalı
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string Password { get; set; } = string.Empty;
        public string LastName { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}
