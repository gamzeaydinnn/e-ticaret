using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ECommerce.Core.Interfaces;

namespace ECommerce.API.Services.Sms;

/// <summary>
/// NetGSM SMS gönderim servisi implementasyonu.
/// Hem INetGsmService hem de ISmsProvider interface'lerini implement eder.
/// </summary>
public class NetGsmService : INetGsmService, ISmsProvider
{
    private readonly HttpClient _httpClient;
    private readonly NetGsmSettings _settings;
    private readonly ILogger<NetGsmService> _logger;

    // NetGSM API Endpoints (SSL zorunlu - https)
    private const string SMS_API_URL = "https://api.netgsm.com.tr/sms/rest/v2/send";
    private const string BALANCE_API_URL = "https://api.netgsm.com.tr/balance";
    
    // Not: OTP için ayrı endpoint yok, SMS_API_URL kullanılır (NetGSM API dok.)

    public NetGsmService(
        HttpClient httpClient,
        IOptions<NetGsmSettings> settings,
        ILogger<NetGsmService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Settings null kontrolü - detaylı hata mesajı
        if (settings?.Value == null)
        {
            _logger.LogError("[NetGSM] NetGsmSettings configuration bulunamadı! appsettings.json'da 'NetGsm' section'ını kontrol edin.");
            throw new InvalidOperationException("NetGSM ayarları yapılandırılmamış! appsettings.json'da 'NetGsm' bölümünü kontrol edin.");
        }

        _settings = settings.Value;

        // Debug log - ayarların yüklenip yüklenmediğini görmek için
        _logger.LogInformation("[NetGSM] Constructor çağrıldı. UserCode='{UserCode}', Password='{PasswordMask}', MsgHeader='{MsgHeader}', AppName='{AppName}'", 
            _settings.UserCode ?? "NULL", 
            string.IsNullOrWhiteSpace(_settings.Password) ? "EMPTY" : "***",
            _settings.MsgHeader ?? "NULL", 
            _settings.AppName ?? "NULL");

        // Kritik ayarların validasyonu (SOLID - Fail Fast prensibi)
        if (string.IsNullOrWhiteSpace(_settings.UserCode))
        {
            _logger.LogError("[NetGSM] UserCode boş! Değer: '{Value}'", _settings.UserCode ?? "NULL");
            throw new InvalidOperationException("NetGSM UserCode yapılandırılmamış! appsettings.json veya User Secrets'ta 'NetGsm:UserCode' değerini kontrol edin.");
        }
        
        if (string.IsNullOrWhiteSpace(_settings.Password))
        {
            _logger.LogError("[NetGSM] Password boş!");
            throw new InvalidOperationException("NetGSM Password yapılandırılmamış! appsettings.json veya User Secrets'ta 'NetGsm:Password' değerini kontrol edin.");
        }
        
        if (string.IsNullOrWhiteSpace(_settings.MsgHeader))
        {
            _logger.LogError("[NetGSM] MsgHeader boş! Değer: '{Value}'", _settings.MsgHeader ?? "NULL");
            throw new InvalidOperationException("NetGSM MsgHeader (Gönderici Adı) yapılandırılmamış! API zorunlu parametre. appsettings.json veya User Secrets'ta 'NetGsm:MsgHeader' değerini kontrol edin.");
        }

        _logger.LogInformation("[NetGSM] Constructor - Settings başarıyla yüklendi: UserCode={UserCode}, MsgHeader={MsgHeader}, AppName={AppName}", 
            _settings.UserCode, _settings.MsgHeader, _settings.AppName);

        // Basic Auth header ayarla (NetGSM API dokümantasyonu: HTTP Basic Authentication)
        var authBytes = Encoding.UTF8.GetBytes($"{_settings.UserCode}:{_settings.Password}");
        var authBase64 = Convert.ToBase64String(authBytes);
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Basic", authBase64);
    }

    /// <summary>
    /// SMS gönderir.
    /// NetGSM API v2 REST endpoint'ini kullanır.
    /// </summary>
    /// <param name="phoneNumber">Alıcı telefon numarası (05xxxxxxxxx veya 5xxxxxxxxx formatında)</param>
    /// <param name="message">Gönderilecek mesaj metni (max 917 karakter)</param>
    /// <returns>SMS gönderim sonucu</returns>
    public async Task<SmsResult> SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            // Guard clauses - Girdi validasyonu
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Telefon numarası boş olamaz", nameof(phoneNumber));
            
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Mesaj metni boş olamaz", nameof(message));

            // Telefon numarasını formatla (başındaki 0'ı kaldır, sadece rakamlar)
            var formattedPhone = FormatPhoneNumber(phoneNumber);
            
            _logger.LogInformation("[NetGSM] SMS gönderiliyor: {Phone}, Mesaj uzunluğu: {Length}", 
                formattedPhone, message.Length);

            // API dokümantasyonuna göre request body hazırlama
            // msgheader: Sistemde tanımlı gönderici adı (ZORUNLU - boş olamaz)
            // encoding: "TR" Türkçe karakter desteği için
            // iysfilter: "0" Bilgilendirme SMS'i (İYS kontrolsüz), "11" Ticari/Bireysel, "12" Ticari/Tacir
            var requestData = new
            {
                msgheader = _settings.MsgHeader,
                messages = new[]
                {
                    new
                    {
                        msg = message,
                        no = formattedPhone
                    }
                },
                encoding = "TR",
                iysfilter = "0",  // Bilgilendirme amaçlı SMS (İYS kontrolü yapılmaz)
                appname = _settings.AppName
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(SMS_API_URL, content);
            var responseText = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[NetGSM] Response: {Response}", responseText);

            // Response'u parse et
            var result = ParseSmsResponse(responseText);
            
            if (result.Success)
            {
                _logger.LogInformation("[NetGSM] SMS başarıyla gönderildi. JobId: {JobId}", result.JobId);
            }
            else
            {
                _logger.LogWarning("[NetGSM] SMS gönderilemedi. Code: {Code}, Error: {Error}", 
                    result.Code, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetGSM] SMS gönderim hatası: {Phone}", phoneNumber);
            return new SmsResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// NetGSM bakiye sorgulama API'sini çağırır.
    /// </summary>
    /// <returns>Hesaptaki kalan SMS kredisi</returns>
    public async Task<decimal> GetBalanceAsync()
    {
        try
        {
            // NetGSM Balance API için request body
            var requestData = new
            {
                usercode = _settings.UserCode,
                password = _settings.Password,
                stip = 3  // SMS kredisi sorgusu (API dok.)
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(BALANCE_API_URL, content);
            var responseText = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[NetGSM] Balance response: {Response}", responseText);

            // Response direkt numeric değer dönüyor (API dok.)
            if (decimal.TryParse(responseText.Trim(), out var balance))
            {
                _logger.LogInformation("[NetGSM] Bakiye sorgulandı: {Balance} SMS", balance);
                return balance;
            }

            _logger.LogWarning("[NetGSM] Bakiye parse edilemedi: {Response}", responseText);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetGSM] Bakiye sorgulama hatası");
            return 0;
        }
    }

    /// <summary>
    /// Telefon numarasını NetGSM API formatına dönüştürür.
    /// Giriş: 05xxxxxxxxx, +905xxxxxxxxx, 905xxxxxxxxx
    /// Çıkış: 5xxxxxxxxx (başında 0 yok)
    /// </summary>
    /// <param name="phone">Ham telefon numarası</param>
    /// <returns>Formatlanmış telefon numarası</returns>
    private static string FormatPhoneNumber(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Telefon numarası boş olamaz", nameof(phone));

        // Sadece rakamları al (boşluk, tire, parantez vb. temizle)
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.Length < 10)
            throw new ArgumentException($"Geçersiz telefon numarası: {phone}", nameof(phone));

        // Başındaki 0'ı kaldır (05xx -> 5xx)
        if (digits.StartsWith("0"))
            digits = digits[1..];

        // Türkiye kodu varsa kaldır (905xx -> 5xx)
        if (digits.StartsWith("90") && digits.Length > 10)
            digits = digits[2..];

        // Final validasyon: 10 haneli olmalı (5xxxxxxxxx)
        if (digits.Length != 10 || !digits.StartsWith("5"))
            throw new ArgumentException($"Geçersiz telefon numarası formatı: {phone}", nameof(phone));

        return digits;
    }

    /// <summary>
    /// NetGSM API response'unu parse eder.
    /// Başarılı response: {"code":"00", "jobid":"17377215342605050417149344", "description":"queued"}
    /// Hatalı response: "30" veya "40" gibi direkt hata kodu
    /// </summary>
    /// <param name="responseText">API'den dönen raw response</param>
    /// <returns>Parse edilmiş SMS sonucu</returns>
    private static SmsResult ParseSmsResponse(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return new SmsResult
            {
                Success = false,
                ErrorMessage = "API'den boş response döndü"
            };
        }

        try
        {
            // JSON response'u parse et
            using var doc = JsonDocument.Parse(responseText);
            var root = doc.RootElement;

            var code = root.TryGetProperty("code", out var codeEl) 
                ? codeEl.GetString() : null;
            var jobId = root.TryGetProperty("jobid", out var jobEl) 
                ? jobEl.GetString() : null;
            var description = root.TryGetProperty("description", out var descEl) 
                ? descEl.GetString() : null;

            return new SmsResult
            {
                Success = code == "00",
                Code = code,
                JobId = jobId,
                Description = description,
                ErrorMessage = code != "00" ? GetErrorMessage(code) : null
            };
        }
        catch (JsonException)
        {
            // JSON parse hatası - muhtemelen direkt hata kodu döndü ("30", "40" vb.)
            var code = responseText.Trim();
            return new SmsResult
            {
                Success = false,
                Code = code,
                ErrorMessage = GetErrorMessage(code) ?? $"Bilinmeyen hata: {code}"
            };
        }
        catch (Exception ex)
        {
            return new SmsResult
            {
                Success = false,
                ErrorMessage = $"Response parse hatası: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// NetGSM hata kodlarını Türkçe açıklamaya çevirir.
    /// API dokümantasyonundaki tüm hata kodları dahil edilmiştir.
    /// </summary>
    /// <param name="code">NetGSM API hata kodu</param>
    /// <returns>Hata açıklaması (null = başarılı)</returns>
    private static string? GetErrorMessage(string? code)
    {
        return code switch
        {
            "00" => null, // Başarılı - tarih formatı doğru
            "01" => "Mesaj gönderim başlangıç tarihinde hata var (sistem tarihi ile değiştirildi)",
            "02" => "Mesaj gönderim bitiş tarihinde hata var (sistem tarihi ile değiştirildi)",
            "20" => "Mesaj metni boş veya maksimum karakter sayısı (917) aşıldı",
            "30" => "Geçersiz kullanıcı adı/şifre veya API erişim izni yok (IP kısıtlaması da olabilir)",
            "40" => "Mesaj başlığı (gönderici adı) sistemde tanımlı değil",
            "50" => "İYS kontrollü gönderim yapılamıyor (abone hesabı uygun değil)",
            "51" => "Aboneliğe tanımlı İYS Marka bilgisi bulunamadı",
            "70" => "Hatalı sorgulama - Parametrelerden biri hatalı veya eksik",
            "80" => "Gönderim sınır aşımı",
            "85" => "Mükerrer gönderim sınır aşımı (aynı numaraya 1 dakikada 20'den fazla)",
            _ => $"Bilinmeyen hata kodu: {code}"
        };
    }

    #region ISmsProvider Implementation

    /// <summary>
    /// ISmsProvider.SendSmsAsync implementasyonu
    /// </summary>
    async Task<SmsSendResult> ISmsProvider.SendSmsAsync(string phoneNumber, string message)
    {
        var result = await SendSmsAsync(phoneNumber, message);
        return new SmsSendResult
        {
            Success = result.Success,
            Code = result.Code,
            JobId = result.JobId,
            Description = result.Description,
            ErrorMessage = result.ErrorMessage
        };
    }

    /// <summary>
    /// OTP SMS gönderir.
    /// NetGSM API'sine göre OTP da normal SMS endpoint'i ile gönderilir.
    /// Bilgilendirme amaçlı olduğu için İYS kontrolü yapılmaz.
    /// </summary>
    public async Task<SmsSendResult> SendOtpAsync(string phoneNumber, string message)
    {
        try
        {
            var formattedPhone = FormatPhoneNumber(phoneNumber);
            
            _logger.LogInformation("[NetGSM-OTP] OTP SMS gönderiliyor: {Phone}", formattedPhone);
            _logger.LogInformation("[NetGSM-OTP] Settings - MsgHeader: '{Header}', AppName: '{AppName}'", 
                _settings.MsgHeader, _settings.AppName);

            // NetGSM API dokümantasyonuna göre OTP da normal SMS endpoint'i kullanır
            // msgheader: Sistemde tanımlı gönderici adı (ZORUNLU - boş olamaz)
            // messages: SMS detayları array formatında
            // encoding: "TR" Türkçe karakter desteği
            // iysfilter: "0" OTP bilgilendirme SMS'i olduğu için İYS kontrolü yapılmaz
            var requestData = new
            {
                msgheader = _settings.MsgHeader,  // ZORUNLU: Boş olmamalı!
                messages = new[]
                {
                    new
                    {
                        msg = message,
                        no = formattedPhone
                    }
                },
                encoding = "TR",  // Türkçe karakter desteği
                iysfilter = "0",  // Bilgilendirme amaçlı (İYS kontrolsüz)
                appname = _settings.AppName
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // OTP için de standart SMS endpoint'i kullanılıyor (API dok.)
            var response = await _httpClient.PostAsync(SMS_API_URL, content);
            var responseText = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[NetGSM-OTP] Response: {Response}", responseText);

            // Response'u parse et
            var smsResult = ParseSmsResponse(responseText);

            if (smsResult.Success)
            {
                _logger.LogInformation("[NetGSM-OTP] OTP başarıyla gönderildi. JobId: {JobId}", smsResult.JobId);
                return SmsSendResult.SuccessResult(smsResult.JobId ?? string.Empty, smsResult.Code);
            }
            else
            {
                _logger.LogWarning("[NetGSM-OTP] OTP gönderilemedi. Code: {Code}, Error: {Error}", 
                    smsResult.Code, smsResult.ErrorMessage);
                return SmsSendResult.FailResult(smsResult.ErrorMessage ?? "Bilinmeyen hata", smsResult.Code);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NetGSM-OTP] OTP gönderim hatası: {Phone}", phoneNumber);
            return SmsSendResult.FailResult(ex.Message);
        }
    }

    /// <summary>
    /// ISmsProvider.GetBalanceAsync implementasyonu
    /// </summary>
    async Task<decimal> ISmsProvider.GetBalanceAsync()
    {
        return await GetBalanceAsync();
    }

    #endregion
}
