using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.DTOs.Auth
{
    /// <summary>
    /// Kullanıcı kayıt DTO'su.
    /// SMS doğrulama için PhoneNumber alanı eklendi.
    /// </summary>
    public class RegisterDto
    {
        /// <summary>Email adresi</summary>
        [Required(ErrorMessage = "Email adresi gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>Şifre (minimum 6 karakter)</summary>
        [Required(ErrorMessage = "Şifre gereklidir.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string Password { get; set; } = string.Empty;

        /// <summary>Ad</summary>
        [Required(ErrorMessage = "Ad gereklidir.")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>Soyad</summary>
        [Required(ErrorMessage = "Soyad gereklidir.")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Telefon numarası (05XXXXXXXXX veya 5XXXXXXXXX formatında).
        /// SMS doğrulama için gereklidir.
        /// </summary>
        [Required(ErrorMessage = "Telefon numarası gereklidir.")]
        [RegularExpression(@"^(0?5\d{9})$", 
            ErrorMessage = "Geçerli bir Türkiye cep telefonu numarası giriniz. (05XXXXXXXXX)")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
