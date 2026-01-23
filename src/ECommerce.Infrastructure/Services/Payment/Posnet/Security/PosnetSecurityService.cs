// ═══════════════════════════════════════════════════════════════════════════════════════════════
// POSNET GÜVENLİK SERVİSİ
// PCI-DSS uyumlu kart verisi şifreleme, maskeleme ve güvenlik kontrolleri
// Bu servis hassas ödeme verilerinin güvenli işlenmesini sağlar
// 
// PCI-DSS GEREKSİNİMLERİ:
// - Kart numaraları asla düz metin olarak loglanmaz
// - CVV asla saklanmaz (sadece işlem anında kullanılır)
// - Kart verisi şifreli iletilir (TLS 1.2+)
// - Maskeleme standardı: ilk 6, son 4 hane görünür
// ═══════════════════════════════════════════════════════════════════════════════════════════════

using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services.Payment.Posnet.Security
{
    /// <summary>
    /// POSNET güvenlik servisi interface
    /// Kart verisi şifreleme, maskeleme ve güvenlik doğrulamaları
    /// </summary>
    public interface IPosnetSecurityService
    {
        /// <summary>
        /// Kart numarasını PCI-DSS uyumlu maskeler (ilk 6, son 4 görünür)
        /// Örnek: 4506349116543211 → 450634******3211
        /// </summary>
        string MaskCardNumber(string cardNumber);

        /// <summary>
        /// Tüm hassas verileri log için maskeler
        /// </summary>
        string MaskSensitiveData(string data, SensitiveDataType dataType);

        /// <summary>
        /// XML içindeki hassas verileri maskeler (loglama için)
        /// </summary>
        string MaskXmlSensitiveData(string xml);

        /// <summary>
        /// Kart numarası format ve Luhn doğrulaması yapar
        /// </summary>
        CardValidationResult ValidateCardNumber(string cardNumber);

        /// <summary>
        /// CVV format doğrulaması (3-4 hane)
        /// </summary>
        bool ValidateCvv(string cvv, string cardNumber);

        /// <summary>
        /// Son kullanma tarihi doğrulaması
        /// </summary>
        bool ValidateExpiryDate(string expiryYear, string expiryMonth);

        /// <summary>
        /// Rate limiting kontrolü (IP bazlı)
        /// </summary>
        RateLimitResult CheckRateLimit(string ipAddress, string endpoint);

        /// <summary>
        /// Güvenli rastgele transaction ID üretir
        /// </summary>
        string GenerateSecureTransactionId();

        /// <summary>
        /// IP whitelist kontrolü
        /// </summary>
        bool IsIpWhitelisted(string ipAddress, string[] whitelist);

        /// <summary>
        /// Fraud detection - şüpheli işlem kontrolü
        /// </summary>
        FraudCheckResult CheckForFraud(FraudCheckRequest request);
    }

    /// <summary>
    /// Hassas veri türleri
    /// </summary>
    public enum SensitiveDataType
    {
        CardNumber,
        Cvv,
        ExpiryDate,
        EncKey,
        Mac,
        Password,
        Email,
        Phone
    }

    /// <summary>
    /// Kart doğrulama sonucu
    /// </summary>
    public class CardValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public string? CardBrand { get; set; } // Visa, Mastercard, Amex, Troy
        public string? MaskedNumber { get; set; }
    }

    /// <summary>
    /// Rate limit sonucu
    /// </summary>
    public class RateLimitResult
    {
        public bool IsAllowed { get; set; }
        public int RemainingRequests { get; set; }
        public int RetryAfterSeconds { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Fraud kontrol isteği
    /// </summary>
    public class FraudCheckRequest
    {
        public string CardNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int? CustomerId { get; set; }
        public string? UserAgent { get; set; }
        public int RecentFailedAttempts { get; set; }
    }

    /// <summary>
    /// Fraud kontrol sonucu
    /// </summary>
    public class FraudCheckResult
    {
        public bool IsSuspicious { get; set; }
        public FraudRiskLevel RiskLevel { get; set; }
        public string[] Warnings { get; set; } = Array.Empty<string>();
        public bool ShouldBlock { get; set; }
        public int Score { get; set; } // 0-100 arası risk skoru
    }

    /// <summary>
    /// Fraud risk seviyeleri
    /// </summary>
    public enum FraudRiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// POSNET güvenlik servisi implementasyonu
    /// </summary>
    public class PosnetSecurityService : IPosnetSecurityService
    {
        private readonly ILogger<PosnetSecurityService> _logger;

        // Rate limiting için basit in-memory cache (production'da Redis kullanılmalı)
        private static readonly Dictionary<string, RateLimitEntry> _rateLimitCache = new();
        private static readonly object _rateLimitLock = new();

        // Rate limit ayarları
        private const int MaxRequestsPerMinute = 30;
        private const int MaxRequestsPerHour = 200;
        private const int BlockDurationMinutes = 15;

        public PosnetSecurityService(ILogger<PosnetSecurityService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Kart Maskeleme

        /// <inheritdoc/>
        public string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                return "****";

            // Sadece rakamları al
            var digitsOnly = new string(cardNumber.Where(char.IsDigit).ToArray());

            if (digitsOnly.Length < 10)
                return "****";

            // PCI-DSS standardı: ilk 6 ve son 4 hane görünür
            // Örnek: 4506349116543211 → 450634******3211
            var firstSix = digitsOnly[..6];
            var lastFour = digitsOnly[^4..];
            var maskedMiddle = new string('*', digitsOnly.Length - 10);

            return $"{firstSix}{maskedMiddle}{lastFour}";
        }

        /// <inheritdoc/>
        public string MaskSensitiveData(string data, SensitiveDataType dataType)
        {
            if (string.IsNullOrWhiteSpace(data))
                return "****";

            return dataType switch
            {
                SensitiveDataType.CardNumber => MaskCardNumber(data),
                SensitiveDataType.Cvv => "***", // CVV asla gösterilmez
                SensitiveDataType.ExpiryDate => data.Length >= 4 ? $"{data[..2]}/**" : "**/**",
                SensitiveDataType.EncKey => $"{data[..4]}...{data[^4..]}", // Sadece uçlar
                SensitiveDataType.Mac => data.Length > 8 ? $"{data[..8]}..." : "********",
                SensitiveDataType.Password => "********",
                SensitiveDataType.Email => MaskEmail(data),
                SensitiveDataType.Phone => MaskPhone(data),
                _ => "****"
            };
        }

        /// <summary>
        /// E-posta maskeleme: test@email.com → t***@e***.com
        /// </summary>
        private static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return "***@***.***";

            var parts = email.Split('@');
            var local = parts[0].Length > 1 ? $"{parts[0][0]}***" : "***";
            var domainParts = parts[1].Split('.');
            var domain = domainParts[0].Length > 1 ? $"{domainParts[0][0]}***" : "***";
            var ext = domainParts.Length > 1 ? domainParts[^1] : "***";

            return $"{local}@{domain}.{ext}";
        }

        /// <summary>
        /// Telefon maskeleme: 5551234567 → 555***4567
        /// </summary>
        private static string MaskPhone(string phone)
        {
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (digits.Length < 7)
                return "***";

            return $"{digits[..3]}***{digits[^4..]}";
        }

        /// <inheritdoc/>
        public string MaskXmlSensitiveData(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return xml;

            // Kart numarası maskeleme
            xml = Regex.Replace(xml,
                @"<ccno>(\d{6})(\d+)(\d{4})</ccno>",
                m => $"<ccno>{m.Groups[1].Value}{"*".PadRight(m.Groups[2].Value.Length, '*')}{m.Groups[3].Value}</ccno>",
                RegexOptions.IgnoreCase);

            // CVV tamamen maskeleme
            xml = Regex.Replace(xml,
                @"<cvc>(\d+)</cvc>",
                "<cvc>***</cvc>",
                RegexOptions.IgnoreCase);

            // EncKey maskeleme
            xml = Regex.Replace(xml,
                @"<encKey>(.{4})(.+)(.{4})</encKey>",
                m => $"<encKey>{m.Groups[1].Value}...{m.Groups[3].Value}</encKey>",
                RegexOptions.IgnoreCase);

            // MAC maskeleme
            xml = Regex.Replace(xml,
                @"<mac>(.{8}).+</mac>",
                m => $"<mac>{m.Groups[1].Value}...</mac>",
                RegexOptions.IgnoreCase);

            return xml;
        }

        #endregion

        #region Kart Doğrulama

        /// <inheritdoc/>
        public CardValidationResult ValidateCardNumber(string cardNumber)
        {
            var result = new CardValidationResult();

            if (string.IsNullOrWhiteSpace(cardNumber))
            {
                result.IsValid = false;
                result.ErrorMessage = "Kart numarası boş olamaz";
                return result;
            }

            // Sadece rakamları al
            var digitsOnly = new string(cardNumber.Where(char.IsDigit).ToArray());

            // Uzunluk kontrolü (13-19 hane)
            if (digitsOnly.Length < 13 || digitsOnly.Length > 19)
            {
                result.IsValid = false;
                result.ErrorMessage = "Kart numarası 13-19 hane olmalıdır";
                return result;
            }

            // Luhn algoritması doğrulaması
            if (!ValidateLuhn(digitsOnly))
            {
                result.IsValid = false;
                result.ErrorMessage = "Geçersiz kart numarası (Luhn doğrulaması başarısız)";
                return result;
            }

            // Kart markasını belirle
            result.CardBrand = DetermineCardBrand(digitsOnly);
            result.MaskedNumber = MaskCardNumber(digitsOnly);
            result.IsValid = true;

            return result;
        }

        /// <summary>
        /// Luhn algoritması ile kart numarası doğrulaması
        /// ISO/IEC 7812 standardına uygun checksum kontrolü
        /// </summary>
        private static bool ValidateLuhn(string number)
        {
            int sum = 0;
            bool alternate = false;

            for (int i = number.Length - 1; i >= 0; i--)
            {
                int digit = number[i] - '0';

                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit -= 9;
                }

                sum += digit;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }

        /// <summary>
        /// Kart markası belirleme (BIN aralıklarına göre)
        /// </summary>
        private static string DetermineCardBrand(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 2)
                return "Unknown";

            // Visa: 4 ile başlar
            if (cardNumber.StartsWith("4"))
                return "Visa";

            // Mastercard: 51-55 veya 2221-2720
            if (cardNumber.Length >= 2)
            {
                var firstTwo = int.Parse(cardNumber[..2]);
                if (firstTwo >= 51 && firstTwo <= 55)
                    return "Mastercard";

                if (cardNumber.Length >= 4)
                {
                    var firstFour = int.Parse(cardNumber[..4]);
                    if (firstFour >= 2221 && firstFour <= 2720)
                        return "Mastercard";
                }
            }

            // Amex: 34 veya 37 ile başlar
            if (cardNumber.StartsWith("34") || cardNumber.StartsWith("37"))
                return "Amex";

            // Troy: 65 veya 9792 ile başlar
            if (cardNumber.StartsWith("65") || cardNumber.StartsWith("9792"))
                return "Troy";

            return "Unknown";
        }

        /// <inheritdoc/>
        public bool ValidateCvv(string cvv, string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cvv))
                return false;

            var digitsOnly = new string(cvv.Where(char.IsDigit).ToArray());
            var cardBrand = DetermineCardBrand(new string(cardNumber.Where(char.IsDigit).ToArray()));

            // Amex için 4 hane, diğerleri için 3 hane
            int expectedLength = cardBrand == "Amex" ? 4 : 3;

            return digitsOnly.Length == expectedLength;
        }

        /// <inheritdoc/>
        public bool ValidateExpiryDate(string expiryYear, string expiryMonth)
        {
            if (string.IsNullOrWhiteSpace(expiryYear) || string.IsNullOrWhiteSpace(expiryMonth))
                return false;

            if (!int.TryParse(expiryMonth, out int month) || month < 1 || month > 12)
                return false;

            if (!int.TryParse(expiryYear, out int year))
                return false;

            // 2 haneli yıl ise 2000 ekle
            if (year < 100)
                year += 2000;

            // Son kullanma tarihi geçmiş mi kontrol et
            var expiryDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            return expiryDate >= DateTime.Today;
        }

        #endregion

        #region Rate Limiting

        /// <inheritdoc/>
        public RateLimitResult CheckRateLimit(string ipAddress, string endpoint)
        {
            var key = $"{ipAddress}:{endpoint}";
            var now = DateTime.UtcNow;

            lock (_rateLimitLock)
            {
                // Eski kayıtları temizle
                CleanupOldEntries();

                if (!_rateLimitCache.TryGetValue(key, out var entry))
                {
                    entry = new RateLimitEntry { FirstRequestTime = now };
                    _rateLimitCache[key] = entry;
                }

                // Block durumu kontrolü
                if (entry.BlockedUntil.HasValue && entry.BlockedUntil > now)
                {
                    return new RateLimitResult
                    {
                        IsAllowed = false,
                        RemainingRequests = 0,
                        RetryAfterSeconds = (int)(entry.BlockedUntil.Value - now).TotalSeconds,
                        Message = "Çok fazla istek gönderdiniz. Lütfen bekleyin."
                    };
                }

                // Dakikalık limit kontrolü
                var minuteRequests = entry.RequestTimes.Count(t => t > now.AddMinutes(-1));
                if (minuteRequests >= MaxRequestsPerMinute)
                {
                    entry.BlockedUntil = now.AddMinutes(BlockDurationMinutes);
                    _logger.LogWarning("[RATE-LIMIT] IP {IpAddress} dakikalık limiti aştı, bloke edildi", ipAddress);

                    return new RateLimitResult
                    {
                        IsAllowed = false,
                        RemainingRequests = 0,
                        RetryAfterSeconds = BlockDurationMinutes * 60,
                        Message = "Dakikalık istek limiti aşıldı."
                    };
                }

                // Saatlik limit kontrolü
                var hourlyRequests = entry.RequestTimes.Count(t => t > now.AddHours(-1));
                if (hourlyRequests >= MaxRequestsPerHour)
                {
                    entry.BlockedUntil = now.AddMinutes(BlockDurationMinutes * 2);
                    _logger.LogWarning("[RATE-LIMIT] IP {IpAddress} saatlik limiti aştı, bloke edildi", ipAddress);

                    return new RateLimitResult
                    {
                        IsAllowed = false,
                        RemainingRequests = 0,
                        RetryAfterSeconds = BlockDurationMinutes * 2 * 60,
                        Message = "Saatlik istek limiti aşıldı."
                    };
                }

                // İsteği kaydet
                entry.RequestTimes.Add(now);

                return new RateLimitResult
                {
                    IsAllowed = true,
                    RemainingRequests = MaxRequestsPerMinute - minuteRequests - 1,
                    RetryAfterSeconds = 0,
                    Message = null
                };
            }
        }

        private static void CleanupOldEntries()
        {
            var threshold = DateTime.UtcNow.AddHours(-2);
            var keysToRemove = _rateLimitCache
                .Where(kvp => kvp.Value.RequestTimes.All(t => t < threshold))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
                _rateLimitCache.Remove(key);
        }

        private class RateLimitEntry
        {
            public DateTime FirstRequestTime { get; set; }
            public List<DateTime> RequestTimes { get; set; } = new();
            public DateTime? BlockedUntil { get; set; }
        }

        #endregion

        #region Diğer Güvenlik Metodları

        /// <inheritdoc/>
        public string GenerateSecureTransactionId()
        {
            // Cryptographically secure random ID
            var bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            // Timestamp prefix + random hex
            var timestamp = DateTime.UtcNow.ToString("yyMMddHHmm");
            var randomPart = Convert.ToHexString(bytes)[..12].ToUpperInvariant();

            return $"TXN{timestamp}{randomPart}";
        }

        /// <inheritdoc/>
        public bool IsIpWhitelisted(string ipAddress, string[] whitelist)
        {
            if (string.IsNullOrWhiteSpace(ipAddress) || whitelist == null || whitelist.Length == 0)
                return false;

            // Tam eşleşme veya CIDR kontrolü
            foreach (var entry in whitelist)
            {
                if (string.IsNullOrWhiteSpace(entry))
                    continue;

                // Tam eşleşme
                if (entry.Trim() == ipAddress)
                    return true;

                // CIDR notation kontrolü (basit implementasyon)
                if (entry.Contains('/'))
                {
                    if (IsIpInCidrRange(ipAddress, entry))
                        return true;
                }
            }

            return false;
        }

        private static bool IsIpInCidrRange(string ipAddress, string cidr)
        {
            try
            {
                var parts = cidr.Split('/');
                if (parts.Length != 2)
                    return false;

                var networkAddress = parts[0];
                if (!int.TryParse(parts[1], out int prefixLength))
                    return false;

                var ipBytes = System.Net.IPAddress.Parse(ipAddress).GetAddressBytes();
                var networkBytes = System.Net.IPAddress.Parse(networkAddress).GetAddressBytes();

                if (ipBytes.Length != networkBytes.Length)
                    return false;

                int fullBytes = prefixLength / 8;
                int remainingBits = prefixLength % 8;

                for (int i = 0; i < fullBytes; i++)
                {
                    if (ipBytes[i] != networkBytes[i])
                        return false;
                }

                if (remainingBits > 0 && fullBytes < ipBytes.Length)
                {
                    byte mask = (byte)(0xFF << (8 - remainingBits));
                    if ((ipBytes[fullBytes] & mask) != (networkBytes[fullBytes] & mask))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public FraudCheckResult CheckForFraud(FraudCheckRequest request)
        {
            var result = new FraudCheckResult
            {
                IsSuspicious = false,
                RiskLevel = FraudRiskLevel.Low,
                Score = 0,
                ShouldBlock = false
            };

            var warnings = new List<string>();

            // 1. Yüksek tutar kontrolü (10.000 TL üzeri)
            if (request.Amount > 10000)
            {
                result.Score += 15;
                warnings.Add($"Yüksek tutar: {request.Amount:N2} TL");
            }

            // 2. Çok yüksek tutar (50.000 TL üzeri)
            if (request.Amount > 50000)
            {
                result.Score += 25;
                warnings.Add($"Çok yüksek tutar: {request.Amount:N2} TL - Manuel onay gerekebilir");
            }

            // 3. Son başarısız deneme sayısı
            if (request.RecentFailedAttempts >= 3)
            {
                result.Score += 20;
                warnings.Add($"Son 1 saatte {request.RecentFailedAttempts} başarısız deneme");
            }

            if (request.RecentFailedAttempts >= 5)
            {
                result.Score += 30;
                result.ShouldBlock = true;
                warnings.Add("Çok fazla başarısız deneme - İşlem bloke edilmeli");
            }

            // 4. Misafir kullanıcı + yüksek tutar
            if (!request.CustomerId.HasValue && request.Amount > 5000)
            {
                result.Score += 10;
                warnings.Add("Misafir kullanıcı ile yüksek tutarlı işlem");
            }

            // 5. VPN/Proxy IP kontrolü (basit - gerçekte IP reputation servisi kullanılır)
            if (IsSuspiciousIp(request.IpAddress))
            {
                result.Score += 15;
                warnings.Add("Şüpheli IP adresi tespit edildi");
            }

            // Risk seviyesi belirleme
            result.RiskLevel = result.Score switch
            {
                < 20 => FraudRiskLevel.Low,
                < 40 => FraudRiskLevel.Medium,
                < 60 => FraudRiskLevel.High,
                _ => FraudRiskLevel.Critical
            };

            result.IsSuspicious = result.Score >= 30;
            result.Warnings = warnings.ToArray();

            // Loglama
            if (result.IsSuspicious)
            {
                _logger.LogWarning(
                    "[FRAUD-CHECK] Şüpheli işlem - IP: {IpAddress}, Tutar: {Amount}, Skor: {Score}, Uyarılar: {Warnings}",
                    MaskSensitiveData(request.IpAddress, SensitiveDataType.Phone),
                    request.Amount,
                    result.Score,
                    string.Join("; ", warnings));
            }

            return result;
        }

        private static bool IsSuspiciousIp(string ipAddress)
        {
            // Basit kontroller - gerçekte MaxMind, IPQualityScore gibi servisler kullanılır
            if (string.IsNullOrWhiteSpace(ipAddress))
                return true;

            // Localhost kontrolü (test ortamında normal)
            if (ipAddress == "127.0.0.1" || ipAddress == "::1")
                return false;

            // Private IP kontrolü (genellikle güvenli)
            if (ipAddress.StartsWith("10.") ||
                ipAddress.StartsWith("192.168.") ||
                ipAddress.StartsWith("172.16.") ||
                ipAddress.StartsWith("172.17.") ||
                ipAddress.StartsWith("172.18.") ||
                ipAddress.StartsWith("172.19.") ||
                ipAddress.StartsWith("172.2") ||
                ipAddress.StartsWith("172.30.") ||
                ipAddress.StartsWith("172.31."))
            {
                return false;
            }

            return false; // Varsayılan olarak güvenli kabul et
        }

        #endregion
    }
}
