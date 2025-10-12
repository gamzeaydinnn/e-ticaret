using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
namespace ECommerce.Core.DTOs.Auth
{//Bu DTO, sisteme giriş yapmış bir kullanıcının mevcut şifresini değiştirirken kullanılır. Kullanıcının güvenliği için eski şifresini de girmesi istenir.
    public class ChangePasswordDto
    {
        // Genellikle bu bilgi JWT token içerisinden alınır,
        // ancak DTO'da da isteğe bağlı tutulabilir.
        public string Email { get; set; } = string.Empty;

        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
} 