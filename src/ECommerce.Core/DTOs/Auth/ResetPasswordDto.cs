using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
namespace ECommerce.Core.DTOs.Auth
{
    //Bu DTO, kullanıcı e-postasına gelen sıfırlama bağlantısındaki token ile birlikte yeni şifresini ve yeni şifre tekrarını göndermesi için kullanılır.
    public class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}