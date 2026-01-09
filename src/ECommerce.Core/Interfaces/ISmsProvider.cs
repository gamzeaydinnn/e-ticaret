using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// SMS gönderim servisi interface.
    /// NetGSM veya başka bir provider ile çalışabilir.
    /// SOLID: Dependency Inversion - Üst katman alt katmana bağımlı değil, interface'e bağımlı
    /// </summary>
    public interface ISmsProvider
    {
        /// <summary>
        /// SMS gönderir.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası (5xxxxxxxxx formatında)</param>
        /// <param name="message">SMS içeriği</param>
        /// <returns>Gönderim sonucu</returns>
        Task<SmsSendResult> SendSmsAsync(string phoneNumber, string message);

        /// <summary>
        /// OTP (One-Time Password) SMS gönderir.
        /// Yüksek öncelikli, 3 dakika içinde iletim garantisi.
        /// </summary>
        /// <param name="phoneNumber">Telefon numarası</param>
        /// <param name="code">OTP kodu</param>
        /// <returns>Gönderim sonucu</returns>
        Task<SmsSendResult> SendOtpAsync(string phoneNumber, string code);

        /// <summary>
        /// SMS kredisi/bakiye sorgular.
        /// </summary>
        /// <returns>Kalan kredi</returns>
        Task<decimal> GetBalanceAsync();
    }

    /// <summary>
    /// SMS gönderim sonucu
    /// </summary>
    public class SmsSendResult
    {
        /// <summary>Gönderim başarılı mı?</summary>
        public bool Success { get; set; }

        /// <summary>NetGSM/Provider'dan dönen kod</summary>
        public string? Code { get; set; }

        /// <summary>Görev ID'si (SMS durumu sorgulama için)</summary>
        public string? JobId { get; set; }

        /// <summary>Açıklama</summary>
        public string? Description { get; set; }

        /// <summary>Hata mesajı (başarısız ise)</summary>
        public string? ErrorMessage { get; set; }

        #region Factory Methods

        public static SmsSendResult SuccessResult(string jobId, string? code = null)
            => new()
            {
                Success = true,
                JobId = jobId,
                Code = code ?? "00",
                Description = "SMS başarıyla gönderildi"
            };

        public static SmsSendResult FailResult(string errorMessage, string? code = null)
            => new()
            {
                Success = false,
                ErrorMessage = errorMessage,
                Code = code
            };

        #endregion
    }
}
