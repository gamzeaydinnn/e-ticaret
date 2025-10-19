using System.Net.Mail;
using ECommerce.Core.DTOs.User;
using ECommerce.Core.DTOs.Auth;
namespace ECommerce.Core.Validators
{
    public static class UserValidator
    {
        // Giriş formu validasyonu (email + parola)
        public static bool Validate(LoginDto dto, out string error)
        {
            error = null;
            if (dto is null) { error = "İstek gövdesi gerekli"; return false; }
            if (string.IsNullOrWhiteSpace(dto.Email)) { error = "Email gerekli"; return false; }
            if (string.IsNullOrWhiteSpace(dto.Password)) { error = "Parola gerekli"; return false; }

            // Basit email format kontrolü
            try { _ = new MailAddress(dto.Email); }
            catch { error = "Geçersiz email formatı"; return false; }

            return true;
        }

        // Okuma amaçlı kullanıcı DTO doğrulaması (yalnızca email kontrolü)
        public static bool Validate(UserLoginDto dto, out string error)
        {
            error = null;
            if (dto is null) { error = "İstek gövdesi gerekli"; return false; }
            if (string.IsNullOrWhiteSpace(dto.Email)) { error = "Email gerekli"; return false; }
            try { _ = new MailAddress(dto.Email); }
            catch { error = "Geçersiz email formatı"; return false; }
            return true;
        }
    }
}
