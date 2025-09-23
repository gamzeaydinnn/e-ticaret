namespace ECommerce.Core.Validators
{
    using ECommerce.Core.DTOs.User;

    public static class UserValidator
    {
        public static bool Validate(UserLoginDto dto, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(dto.Email)) { error = "Email gerekli"; return false; }
            if (string.IsNullOrWhiteSpace(dto.Password)) { error = "Parola gerekli"; return false; }
            return true;
        }
    }
}
