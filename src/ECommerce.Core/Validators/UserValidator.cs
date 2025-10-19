using ECommerce.Core.DTOs.User;
using ECommerce.Core.DTOs.Auth;
namespace ECommerce.Core.Validators
{
    using ECommerce.Core.DTOs.User;

    public static class UserValidator
    {
        // Giriş formu validasyonu (email + parola)
        public static bool Validate(LoginDto dto, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(dto.Email)) { error = "Email gerekli"; return false; }
            if (string.IsNullOrWhiteSpace(dto.Password)) { error = "Parola gerekli"; return false; }
            return true;
        }

        // Okuma amaçlı kullanıcı DTO doğrulaması (yalnızca email kontrolü)
        public static bool Validate(UserLoginDto dto, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(dto.Email)) { error = "Email gerekli"; return false; }
            return true;
        }
    }
}
