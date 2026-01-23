using ECommerce.Core.Interfaces;

namespace ECommerce.API.Services.Sms;

/// <summary>
/// Mock SMS Provider - Development/Test ortamları için.
/// Gerçek SMS göndermez, sadece console'a yazar.
/// </summary>
public class MockSmsProvider : ISmsProvider
{
    private readonly ILogger<MockSmsProvider> _logger;

    public MockSmsProvider(ILogger<MockSmsProvider> logger)
    {
        _logger = logger;
        _logger.LogWarning("[MOCK SMS] MockSmsProvider aktif - Gerçek SMS gönderilmeyecek!");
    }

    public Task<SmsSendResult> SendSmsAsync(string phoneNumber, string message)
    {
        _logger.LogInformation("[MOCK SMS] ========================================");
        _logger.LogInformation("[MOCK SMS] TELEFON: {Phone}", phoneNumber);
        _logger.LogInformation("[MOCK SMS] MESAJ: {Message}", message);
        _logger.LogInformation("[MOCK SMS] ========================================");
        
        // Console'a da yaz (daha görünür olsun)
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n[MOCK SMS] >>> TELEFON: {phoneNumber}");
        Console.WriteLine($"[MOCK SMS] >>> MESAJ: {message}\n");
        Console.ResetColor();

        return Task.FromResult(new SmsSendResult
        {
            Success = true,
            JobId = $"MOCK-{Guid.NewGuid():N}",
            Description = "Mock SMS başarıyla 'gönderildi' (console'a yazıldı)"
        });
    }

    public Task<SmsSendResult> SendOtpAsync(string phoneNumber, string code)
    {
        _logger.LogInformation("[MOCK SMS OTP] ========================================");
        _logger.LogInformation("[MOCK SMS OTP] TELEFON: {Phone}", phoneNumber);
        _logger.LogInformation("[MOCK SMS OTP] KOD: {Code}", code);
        _logger.LogInformation("[MOCK SMS OTP] ========================================");
        
        // Console'a da yaz (daha görünür olsun)
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n[MOCK SMS OTP] >>> TELEFON: {phoneNumber}");
        Console.WriteLine($"[MOCK SMS OTP] >>> DOĞRULAMA KODU: {code}\n");
        Console.ResetColor();

        return Task.FromResult(new SmsSendResult
        {
            Success = true,
            JobId = $"MOCK-OTP-{Guid.NewGuid():N}",
            Description = "Mock OTP başarıyla 'gönderildi' (console'a yazıldı)"
        });
    }

    public Task<decimal> GetBalanceAsync()
    {
        _logger.LogInformation("[MOCK SMS] Bakiye sorgusu - Mock: 999.99 TL");
        return Task.FromResult(999.99m);
    }
}
