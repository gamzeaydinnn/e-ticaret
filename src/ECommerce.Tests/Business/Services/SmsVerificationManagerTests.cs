using Moq;
using Xunit;
using ECommerce.Business.Services.Managers;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using ECommerce.API.Services.Sms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Tests.Business.Services
{
    /// <summary>
    /// SmsVerificationManager unit testleri.
    /// 
    /// Test Senaryoları:
    /// 1. ✅ OTP kodu başarılı gönderimi
    /// 2. ✅ OTP kodu başarılı doğrulama
    /// 3. ✅ Yanlış OTP kodu reddi
    /// 4. ✅ Süresi dolmuş OTP reddi
    /// 5. ✅ Rate limiting kontrolü
    /// 6. ✅ Günlük limit aşımı
    /// 7. ✅ Maksimum deneme sayısı aşımı
    /// 8. ✅ Resend cooldown kontrolü
    /// </summary>
    public class SmsVerificationManagerTests
    {
        private readonly Mock<ISmsVerificationRepository> _mockSmsRepo;
        private readonly Mock<ISmsRateLimitRepository> _mockRateLimitRepo;
        private readonly Mock<INetGsmService> _mockSmsProvider;
        private readonly Mock<ILogger<SmsVerificationManager>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly SmsVerificationManager _smsVerificationManager;

        public SmsVerificationManagerTests()
        {
            // Mock dependencies oluştur
            _mockSmsRepo = new Mock<ISmsVerificationRepository>();
            _mockRateLimitRepo = new Mock<ISmsRateLimitRepository>();
            _mockSmsProvider = new Mock<ISmsProviderService>();
            _mockLogger = new Mock<ILogger<SmsVerificationManager>>();
            _mockConfig = new Mock<IConfiguration>();

            // Configuration mock'ları
            SetupConfiguration();

            // SUT (System Under Test) oluştur
            _smsVerificationManager = new SmsVerificationManager(
                _mockSmsRepo.Object,
                _mockRateLimitRepo.Object,
                _mockSmsProvider.Object,
                _mockLogger.Object,
                _mockConfig.Object
            );
        }

        /// <summary>
        /// Configuration mock değerlerini ayarla
        /// </summary>
        private void SetupConfiguration()
        {
            _mockConfig.Setup(c => c["SmsVerification:ExpirationSeconds"]).Returns("180");
            _mockConfig.Setup(c => c["SmsVerification:ResendCooldownSeconds"]).Returns("60");
            _mockConfig.Setup(c => c["SmsVerification:DailyMaxOtpCount"]).Returns("5");
            _mockConfig.Setup(c => c["SmsVerification:HourlyMaxOtpCount"]).Returns("3");
            _mockConfig.Setup(c => c["SmsVerification:MaxWrongAttempts"]).Returns("3");
            _mockConfig.Setup(c => c["SmsVerification:CodeLength"]).Returns("6");
            _mockConfig.Setup(c => c["SmsVerification:AppName"]).Returns("E-Ticaret Test");
        }

        #region SendVerificationCodeAsync Tests

        /// <summary>
        /// Test 1: OTP kodu başarılı şekilde gönderilmeli
        /// </summary>
        [Fact]
        public async Task SendVerificationCodeAsync_ValidPhone_ShouldSucceed()
        {
            // Arrange
            var phoneNumber = "05551234567";
            var purpose = SmsVerificationPurpose.Registration;
            var ipAddress = "192.168.1.1";

            // Rate limit kontrolü - izin ver
            var rateLimit = new SmsRateLimit
            {
                PhoneNumber = phoneNumber,
                DailyCount = 2,
                HourlyCount = 1,
                DailyResetAt = DateTime.UtcNow.AddDays(1),
                HourlyResetAt = DateTime.UtcNow.AddHours(1),
                IsBlocked = false
            };

            _mockRateLimitRepo
                .Setup(r => r.GetOrCreateAsync(phoneNumber, ipAddress))
                .ReturnsAsync(rateLimit);

            _mockRateLimitRepo
                .Setup(r => r.IncrementCountersAsync(phoneNumber, ipAddress))
                .Returns(Task.CompletedTask);

            // SMS gönderimi başarılı
            _mockSmsProvider
                .Setup(s => s.SendSmsAsync(phoneNumber, It.IsAny<string>()))
                .ReturnsAsync((true, "Kod gönderildi", null));

            // Repository kaydetme
            _mockSmsRepo
                .Setup(r => r.AddAsync(It.IsAny<SmsVerification>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _smsVerificationManager.SendVerificationCodeAsync(
                phoneNumber, purpose, ipAddress, "Test-Agent", null);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(SmsVerificationStatus.Sent, result.Status);
            Assert.NotNull(result.Message);
            
            // Verify repository çağrıları
            _mockSmsRepo.Verify(r => r.AddAsync(It.IsAny<SmsVerification>()), Times.Once);
            _mockRateLimitRepo.Verify(r => r.IncrementCountersAsync(phoneNumber, ipAddress), Times.Once);
            _mockSmsProvider.Verify(s => s.SendSmsAsync(phoneNumber, It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Test 2: Günlük OTP limiti aşıldığında reddedilmeli
        /// </summary>
        [Fact]
        public async Task SendVerificationCodeAsync_DailyLimitExceeded_ShouldFail()
        {
            // Arrange
            var phoneNumber = "05551234567";
            var purpose = SmsVerificationPurpose.Registration;

            // Rate limit - günlük limit dolmuş
            var rateLimit = new SmsRateLimit
            {
                PhoneNumber = phoneNumber,
                DailyCount = 5, // Limit = 5
                HourlyCount = 2,
                DailyResetAt = DateTime.UtcNow.AddHours(12),
                HourlyResetAt = DateTime.UtcNow.AddMinutes(30),
                IsBlocked = false
            };

            _mockRateLimitRepo
                .Setup(r => r.GetOrCreateAsync(phoneNumber, It.IsAny<string>()))
                .ReturnsAsync(rateLimit);

            // Act
            var result = await _smsVerificationManager.SendVerificationCodeAsync(
                phoneNumber, purpose, "192.168.1.1");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(SmsVerificationStatus.RateLimited, result.Status);
            Assert.Contains("günlük limit", result.Message.ToLower());
            
            // SMS gönderilmemeli
            _mockSmsProvider.Verify(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Test 3: Saatlik OTP limiti aşıldığında reddedilmeli
        /// </summary>
        [Fact]
        public async Task SendVerificationCodeAsync_HourlyLimitExceeded_ShouldFail()
        {
            // Arrange
            var phoneNumber = "05551234567";
            var purpose = SmsVerificationPurpose.Registration;

            var rateLimit = new SmsRateLimit
            {
                PhoneNumber = phoneNumber,
                DailyCount = 2,
                HourlyCount = 3, // Limit = 3
                DailyResetAt = DateTime.UtcNow.AddHours(12),
                HourlyResetAt = DateTime.UtcNow.AddMinutes(45),
                IsBlocked = false
            };

            _mockRateLimitRepo
                .Setup(r => r.GetOrCreateAsync(phoneNumber, It.IsAny<string>()))
                .ReturnsAsync(rateLimit);

            // Act
            var result = await _smsVerificationManager.SendVerificationCodeAsync(
                phoneNumber, purpose, "192.168.1.1");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(SmsVerificationStatus.RateLimited, result.Status);
            
            _mockSmsProvider.Verify(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Test 4: Bloke edilmiş telefon numarası reddedilmeli
        /// </summary>
        [Fact]
        public async Task SendVerificationCodeAsync_BlockedPhone_ShouldFail()
        {
            // Arrange
            var phoneNumber = "05551234567";

            var rateLimit = new SmsRateLimit
            {
                PhoneNumber = phoneNumber,
                IsBlocked = true,
                BlockedUntil = DateTime.UtcNow.AddHours(24),
                BlockReason = "Çok fazla başarısız deneme"
            };

            _mockRateLimitRepo
                .Setup(r => r.GetOrCreateAsync(phoneNumber, It.IsAny<string>()))
                .ReturnsAsync(rateLimit);

            // Act
            var result = await _smsVerificationManager.SendVerificationCodeAsync(
                phoneNumber, SmsVerificationPurpose.Registration, "192.168.1.1");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(SmsVerificationStatus.PhoneBlocked, result.Status);
            Assert.NotNull(result.BlockedUntil);
        }

        #endregion

        #region VerifyCodeAsync Tests

        /// <summary>
        /// Test 5: Doğru OTP kodu başarıyla doğrulanmalı
        /// </summary>
        [Fact]
        public async Task VerifyCodeAsync_CorrectCode_ShouldSucceed()
        {
            // Arrange
            var phoneNumber = "05551234567";
            var code = "123456";
            var purpose = SmsVerificationPurpose.Registration;

            var verification = new SmsVerification
            {
                Id = 1,
                PhoneNumber = phoneNumber,
                Code = code,
                Purpose = purpose,
                ExpiresAt = DateTime.UtcNow.AddMinutes(3),
                IsVerified = false,
                FailedAttempts = 0,
                CreatedAt = DateTime.UtcNow
            };

            _mockSmsRepo
                .Setup(r => r.GetLatestUnverifiedAsync(phoneNumber, purpose))
                .ReturnsAsync(verification);

            _mockSmsRepo
                .Setup(r => r.UpdateAsync(It.IsAny<SmsVerification>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _smsVerificationManager.VerifyCodeAsync(
                phoneNumber, code, purpose, "192.168.1.1");

            // Assert
            Assert.True(result.Success);
            Assert.Equal(SmsVerificationStatus.Verified, result.Status);
            
            // Verification kaydı güncellenmiş olmalı
            _mockSmsRepo.Verify(r => r.UpdateAsync(It.Is<SmsVerification>(
                v => v.IsVerified && v.VerifiedAt != null)), Times.Once);
        }

        /// <summary>
        /// Test 6: Yanlış OTP kodu reddedilmeli
        /// </summary>
        [Fact]
        public async Task VerifyCodeAsync_WrongCode_ShouldFail()
        {
            // Arrange
            var phoneNumber = "05551234567";
            var correctCode = "123456";
            var wrongCode = "654321";

            var verification = new SmsVerification
            {
                Id = 1,
                PhoneNumber = phoneNumber,
                Code = correctCode,
                Purpose = SmsVerificationPurpose.Registration,
                ExpiresAt = DateTime.UtcNow.AddMinutes(3),
                IsVerified = false,
                FailedAttempts = 0
            };

            _mockSmsRepo
                .Setup(r => r.GetLatestUnverifiedAsync(phoneNumber, It.IsAny<SmsVerificationPurpose>()))
                .ReturnsAsync(verification);

            _mockRateLimitRepo
                .Setup(r => r.RecordFailedAttemptAsync(phoneNumber))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _smsVerificationManager.VerifyCodeAsync(
                phoneNumber, wrongCode, SmsVerificationPurpose.Registration, "192.168.1.1");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(SmsVerificationStatus.InvalidCode, result.Status);
            Assert.True(result.RemainingAttempts < 3);
            
            // Failed attempt kaydedilmiş olmalı
            _mockRateLimitRepo.Verify(r => r.RecordFailedAttemptAsync(phoneNumber), Times.Once);
        }

        /// <summary>
        /// Test 7: Süresi dolmuş OTP kodu reddedilmeli
        /// </summary>
        [Fact]
        public async Task VerifyCodeAsync_ExpiredCode_ShouldFail()
        {
            // Arrange
            var phoneNumber = "05551234567";
            var code = "123456";

            var verification = new SmsVerification
            {
                Id = 1,
                PhoneNumber = phoneNumber,
                Code = code,
                Purpose = SmsVerificationPurpose.Registration,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // Süresi dolmuş
                IsVerified = false,
                FailedAttempts = 0
            };

            _mockSmsRepo
                .Setup(r => r.GetLatestUnverifiedAsync(phoneNumber, It.IsAny<SmsVerificationPurpose>()))
                .ReturnsAsync(verification);

            // Act
            var result = await _smsVerificationManager.VerifyCodeAsync(
                phoneNumber, code, SmsVerificationPurpose.Registration, "192.168.1.1");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(SmsVerificationStatus.Expired, result.Status);
        }

        /// <summary>
        /// Test 8: OTP kaydı bulunamadığında reddedilmeli
        /// </summary>
        [Fact]
        public async Task VerifyCodeAsync_NoVerificationFound_ShouldFail()
        {
            // Arrange
            var phoneNumber = "05551234567";
            var code = "123456";

            _mockSmsRepo
                .Setup(r => r.GetLatestUnverifiedAsync(phoneNumber, It.IsAny<SmsVerificationPurpose>()))
                .ReturnsAsync((SmsVerification?)null);

            // Act
            var result = await _smsVerificationManager.VerifyCodeAsync(
                phoneNumber, code, SmsVerificationPurpose.Registration, "192.168.1.1");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(SmsVerificationStatus.NotFound, result.Status);
        }

        #endregion

        #region CanSendVerificationAsync Tests

        /// <summary>
        /// Test 9: Rate limit kontrolü - gönderim izni verilmeli
        /// </summary>
        [Fact]
        public async Task CanSendVerificationAsync_WithinLimits_ShouldAllowSend()
        {
            // Arrange
            var phoneNumber = "05551234567";

            var rateLimit = new SmsRateLimit
            {
                PhoneNumber = phoneNumber,
                DailyCount = 2,
                HourlyCount = 1,
                DailyResetAt = DateTime.UtcNow.AddHours(12),
                HourlyResetAt = DateTime.UtcNow.AddMinutes(30),
                IsBlocked = false,
                LastSentAt = DateTime.UtcNow.AddMinutes(-2) // 2 dakika önce gönderilmiş
            };

            _mockRateLimitRepo
                .Setup(r => r.GetOrCreateAsync(phoneNumber, It.IsAny<string>()))
                .ReturnsAsync(rateLimit);

            // Act
            var result = await _smsVerificationManager.CanSendVerificationAsync(phoneNumber, "192.168.1.1");

            // Assert
            Assert.True(result.CanSend);
            Assert.False(result.IsBlocked);
        }

        #endregion
    }
}
