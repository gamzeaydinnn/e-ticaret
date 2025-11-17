using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.DTOs.Auth;
using ECommerce.Data.Context;
using ECommerce.Data.Repositories;
using ECommerce.Entities.Concrete;
using ECommerce.Infrastructure.Config;
using ECommerce.Infrastructure.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.Net;
using Microsoft.AspNetCore.DataProtection;

namespace ECommerce.Tests.Services
{
    public class AuthManagerTests
    {
        private static ECommerceDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ECommerceDbContext(options);
        }

        private static IConfiguration BuildConfiguration()
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "super_secret_jwt_signing_key_1234567890",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["AppSettings:BaseUrl"] = "https://test.local"
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        private static EmailSender CreateEmailSender()
        {
            var emailSettings = new EmailSettings
            {
                FromEmail = "no-reply@test.local",
                FromName = "Test",
                // Use pickup folder so no real SMTP is required during tests
                UsePickupFolder = true,
                PickupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestEmails")
            };

            var options = Options.Create(emailSettings);

            var envMock = new Mock<IHostEnvironment>();
            envMock.SetupGet(e => e.ContentRootPath)
                .Returns(Directory.GetCurrentDirectory());

            return new EmailSender(options, envMock.Object);
        }

        private static UserManager<User> CreateUserManager(ECommerceDbContext context)
{
    var store = new UserStore<User, IdentityRole<int>, ECommerceDbContext, int>(context);

    var identityOptions = new IdentityOptions();
    identityOptions.Password.RequireDigit = false;
    identityOptions.Password.RequireLowercase = false;
    identityOptions.Password.RequireUppercase = false;
    identityOptions.Password.RequireNonAlphanumeric = false;
    identityOptions.Password.RequiredLength = 6;

    var options = Options.Create(identityOptions);

    var userValidators = new List<IUserValidator<User>>();
    var passwordValidators = new List<IPasswordValidator<User>>
    {
        new PasswordValidator<User>()
    };

    var logger = new Mock<ILogger<UserManager<User>>>().Object;

    var userManager = new UserManager<User>(
        store,
        options,
        new PasswordHasher<User>(),
        userValidators,
        passwordValidators,
        new SimpleLookupNormalizer(),
        new IdentityErrorDescriber(),
        null,
        logger
    );

    // ⭐ Email token provider'ı TEST ortamında manuel ekle
    var dataProtectionProvider = new EphemeralDataProtectionProvider();
    var protector = dataProtectionProvider.CreateProtector("Identity");
    var tokenProviderOptions = Options.Create(new DataProtectionTokenProviderOptions());
    var tokenProviderLogger = new Mock<ILogger<DataProtectorTokenProvider<User>>>().Object;

    var provider = new DataProtectorTokenProvider<User>(
        dataProtectionProvider,
        tokenProviderOptions,
        tokenProviderLogger
    );

    userManager.RegisterTokenProvider(TokenOptions.DefaultEmailProvider, provider);
    userManager.Options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;

    return userManager;
}

        private static AuthManager CreateAuthManager(
            UserManager<User> userManager,
            IConfiguration config,
            EmailSender emailSender,
            ECommerceDbContext context,
            string ipAddress = "127.0.0.1")
        {
            var refreshRepository = new RefreshTokenRepository(context);
            var httpContextAccessor = new HttpContextAccessor();
            var defaultContext = new DefaultHttpContext();
            if (IPAddress.TryParse(ipAddress, out var parsed))
            {
                defaultContext.Connection.RemoteIpAddress = parsed;
            }
            httpContextAccessor.HttpContext = defaultContext;

            return new AuthManager(userManager, config, emailSender, refreshRepository, httpContextAccessor);
        }

        private static string GenerateExpiredToken(int userId, string email, string role, string key, string jti)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, jti)
            };

            var token = new JwtSecurityToken(
                issuer: "TestIssuer",
                audience: "TestAudience",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(-5), // treat as expired
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private class SimpleLookupNormalizer : ILookupNormalizer
        {
            public string NormalizeEmail(string email)
            {
                return email?.ToUpperInvariant() ?? string.Empty;
            }

            public string NormalizeName(string name)
            {
                return name?.ToUpperInvariant() ?? string.Empty;
            }
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrow_WhenUserAlreadyExists()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var userManager = CreateUserManager(context);
            var config = BuildConfiguration();
            var emailSender = CreateEmailSender();
            var authManager = CreateAuthManager(userManager, config, emailSender, context);

            var existingUser = new User
            {
                Email = "existing@test.com",
                UserName = "existing@test.com",
                FirstName = "Existing",
                LastName = "User"
            };

            await userManager.CreateAsync(existingUser, "Password123");

            var dto = new RegisterDto
            {
                Email = "existing@test.com",
                Password = "Password123",
                FirstName = "New",
                LastName = "User"
            };

            // Act
            var exception = await Assert.ThrowsAsync<Exception>(() => authManager.RegisterAsync(dto));

            // Assert
            Assert.Equal("User already exists", exception.Message);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrow_WhenCreateFails()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var userManager = CreateUserManager(context);
            var config = BuildConfiguration();
            var emailSender = CreateEmailSender();
            var authManager = CreateAuthManager(userManager, config, emailSender, context);

            // Intentionally use an invalid password (too short) to force Identity failure
            var dto = new RegisterDto
            {
                Email = "shortpass@test.com",
                Password = "123",
                FirstName = "Short",
                LastName = "Password"
            };

            // Act
            var exception = await Assert.ThrowsAsync<Exception>(() => authManager.RegisterAsync(dto));

            // Assert
            Assert.StartsWith("Kayıt işlemi başarısız:", exception.Message);
        }

        [Fact]
        public async Task RegisterAsync_ShouldCreateUser_AndReturnEmptyToken_WhenSuccessful()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var userManager = CreateUserManager(context);
            var config = BuildConfiguration();
            var emailSender = CreateEmailSender();
            var authManager = CreateAuthManager(userManager, config, emailSender, context);

            var dto = new RegisterDto
            {
                Email = "newuser@test.com",
                Password = "Password123",
                FirstName = "New",
                LastName = "User"
            };

            // Act
            var result = await authManager.RegisterAsync(dto);

            // Assert
            Assert.Equal(string.Empty, result);

            var createdUser = await userManager.FindByEmailAsync(dto.Email);
            Assert.NotNull(createdUser);
            Assert.Equal(dto.Email, createdUser!.Email);
            Assert.Equal(dto.FirstName, createdUser.FirstName);
            Assert.Equal(dto.LastName, createdUser.LastName);
            Assert.Equal("User", createdUser.Role);
            Assert.True(createdUser.IsActive);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrow_WhenUserNotFound()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var userManager = CreateUserManager(context);
            var config = BuildConfiguration();
            var emailSender = CreateEmailSender();
            var authManager = CreateAuthManager(userManager, config, emailSender, context);

            var dto = new LoginDto
            {
                Email = "missing@test.com",
                Password = "Password123"
            };

            // Act
            var exception = await Assert.ThrowsAsync<Exception>(() => authManager.LoginAsync(dto));

            // Assert
            Assert.Equal("Kullanıcı bulunamadı", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrow_WhenEmailNotConfirmed()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var userManager = CreateUserManager(context);
            var config = BuildConfiguration();
            var emailSender = CreateEmailSender();
            var authManager = CreateAuthManager(userManager, config, emailSender, context);

            var user = new User
            {
                Email = "unconfirmed@test.com",
                UserName = "unconfirmed@test.com",
                FirstName = "Unconfirmed",
                LastName = "User",
                EmailConfirmed = false
            };

            await userManager.CreateAsync(user, "Password123");

            var dto = new LoginDto
            {
                Email = "unconfirmed@test.com",
                Password = "Password123"
            };

            // Act
            var exception = await Assert.ThrowsAsync<Exception>(() => authManager.LoginAsync(dto));

            // Assert
            Assert.Equal("E-posta adresiniz doğrulanmamış. Lütfen e-postanızı doğrulayın.", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrow_WhenPasswordInvalid()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var userManager = CreateUserManager(context);
            var config = BuildConfiguration();
            var emailSender = CreateEmailSender();
            var authManager = CreateAuthManager(userManager, config, emailSender, context);

            var user = new User
            {
                Email = "user@test.com",
                UserName = "user@test.com",
                FirstName = "Test",
                LastName = "User",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(user, "CorrectPassword");

            var dto = new LoginDto
            {
                Email = "user@test.com",
                Password = "WrongPassword"
            };

            // Act
            var exception = await Assert.ThrowsAsync<Exception>(() => authManager.LoginAsync(dto));

            // Assert
            Assert.Equal("Şifre yanlış", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnJwtToken_WhenCredentialsAreValid()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var userManager = CreateUserManager(context);
            var config = BuildConfiguration();
            var emailSender = CreateEmailSender();
            var authManager = CreateAuthManager(userManager, config, emailSender, context);

            var user = new User
            {
                Email = "valid@test.com",
                UserName = "valid@test.com",
                FirstName = "Valid",
                LastName = "User",
                Role = "User",
                EmailConfirmed = true
            };

            const string password = "Password123";
            await userManager.CreateAsync(user, password);

            var dto = new LoginDto
            {
                Email = "valid@test.com",
                Password = password
            };

            // Act
            var tokens = await authManager.LoginAsync(dto);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(tokens.accessToken));
            Assert.False(string.IsNullOrWhiteSpace(tokens.refreshToken));

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(tokens.accessToken);

            Assert.Equal("TestIssuer", jwt.Issuer);
            Assert.Equal("TestAudience", jwt.Audiences is null ? null : Assert.Single(jwt.Audiences));

            Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
            Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == user.Role);
            Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldThrow_WhenTokenIsInvalid()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var userManager = CreateUserManager(context);
            var config = BuildConfiguration();
            var emailSender = CreateEmailSender();
            var authManager = CreateAuthManager(userManager, config, emailSender, context);

            var invalidToken = "this_is_not_a_valid_jwt";

            // Act
            var exception = await Assert.ThrowsAsync<Exception>(() => authManager.RefreshTokenAsync(invalidToken, "dummy-refresh"));

            // Assert
            Assert.Equal("Invalid token", exception.Message);
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldThrow_WhenRefreshTokenMissing()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var userManager = CreateUserManager(context);
            var config = BuildConfiguration();
            var emailSender = CreateEmailSender();
            var authManager = CreateAuthManager(userManager, config, emailSender, context);

            var user = new User
            {
                Email = "unknown@test.com",
                UserName = "unknown@test.com",
                FirstName = "Unknown",
                LastName = "User",
                Role = "User",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(user, "Password123");

            var jti = Guid.NewGuid().ToString();
            var token = GenerateExpiredToken(user.Id, user.Email!, user.Role!, config["Jwt:Key"]!, jti);

            // Act
            var exception = await Assert.ThrowsAsync<Exception>(() => authManager.RefreshTokenAsync(token, "refresh-token"));

            // Assert
            Assert.Equal("Refresh token bulunamadı.", exception.Message);
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnNewToken_WhenUserExists()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var userManager = CreateUserManager(context);
            var config = BuildConfiguration();
            var emailSender = CreateEmailSender();
            var authManager = CreateAuthManager(userManager, config, emailSender, context);

            var user = new User
            {
                Email = "refresh@test.com",
                UserName = "refresh@test.com",
                FirstName = "Refresh",
                LastName = "User",
                Role = "User",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(user, "Password123");

            var refreshTokenValue = "refresh-token";
            var jti = Guid.NewGuid().ToString();
            var token = GenerateExpiredToken(user.Id, user.Email!, user.Role!, config["Jwt:Key"]!, jti);

            context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenValue,
                JwtId = jti,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedIp = "127.0.0.1"
            });
            await context.SaveChangesAsync();

            // Act
            var newToken = await authManager.RefreshTokenAsync(token, refreshTokenValue);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(newToken));
            Assert.NotEqual(token, newToken);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(newToken);

            Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
            Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == user.Role);
            Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());

            var storedToken = await context.RefreshTokens.FirstAsync();
            Assert.NotEqual(jti, storedToken.JwtId);
        }
    }
}
