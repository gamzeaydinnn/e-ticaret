using ECommerce.Infrastructure.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Site ayarları API'si - Footer, iletişim bilgileri vb.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SiteSettingsController : ControllerBase
    {
        private readonly SiteSettings _siteSettings;
        private readonly ILogger<SiteSettingsController> _logger;

        public SiteSettingsController(
            IOptions<SiteSettings> siteSettings,
            ILogger<SiteSettingsController> logger)
        {
            _siteSettings = siteSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Tüm site ayarlarını getirir
        /// </summary>
        [HttpGet]
        public ActionResult<SiteSettings> GetSiteSettings()
        {
            return Ok(_siteSettings);
        }

        /// <summary>
        /// Sadece footer için gereken verileri getirir
        /// </summary>
        [HttpGet("footer")]
        public ActionResult<FooterData> GetFooterData()
        {
            var footerData = new FooterData
            {
                Company = _siteSettings.Company,
                Contact = _siteSettings.Contact,
                SocialMedia = _siteSettings.SocialMedia,
                Footer = _siteSettings.Footer
            };

            return Ok(footerData);
        }

        /// <summary>
        /// Firma bilgilerini getirir
        /// </summary>
        [HttpGet("company")]
        public ActionResult<CompanyInfo> GetCompanyInfo()
        {
            return Ok(_siteSettings.Company);
        }

        /// <summary>
        /// İletişim bilgilerini getirir
        /// </summary>
        [HttpGet("contact")]
        public ActionResult<ContactInfo> GetContactInfo()
        {
            return Ok(_siteSettings.Contact);
        }
    }

    /// <summary>
    /// Footer için özel DTO
    /// </summary>
    public class FooterData
    {
        public CompanyInfo Company { get; set; } = new();
        public ContactInfo Contact { get; set; } = new();
        public SocialMediaLinks SocialMedia { get; set; } = new();
        public FooterSettings Footer { get; set; } = new();
    }
}
