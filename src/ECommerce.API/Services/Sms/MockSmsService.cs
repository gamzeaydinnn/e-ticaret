using ECommerce.API.Services.Sms;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Services.Sms
{
    /// <summary>
    /// Test ve development ortamı için Mock SMS servisi.
    /// 
    /// Gerçek SMS göndermeden OTP akışlarını test etmeyi sağlar.
    /// Production ortamında kullanılmamalıdır!
    /// 
    /// Özellikler:
    /// - Tüm gönderimler başarılı döner
    /// - Gönderilen kodlar log'lanır
    /// - Maliyetsiz test imkanı
    /// - Rate limit testleri için ideal
    /// </summary>
    public class MockSmsService : INetGsmService
    {
        private readonly ILogger<MockSmsService> _logger;
        private readonly List<MockSmsRecord> _sentMessages = new();

        public MockSmsService(ILogger<MockSmsService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Mock SMS gönderimi - Gerçekte SMS gönderilmez, sadece log'lanır
        /// </summary>
        public Task<SmsResult> SendSmsAsync(string phoneNumber, string message)
        {
            // Mock kaydı oluştur
            var record = new MockSmsRecord
            {
                PhoneNumber = phoneNumber,
                Message = message,
                Header = "MOCKHEADER",
                SentAt = DateTime.UtcNow,
                JobId = Guid.NewGuid().ToString()
            };

            _sentMessages.Add(record);

            // Console ve log'a yaz
            _logger.LogInformation(
                "🚀 [MOCK SMS] Gönderildi - Numara: {Phone}, Mesaj: {Message}, JobId: {JobId}",
                MaskPhoneNumber(phoneNumber),
                message,
                record.JobId);

            Console.WriteLine($"📱 MOCK SMS: {MaskPhoneNumber(phoneNumber)} -> {message}");

            // Her zaman başarılı dön
            var result = new SmsResult
            {
                Success = true,
                Code = "00",
                JobId = record.JobId,
                Description = "Mock SMS sent successfully"
            };

            return Task.FromResult(result);
        }

        /// <summary>
        /// Mock bakiye sorgulama - Her zaman 1000 kredisi var gibi döner
        /// </summary>
        public Task<decimal> GetBalanceAsync()
        {
            _logger.LogInformation("🚀 [MOCK SMS] Bakiye sorgulandı: 1000 kredi");
            return Task.FromResult(1000m);
        }

        /// <summary>
        /// Gönderilen tüm mock SMS'leri getir (test amaçlı)
        /// </summary>
        public List<MockSmsRecord> GetSentMessages() => _sentMessages.ToList();

        /// <summary>
        /// Belirli bir telefon numarasına gönderilen son SMS'i getir
        /// </summary>
        public MockSmsRecord? GetLastMessageFor(string phoneNumber)
        {
            return _sentMessages
                .Where(m => m.PhoneNumber == phoneNumber)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefault();
        }

        /// <summary>
        /// Tüm mock kayıtları temizle
        /// </summary>
        public void ClearHistory()
        {
            _sentMessages.Clear();
            _logger.LogInformation("🗑️ [MOCK SMS] Tüm kayıtlar temizlendi");
        }

        /// <summary>
        /// Telefon numarasını maskele (KVKV uyumu için)
        /// </summary>
        private string MaskPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone) || phone.Length < 7)
                return "***";

            return $"{phone[..3]}****{phone[^2..]}";
        }
    }

    /// <summary>
    /// Mock SMS kaydı
    /// </summary>
    public class MockSmsRecord
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Header { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public string JobId { get; set; } = string.Empty;
    }
}
