namespace ECommerce.API.Services.Sms;

/// <summary>
/// Mock NetGSM Service - Development/Test ortamları için.
/// INetGsmService interface'ini implement eder ama gerçek SMS göndermez.
/// </summary>
public class MockNetGsmService : INetGsmService
{
    private readonly ILogger<MockNetGsmService> _logger;

    public MockNetGsmService(ILogger<MockNetGsmService> logger)
    {
        _logger = logger;
        _logger.LogWarning("[MOCK NETGSM] MockNetGsmService aktif - Gerçek SMS gönderilmeyecek!");
    }

    public Task<SmsResult> SendSmsAsync(string phoneNumber, string message)
    {
        _logger.LogInformation("[MOCK NETGSM] ========================================");
        _logger.LogInformation("[MOCK NETGSM] TELEFON: {Phone}", phoneNumber);
        _logger.LogInformation("[MOCK NETGSM] MESAJ: {Message}", message);
        _logger.LogInformation("[MOCK NETGSM] ========================================");
        
        // Console'a da yaz (daha görünür olsun)
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[MOCK NETGSM] >>> TELEFON: {phoneNumber}");
        Console.WriteLine($"[MOCK NETGSM] >>> MESAJ: {message}\n");
        Console.ResetColor();

        return Task.FromResult(new SmsResult
        {
            Success = true,
            Code = "00",
            JobId = $"MOCK-{Guid.NewGuid():N}"
        });
    }

    public Task<decimal> GetBalanceAsync()
    {
        _logger.LogInformation("[MOCK NETGSM] Bakiye sorgusu - Mock: 999.99 TL");
        return Task.FromResult(999.99m);
    }
}
