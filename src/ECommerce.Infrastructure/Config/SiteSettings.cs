namespace ECommerce.Infrastructure.Config
{
    /// <summary>
    /// Site geneli ayarlar - Footer, iletişim bilgileri, sosyal medya vb.
    /// Gölköy Gurme'ye özgü bilgiler burada tutulur
    /// </summary>
    public class SiteSettings
    {
        /// <summary>
        /// Firma bilgileri
        /// </summary>
        public CompanyInfo Company { get; set; } = new CompanyInfo();

        /// <summary>
        /// İletişim bilgileri
        /// </summary>
        public ContactInfo Contact { get; set; } = new ContactInfo();

        /// <summary>
        /// Sosyal medya linkleri
        /// </summary>
        public SocialMediaLinks SocialMedia { get; set; } = new SocialMediaLinks();

        /// <summary>
        /// Footer özel ayarları
        /// </summary>
        public FooterSettings Footer { get; set; } = new FooterSettings();
    }

    public class CompanyInfo
    {
        public string Name { get; set; } = "Gölköy Gurme";
        public string ShortName { get; set; } = "Gölköy Gurme";
        public string LegalName { get; set; } = "Gölköy Gurme Market";
        public string Description { get; set; } = "Gölköy Gurme olarak, doğanın bize sunduğu en saf ve lezzetli ürünleri, en yüksek kalite standartlarında siz değerli müşterilerimize sunmayı amaçlıyoruz.";
        public string LogoUrl { get; set; } = "/images/golkoy-logo1.png";
        public string SecondaryLogoUrl { get; set; } = "/images/dogadan-sofranza-logo.png";
        public string CopyrightText { get; set; } = "Tüm haklar Gölköy Gurme Markete aittir.";
    }

    public class ContactInfo
    {
        public string Phone { get; set; } = "+90 533 478 30 72";
        public string PhoneDisplay { get; set; } = "+90 533 478 30 72";
        public string PhoneLabel { get; set; } = "Müşteri Hizmetleri";
        public string WhatsAppNumber { get; set; } = "905334783072";
        public string WhatsAppMessage { get; set; } = "Merhaba, sipariş hakkında bilgi almak istiyorum.";
        public string Email { get; set; } = "golturkbuku@golkoygurme.com.tr";
        public string EmailLabel { get; set; } = "Genel bilgi ve destek";
        public string Address { get; set; } = "Gölköy Mah. 67 Sokak No: 1/A Bodrum/Muğla";
        public string AddressLabel { get; set; } = "Merkez Ofis";
    }

    public class SocialMediaLinks
    {
        public string Facebook { get; set; } = "https://www.facebook.com/golkoygurmebodrum";
        public string Instagram { get; set; } = "https://www.instagram.com/golkoygurmebodrum?igsh=aWJwMHJsbXdjYmt4";
        public string Twitter { get; set; } = "";
        public string YouTube { get; set; } = "";
        public string LinkedIn { get; set; } = "";
    }

    public class FooterSettings
    {
        public bool ShowSecondaryLogo { get; set; } = true;
        public bool ShowSSLBadge { get; set; } = true;
        public bool ShowPaymentMethods { get; set; } = true;
        public string[] PaymentMethods { get; set; } = new[] { "VISA", "MC", "TROY" };
        public string[] SecurityFeatures { get; set; } = new[] 
        { 
            "SSL güvenli alışveriş", 
            "Güvenli ödeme sistemi", 
            "Müşteri memnuniyeti odaklı" 
        };
    }
}
