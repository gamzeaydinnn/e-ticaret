using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ECommerce.Infrastructure.Config;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace ECommerce.Infrastructure.Services.MicroServices
{
    /// <summary>
    /// VPN Test ortamı için Mikro API entegrasyon servisi
    /// appsettings.VpnTest.json'dan konfigürasyonu otomatik yükler
    /// </summary>
    public class MikroApiVpnTestService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MikroSettings _mikroSettings;
        private readonly ILogger<MikroApiVpnTestService> _logger;
        private static readonly Regex Md5Regex = new("^[a-fA-F0-9]{32}$", RegexOptions.Compiled);

        public MikroApiVpnTestService(
            IHttpClientFactory httpClientFactory,
            IOptions<MikroSettings> mikroSettings,
            ILogger<MikroApiVpnTestService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _mikroSettings = mikroSettings.Value;
            _logger = logger;

            // 🔍 Init sırasında config'i log et (debugging için)
            LogConfiguration();
        }

        /// <summary>
        /// Konfigürasyonu loglar
        /// </summary>
        private void LogConfiguration()
        {
            _logger.LogInformation("═══ MikroSettings VPN Test Konfigürasyonu ═══");
            _logger.LogInformation($"📡 API URL: {_mikroSettings.ApiUrl}");
            _logger.LogInformation($"📋 Firma Kodu: {_mikroSettings.FirmaKodu}");
            _logger.LogInformation($"👤 Kullanıcı Kodu: {_mikroSettings.KullaniciKodu}");
            _logger.LogInformation($"📅 Çalışma Yılı: {_mikroSettings.CalismaYili}");
            _logger.LogInformation("═════════════════════════════════════════════");
        }

        /// <summary>
        /// Mikro API'ye giriş yapma (APILogin endpoint'i)
        /// </summary>
        public async Task<ApiLoginResponse> LoginAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var outgoingPassword = ResolveOutgoingPassword();
                
                var loginRequest = new
                {
                    ApiKey = _mikroSettings.ApiKey,
                    CalismaYili = _mikroSettings.CalismaYili,
                    FirmaKodu = _mikroSettings.FirmaKodu,
                    KullaniciKodu = _mikroSettings.KullaniciKodu,
                    Sifre = outgoingPassword,
                    SubeNo = _mikroSettings.DefaultSubeNo
                };

                var url = $"{_mikroSettings.ApiUrl}/Api/APILogin";
                _logger.LogInformation($"🔐 Login isteği gönderiliyor: {url}");

                var content = new StringContent(
                    JsonSerializer.Serialize(loginRequest),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"📨 Login Yanıtı Status: {response.StatusCode}");
                _logger.LogInformation($"📨 Login Yanıtı Body: {responseContent}");

                return JsonSerializer.Deserialize<ApiLoginResponse>(responseContent) 
                    ?? new ApiLoginResponse { IsError = true };
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Login hatası: {ex.Message}");
                return new ApiLoginResponse { IsError = true, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Mikro API'ye gönderilecek şifreyi belirler.
        /// - PasswordIsPreHashed=true veya 32 haneli hex ise doğrudan kullanır
        /// - Diğer durumda günlük formatla MD5 üretir: yyyy-MM-dd + " " + şifre
        /// </summary>
        private string ResolveOutgoingPassword()
        {
            var password = _mikroSettings.Sifre;
            if (password == null) return string.Empty;

            if (_mikroSettings.PasswordIsPreHashed || Md5Regex.IsMatch(password))
            {
                return password.ToLowerInvariant();
            }

            var plainPassword = string.IsNullOrWhiteSpace(password) ? string.Empty : password;
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            return GenerateMd5($"{today} {plainPassword}");
        }

        /// <summary>
        /// Ürün/Stok sorgulaması (AnaUrun endpoint'i)
        /// </summary>
        public async Task<string> GetProductAsync(string productKey)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_mikroSettings.ApiUrl}/Api/AnaUrun?anahtar={productKey}";
                
                _logger.LogInformation($"🔍 Ürün sorgulanıyor: {url}");

                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"📦 Ürün Yanıtı: {content}");

                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Ürün sorgusu hatası: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Müşteri sorgulaması (AnaMusteri endpoint'i)
        /// </summary>
        public async Task<string> GetCustomerAsync(string customerKey)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_mikroSettings.ApiUrl}/Api/AnaMusteri?musteriAnahtari={customerKey}";
                
                _logger.LogInformation($"👥 Müşteri sorgulanıyor: {url}");

                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"👥 Müşteri Yanıtı: {content}");

                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Müşteri sorgusu hatası: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sistem bilgisi al (AnaBilgisayar endpoint'i)
        /// </summary>
        public async Task<string> GetSystemInfoAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_mikroSettings.ApiUrl}/Api/AnaBilgisayar";
                
                _logger.LogInformation($"⚙️ Sistem bilgisi alınıyor: {url}");

                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"⚙️ Sistem Bilgisi Yanıtı: {content}");

                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Sistem bilgisi alma hatası: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// MD5 hash'ı hesapla
        /// </summary>
        private string GenerateMd5(string input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
                var hashBytes = md5.ComputeHash(inputBytes);
                return System.Convert.ToHexString(hashBytes).ToLowerInvariant();
            }
        }
    }

    /// <summary>
    /// API Login yanıtı modeli
    /// </summary>
    public class ApiLoginResponse
    {
        public bool IsError { get; set; }
        public string? ErrorMessage { get; set; }
        public object? Data { get; set; }
    }
}
