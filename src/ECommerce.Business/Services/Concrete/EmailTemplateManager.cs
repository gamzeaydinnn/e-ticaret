// ==========================================================================
// EmailTemplateManager.cs - Email Template Servisi Implementasyonu
// ==========================================================================
// Teslimat süreçleri için profesyonel, mobil uyumlu HTML email template'leri.
// Tüm template'ler responsive tasarıma sahiptir ve tüm email istemcilerinde
// düzgün görüntülenir.
// ==========================================================================

using System;
using System.Text;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Concrete
{
    /// <summary>
    /// Email template yönetim servisi.
    /// Profesyonel, mobil uyumlu HTML email şablonları oluşturur.
    /// </summary>
    public class EmailTemplateManager : IEmailTemplateService
    {
        private readonly ILogger<EmailTemplateManager> _logger;
        private readonly IConfiguration _configuration;

        // Şirket bilgileri
        private readonly string _companyName;
        private readonly string _companyLogo;
        private readonly string _primaryColor;
        private readonly string _supportEmail;
        private readonly string _supportPhone;
        private readonly string _websiteUrl;

        public EmailTemplateManager(
            ILogger<EmailTemplateManager> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            // Email template konfigürasyonunu yükle
            _companyName = _configuration["Email:CompanyName"] ?? "E-Ticaret";
            _companyLogo = _configuration["Email:LogoUrl"] ?? "/images/logo.png";
            _primaryColor = _configuration["Email:PrimaryColor"] ?? "#007bff";
            _supportEmail = _configuration["Email:SupportEmail"] ?? "destek@eticaret.com";
            _supportPhone = _configuration["Email:SupportPhone"] ?? "0850 XXX XX XX";
            _websiteUrl = _configuration["Email:WebsiteUrl"] ?? "https://eticaret.com";
        }

        #region Müşteri Email Template'leri

        /// <summary>
        /// Kurye atandı bildirimi email'i oluşturur
        /// </summary>
        public Task<EmailTemplate> GetCourierAssignedTemplateAsync(CourierAssignedEmailData data)
        {
            var subject = $"📦 Siparişiniz Yola Çıkmaya Hazır! - {data.OrderNumber}";

            var htmlBody = GetBaseTemplate()
                .Replace("{{CONTENT}}", $@"
                    <h1 style='color: {_primaryColor}; margin-bottom: 20px;'>
                        Merhaba {data.CustomerName}! 👋
                    </h1>
                    
                    <p style='font-size: 16px; line-height: 1.6;'>
                        Siparişiniz (<strong>{data.OrderNumber}</strong>) için kuryemiz atandı ve 
                        en kısa sürede size teslim edilecek!
                    </p>

                    <div style='background: #f8f9fa; border-radius: 12px; padding: 20px; margin: 25px 0;'>
                        <h3 style='margin-top: 0; color: #333;'>🚚 Kurye Bilgileri</h3>
                        <table style='width: 100%; font-size: 14px;'>
                            <tr>
                                <td style='padding: 8px 0; color: #666;'>Kurye Adı:</td>
                                <td style='padding: 8px 0; font-weight: bold;'>{data.CourierName}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; color: #666;'>Telefon:</td>
                                <td style='padding: 8px 0; font-weight: bold;'>
                                    <a href='tel:{data.CourierPhone}' style='color: {_primaryColor}; text-decoration: none;'>
                                        {data.CourierPhone}
                                    </a>
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; color: #666;'>Tahmini Teslimat:</td>
                                <td style='padding: 8px 0; font-weight: bold; color: #28a745;'>
                                    {data.EstimatedDeliveryTime:dd MMMM yyyy HH:mm}
                                </td>
                            </tr>
                        </table>
                    </div>

                    <div style='background: #e8f4ff; border-radius: 12px; padding: 20px; margin: 25px 0;'>
                        <h3 style='margin-top: 0; color: #333;'>📍 Teslimat Adresi</h3>
                        <p style='margin: 0; font-size: 14px;'>{data.DeliveryAddress}</p>
                    </div>

                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{data.TrackingUrl}' 
                           style='display: inline-block; background: {_primaryColor}; color: white; 
                                  padding: 15px 40px; border-radius: 8px; text-decoration: none; 
                                  font-weight: bold; font-size: 16px;'>
                            📍 Teslimatı Takip Et
                        </a>
                    </div>

                    <p style='font-size: 14px; color: #666; text-align: center;'>
                        Sorularınız için <a href='tel:{_supportPhone}' style='color: {_primaryColor};'>{_supportPhone}</a> 
                        numaralı hattımızı arayabilirsiniz.
                    </p>
                ");

            var textBody = $@"
Merhaba {data.CustomerName}!

Siparişiniz ({data.OrderNumber}) için kuryemiz atandı!

Kurye Bilgileri:
- Kurye Adı: {data.CourierName}
- Telefon: {data.CourierPhone}
- Tahmini Teslimat: {data.EstimatedDeliveryTime:dd MMMM yyyy HH:mm}

Teslimat Adresi:
{data.DeliveryAddress}

Teslimatı takip etmek için: {data.TrackingUrl}

Sorularınız için: {_supportPhone}
";

            return Task.FromResult(new EmailTemplate
            {
                Subject = subject,
                HtmlBody = htmlBody,
                TextBody = textBody,
                TemplateType = EmailTemplateType.CourierAssigned
            });
        }

        /// <summary>
        /// Kurye yola çıktı bildirimi email'i oluşturur
        /// </summary>
        public Task<EmailTemplate> GetCourierEnRouteTemplateAsync(CourierEnRouteEmailData data)
        {
            var subject = $"🚀 Kuryeniz Yola Çıktı! - {data.OrderNumber}";

            var htmlBody = GetBaseTemplate()
                .Replace("{{CONTENT}}", $@"
                    <h1 style='color: {_primaryColor}; margin-bottom: 20px;'>
                        Heyecanlı Haberler! 🎉
                    </h1>
                    
                    <p style='font-size: 18px; line-height: 1.6; text-align: center;'>
                        <strong>{data.CourierName}</strong> siparişinizi teslim etmek için yola çıktı!
                    </p>

                    <div style='background: linear-gradient(135deg, #28a745, #20c997); 
                                border-radius: 16px; padding: 30px; margin: 30px 0; text-align: center; color: white;'>
                        <p style='margin: 0 0 10px 0; font-size: 14px; opacity: 0.9;'>Tahmini Varış Süresi</p>
                        <p style='margin: 0; font-size: 48px; font-weight: bold;'>
                            ~{data.EstimatedMinutes} dk
                        </p>
                    </div>

                    <div style='background: #f8f9fa; border-radius: 12px; padding: 20px; margin: 25px 0;'>
                        <h3 style='margin-top: 0; color: #333;'>📍 Teslimat Adresi</h3>
                        <p style='margin: 0; font-size: 14px;'>{data.DeliveryAddress}</p>
                    </div>

                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{data.TrackingUrl}' 
                           style='display: inline-block; background: {_primaryColor}; color: white; 
                                  padding: 15px 40px; border-radius: 8px; text-decoration: none; 
                                  font-weight: bold; font-size: 16px;'>
                            🗺️ Canlı Takip
                        </a>
                    </div>

                    <p style='font-size: 14px; color: #666; text-align: center;'>
                        💡 <strong>İpucu:</strong> Teslimat sırasında evde olduğunuzdan emin olun.
                    </p>
                ");

            var textBody = $@"
Heyecanlı Haberler!

{data.CourierName} siparişinizi ({data.OrderNumber}) teslim etmek için yola çıktı!

Tahmini Varış: Yaklaşık {data.EstimatedMinutes} dakika

Teslimat Adresi:
{data.DeliveryAddress}

Canlı takip için: {data.TrackingUrl}
";

            return Task.FromResult(new EmailTemplate
            {
                Subject = subject,
                HtmlBody = htmlBody,
                TextBody = textBody,
                TemplateType = EmailTemplateType.CourierEnRoute
            });
        }

        /// <summary>
        /// Teslimat tamamlandı bildirimi email'i oluşturur
        /// </summary>
        public Task<EmailTemplate> GetDeliveryCompletedTemplateAsync(DeliveryCompletedEmailData data)
        {
            var subject = $"✅ Siparişiniz Teslim Edildi! - {data.OrderNumber}";

            var itemsHtml = new StringBuilder();
            foreach (var item in data.OrderItems)
            {
                itemsHtml.Append($@"
                    <tr>
                        <td style='padding: 12px; border-bottom: 1px solid #eee;'>{item.ProductName}</td>
                        <td style='padding: 12px; border-bottom: 1px solid #eee; text-align: center;'>{item.Quantity}</td>
                        <td style='padding: 12px; border-bottom: 1px solid #eee; text-align: right;'>{item.Price:C}</td>
                    </tr>
                ");
            }

            var proofHtml = "";
            if (!string.IsNullOrEmpty(data.ProofOfDeliveryUrl))
            {
                proofHtml = $@"
                    <div style='margin: 20px 0;'>
                        <p style='font-size: 14px; color: #666;'>📷 Teslimat Fotoğrafı:</p>
                        <img src='{data.ProofOfDeliveryUrl}' alt='Teslimat kanıtı' 
                             style='max-width: 100%; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'/>
                    </div>
                ";
            }

            var htmlBody = GetBaseTemplate()
                .Replace("{{CONTENT}}", $@"
                    <div style='text-align: center; margin-bottom: 30px;'>
                        <div style='font-size: 64px; margin-bottom: 10px;'>🎉</div>
                        <h1 style='color: #28a745; margin: 0;'>Teslim Edildi!</h1>
                    </div>
                    
                    <p style='font-size: 16px; line-height: 1.6; text-align: center;'>
                        Merhaba <strong>{data.CustomerName}</strong>,<br/>
                        Siparişiniz başarıyla teslim edilmiştir.
                    </p>

                    <div style='background: #f8f9fa; border-radius: 12px; padding: 20px; margin: 25px 0;'>
                        <table style='width: 100%; font-size: 14px;'>
                            <tr>
                                <td style='padding: 8px 0; color: #666;'>Sipariş No:</td>
                                <td style='padding: 8px 0; font-weight: bold;'>{data.OrderNumber}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; color: #666;'>Teslim Tarihi:</td>
                                <td style='padding: 8px 0; font-weight: bold;'>{data.DeliveredAt:dd MMMM yyyy HH:mm}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; color: #666;'>Teslim Alan:</td>
                                <td style='padding: 8px 0; font-weight: bold;'>{data.ReceiverName}</td>
                            </tr>
                        </table>
                    </div>

                    {proofHtml}

                    <h3 style='color: #333; margin-top: 30px;'>📦 Sipariş Özeti</h3>
                    <table style='width: 100%; border-collapse: collapse; margin: 15px 0;'>
                        <thead>
                            <tr style='background: #f8f9fa;'>
                                <th style='padding: 12px; text-align: left;'>Ürün</th>
                                <th style='padding: 12px; text-align: center;'>Adet</th>
                                <th style='padding: 12px; text-align: right;'>Fiyat</th>
                            </tr>
                        </thead>
                        <tbody>
                            {itemsHtml}
                        </tbody>
                        <tfoot>
                            <tr style='background: #28a745; color: white;'>
                                <td colspan='2' style='padding: 12px; font-weight: bold;'>TOPLAM</td>
                                <td style='padding: 12px; text-align: right; font-weight: bold;'>{data.TotalAmount:C}</td>
                            </tr>
                        </tfoot>
                    </table>

                    <div style='text-align: center; margin: 30px 0;'>
                        <p style='font-size: 16px; margin-bottom: 15px;'>
                            Deneyiminizi değerlendirin! ⭐
                        </p>
                        <a href='{data.RatingUrl}' 
                           style='display: inline-block; background: {_primaryColor}; color: white; 
                                  padding: 15px 40px; border-radius: 8px; text-decoration: none; 
                                  font-weight: bold; font-size: 16px;'>
                            Değerlendir
                        </a>
                    </div>
                ");

            var textBody = $@"
Siparişiniz Teslim Edildi!

Merhaba {data.CustomerName},
Siparişiniz başarıyla teslim edilmiştir.

Sipariş No: {data.OrderNumber}
Teslim Tarihi: {data.DeliveredAt:dd MMMM yyyy HH:mm}
Teslim Alan: {data.ReceiverName}
Toplam: {data.TotalAmount:C}

Değerlendirmek için: {data.RatingUrl}

Bizi tercih ettiğiniz için teşekkür ederiz!
";

            return Task.FromResult(new EmailTemplate
            {
                Subject = subject,
                HtmlBody = htmlBody,
                TextBody = textBody,
                TemplateType = EmailTemplateType.DeliveryCompleted
            });
        }

        /// <summary>
        /// Teslimat başarısız bildirimi email'i oluşturur
        /// </summary>
        public Task<EmailTemplate> GetDeliveryFailedTemplateAsync(DeliveryFailedEmailData data)
        {
            var subject = $"⚠️ Teslimat Gerçekleştirilemedi - {data.OrderNumber}";

            var htmlBody = GetBaseTemplate()
                .Replace("{{CONTENT}}", $@"
                    <div style='text-align: center; margin-bottom: 30px;'>
                        <div style='font-size: 64px; margin-bottom: 10px;'>😔</div>
                        <h1 style='color: #dc3545; margin: 0;'>Teslimat Başarısız</h1>
                    </div>
                    
                    <p style='font-size: 16px; line-height: 1.6;'>
                        Merhaba <strong>{data.CustomerName}</strong>,<br/>
                        Maalesef siparişinizi (<strong>{data.OrderNumber}</strong>) teslim edemedik.
                    </p>

                    <div style='background: #fff3cd; border: 1px solid #ffc107; border-radius: 12px; 
                                padding: 20px; margin: 25px 0;'>
                        <h3 style='margin-top: 0; color: #856404;'>⚠️ Başarısızlık Nedeni</h3>
                        <p style='margin: 0; font-size: 14px; color: #856404;'>{data.FailureReason}</p>
                        <p style='margin: 10px 0 0 0; font-size: 12px; color: #856404;'>
                            Tarih: {data.FailedAt:dd MMMM yyyy HH:mm}
                        </p>
                    </div>

                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{data.RescheduleUrl}' 
                           style='display: inline-block; background: #28a745; color: white; 
                                  padding: 15px 40px; border-radius: 8px; text-decoration: none; 
                                  font-weight: bold; font-size: 16px;'>
                            🗓️ Yeniden Planla
                        </a>
                    </div>

                    <div style='background: #f8f9fa; border-radius: 12px; padding: 20px; margin: 25px 0;'>
                        <h3 style='margin-top: 0; color: #333;'>📞 Destek</h3>
                        <p style='margin: 0 0 10px 0; font-size: 14px;'>
                            Yardıma mı ihtiyacınız var? Bize ulaşın:
                        </p>
                        <p style='margin: 0; font-size: 14px;'>
                            📞 <a href='tel:{data.SupportPhone}' style='color: {_primaryColor};'>{data.SupportPhone}</a><br/>
                            ✉️ <a href='mailto:{data.SupportEmail}' style='color: {_primaryColor};'>{data.SupportEmail}</a>
                        </p>
                    </div>
                ");

            var textBody = $@"
Teslimat Başarısız

Merhaba {data.CustomerName},
Maalesef siparişinizi ({data.OrderNumber}) teslim edemedik.

Başarısızlık Nedeni: {data.FailureReason}
Tarih: {data.FailedAt:dd MMMM yyyy HH:mm}

Yeniden planlamak için: {data.RescheduleUrl}

Destek:
Telefon: {data.SupportPhone}
E-posta: {data.SupportEmail}
";

            return Task.FromResult(new EmailTemplate
            {
                Subject = subject,
                HtmlBody = htmlBody,
                TextBody = textBody,
                TemplateType = EmailTemplateType.DeliveryFailed
            });
        }

        /// <summary>
        /// Teslimat yeniden programlandı bildirimi email'i oluşturur
        /// </summary>
        public Task<EmailTemplate> GetDeliveryRescheduledTemplateAsync(DeliveryRescheduledEmailData data)
        {
            var subject = $"📅 Teslimatınız Yeniden Planlandı - {data.OrderNumber}";

            var htmlBody = GetBaseTemplate()
                .Replace("{{CONTENT}}", $@"
                    <h1 style='color: {_primaryColor}; margin-bottom: 20px;'>
                        Teslimat Yeniden Planlandı 📅
                    </h1>
                    
                    <p style='font-size: 16px; line-height: 1.6;'>
                        Merhaba <strong>{data.CustomerName}</strong>,<br/>
                        Siparişiniz (<strong>{data.OrderNumber}</strong>) için yeni teslimat tarihi belirlendi.
                    </p>

                    <div style='background: linear-gradient(135deg, {_primaryColor}, #0056b3); 
                                border-radius: 16px; padding: 30px; margin: 30px 0; text-align: center; color: white;'>
                        <p style='margin: 0 0 10px 0; font-size: 14px; opacity: 0.9;'>Yeni Teslimat Tarihi</p>
                        <p style='margin: 0; font-size: 32px; font-weight: bold;'>
                            {data.NewDeliveryDate:dd MMMM yyyy}
                        </p>
                        <p style='margin: 10px 0 0 0; font-size: 18px;'>
                            ⏰ {data.TimeSlot}
                        </p>
                    </div>

                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{data.TrackingUrl}' 
                           style='display: inline-block; background: {_primaryColor}; color: white; 
                                  padding: 15px 40px; border-radius: 8px; text-decoration: none; 
                                  font-weight: bold; font-size: 16px;'>
                            📍 Takip Et
                        </a>
                    </div>

                    <p style='font-size: 14px; color: #666; text-align: center;'>
                        💡 Belirtilen tarih ve saat diliminde adresinizde olduğunuzdan emin olun.
                    </p>
                ");

            var textBody = $@"
Teslimat Yeniden Planlandı

Merhaba {data.CustomerName},
Siparişiniz ({data.OrderNumber}) için yeni teslimat tarihi belirlendi.

Yeni Teslimat: {data.NewDeliveryDate:dd MMMM yyyy}
Saat Dilimi: {data.TimeSlot}

Takip için: {data.TrackingUrl}
";

            return Task.FromResult(new EmailTemplate
            {
                Subject = subject,
                HtmlBody = htmlBody,
                TextBody = textBody,
                TemplateType = EmailTemplateType.DeliveryRescheduled
            });
        }

        #endregion

        #region Kurye Email Template'leri

        /// <summary>
        /// Kuryeye yeni görev atandı bildirimi email'i oluşturur
        /// </summary>
        public Task<EmailTemplate> GetNewTaskAssignedToCourrierTemplateAsync(NewTaskEmailData data)
        {
            var subject = $"🆕 Yeni Teslimat Görevi - {data.OrderNumber}";

            var specialInstructionsHtml = "";
            if (!string.IsNullOrEmpty(data.SpecialInstructions))
            {
                specialInstructionsHtml = $@"
                    <div style='background: #fff3cd; border-radius: 8px; padding: 15px; margin: 15px 0;'>
                        <strong>📝 Özel Talimatlar:</strong><br/>
                        {data.SpecialInstructions}
                    </div>
                ";
            }

            var htmlBody = GetBaseTemplate()
                .Replace("{{CONTENT}}", $@"
                    <h1 style='color: {_primaryColor}; margin-bottom: 20px;'>
                        Yeni Görev Atandı! 📦
                    </h1>
                    
                    <p style='font-size: 16px; line-height: 1.6;'>
                        Merhaba <strong>{data.CourierName}</strong>,<br/>
                        Size yeni bir teslimat görevi atandı.
                    </p>

                    <div style='background: #e8f4ff; border-radius: 12px; padding: 20px; margin: 25px 0;'>
                        <h3 style='margin-top: 0; color: {_primaryColor};'>📋 Görev Detayları</h3>
                        <table style='width: 100%; font-size: 14px;'>
                            <tr>
                                <td style='padding: 8px 0; color: #666;'>Sipariş No:</td>
                                <td style='padding: 8px 0; font-weight: bold;'>{data.OrderNumber}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; color: #666;'>Müşteri:</td>
                                <td style='padding: 8px 0; font-weight: bold;'>{data.CustomerName}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; color: #666;'>Telefon:</td>
                                <td style='padding: 8px 0; font-weight: bold;'>
                                    <a href='tel:{data.CustomerPhone}' style='color: {_primaryColor};'>{data.CustomerPhone}</a>
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; color: #666;'>Paket Sayısı:</td>
                                <td style='padding: 8px 0; font-weight: bold;'>{data.PackageCount} adet</td>
                            </tr>
                            {(data.TotalWeight.HasValue ? $@"
                            <tr>
                                <td style='padding: 8px 0; color: #666;'>Toplam Ağırlık:</td>
                                <td style='padding: 8px 0; font-weight: bold;'>{data.TotalWeight:F2} kg</td>
                            </tr>
                            " : "")}
                        </table>
                    </div>

                    <div style='background: #f8f9fa; border-radius: 12px; padding: 20px; margin: 25px 0;'>
                        <h3 style='margin-top: 0; color: #333;'>📍 Teslimat Adresi</h3>
                        <p style='margin: 0 0 15px 0; font-size: 14px;'>{data.DeliveryAddress}</p>
                        <a href='https://maps.google.com/?q={Uri.EscapeDataString(data.DeliveryAddress)}' 
                           style='color: {_primaryColor}; text-decoration: none; font-size: 14px;'>
                            🗺️ Haritada Göster
                        </a>
                    </div>

                    {specialInstructionsHtml}

                    <div style='background: #dc3545; color: white; border-radius: 12px; padding: 20px; margin: 25px 0;'>
                        <h3 style='margin-top: 0;'>⏰ Zaman Çizelgesi</h3>
                        <p style='margin: 0 0 10px 0;'>
                            <strong>Teslim Alım Son Tarih:</strong> {data.PickupDeadline:dd.MM.yyyy HH:mm}
                        </p>
                        <p style='margin: 0;'>
                            <strong>Teslimat Son Tarih:</strong> {data.DeliveryDeadline:dd.MM.yyyy HH:mm}
                        </p>
                    </div>
                ");

            var textBody = $@"
Yeni Görev Atandı!

Merhaba {data.CourierName},
Size yeni bir teslimat görevi atandı.

Sipariş No: {data.OrderNumber}
Müşteri: {data.CustomerName}
Telefon: {data.CustomerPhone}
Paket Sayısı: {data.PackageCount} adet

Teslimat Adresi:
{data.DeliveryAddress}

{(string.IsNullOrEmpty(data.SpecialInstructions) ? "" : $"Özel Talimatlar: {data.SpecialInstructions}")}

Teslim Alım Son Tarih: {data.PickupDeadline:dd.MM.yyyy HH:mm}
Teslimat Son Tarih: {data.DeliveryDeadline:dd.MM.yyyy HH:mm}
";

            return Task.FromResult(new EmailTemplate
            {
                Subject = subject,
                HtmlBody = htmlBody,
                TextBody = textBody,
                TemplateType = EmailTemplateType.NewTaskAssigned
            });
        }

        /// <summary>
        /// Kurye günlük özet raporu email'i oluşturur
        /// </summary>
        public Task<EmailTemplate> GetCourierDailySummaryTemplateAsync(CourierDailySummaryData data)
        {
            var subject = $"📊 Günlük Özet - {data.Date:dd MMMM yyyy}";
            var successRate = data.TotalDeliveries > 0 
                ? (data.SuccessfulDeliveries * 100.0 / data.TotalDeliveries) 
                : 0;

            var htmlBody = GetBaseTemplate()
                .Replace("{{CONTENT}}", $@"
                    <h1 style='color: {_primaryColor}; margin-bottom: 20px;'>
                        Günlük Performans Özeti 📊
                    </h1>
                    
                    <p style='font-size: 16px; line-height: 1.6;'>
                        Merhaba <strong>{data.CourierName}</strong>,<br/>
                        İşte {data.Date:dd MMMM yyyy} tarihli performans özetin:
                    </p>

                    <div style='display: flex; flex-wrap: wrap; gap: 15px; margin: 25px 0;'>
                        <div style='flex: 1; min-width: 120px; background: #28a745; color: white; 
                                    border-radius: 12px; padding: 20px; text-align: center;'>
                            <p style='margin: 0; font-size: 14px; opacity: 0.9;'>Başarılı</p>
                            <p style='margin: 5px 0 0 0; font-size: 32px; font-weight: bold;'>{data.SuccessfulDeliveries}</p>
                        </div>
                        <div style='flex: 1; min-width: 120px; background: #dc3545; color: white; 
                                    border-radius: 12px; padding: 20px; text-align: center;'>
                            <p style='margin: 0; font-size: 14px; opacity: 0.9;'>Başarısız</p>
                            <p style='margin: 5px 0 0 0; font-size: 32px; font-weight: bold;'>{data.FailedDeliveries}</p>
                        </div>
                        <div style='flex: 1; min-width: 120px; background: {_primaryColor}; color: white; 
                                    border-radius: 12px; padding: 20px; text-align: center;'>
                            <p style='margin: 0; font-size: 14px; opacity: 0.9;'>Toplam</p>
                            <p style='margin: 5px 0 0 0; font-size: 32px; font-weight: bold;'>{data.TotalDeliveries}</p>
                        </div>
                    </div>

                    <div style='background: #f8f9fa; border-radius: 12px; padding: 20px; margin: 25px 0;'>
                        <h3 style='margin-top: 0; color: #333;'>📈 Detaylı İstatistikler</h3>
                        <table style='width: 100%; font-size: 14px;'>
                            <tr>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee;'>Başarı Oranı:</td>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee; font-weight: bold; text-align: right;'>
                                    %{successRate:F1}
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee;'>Toplam Mesafe:</td>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee; font-weight: bold; text-align: right;'>
                                    {data.TotalDistance:F1} km
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee;'>Aktif Süre:</td>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee; font-weight: bold; text-align: right;'>
                                    {data.TotalActiveTime:hh\\:mm} saat
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee;'>Ortalama Puan:</td>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee; font-weight: bold; text-align: right;'>
                                    ⭐ {data.AverageRating:F1}/5
                                </td>
                            </tr>
                        </table>
                    </div>

                    <div style='background: linear-gradient(135deg, #28a745, #20c997); 
                                border-radius: 16px; padding: 25px; margin: 25px 0; text-align: center; color: white;'>
                        <p style='margin: 0 0 5px 0; font-size: 14px; opacity: 0.9;'>Bugünkü Kazanç</p>
                        <p style='margin: 0; font-size: 40px; font-weight: bold;'>{data.EarningsToday:C}</p>
                    </div>

                    <p style='font-size: 14px; color: #666; text-align: center;'>
                        Harika bir iş çıkardın! 💪 Yarın da böyle devam et.
                    </p>
                ");

            var textBody = $@"
Günlük Performans Özeti

Merhaba {data.CourierName},
İşte {data.Date:dd MMMM yyyy} tarihli performans özetin:

Teslimatlar:
- Başarılı: {data.SuccessfulDeliveries}
- Başarısız: {data.FailedDeliveries}
- Toplam: {data.TotalDeliveries}
- Başarı Oranı: %{successRate:F1}

Diğer İstatistikler:
- Toplam Mesafe: {data.TotalDistance:F1} km
- Aktif Süre: {data.TotalActiveTime:hh\\:mm} saat
- Ortalama Puan: {data.AverageRating:F1}/5

Bugünkü Kazanç: {data.EarningsToday:C}

Harika bir iş çıkardın!
";

            return Task.FromResult(new EmailTemplate
            {
                Subject = subject,
                HtmlBody = htmlBody,
                TextBody = textBody,
                TemplateType = EmailTemplateType.CourierDailySummary
            });
        }

        #endregion

        #region Admin Email Template'leri

        /// <summary>
        /// Admin için başarısız teslimat uyarı email'i oluşturur
        /// </summary>
        public Task<EmailTemplate> GetAdminDeliveryAlertTemplateAsync(AdminDeliveryAlertData data)
        {
            var subject = $"🚨 {data.AlertType} - {data.OrderNumber}";

            var htmlBody = GetBaseTemplate()
                .Replace("{{CONTENT}}", $@"
                    <div style='background: #dc3545; color: white; border-radius: 12px; padding: 20px; margin-bottom: 25px;'>
                        <h1 style='margin: 0;'>🚨 {data.AlertType}</h1>
                    </div>
                    
                    <div style='background: #fff3cd; border: 1px solid #ffc107; border-radius: 12px; 
                                padding: 20px; margin: 25px 0;'>
                        <p style='margin: 0; font-size: 16px;'>{data.AlertMessage}</p>
                    </div>

                    <div style='background: #f8f9fa; border-radius: 12px; padding: 20px; margin: 25px 0;'>
                        <h3 style='margin-top: 0; color: #333;'>📋 Detaylar</h3>
                        <table style='width: 100%; font-size: 14px;'>
                            <tr>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee;'>Sipariş No:</td>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee; font-weight: bold;'>{data.OrderNumber}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee;'>Kurye:</td>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee; font-weight: bold;'>{data.CourierName}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee;'>Müşteri:</td>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee; font-weight: bold;'>{data.CustomerName}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px 0;'>Zaman:</td>
                                <td style='padding: 10px 0; font-weight: bold;'>{data.OccurredAt:dd.MM.yyyy HH:mm}</td>
                            </tr>
                        </table>
                    </div>

                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{data.ActionUrl}' 
                           style='display: inline-block; background: #dc3545; color: white; 
                                  padding: 15px 40px; border-radius: 8px; text-decoration: none; 
                                  font-weight: bold; font-size: 16px;'>
                            🔧 Aksiyon Al
                        </a>
                    </div>
                ");

            var textBody = $@"
🚨 {data.AlertType}

{data.AlertMessage}

Detaylar:
- Sipariş No: {data.OrderNumber}
- Kurye: {data.CourierName}
- Müşteri: {data.CustomerName}
- Zaman: {data.OccurredAt:dd.MM.yyyy HH:mm}

Aksiyon için: {data.ActionUrl}
";

            return Task.FromResult(new EmailTemplate
            {
                Subject = subject,
                HtmlBody = htmlBody,
                TextBody = textBody,
                TemplateType = EmailTemplateType.AdminDeliveryAlert
            });
        }

        /// <summary>
        /// Admin günlük teslimat raporu email'i oluşturur
        /// </summary>
        public Task<EmailTemplate> GetAdminDailyReportTemplateAsync(AdminDailyReportData data)
        {
            var subject = $"📊 Günlük Teslimat Raporu - {data.Date:dd MMMM yyyy}";

            var topCouriersHtml = new StringBuilder();
            foreach (var courier in data.TopCouriers)
            {
                topCouriersHtml.Append($@"
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{courier.CourierName}</td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: center;'>{courier.DeliveryCount}</td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: center;'>%{courier.SuccessRate:F1}</td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: center;'>⭐ {courier.AverageRating:F1}</td>
                    </tr>
                ");
            }

            var failureReasonsHtml = new StringBuilder();
            foreach (var reason in data.FailureReasons)
            {
                failureReasonsHtml.Append($@"
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{reason.Reason}</td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: center;'>{reason.Count}</td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: center;'>%{reason.Percentage:F1}</td>
                    </tr>
                ");
            }

            var htmlBody = GetBaseTemplate()
                .Replace("{{CONTENT}}", $@"
                    <h1 style='color: {_primaryColor}; margin-bottom: 20px;'>
                        Günlük Teslimat Raporu 📊
                    </h1>
                    
                    <p style='font-size: 16px; color: #666;'>
                        {data.Date:dd MMMM yyyy dddd} - Teslimat Özeti
                    </p>

                    <div style='display: flex; flex-wrap: wrap; gap: 15px; margin: 25px 0;'>
                        <div style='flex: 1; min-width: 140px; background: {_primaryColor}; color: white; 
                                    border-radius: 12px; padding: 20px; text-align: center;'>
                            <p style='margin: 0; font-size: 14px; opacity: 0.9;'>Toplam Sipariş</p>
                            <p style='margin: 5px 0 0 0; font-size: 32px; font-weight: bold;'>{data.TotalOrders}</p>
                        </div>
                        <div style='flex: 1; min-width: 140px; background: #28a745; color: white; 
                                    border-radius: 12px; padding: 20px; text-align: center;'>
                            <p style='margin: 0; font-size: 14px; opacity: 0.9;'>Teslim Edilen</p>
                            <p style='margin: 5px 0 0 0; font-size: 32px; font-weight: bold;'>{data.DeliveredOrders}</p>
                        </div>
                        <div style='flex: 1; min-width: 140px; background: #dc3545; color: white; 
                                    border-radius: 12px; padding: 20px; text-align: center;'>
                            <p style='margin: 0; font-size: 14px; opacity: 0.9;'>Başarısız</p>
                            <p style='margin: 5px 0 0 0; font-size: 32px; font-weight: bold;'>{data.FailedOrders}</p>
                        </div>
                        <div style='flex: 1; min-width: 140px; background: #ffc107; color: #333; 
                                    border-radius: 12px; padding: 20px; text-align: center;'>
                            <p style='margin: 0; font-size: 14px; opacity: 0.9;'>Bekleyen</p>
                            <p style='margin: 5px 0 0 0; font-size: 32px; font-weight: bold;'>{data.PendingOrders}</p>
                        </div>
                    </div>

                    <div style='background: #f8f9fa; border-radius: 12px; padding: 20px; margin: 25px 0;'>
                        <h3 style='margin-top: 0; color: #333;'>📈 Performans Metrikleri</h3>
                        <table style='width: 100%; font-size: 14px;'>
                            <tr>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee;'>Başarı Oranı:</td>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee; font-weight: bold; text-align: right;'>
                                    %{data.SuccessRate:F1}
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee;'>Ortalama Teslimat Süresi:</td>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee; font-weight: bold; text-align: right;'>
                                    {data.AverageDeliveryTime:F0} dk
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee;'>Aktif Kurye Sayısı:</td>
                                <td style='padding: 10px 0; border-bottom: 1px solid #eee; font-weight: bold; text-align: right;'>
                                    {data.ActiveCouriers}
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 10px 0;'>Toplam Ciro:</td>
                                <td style='padding: 10px 0; font-weight: bold; text-align: right; color: #28a745;'>
                                    {data.TotalRevenue:C}
                                </td>
                            </tr>
                        </table>
                    </div>

                    {(data.TopCouriers.Count > 0 ? $@"
                    <h3 style='color: #333;'>🏆 En İyi Kuryeler</h3>
                    <table style='width: 100%; border-collapse: collapse; margin: 15px 0;'>
                        <thead>
                            <tr style='background: #f8f9fa;'>
                                <th style='padding: 12px; text-align: left;'>Kurye</th>
                                <th style='padding: 12px; text-align: center;'>Teslimat</th>
                                <th style='padding: 12px; text-align: center;'>Başarı</th>
                                <th style='padding: 12px; text-align: center;'>Puan</th>
                            </tr>
                        </thead>
                        <tbody>
                            {topCouriersHtml}
                        </tbody>
                    </table>
                    " : "")}

                    {(data.FailureReasons.Count > 0 ? $@"
                    <h3 style='color: #dc3545;'>⚠️ Başarısızlık Nedenleri</h3>
                    <table style='width: 100%; border-collapse: collapse; margin: 15px 0;'>
                        <thead>
                            <tr style='background: #f8f9fa;'>
                                <th style='padding: 12px; text-align: left;'>Neden</th>
                                <th style='padding: 12px; text-align: center;'>Sayı</th>
                                <th style='padding: 12px; text-align: center;'>Oran</th>
                            </tr>
                        </thead>
                        <tbody>
                            {failureReasonsHtml}
                        </tbody>
                    </table>
                    " : "")}
                ");

            var textBody = $@"
Günlük Teslimat Raporu - {data.Date:dd MMMM yyyy}

Özet:
- Toplam Sipariş: {data.TotalOrders}
- Teslim Edilen: {data.DeliveredOrders}
- Başarısız: {data.FailedOrders}
- Bekleyen: {data.PendingOrders}

Performans:
- Başarı Oranı: %{data.SuccessRate:F1}
- Ortalama Teslimat Süresi: {data.AverageDeliveryTime:F0} dk
- Aktif Kurye: {data.ActiveCouriers}
- Toplam Ciro: {data.TotalRevenue:C}
";

            return Task.FromResult(new EmailTemplate
            {
                Subject = subject,
                HtmlBody = htmlBody,
                TextBody = textBody,
                TemplateType = EmailTemplateType.AdminDailyReport
            });
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Temel email template'ini döndürür (responsive, mobil uyumlu)
        /// </summary>
        private string GetBaseTemplate()
        {
            return $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta http-equiv='X-UA-Compatible' content='IE=edge'>
    <title>Email</title>
    <!--[if mso]>
    <noscript>
        <xml>
            <o:OfficeDocumentSettings>
                <o:PixelsPerInch>96</o:PixelsPerInch>
            </o:OfficeDocumentSettings>
        </xml>
    </noscript>
    <![endif]-->
    <style>
        /* Reset styles */
        body, table, td, p, a, li, blockquote {{
            -webkit-text-size-adjust: 100%;
            -ms-text-size-adjust: 100%;
        }}
        table, td {{
            mso-table-lspace: 0pt;
            mso-table-rspace: 0pt;
        }}
        img {{
            -ms-interpolation-mode: bicubic;
            border: 0;
            height: auto;
            line-height: 100%;
            outline: none;
            text-decoration: none;
        }}
        
        /* Responsive styles */
        @media screen and (max-width: 600px) {{
            .email-container {{
                width: 100% !important;
                padding: 10px !important;
            }}
            .mobile-padding {{
                padding: 15px !important;
            }}
            .mobile-center {{
                text-align: center !important;
            }}
            .mobile-full-width {{
                width: 100% !important;
                display: block !important;
            }}
        }}
    </style>
</head>
<body style='margin: 0; padding: 0; background-color: #f4f4f4; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif;'>
    
    <!-- Email Container -->
    <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='background-color: #f4f4f4;'>
        <tr>
            <td style='padding: 20px 0;'>
                <table role='presentation' cellspacing='0' cellpadding='0' border='0' 
                       class='email-container' 
                       style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>
                    
                    <!-- Header -->
                    <tr>
                        <td style='padding: 30px; text-align: center; background: linear-gradient(135deg, {_primaryColor}, #0056b3); border-radius: 16px 16px 0 0;'>
                            <img src='{_companyLogo}' alt='{_companyName}' 
                                 style='max-height: 50px; max-width: 200px;'
                                 onerror=""this.style.display='none'""/>
                            <h2 style='color: white; margin: 10px 0 0 0; font-size: 24px;'>{_companyName}</h2>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td class='mobile-padding' style='padding: 30px;'>
                            {{{{CONTENT}}}}
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style='padding: 20px 30px; background-color: #f8f9fa; border-radius: 0 0 16px 16px; text-align: center;'>
                            <p style='margin: 0 0 10px 0; font-size: 14px; color: #666;'>
                                Bu email {_companyName} tarafından gönderilmiştir.
                            </p>
                            <p style='margin: 0; font-size: 12px; color: #999;'>
                                📞 {_supportPhone} | ✉️ {_supportEmail}
                            </p>
                            <p style='margin: 10px 0 0 0; font-size: 12px; color: #999;'>
                                <a href='{_websiteUrl}' style='color: {_primaryColor}; text-decoration: none;'>{_websiteUrl}</a>
                            </p>
                        </td>
                    </tr>
                    
                </table>
            </td>
        </tr>
    </table>
    
</body>
</html>
";
        }

        #endregion
    }
}
