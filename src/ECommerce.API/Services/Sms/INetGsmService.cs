namespace ECommerce.API.Services.Sms;

/// <summary>
/// NetGSM SMS gönderim servisi interface
/// </summary>
public interface INetGsmService
{
    /// <summary>
    /// SMS gönderir
    /// </summary>
    /// <param name="phoneNumber">Telefon numarası (5XXXXXXXXX formatında)</param>
    /// <param name="message">SMS içeriği</param>
    /// <returns>Başarılı ise true, değilse false</returns>
    Task<SmsResult> SendSmsAsync(string phoneNumber, string message);
    
    /// <summary>
    /// Bakiye sorgular
    /// </summary>
    /// <returns>Kalan SMS kredisi</returns>
    Task<decimal> GetBalanceAsync();
}

/// <summary>
/// SMS gönderim sonucu
/// </summary>
public class SmsResult
{
    public bool Success { get; set; }
    public string? Code { get; set; }
    public string? JobId { get; set; }
    public string? Description { get; set; }
    public string? ErrorMessage { get; set; }
}
