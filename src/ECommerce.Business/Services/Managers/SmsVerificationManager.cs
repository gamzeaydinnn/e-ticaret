using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// SMS doğrulama servisi implementasyonu.
    /// 
    /// SOLID Prensipleri:
    /// - Single Responsibility: Sadece SMS doğrulama işlemlerini yönetir
    /// - Open/Closed: Yeni doğrulama türleri enum ile eklenebilir
    /// - Liskov Substitution: ISmsVerificationService interface'i uygulanır
    /// - Interface Segregation: Spesifik interface kullanılır
    /// - Dependency Inversion: Repository ve SMS provider interface ile inject edilir
    /// </summary>
    public class SmsVerificationManager : ISmsVerificationService
    {
        private readonly ISmsVerificationRepository _verificationRepository;
        private readonly ISmsRateLimitRepository _rateLimitRepository;
        private readonly ISmsProvider _smsProvider;
        private readonly SmsVerificationSettings _settings;
        private readonly ILogger<SmsVerificationManager> _logger;

        public SmsVerificationManager(
            ISmsVerificationRepository verificationRepository,
            ISmsRateLimitRepository rateLimitRepository,
            ISmsProvider smsProvider,
            IOptions<SmsVerificationSettings> settings,
            ILogger<SmsVerificationManager> logger)
        {
            _verificationRepository = verificationRepository ?? throw new ArgumentNullException(nameof(verificationRepository));
            _rateLimitRepository = rateLimitRepository ?? throw new ArgumentNullException(nameof(rateLimitRepository));
            _smsProvider = smsProvider ?? throw new ArgumentNullException(nameof(smsProvider));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<SmsVerificationResult> SendVerificationCodeAsync(
            string phoneNumber,
            SmsVerificationPurpose purpose,
            string? ipAddress = null,
            string? userAgent = null,
            int? userId = null)
        {
            try
            {
                // 1. Telefon numarasını normalize et
                var normalizedPhone = NormalizePhoneNumber(phoneNumber);
                if (string.IsNullOrEmpty(normalizedPhone))
                {
                    _logger.LogWarning("[SMS] Geçersiz telefon numarası: {Phone}", phoneNumber);
                    return SmsVerificationResult.Error("Geçersiz telefon numarası.", "INVALID_PHONE");
                }

                _logger.LogInformation("[SMS] OTP gönderim isteği: {Phone}, Amaç: {Purpose}", 
                    normalizedPhone, purpose);

                // 2. Rate limit kontrolü
                var rateLimitCheck = await CanSendVerificationAsync(normalizedPhone, ipAddress);
                if (!rateLimitCheck.CanSend)
                {
                    _logger.LogWarning("[SMS] Rate limit aşıldı: {Phone}, Neden: {Reason}", 
                        normalizedPhone, rateLimitCheck.Reason);
                    
                    if (rateLimitCheck.IsBlocked)
                    {
                        return SmsVerificationResult.PhoneBlocked(rateLimitCheck.BlockedUntil!.Value);
                    }
                    
                    return SmsVerificationResult.RateLimited(rateLimitCheck.RetryAfterSeconds);
                }

                // 3. Mevcut pending kayıtları iptal et
                await _verificationRepository.CancelPendingByPhoneAsync(normalizedPhone, purpose);

                // 4. Yeni OTP kodu oluştur
                var code = GenerateSecureOtpCode();
                var now = DateTime.UtcNow;

                // 5. Veritabanına kaydet
                var verification = new SmsVerification
                {
                    PhoneNumber = normalizedPhone,
                    Code = code,
                    Purpose = purpose,
                    Status = SmsVerificationStatus.Pending,
                    ExpiresAt = now.AddSeconds(_settings.ExpirationSeconds),
                    MaxAttempts = _settings.MaxWrongAttempts,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    UserId = userId,
                    CreatedAt = now,
                    IsActive = true
                };

                await _verificationRepository.AddAsync(verification);

                // 6. SMS gönder
                var message = FormatOtpMessage(code);
                var smsResult = await _smsProvider.SendOtpAsync(normalizedPhone, message);

                // 7. SMS sonucunu kaydet
                verification.SmsSent = smsResult.Success;
                verification.JobId = smsResult.JobId;
                verification.SmsErrorMessage = smsResult.ErrorMessage;
                await _verificationRepository.UpdateAsync(verification);

                if (!smsResult.Success)
                {
                    _logger.LogError("[SMS] SMS gönderilemedi: {Phone}, Hata: {Error}", 
                        normalizedPhone, smsResult.ErrorMessage);
                    
                    // Başarısız gönderimde kaydı iptal et
                    verification.Status = SmsVerificationStatus.Cancelled;
                    await _verificationRepository.UpdateAsync(verification);
                    
                    return SmsVerificationResult.SmsSendFailed(smsResult.ErrorMessage ?? "Bilinmeyen hata");
                }

                // 8. Rate limit sayacını artır
                await _rateLimitRepository.IncrementCountersAsync(normalizedPhone, ipAddress);

                _logger.LogInformation("[SMS] OTP başarıyla gönderildi: {Phone}, JobId: {JobId}", 
                    normalizedPhone, smsResult.JobId);

                return new SmsVerificationResult
                {
                    Success = true,
                    Message = "Doğrulama kodu telefonunuza gönderildi.",
                    ExpiresInSeconds = _settings.ExpirationSeconds,
                    JobId = smsResult.JobId,
                    VerificationId = verification.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SMS] OTP gönderim hatası: {Phone}", phoneNumber);
                return SmsVerificationResult.Error("Beklenmeyen bir hata oluştu.", "INTERNAL_ERROR");
            }
        }

        /// <inheritdoc />
        public async Task<SmsVerificationResult> VerifyCodeAsync(
            string phoneNumber,
            string code,
            SmsVerificationPurpose purpose,
            string? ipAddress = null)
        {
            try
            {
                var normalizedPhone = NormalizePhoneNumber(phoneNumber);
                if (string.IsNullOrEmpty(normalizedPhone))
                {
                    return SmsVerificationResult.Error("Geçersiz telefon numarası.", "INVALID_PHONE");
                }

                _logger.LogInformation("[SMS] Kod doğrulama isteği: {Phone}, Amaç: {Purpose}", 
                    normalizedPhone, purpose);

                // 1. Aktif doğrulama kaydını bul
                var verification = await _verificationRepository.GetActiveByPhoneAsync(normalizedPhone, purpose);
                
                if (verification == null)
                {
                    _logger.LogWarning("[SMS] Aktif doğrulama kaydı bulunamadı: {Phone}", normalizedPhone);
                    return SmsVerificationResult.Error(
                        "Aktif doğrulama kaydı bulunamadı. Lütfen yeni kod isteyin.", 
                        "NO_ACTIVE_VERIFICATION");
                }

                // 2. Süre kontrolü
                if (verification.IsExpired)
                {
                    _logger.LogWarning("[SMS] Kod süresi dolmuş: {Phone}", normalizedPhone);
                    verification.Status = SmsVerificationStatus.Expired;
                    await _verificationRepository.UpdateAsync(verification);
                    return SmsVerificationResult.ExpiredCode();
                }

                // 3. Deneme sayısı kontrolü
                if (verification.IsMaxAttemptsExceeded)
                {
                    _logger.LogWarning("[SMS] Maksimum deneme aşıldı: {Phone}", normalizedPhone);
                    verification.Status = SmsVerificationStatus.MaxAttemptsExceeded;
                    await _verificationRepository.UpdateAsync(verification);
                    return SmsVerificationResult.MaxAttemptsExceeded();
                }

                // 4. Kod kontrolü
                var normalizedCode = code?.Trim();
                if (!string.Equals(verification.Code, normalizedCode, StringComparison.Ordinal))
                {
                    verification.WrongAttempts++;
                    await _verificationRepository.UpdateAsync(verification);
                    
                    // Şüpheli aktivite kaydı
                    await _rateLimitRepository.RecordFailedAttemptAsync(normalizedPhone);
                    
                    _logger.LogWarning("[SMS] Yanlış kod girişi: {Phone}, Kalan deneme: {Remaining}", 
                        normalizedPhone, verification.RemainingAttempts);

                    // Son deneme de yanlışsa
                    if (verification.RemainingAttempts <= 0)
                    {
                        verification.Status = SmsVerificationStatus.MaxAttemptsExceeded;
                        await _verificationRepository.UpdateAsync(verification);
                        return SmsVerificationResult.MaxAttemptsExceeded();
                    }
                    
                    return SmsVerificationResult.InvalidCode(verification.RemainingAttempts);
                }

                // 5. Başarılı doğrulama
                verification.Status = SmsVerificationStatus.Verified;
                verification.VerifiedAt = DateTime.UtcNow;
                await _verificationRepository.UpdateAsync(verification);

                _logger.LogInformation("[SMS] Kod başarıyla doğrulandı: {Phone}", normalizedPhone);

                return SmsVerificationResult.SuccessVerify();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SMS] Kod doğrulama hatası: {Phone}", phoneNumber);
                return SmsVerificationResult.Error("Beklenmeyen bir hata oluştu.", "INTERNAL_ERROR");
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsPhoneVerifiedAsync(string phoneNumber, SmsVerificationPurpose purpose)
        {
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);
            if (string.IsNullOrEmpty(normalizedPhone))
                return false;

            // Son 24 saat içinde doğrulanmış kayıt var mı?
            var since = DateTime.UtcNow.AddHours(-24);
            var verifications = await _verificationRepository.GetByPhoneAsync(normalizedPhone);
            
            return verifications.Any(v => 
                v.Purpose == purpose && 
                v.Status == SmsVerificationStatus.Verified && 
                v.VerifiedAt >= since);
        }

        /// <inheritdoc />
        public async Task<RateLimitCheckResult> CanSendVerificationAsync(string phoneNumber, string? ipAddress = null)
        {
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);
            if (string.IsNullOrEmpty(normalizedPhone))
            {
                return new RateLimitCheckResult
                {
                    CanSend = false,
                    Reason = "Geçersiz telefon numarası"
                };
            }

            var rateLimit = await _rateLimitRepository.GetOrCreateAsync(normalizedPhone, ipAddress);

            // 1. Blokaj kontrolü
            if (rateLimit.IsCurrentlyBlocked)
            {
                return new RateLimitCheckResult
                {
                    CanSend = false,
                    Reason = rateLimit.BlockReason ?? "Numara bloklu",
                    IsBlocked = true,
                    BlockedUntil = rateLimit.BlockedUntil
                };
            }

            // 2. Günlük limit kontrolü
            if (rateLimit.DailyCount >= _settings.DailyMaxOtpCount)
            {
                var resetIn = (int)(rateLimit.DailyResetAt - DateTime.UtcNow).TotalSeconds;
                return new RateLimitCheckResult
                {
                    CanSend = false,
                    Reason = "Günlük SMS limiti aşıldı",
                    RetryAfterSeconds = Math.Max(0, resetIn),
                    RemainingDailyCount = 0
                };
            }

            // 3. Cooldown kontrolü (60 saniye)
            if (!rateLimit.IsCooldownExpired)
            {
                return new RateLimitCheckResult
                {
                    CanSend = false,
                    Reason = "Lütfen bekleyin",
                    RetryAfterSeconds = rateLimit.CooldownRemainingSeconds,
                    RemainingDailyCount = _settings.DailyMaxOtpCount - rateLimit.DailyCount
                };
            }

            return new RateLimitCheckResult
            {
                CanSend = true,
                RemainingDailyCount = _settings.DailyMaxOtpCount - rateLimit.DailyCount
            };
        }

        /// <inheritdoc />
        public async Task<VerificationStatusResult> GetVerificationStatusAsync(
            string phoneNumber, 
            SmsVerificationPurpose purpose)
        {
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);
            if (string.IsNullOrEmpty(normalizedPhone))
            {
                return new VerificationStatusResult { HasActiveVerification = false };
            }

            var verification = await _verificationRepository.GetActiveByPhoneAsync(normalizedPhone, purpose);
            
            if (verification == null)
            {
                return new VerificationStatusResult { HasActiveVerification = false };
            }

            var rateLimit = await _rateLimitRepository.GetByPhoneAsync(normalizedPhone);
            var resendAfter = rateLimit?.CooldownRemainingSeconds ?? 0;

            return new VerificationStatusResult
            {
                HasActiveVerification = true,
                Status = verification.Status.ToString(),
                RemainingSeconds = verification.RemainingSeconds,
                RemainingAttempts = verification.RemainingAttempts,
                ResendAfterSeconds = resendAfter
            };
        }

        /// <inheritdoc />
        public async Task<int> CleanupExpiredVerificationsAsync()
        {
            _logger.LogInformation("[SMS] Süresi dolmuş kayıtlar temizleniyor...");
            
            var cleanedCount = await _verificationRepository.CleanupExpiredAsync();
            
            // Rate limit sayaçlarını da sıfırla
            await _rateLimitRepository.ResetDailyCountersAsync();
            await _rateLimitRepository.ResetHourlyCountersAsync();
            
            _logger.LogInformation("[SMS] {Count} kayıt temizlendi", cleanedCount);
            
            return cleanedCount;
        }

        #region Private Helper Methods

        /// <summary>
        /// Telefon numarasını normalize eder (5xxxxxxxxx formatına çevirir).
        /// </summary>
        private static string NormalizePhoneNumber(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            // Sadece rakamları al
            var digits = Regex.Replace(phone, @"[^\d]", "");

            // Türkiye kodu varsa kaldır
            if (digits.StartsWith("90") && digits.Length > 10)
                digits = digits[2..];

            // Başındaki 0'ı kaldır
            if (digits.StartsWith("0") && digits.Length > 10)
                digits = digits[1..];

            // 10 haneli olmalı (5xx xxx xx xx)
            if (digits.Length != 10 || !digits.StartsWith("5"))
                return string.Empty;

            return digits;
        }

        /// <summary>
        /// Güvenli rastgele OTP kodu oluşturur.
        /// </summary>
        private string GenerateSecureOtpCode()
        {
            var bytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            
            var random = BitConverter.ToUInt32(bytes, 0);
            var max = (uint)Math.Pow(10, _settings.CodeLength);
            var code = (random % max).ToString().PadLeft(_settings.CodeLength, '0');
            
            return code;
        }

        /// <summary>
        /// OTP mesaj metnini formatlar.
        /// </summary>
        private string FormatOtpMessage(string code)
        {
            var expiryMinutes = _settings.ExpirationSeconds / 60;
            return $"Dogrulama kodunuz: {code}\nBu kod {expiryMinutes} dakika gecerlidir.\n- {_settings.AppName}";
        }

        #endregion
    }
}
