using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ECommerce.Infrastructure.Services.Email;
using ECommerce.Infrastructure.Config;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Geri Bildirim Controller
    /// Müşterilerden gelen geri bildirimleri Gölköy Gurme mail adresine iletir
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {  
        private readonly EmailSender _emailSender;
        private readonly SiteSettings _siteSettings;

        public FeedbackController(
            EmailSender emailSender,
            IOptions<SiteSettings> siteSettings)
        {
            _emailSender = emailSender;
            _siteSettings = siteSettings.Value;
        }

        /// <summary>
        /// Müşteri geri bildirimini e-posta olarak gönderir
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] FeedbackRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { success = false, message = "Mesaj alanı zorunludur." });
            }

            // Hedef e-posta adresi - SiteSettings'ten al veya varsayılan kullan
            var targetEmail = _siteSettings?.Contact?.Email ?? "golturkbuku@golkoygurme.com.tr";
            
            // E-posta içeriğini hazırla
            var subject = "🛒 Yeni Müşteri Geri Bildirimi - Gölköy Gurme";
            var customerEmail = string.IsNullOrWhiteSpace(request.Email) ? "Belirtilmemiş" : request.Email;
            
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f57c00, #ff9800); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-top: none; border-radius: 0 0 8px 8px; }}
        .field {{ margin-bottom: 15px; }}
        .label {{ font-weight: bold; color: #555; }}
        .value {{ margin-top: 5px; padding: 10px; background: white; border-radius: 4px; border: 1px solid #eee; }}
        .message-box {{ white-space: pre-wrap; }}
        .footer {{ margin-top: 20px; padding-top: 15px; border-top: 1px solid #ddd; font-size: 12px; color: #888; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin: 0;'>📬 Yeni Geri Bildirim</h2>
        </div>
        <div class='content'>
            <div class='field'>
                <div class='label'>📧 Müşteri E-posta:</div>
                <div class='value'>{System.Net.WebUtility.HtmlEncode(customerEmail)}</div>
            </div>
            <div class='field'>
                <div class='label'>💬 Mesaj:</div>
                <div class='value message-box'>{System.Net.WebUtility.HtmlEncode(request.Message)}</div>
            </div>
            <div class='footer'>
                Bu mesaj Gölköy Gurme web sitesi geri bildirim formu aracılığıyla gönderilmiştir.
            </div>
        </div>
    </div>
</body>
</html>";

            var sent = await _emailSender.SendEmailAsync(targetEmail, subject, body, isHtml: true);
            
            if (sent)
            {
                return Ok(new { 
                    success = true, 
                    message = "Geri bildiriminiz başarıyla iletildi. Teşekkür ederiz!" 
                });
            }
            else
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Geri bildirim gönderilirken bir hata oluştu. Lütfen daha sonra tekrar deneyiniz." 
                });
            }
        }
    }

    /// <summary>
    /// Geri bildirim isteği modeli
    /// </summary>
    public class FeedbackRequest
    {
        /// <summary>
        /// Müşteri e-posta adresi (isteğe bağlı)
        /// </summary>
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string? Email { get; set; }

        /// <summary>
        /// Geri bildirim mesajı (zorunlu)
        /// </summary>
        [Required(ErrorMessage = "Mesaj alanı zorunludur.")]
        [MinLength(10, ErrorMessage = "Mesaj en az 10 karakter olmalıdır.")]
        [MaxLength(2000, ErrorMessage = "Mesaj en fazla 2000 karakter olabilir.")]
        public string Message { get; set; } = string.Empty;
    }
}
