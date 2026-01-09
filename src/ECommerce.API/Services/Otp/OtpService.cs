using System.Collections.Concurrent;
using ECommerce.API.Services.Sms;
using Microsoft.Extensions.Options;

namespace ECommerce.API.Services.Otp;

/// <summary>
/// OTP yapılandırması
/// </summary>
public class OtpSettings
{
    /// <summary>OTP kodunun geçerlilik süresi (saniye)</summary>
    public int ExpirationSeconds { get; set; } = 180; // 3 dakika
    
    /// <summary>Aynı numaraya tekrar OTP göndermek için bekleme süresi (saniye)</summary>
    public int ResendCooldownSeconds { get; set; } = 60;
    
    /// <summary>Günlük maksimum OTP gönderim sayısı</summary>
    public int DailyMaxOtpCount { get; set; } = 5;
    
    /// <summary>Maksimum yanlış deneme sayısı</summary>
    public int MaxWrongAttempts { get; set; } = 3;
    
    /// <summary>OTP kod uzunluğu</summary>
    public int CodeLength { get; set; } = 6;
}

/// <summary>
/// Memory-based OTP kayıt modeli
/// </summary>
internal class OtpRecord
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public int WrongAttempts { get; set; }
}

/// <summary>
/// Rate limit kayıt modeli
/// </summary>
internal class RateLimitRecord
{
    public DateTime LastSentAt { get; set; }
    public int DailyCount { get; set; }
    public DateTime DailyResetAt { get; set; }
}

/// <summary>
/// OTP servisi implementasyonu (Memory-based)
/// Production'da Redis kullanılması önerilir
/// </summary>
public class OtpService : IOtpService
{
    private readonly INetGsmService _smsService;
    private readonly OtpSettings _settings;
    private readonly ILogger<OtpService> _logger;

    // Memory-based storage (Production'da Redis kullan!)
    private static readonly ConcurrentDictionary<string, OtpRecord> _otpStore = new();
    private static readonly ConcurrentDictionary<string, RateLimitRecord> _rateLimitStore = new();

    public OtpService(
        INetGsmService smsService,
        IOptions<OtpSettings> settings,
        ILogger<OtpService> logger)
    {
        _smsService = smsService;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Yeni OTP oluşturur ve SMS gönderir
    /// </summary>
    public async Task<OtpSendResult> SendOtpAsync(string phoneNumber)
    {
        var normalizedPhone = NormalizePhone(phoneNumber);
        
        _logger.LogInformation("[OTP] OTP isteği: {Phone}", normalizedPhone);

        // Rate limit kontrolü
        var canSend = await CanSendOtpAsync(normalizedPhone);
        if (!canSend)
        {
            var rateLimitInfo = GetRateLimitInfo(normalizedPhone);
            return new OtpSendResult
            {
                Success = false,
                Message = "Çok fazla istek gönderdiniz. Lütfen bekleyin.",
                RetryAfterSeconds = rateLimitInfo.retryAfter
            };
        }

        // OTP kodu oluştur
        var code = GenerateOtpCode();
        var now = DateTime.UtcNow;

        // OTP kaydını oluştur/güncelle
        var otpRecord = new OtpRecord
        {
            PhoneNumber = normalizedPhone,
            Code = code,
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(_settings.ExpirationSeconds),
            IsUsed = false,
            WrongAttempts = 0
        };

        _otpStore[normalizedPhone] = otpRecord;

        // Rate limit güncelle
        UpdateRateLimit(normalizedPhone);

        // SMS gönder
        var message = $"Doğrulama kodunuz: {code}\nBu kod {_settings.ExpirationSeconds / 60} dakika geçerlidir.\n- Gölköy Gürme";
        
        var smsResult = await _smsService.SendSmsAsync(normalizedPhone, message);

        if (smsResult.Success)
        {
            _logger.LogInformation("[OTP] SMS gönderildi: {Phone}, JobId: {JobId}", 
                normalizedPhone, smsResult.JobId);

            return new OtpSendResult
            {
                Success = true,
                Message = "Doğrulama kodu telefonunuza gönderildi.",
                ExpiresInSeconds = _settings.ExpirationSeconds
            };
        }
        else
        {
            _logger.LogError("[OTP] SMS gönderilemedi: {Phone}, Error: {Error}", 
                normalizedPhone, smsResult.ErrorMessage);

            // OTP kaydını sil (SMS gitmedi)
            _otpStore.TryRemove(normalizedPhone, out _);

            return new OtpSendResult
            {
                Success = false,
                Message = "SMS gönderilemedi. Lütfen daha sonra tekrar deneyin."
            };
        }
    }

    /// <summary>
    /// OTP'yi doğrular
    /// </summary>
    public async Task<OtpVerifyResult> VerifyOtpAsync(string phoneNumber, string code)
    {
        var normalizedPhone = NormalizePhone(phoneNumber);
        
        _logger.LogInformation("[OTP] Doğrulama isteği: {Phone}", normalizedPhone);

        // OTP kaydını bul
        if (!_otpStore.TryGetValue(normalizedPhone, out var otpRecord))
        {
            _logger.LogWarning("[OTP] Kayıt bulunamadı: {Phone}", normalizedPhone);
            return new OtpVerifyResult
            {
                Success = false,
                Message = "Doğrulama kodu bulunamadı. Lütfen yeni kod isteyin."
            };
        }

        // Zaten kullanıldı mı?
        if (otpRecord.IsUsed)
        {
            _logger.LogWarning("[OTP] Kod zaten kullanıldı: {Phone}", normalizedPhone);
            return new OtpVerifyResult
            {
                Success = false,
                Message = "Bu kod zaten kullanıldı. Lütfen yeni kod isteyin."
            };
        }

        // Süresi doldu mu?
        if (DateTime.UtcNow > otpRecord.ExpiresAt)
        {
            _logger.LogWarning("[OTP] Kod süresi doldu: {Phone}", normalizedPhone);
            _otpStore.TryRemove(normalizedPhone, out _);
            return new OtpVerifyResult
            {
                Success = false,
                Message = "Kodun süresi doldu. Lütfen yeni kod isteyin."
            };
        }

        // Maksimum deneme kontrolü
        if (otpRecord.WrongAttempts >= _settings.MaxWrongAttempts)
        {
            _logger.LogWarning("[OTP] Maksimum deneme aşıldı: {Phone}", normalizedPhone);
            _otpStore.TryRemove(normalizedPhone, out _);
            return new OtpVerifyResult
            {
                Success = false,
                Message = "Çok fazla yanlış deneme. Lütfen yeni kod isteyin."
            };
        }

        // Kod doğru mu?
        if (otpRecord.Code != code.Trim())
        {
            otpRecord.WrongAttempts++;
            var remaining = _settings.MaxWrongAttempts - otpRecord.WrongAttempts;
            
            _logger.LogWarning("[OTP] Yanlış kod: {Phone}, Kalan deneme: {Remaining}", 
                normalizedPhone, remaining);

            return new OtpVerifyResult
            {
                Success = false,
                Message = $"Yanlış kod. {remaining} deneme hakkınız kaldı.",
                RemainingAttempts = remaining
            };
        }

        // Başarılı doğrulama
        otpRecord.IsUsed = true;
        
        _logger.LogInformation("[OTP] Doğrulama başarılı: {Phone}", normalizedPhone);

        // OTP kaydını temizle
        _otpStore.TryRemove(normalizedPhone, out _);

        return await Task.FromResult(new OtpVerifyResult
        {
            Success = true,
            Message = "Doğrulama başarılı.",
            // Token burada AuthService tarafından üretilecek
            // Şimdilik sadece başarı dönüyoruz
        });
    }

    /// <summary>
    /// Rate limit kontrolü yapar
    /// </summary>
    public Task<bool> CanSendOtpAsync(string phoneNumber)
    {
        var normalizedPhone = NormalizePhone(phoneNumber);
        var (canSend, _) = GetRateLimitInfo(normalizedPhone);
        return Task.FromResult(canSend);
    }

    /// <summary>
    /// Rate limit bilgisi döner
    /// </summary>
    private (bool canSend, int retryAfter) GetRateLimitInfo(string phoneNumber)
    {
        var now = DateTime.UtcNow;

        if (!_rateLimitStore.TryGetValue(phoneNumber, out var record))
        {
            return (true, 0);
        }

        // Günlük reset kontrolü
        if (now > record.DailyResetAt)
        {
            record.DailyCount = 0;
            record.DailyResetAt = now.Date.AddDays(1);
        }

        // Günlük limit kontrolü
        if (record.DailyCount >= _settings.DailyMaxOtpCount)
        {
            var resetInSeconds = (int)(record.DailyResetAt - now).TotalSeconds;
            return (false, resetInSeconds);
        }

        // Cooldown kontrolü
        var cooldownEnd = record.LastSentAt.AddSeconds(_settings.ResendCooldownSeconds);
        if (now < cooldownEnd)
        {
            var retryAfter = (int)(cooldownEnd - now).TotalSeconds;
            return (false, retryAfter);
        }

        return (true, 0);
    }

    /// <summary>
    /// Rate limit kaydını günceller
    /// </summary>
    private void UpdateRateLimit(string phoneNumber)
    {
        var now = DateTime.UtcNow;

        _rateLimitStore.AddOrUpdate(
            phoneNumber,
            _ => new RateLimitRecord
            {
                LastSentAt = now,
                DailyCount = 1,
                DailyResetAt = now.Date.AddDays(1)
            },
            (_, existing) =>
            {
                // Günlük reset kontrolü
                if (now > existing.DailyResetAt)
                {
                    existing.DailyCount = 0;
                    existing.DailyResetAt = now.Date.AddDays(1);
                }

                existing.LastSentAt = now;
                existing.DailyCount++;
                return existing;
            });
    }

    /// <summary>
    /// OTP kodu üretir
    /// </summary>
    private string GenerateOtpCode()
    {
        var min = (int)Math.Pow(10, _settings.CodeLength - 1);
        var max = (int)Math.Pow(10, _settings.CodeLength) - 1;
        return Random.Shared.Next(min, max + 1).ToString();
    }

    /// <summary>
    /// Telefon numarasını normalize eder
    /// </summary>
    private static string NormalizePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return phone;

        // Sadece rakamları al
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        // Başındaki 0'ı kaldır
        if (digits.StartsWith("0"))
            digits = digits[1..];

        // Türkiye kodu varsa kaldır
        if (digits.StartsWith("90") && digits.Length > 10)
            digits = digits[2..];

        return digits;
    }
}
