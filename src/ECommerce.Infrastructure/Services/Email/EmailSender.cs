using ECommerce.Infrastructure.Config;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.IO;


namespace ECommerce.Infrastructure.Services.Email
{
    
    public class EmailSender
    {
        private readonly EmailSettings _settings;
        private readonly IHostEnvironment _env;

        public EmailSender(IOptions<EmailSettings> options, IHostEnvironment env)
        {
            _settings = options.Value;
            _env = env;
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

                using var client = new SmtpClient();
                if (_settings.UsePickupFolder && !string.IsNullOrWhiteSpace(_settings.PickupDirectory))
                {
                    // Resolve to absolute path under ContentRoot if relative provided
                    var dir = _settings.PickupDirectory;
                    if (!Path.IsPathRooted(dir))
                    {
                        // Prefer ContentRootPath (API project root)
                        var root = _env.ContentRootPath ?? Directory.GetCurrentDirectory();
                        dir = Path.Combine(root, dir);
                    }
                    Directory.CreateDirectory(dir);

                    client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                    client.PickupDirectoryLocation = dir;
                }
                else
                {
                    client.Host = _settings.SmtpHost;
                    client.Port = _settings.SmtpPort;
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPass);
                }

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
