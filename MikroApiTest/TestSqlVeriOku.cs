using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// MikroAPI SqlVeriOkuV2 Endpoint Test
/// 
/// Bu script, MikroAPI'nin SqlVeriOkuV2 endpoint'ini kullanarak
/// STOK_SATIS_FIYAT_LISTE_TANIMLARI tablosundan fiyat listesi tanımlarını çeker.
/// 
/// Kullanım: dotnet run
/// </summary>
class TestSqlVeriOku
{
    // ==================== KONFİGÜRASYON ====================
    // Bu değerleri kendi Mikro API ayarlarınıza göre düzenleyin
    
    private const string API_URL = "http://localhost:8094";
    private const string API_KEY = "PZDEzh44zNcY2WKpOaoPHV5+mlVG1420SPXn3QBuVqcO6MvOk1j6NlSSFONwtTJV0ovN+6CjB6IKfPYN4TLXmjs6ESUxcwa2Yp+abW9+lac=";
    private const string FIRMA_KODU = "Ze-Me 2023";
    private const string KULLANICI_KODU = "Golkoy2";
    private const string SIFRE = "ZeMe@48.golkoy2";
    private const string CALISMA_YILI = "2026";

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     MIKRO API - SqlVeriOkuV2 ENDPOINT TEST                      ║");
        Console.WriteLine("║     STOK_SATIS_FIYAT_LISTE_TANIMLARI Tablosu Çekimi             ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // 1) MD5 Hash oluştur (Mikro API formatı: "YYYY-MM-DD şifre")
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var dataToHash = today + " " + SIFRE;
        
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(dataToHash);
        var hashBytes = md5.ComputeHash(inputBytes);
        var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        Console.WriteLine("┌─────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ [1] AUTHENTICATION BİLGİLERİ                                    │");
        Console.WriteLine("├─────────────────────────────────────────────────────────────────┤");
        Console.WriteLine($"│ Tarih         : {today}");
        Console.WriteLine($"│ Firma Kodu    : {FIRMA_KODU}");
        Console.WriteLine($"│ Kullanıcı     : {KULLANICI_KODU}");
        Console.WriteLine($"│ MD5 Hash      : {hash}");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        // HTTP Client (SSL bypass for localhost)
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true;
        using var client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(60);

        // ==================== TEST 1: Fiyat Listesi Tanımları ====================
        Console.WriteLine("┌─────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ [2] STOK_SATIS_FIYAT_LISTE_TANIMLARI SORGUSU                    │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────┘");

        var fiyatListeTanimlariSql = @"
SELECT 
    sfl_sirano,
    sfl_aciklama,
    sfl_kdvdahil,
    sfl_ilktarih,
    sfl_sontarih,
    sfl_doviz_uygulama,
    sfl_iskonto_uygulama
FROM STOK_SATIS_FIYAT_LISTE_TANIMLARI
ORDER BY sfl_sirano";

        await ExecuteSqlQuery(client, hash, "Fiyat Listesi Tanımları", fiyatListeTanimlariSql);

        // ==================== TEST 2: Stok Fiyatları ====================
        Console.WriteLine();
        Console.WriteLine("┌─────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ [3] STOK_SATIS_FIYAT_LISTELERI SORGUSU (İlk 20 Kayıt)           │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────┘");

        var stokFiyatlariSql = @"
SELECT TOP 20
    sfiyat_stokkod AS StokKodu,
    sfiyat_listession AS ListeNo,
    sfiyat_fiyati AS Fiyat,
    sfiyat_dovession AS DovizCinsi,
    sfiyat_dov_fiyati AS DovizFiyati,
    sfiyat_tarih AS Tarih
FROM STOK_SATIS_FIYAT_LISTELERI
WHERE sfiyat_fiyati > 0
ORDER BY sfiyat_stokkod";

        await ExecuteSqlQuery(client, hash, "Stok Fiyatları", stokFiyatlariSql);

        // ==================== TEST 3: Belirli bir ürünün tüm fiyatları ====================
        Console.WriteLine();
        Console.WriteLine("┌─────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ [4] BELİRLİ ÜRÜN FİYATLARI SORGUSU (Örnek)                      │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────┘");

        var urunFiyatlariSql = @"
SELECT TOP 5
    s.sto_kod AS StokKodu,
    s.sto_isim AS StokAdi,
    f.sfiyat_listession AS ListeNo,
    f.sfiyat_fiyati AS Fiyat,
    t.sfl_aciklama AS ListeAdi
FROM STOKLAR s
LEFT JOIN STOK_SATIS_FIYAT_LISTELERI f ON s.sto_kod = f.sfiyat_stokkod
LEFT JOIN STOK_SATIS_FIYAT_LISTE_TANIMLARI t ON f.sfiyat_listession = t.sfl_sirano
WHERE f.sfiyat_fiyati > 0
ORDER BY s.sto_kod, f.sfiyat_listession";

        await ExecuteSqlQuery(client, hash, "Ürün Fiyat Detayları", urunFiyatlariSql);

        // ==================== TEST 4: Güncel Fiyat Özeti ====================
        Console.WriteLine();
        Console.WriteLine("┌─────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ [5] GÜNCEL FİYAT ÖZETİ (Liste 1 - Perakende)                    │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────────┘");

        var guncelFiyatSql = @"
SELECT TOP 30
    s.sto_kod AS StokKodu,
    s.sto_isim AS StokAdi,
    s.sto_birim1_ad AS Birim,
    ISNULL(f.sfiyat_fiyati, 0) AS SatisFiyati,
    s.sto_toptan_vergi AS KDVOrani
FROM STOKLAR s
LEFT JOIN STOK_SATIS_FIYAT_LISTELERI f ON s.sto_kod = f.sfiyat_stokkod AND f.sfiyat_listession = 1
WHERE s.sto_pasession = 0
ORDER BY s.sto_kod";

        await ExecuteSqlQuery(client, hash, "Güncel Fiyat Özeti", guncelFiyatSql);

        Console.WriteLine();
        Console.WriteLine("══════════════════════════════════════════════════════════════════");
        Console.WriteLine("  TEST TAMAMLANDI");
        Console.WriteLine("══════════════════════════════════════════════════════════════════");
    }

    /// <summary>
    /// SqlVeriOkuV2 endpoint'ine SQL sorgusu gönderir ve sonucu yazdırır.
    /// </summary>
    static async Task ExecuteSqlQuery(HttpClient client, string passwordHash, string queryName, string sqlQuery)
    {
        Console.WriteLine($"\n📋 {queryName}:");
        Console.WriteLine($"   SQL: {sqlQuery.Substring(0, Math.Min(80, sqlQuery.Length)).Replace("\n", " ").Trim()}...");
        Console.WriteLine();

        var requestBody = new
        {
            Mikro = new
            {
                ApiKey = API_KEY,
                CalismaYili = CALISMA_YILI,
                FirmaKodu = FIRMA_KODU,
                KullaniciKodu = KULLANICI_KODU,
                Sifre = passwordHash
            },
            Sql = sqlQuery
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { WriteIndented = false });
        
        try
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{API_URL}/Api/APIMethods/SqlVeriOkuV2", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"   Status: {(int)response.StatusCode} {response.ReasonPhrase}");

            if (response.IsSuccessStatusCode)
            {
                // JSON parse ve güzel yazdırma
                try
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    var formattedJson = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
                    
                    // Uzun cevapları kısalt
                    if (formattedJson.Length > 3000)
                    {
                        Console.WriteLine($"   Response (ilk 3000 karakter):\n{formattedJson.Substring(0, 3000)}...");
                        Console.WriteLine($"\n   [Toplam {formattedJson.Length} karakter]");
                    }
                    else
                    {
                        Console.WriteLine($"   Response:\n{formattedJson}");
                    }

                    // Kayıt sayısını bulmaya çalış
                    if (doc.RootElement.TryGetProperty("Data", out var dataElement))
                    {
                        if (dataElement.ValueKind == JsonValueKind.Array)
                        {
                            Console.WriteLine($"\n   ✅ Toplam {dataElement.GetArrayLength()} kayıt döndü.");
                        }
                    }
                    else if (doc.RootElement.TryGetProperty("data", out var dataElement2))
                    {
                        if (dataElement2.ValueKind == JsonValueKind.Array)
                        {
                            Console.WriteLine($"\n   ✅ Toplam {dataElement2.GetArrayLength()} kayıt döndü.");
                        }
                    }
                }
                catch
                {
                    Console.WriteLine($"   Response: {responseBody}");
                }
            }
            else
            {
                Console.WriteLine($"   ❌ HATA Response: {responseBody}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ İstek Hatası: {ex.Message}");
        }
    }
}
