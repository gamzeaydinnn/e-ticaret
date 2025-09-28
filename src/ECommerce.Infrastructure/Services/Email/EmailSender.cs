using ECommerce.Infrastructure.Config;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Services.Email
{
    
    public class EmailSender
    {
        private readonly EmailSettings _settings;

        public EmailSender(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(_settings.FromEmail, _settings.FromName);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = isHtml;

                using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPass),
                    EnableSsl = true
                };

                await client.SendMailAsync(message);
                return true;
            }
            catch
            {
                // Loglama ekleyebilirsin
                return false;
            }
        }
    }
}
