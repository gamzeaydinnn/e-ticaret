using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
namespace ECommerce.Core.DTOs.Auth
{
    // Şifre sıfırlama talebi: sadece e-posta adresi gerekir
    public class ForgotPasswordDto
    {
        public string Email { get; set; } = string.Empty;
    }
}
