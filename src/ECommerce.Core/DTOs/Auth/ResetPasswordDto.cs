namespace ECommerce.Core.DTOs.Auth
{
    //Bu DTO, kullanıcı e-postasına gelen sıfırlama bağlantısındaki token ile birlikte yeni şifresini ve yeni şifre tekrarını göndermesi için kullanılır.
    public class ResetPasswordDto
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}