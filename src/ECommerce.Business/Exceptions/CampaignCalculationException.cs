using System;

namespace ECommerce.Business.Exceptions
{
    /// <summary>
    /// Kampanya hesaplama işlemleri sırasında oluşan hataları temsil eder.
    /// Bu exception, kampanya indirimlerinin hesaplan(a)maması durumunda fırlatılır.
    /// </summary>
    public class CampaignCalculationException : Exception
    {
        /// <summary>
        /// Hatanın teknik detayları (loglama için)
        /// </summary>
        public string? TechnicalDetails { get; set; }

        /// <summary>
        /// Hatanın kullanıcıya gösterilecek mesajı
        /// </summary>
        public string UserFriendlyMessage { get; set; }

        public CampaignCalculationException(string userMessage, Exception? innerException = null)
            : base(userMessage, innerException)
        {
            UserFriendlyMessage = userMessage;
        }

        public CampaignCalculationException(string userMessage, string technicalDetails, Exception? innerException = null)
            : base(userMessage, innerException)
        {
            UserFriendlyMessage = userMessage;
            TechnicalDetails = technicalDetails;
        }
    }
}
