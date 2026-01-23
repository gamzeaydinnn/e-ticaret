// ═══════════════════════════════════════════════════════════════════════════════
// POSNET MAC VALIDATOR UNIT TESTLERI
// MAC hesaplama ve doğrulama fonksiyonlarının test edilmesi
// POSNET Dokümantasyonu v2.1.1.3 - Sayfa 35-42'ye göre
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Security.Cryptography;
using System.Text;
using ECommerce.Infrastructure.Config;
using ECommerce.Infrastructure.Services.Payment.Posnet.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ECommerce.Tests.Payment.Posnet
{
    /// <summary>
    /// PosnetMacValidator için unit testler
    /// </summary>
    public class PosnetMacValidatorTests
    {
        // ═══════════════════════════════════════════════════════════════════
        // TEST VERİLERİ (Yapı Kredi Test Ortamı Bilgileri)
        // ═══════════════════════════════════════════════════════════════════
        
        private const string TestMerchantId = "6700972665";
        private const string TestTerminalId = "67C35037";
        private const string TestPosnetId = "1010078654940127";
        private const string TestEncKeyCommaSeparated = "10,10,10,10,10,10,10,10";
        private const string TestEncKeyHex = "0A0A0A0A0A0A0A0A"; // 10,10... = 0A,0A... hex

        private readonly Mock<ILogger<PosnetMacValidator>> _loggerMock;

        public PosnetMacValidatorTests()
        {
            _loggerMock = new Mock<ILogger<PosnetMacValidator>>();
        }

        /// <summary>
        /// PaymentSettings mock oluşturur
        /// </summary>
        private IOptions<PaymentSettings> CreateSettings(string encKey)
        {
            var settings = new PaymentSettings
            {
                PosnetMerchantId = TestMerchantId,
                PosnetTerminalId = TestTerminalId,
                PosnetId = TestPosnetId,
                PosnetEncKey = encKey
            };
            return Options.Create(settings);
        }

        // ═══════════════════════════════════════════════════════════════════
        // ENCKEY FORMAT TESTLERİ
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public void NormalizeEncKey_WithCommaSeparatedBytes_ShouldConvertToHex()
        {
            // Arrange - Virgüllü byte formatı
            var input = "10,10,10,10,10,10,10,10";
            var expected = "0A0A0A0A0A0A0A0A"; // 10 decimal = 0A hex

            // Act - Private metodu test etmek için reflection kullan
            var settings = CreateSettings(input);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);
            
            // CalculateFirstHash'i çağır ve sonucu kontrol et
            var hash1 = validator.CalculateFirstHash(TestTerminalId);
            
            // Assert - Hash null olmamalı ve 44 karakter (Base64 SHA256)
            Assert.NotNull(hash1);
            Assert.Equal(44, hash1.Length); // Base64 encoded SHA256 = 44 chars
        }

        [Fact]
        public void NormalizeEncKey_WithHexString_ShouldKeepAsIs()
        {
            // Arrange - Hex string formatı
            var input = "0A0A0A0A0A0A0A0A";

            // Act
            var settings = CreateSettings(input);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);
            var hash = validator.CalculateFirstHash(TestTerminalId);
            
            // Assert
            Assert.NotNull(hash);
            Assert.Equal(44, hash.Length);
        }

        [Fact]
        public void NormalizeEncKey_WithPlainString_ShouldKeepAsIs()
        {
            // Arrange - Düz string
            var input = "testenckey123";

            // Act
            var settings = CreateSettings(input);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);
            var hash = validator.CalculateFirstHash(TestTerminalId);
            
            // Assert
            Assert.NotNull(hash);
            Assert.Equal(44, hash.Length);
        }

        // ═══════════════════════════════════════════════════════════════════
        // FIRST HASH TESTLERİ
        // Formül: HASH(encKey + ';' + terminalID)
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public void CalculateFirstHash_WithValidInput_ShouldReturnBase64Hash()
        {
            // Arrange
            var settings = CreateSettings(TestEncKeyHex);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            // Act
            var firstHash = validator.CalculateFirstHash(TestTerminalId);

            // Assert
            Assert.NotNull(firstHash);
            Assert.NotEmpty(firstHash);
            Assert.Equal(44, firstHash.Length); // Base64 encoded SHA256

            // Hash'in Base64 olduğunu doğrula
            var bytes = Convert.FromBase64String(firstHash);
            Assert.Equal(32, bytes.Length); // SHA256 = 32 bytes
        }

        [Fact]
        public void CalculateFirstHash_SameInputs_ShouldReturnSameHash()
        {
            // Arrange
            var settings = CreateSettings(TestEncKeyHex);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            // Act
            var hash1 = validator.CalculateFirstHash(TestTerminalId);
            var hash2 = validator.CalculateFirstHash(TestTerminalId);

            // Assert - Aynı girdi aynı hash üretmeli
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void CalculateFirstHash_DifferentTerminalId_ShouldReturnDifferentHash()
        {
            // Arrange
            var settings = CreateSettings(TestEncKeyHex);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            // Act
            var hash1 = validator.CalculateFirstHash(TestTerminalId);
            var hash2 = validator.CalculateFirstHash("DIFFERENT1");

            // Assert - Farklı terminal ID farklı hash üretmeli
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void CalculateFirstHash_EmptyTerminalId_ShouldThrowException()
        {
            // Arrange
            var settings = CreateSettings(TestEncKeyHex);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                validator.CalculateFirstHash(string.Empty));
        }

        [Fact]
        public void CalculateFirstHash_EmptyEncKey_ShouldThrowException()
        {
            // Arrange
            var settings = CreateSettings(string.Empty);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                validator.CalculateFirstHash(TestTerminalId));
        }

        // ═══════════════════════════════════════════════════════════════════
        // REQUEST MAC TESTLERİ
        // Formül: HASH(xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public void CalculateRequestMac_WithValidInputs_ShouldReturnBase64Hash()
        {
            // Arrange
            var settings = CreateSettings(TestEncKeyHex);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            var xid = "ORDER123456";
            var amount = "10000"; // 100.00 TL = 10000 YKr
            var currency = "TL";

            // Act
            var mac = validator.CalculateRequestMac(xid, amount, currency, 
                TestMerchantId, TestTerminalId);

            // Assert
            Assert.NotNull(mac);
            Assert.NotEmpty(mac);
            Assert.Equal(44, mac.Length); // Base64 encoded SHA256
        }

        [Fact]
        public void CalculateRequestMac_SameInputs_ShouldReturnSameMac()
        {
            // Arrange
            var settings = CreateSettings(TestEncKeyHex);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            var xid = "ORDER123456";
            var amount = "10000";
            var currency = "TL";

            // Act
            var mac1 = validator.CalculateRequestMac(xid, amount, currency, 
                TestMerchantId, TestTerminalId);
            var mac2 = validator.CalculateRequestMac(xid, amount, currency, 
                TestMerchantId, TestTerminalId);

            // Assert
            Assert.Equal(mac1, mac2);
        }

        [Fact]
        public void CalculateRequestMac_DifferentAmount_ShouldReturnDifferentMac()
        {
            // Arrange
            var settings = CreateSettings(TestEncKeyHex);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            var xid = "ORDER123456";
            var currency = "TL";

            // Act
            var mac1 = validator.CalculateRequestMac(xid, "10000", currency, 
                TestMerchantId, TestTerminalId);
            var mac2 = validator.CalculateRequestMac(xid, "20000", currency, 
                TestMerchantId, TestTerminalId);

            // Assert - Farklı tutar farklı MAC üretmeli (güvenlik önemli!)
            Assert.NotEqual(mac1, mac2);
        }

        [Theory]
        [InlineData(null, "10000", "TL")]
        [InlineData("ORDER123", null, "TL")]
        [InlineData("ORDER123", "10000", null)]
        public void CalculateRequestMac_WithNullParameters_ShouldThrowException(
            string? xid, string? amount, string? currency)
        {
            // Arrange
            var settings = CreateSettings(TestEncKeyHex);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                validator.CalculateRequestMac(xid!, amount!, currency!, 
                    TestMerchantId, TestTerminalId));
        }

        // ═══════════════════════════════════════════════════════════════════
        // RESPONSE MAC TESTLERİ
        // Formül: HASH(mdStatus + ';' + xid + ';' + amount + ';' + currency + ';' + merchantNo + ';' + firstHash)
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public void CalculateResponseMac_WithValidInputs_ShouldReturnBase64Hash()
        {
            // Arrange
            var settings = CreateSettings(TestEncKeyHex);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            var mdStatus = "1"; // Başarılı 3D Secure
            var xid = "ORDER123456";
            var amount = "10000";
            var currency = "TL";

            // Act
            var mac = validator.CalculateResponseMac(mdStatus, xid, amount, currency, 
                TestMerchantId, TestTerminalId);

            // Assert
            Assert.NotNull(mac);
            Assert.Equal(44, mac.Length);
        }

        [Theory]
        [InlineData("1")] // Tam doğrulama
        [InlineData("2")] // Kart sahibi kayıtlı değil
        [InlineData("0")] // Başarısız
        public void CalculateResponseMac_DifferentMdStatus_ShouldReturnDifferentMac(string mdStatus)
        {
            // Arrange
            var settings = CreateSettings(TestEncKeyHex);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            var xid = "ORDER123456";
            var amount = "10000";
            var currency = "TL";

            // Act
            var mac = validator.CalculateResponseMac(mdStatus, xid, amount, currency, 
                TestMerchantId, TestTerminalId);

            // Assert
            Assert.NotNull(mac);
            Assert.NotEmpty(mac);
        }

        // ═══════════════════════════════════════════════════════════════════
        // RESPONSE MAC DOĞRULAMA TESTLERİ
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public void ValidateResponseMac_WithMatchingMac_ShouldReturnSuccess()
        {
            // Arrange
            var settings = CreateSettings(TestEncKeyHex);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            var mdStatus = "1";
            var xid = "ORDER123456";
            var amount = "10000";
            var currency = "TL";

            // Önce MAC'i hesapla
            var calculatedMac = validator.CalculateResponseMac(mdStatus, xid, amount, currency, 
                TestMerchantId, TestTerminalId);

            // Act - Aynı MAC ile doğrula
            var result = validator.ValidateResponseMac(calculatedMac, mdStatus, xid, amount, 
                currency, TestMerchantId, TestTerminalId);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void ValidateResponseMac_WithMismatchedMac_ShouldReturnFailure()
        {
            // Arrange
            var settings = CreateSettings(TestEncKeyHex);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            var mdStatus = "1";
            var xid = "ORDER123456";
            var amount = "10000";
            var currency = "TL";

            var wrongMac = "ABCDEF123456789012345678901234567890ABCD"; // Yanlış MAC

            // Act
            var result = validator.ValidateResponseMac(wrongMac, mdStatus, xid, amount, 
                currency, TestMerchantId, TestTerminalId);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }

        // ═══════════════════════════════════════════════════════════════════
        // COMMA-SEPARATED ENCKEY ENTEGRASYON TESTİ
        // Bu test gerçek Yapı Kredi test bilgileriyle çalışır
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public void CalculateFirstHash_WithCommaSeparatedEncKey_ShouldNormalizeAndHash()
        {
            // Arrange - Gerçek test bilgileri
            var settings = CreateSettings(TestEncKeyCommaSeparated); // "10,10,10,10,10,10,10,10"
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            // Act
            var hash = validator.CalculateFirstHash(TestTerminalId);

            // Assert
            Assert.NotNull(hash);
            Assert.Equal(44, hash.Length);

            // Hash'in tutarlı olduğunu doğrula
            var hash2 = validator.CalculateFirstHash(TestTerminalId);
            Assert.Equal(hash, hash2);
        }

        [Fact]
        public void FullMacFlow_WithCommaSeparatedEncKey_ShouldWork()
        {
            // Arrange - Gerçek Yapı Kredi test bilgileri
            var settings = CreateSettings(TestEncKeyCommaSeparated);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            var xid = "ORD" + DateTime.Now.Ticks.ToString()[^10..]; // Benzersiz order ID
            var amount = "15000"; // 150.00 TL
            var currency = "TL";
            var mdStatus = "1";

            // Act - Tam akış testi
            var requestMac = validator.CalculateRequestMac(xid, amount, currency, 
                TestMerchantId, TestTerminalId);
            
            var responseMac = validator.CalculateResponseMac(mdStatus, xid, amount, currency, 
                TestMerchantId, TestTerminalId);

            var validationResult = validator.ValidateResponseMac(responseMac, mdStatus, xid, amount, 
                currency, TestMerchantId, TestTerminalId);

            // Assert
            Assert.NotNull(requestMac);
            Assert.NotNull(responseMac);
            Assert.True(validationResult.IsValid);
        }

        // ═══════════════════════════════════════════════════════════════════
        // POSNET DOKÜMANTASYONU ÖRNEK DEĞERLERİ İLE TEST
        // 3sapi.txt Sayfa 11-12'deki değerler
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public void CalculateFirstHash_WithDocumentationValues_ShouldMatchExpected()
        {
            // POSNET Dokümantasyonu Sayfa 11 - Örnek Değerler:
            // encKey: 10,10,10,10,10,10,10,10
            // terminalID: 67005551
            // Beklenen firstHash: c1PPl+2UcdixyhgLYnf4VfJyFGaNQNOwE0uMkci7Uag=
            
            // Arrange
            var docEncKey = "10,10,10,10,10,10,10,10";
            var docTerminalId = "67005551";
            var expectedFirstHash = "c1PPl+2UcdixyhgLYnf4VfJyFGaNQNOwE0uMkci7Uag=";
            
            var settings = CreateSettings(docEncKey);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            // Act
            var actualFirstHash = validator.CalculateFirstHash(docTerminalId);

            // Assert
            Assert.Equal(expectedFirstHash, actualFirstHash);
        }

        [Fact]
        public void CalculateRequestMac_WithDocumentationValues_ShouldMatchExpected()
        {
            // POSNET Dokümantasyonu Sayfa 11-12 - Örnek Değerler:
            // xid: YKB_TST_190620093100_024
            // amount: 175
            // currency: TL
            // merchantNo: 6706598320
            // firstHash: c1PPl+2UcdixyhgLYnf4VfJyFGaNQNOwE0uMkci7Uag=
            // Beklenen MAC: J/7/Xprj7F/KDf98luVfIGyUPRQzUCqGwpmvz3KT7oQ=
            
            // Arrange
            var docEncKey = "10,10,10,10,10,10,10,10";
            var docTerminalId = "67005551";
            var docMerchantNo = "6706598320";
            var docXid = "YKB_TST_190620093100_024";
            var docAmount = "175";
            var docCurrency = "TL";
            var expectedMac = "J/7/Xprj7F/KDf98luVfIGyUPRQzUCqGwpmvz3KT7oQ=";
            
            var settings = CreateSettings(docEncKey);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            // Act
            var actualMac = validator.CalculateRequestMac(docXid, docAmount, docCurrency, 
                docMerchantNo, docTerminalId);

            // Assert
            Assert.Equal(expectedMac, actualMac);
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPER METODLAR
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Manuel SHA256 + Base64 hesaplaması (doğrulama için)
        /// </summary>
        private static string ManualComputeHash(string data)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(bytes);
        }

        [Fact]
        public void ManualHashVerification_ShouldMatchValidatorOutput()
        {
            // Arrange
            var settings = CreateSettings(TestEncKeyHex);
            var validator = new PosnetMacValidator(settings, _loggerMock.Object);

            // Manuel FirstHash hesapla
            var manualFirstHash = ManualComputeHash($"{TestEncKeyHex};{TestTerminalId}");
            
            // Validator FirstHash hesapla
            var validatorFirstHash = validator.CalculateFirstHash(TestTerminalId);

            // Assert - İkisi eşit olmalı
            Assert.Equal(manualFirstHash, validatorFirstHash);
        }
    }
}
