using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("==========================================");
        Console.WriteLine("  MIKRO API BAGLANTI TEST");
        Console.WriteLine("==========================================");
        Console.WriteLine();

        // 1) MD5 Hash
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var password = "ZeMe@48.golkoy2";
        var dataToHash = today + " " + password;

        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(dataToHash);
        var hashBytes = md5.ComputeHash(inputBytes);
        var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        Console.WriteLine("[1] MD5 HASH BILGILERI");
        Console.WriteLine($"    Tarih: {today}");
        Console.WriteLine($"    Hash Edilecek: '{dataToHash}'");
        Console.WriteLine($"    MD5 Hash: {hash}");
        Console.WriteLine();

        // HTTP Client (SSL bypass)
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true;
        using var client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(15);

        // 2) HealthCheck HTTP
        Console.WriteLine("[2] HEALTHCHECK (HTTP)");
        try
        {
            var resp = await client.GetAsync("http://localhost:8094/Api/APIMethods/HealthCheck");
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"    Status: {(int)resp.StatusCode} {resp.ReasonPhrase}");
            Console.WriteLine($"    Body: {(body.Length > 300 ? body.Substring(0, 300) + "..." : body)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    HATA: {ex.Message}");
        }
        Console.WriteLine();

        // 3) HealthCheck HTTPS
        Console.WriteLine("[3] HEALTHCHECK (HTTPS)");
        try
        {
            var resp2 = await client.GetAsync("https://localhost:8094/Api/APIMethods/HealthCheck");
            var body2 = await resp2.Content.ReadAsStringAsync();
            Console.WriteLine($"    Status: {(int)resp2.StatusCode} {resp2.ReasonPhrase}");
            Console.WriteLine($"    Body: {(body2.Length > 300 ? body2.Substring(0, 300) + "..." : body2)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    HATA: {ex.Message}");
        }
        Console.WriteLine();

        // 4) StokListesiV2 HTTP
        Console.WriteLine("[4] STOKLISTESIV2 (HTTP - Auth gerekli)");
        var requestBody = new
        {
            Mikro = new
            {
                ApiKey = "PZDEzh44zNcY2WKpOaoPHV5+mlVG1420SPXn3QBuVqcO6MvOk1j6NlSSFONwtTJV0ovN+6CjB6IKfPYN4TLXmjs6ESUxcwa2Yp+abW9+lac=",
                CalismaYili = "2026",
                FirmaKodu = "Ze-Me 2023",
                KullaniciKodu = "Golkoy2",
                Sifre = hash
            },
            Index = 0,
            Size = "1",
            Sort = "sto_kod"
        };
        var json = JsonSerializer.Serialize(requestBody);
        Console.WriteLine($"    JSON: {json}");
        Console.WriteLine();

        try
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp3 = await client.PostAsync("http://localhost:8094/Api/APIMethods/StokListesiV2", content);
            var body3 = await resp3.Content.ReadAsStringAsync();
            Console.WriteLine($"    Status: {(int)resp3.StatusCode} {resp3.ReasonPhrase}");
            Console.WriteLine($"    Body: {(body3.Length > 500 ? body3.Substring(0, 500) + "..." : body3)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    HATA: {ex.Message}");
        }
        Console.WriteLine();

        // 5) StokListesiV2 HTTPS
        Console.WriteLine("[5] STOKLISTESIV2 (HTTPS)");
        try
        {
            var content2 = new StringContent(json, Encoding.UTF8, "application/json");
            var resp4 = await client.PostAsync("https://localhost:8094/Api/APIMethods/StokListesiV2", content2);
            var body4 = await resp4.Content.ReadAsStringAsync();
            Console.WriteLine($"    Status: {(int)resp4.StatusCode} {resp4.ReasonPhrase}");
            Console.WriteLine($"    Body: {(body4.Length > 500 ? body4.Substring(0, 500) + "..." : body4)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    HATA: {ex.Message}");
        }
        Console.WriteLine();

        Console.WriteLine("==========================================");
        Console.WriteLine("  TEST TAMAMLANDI");
        Console.WriteLine("==========================================");
    }
}
