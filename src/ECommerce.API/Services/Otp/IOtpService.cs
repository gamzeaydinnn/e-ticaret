namespace ECommerce.API.Services.Otp;

/// <summary>
/// OTP (One-Time Password) servisi interface
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Yeni OTP oluşturur ve SMS gönderir
    /// </summary>
    /// <param name="phoneNumber">Telefon numarası</param>
    /// <returns>OTP gönderim sonucu</returns>
    Task<OtpSendResult> SendOtpAsync(string phoneNumber);

    /// <summary>
    /// OTP'yi doğrular
    /// </summary>
    /// <param name="phoneNumber">Telefon numarası</param>
    /// <param name="code">Kullanıcının girdiği kod</param>
    /// <returns>Doğrulama sonucu</returns>
    Task<OtpVerifyResult> VerifyOtpAsync(string phoneNumber, string code);

    /// <summary>
    /// Rate limit kontrolü yapar
    /// </summary>
    /// <param name="phoneNumber">Telefon numarası</param>
    /// <returns>OTP gönderilebilir mi?</returns>
    Task<bool> CanSendOtpAsync(string phoneNumber);
}

/// <summary>
/// OTP gönderim sonucu
/// </summary>
public class OtpSendResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int? RetryAfterSeconds { get; set; }
    public int ExpiresInSeconds { get; set; } = 180; // 3 dakika
}

/// <summary>
/// OTP doğrulama sonucu
/// </summary>
public class OtpVerifyResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; } // Doğrulama başarılı ise JWT token
    public int RemainingAttempts { get; set; }
}
