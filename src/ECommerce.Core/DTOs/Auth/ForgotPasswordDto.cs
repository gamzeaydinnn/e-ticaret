using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
namespace ECommerce.Core.DTOs.Auth
{//Bu DTO, kullanıcının şifre sıfırlama talebinde bulunurken sadece e-posta adresini göndermesi için kullanılır.
    public class ForgotPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}