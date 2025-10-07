namespace ECommerce.Core.DTOs.Auth
{//Bu DTO, sisteme giriş yapmış bir kullanıcının mevcut şifresini değiştirirken kullanılır. Kullanıcının güvenliği için eski şifresini de girmesi istenir.
    public class ChangePasswordDto
    {
        // Genellikle bu bilgi JWT token içerisinden alınır,
        // ancak DTO'da da isteğe bağlı tutulabilir.
        public string Email { get; set; } 
        
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
} 