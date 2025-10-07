namespace ECommerce.Core.DTOs.Auth
{//Bu DTO, kullanıcının şifre sıfırlama talebinde bulunurken sadece e-posta adresini göndermesi için kullanılır.
    public class ForgotPasswordDto
    {
        public string Email { get; set; }
    }
}