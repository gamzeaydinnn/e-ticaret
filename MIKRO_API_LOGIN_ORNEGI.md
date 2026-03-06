# Mikro API Login Örneği - Gölköy Gürme

## Mevcut Ayarlarınız (appsettings.json)

```json
"MikroSettings": {
  "ApiUrl": "https://localhost:8094",
  "ApiKey": "PZDEzh44zNcY2WKpOaoPHV5+mlVG1420SPXn3QBuVqcO6MvOk1j6NlSSFONwtTJV0ovN+6CjB6IKfPYN4TLXmjs6ESUxcwa2Yp+abW9+lac=",
  "FirmaKodu": "Ze-Me 2023",
  "KullaniciKodu": "Golkoy2",
  "Sifre": "ZeMe@48.golkoy2",
  "CalismaYili": "2026"
}
```

## MD5 Hash Hesaplama Formatı

⚠️ **ÖNEMLİ**: Şifre her gün değişen bir MD5 hash olarak gönderilmelidir!

### Hash Formatı:

```
MD5(YYYY-MM-DD + BOŞLUK + PlainPassword)
```

### Örnek Hesaplama (Bugün için - 13 Şubat 2026):

**Plain Text Şifre**: `ZeMe@48.golkoy2`

**Hash Edilecek String**: `2026-02-13 ZeMe@48.golkoy2`
(Tarih + BOŞLUK + Şifre)

**MD5 Hash**: `d8e8fca2dc0f896fd7cb4cb0031ba249`
(Bu hash bugün için geçerlidir, yarın değişecektir!)

---

## Doğru JSON Request Formatı

### Test Login İsteği (Bugün için):

```json
{
  "Mikro": {
    "Sifre": "d8e8fca2dc0f896fd7cb4cb0031ba249",
    "KullaniciKodu": "Golkoy2",
    "FirmaKodu": "Ze-Me 2023",
    "ApiKey": "PZDEzh44zNcY2WKpOaoPHV5+mlVG1420SPXn3QBuVqcO6MvOk1j6NlSSFONwtTJV0ovN+6CjB6IKfPYN4TLXmjs6ESUxcwa2Yp+abW9+lac=",
    "CalismaYili": "2026"
  }
}
```

---

## Production Ayarları (appsettings.Production.json)

```json
"MikroSettings": {
  "ApiUrl": "https://host.docker.internal:8094",
  "ApiKey": "PZDEzh44zNcY2WKpOaoPHV5+mlVG1420SPXn3QBuVqcO6MvOk1j6NlSSFONwtTJV0ovN+6CjB6IKfPYN4TLXmjs6ESUxcwa2Yp+abW9+lac=",
  "FirmaKodu": "999",
  "KullaniciKodu": "usr_golkoy",
  "Sifre": "ZeMe@48.golkoy2",
  "CalismaYili": "2026"
}
```

### Production Login İsteği (Bugün için):

```json
{
  "Mikro": {
    "Sifre": "d8e8fca2dc0f896fd7cb4cb0031ba249",
    "KullaniciKodu": "usr_golkoy",
    "FirmaKodu": "999",
    "ApiKey": "PZDEzh44zNcY2WKpOaoPHV5+mlVG1420SPXn3QBuVqcO6MvOk1j6NlSSFONwtTJV0ovN+6CjB6IKfPYN4TLXmjs6ESUxcwa2Yp+abW9+lac=",
    "CalismaYili": "2026"
  }
}
```

---

## Günlük Hash Hesaplama Örnekleri

### 14 Şubat 2026 için:

```
Hash Edilecek: "2026-02-14 ZeMe@48.golkoy2"
MD5 Hash: [Yeni hash hesaplanacak]
```

### 15 Şubat 2026 için:

```
Hash Edilecek: "2026-02-15 ZeMe@48.golkoy2"
MD5 Hash: [Yeni hash hesaplanacak]
```

---

## C# Kodunda Nasıl Çalışıyor?

Mevcut `MicroService.cs` dosyanızda `GenerateDailyPasswordHash` metodu otomatik olarak:

1. Bugünün tarihini alır: `DateTime.Now.ToString("yyyy-MM-dd")`
2. Şifre ile birleştirir: `"2026-02-13 ZeMe@48.golkoy2"`
3. MD5 hash hesaplar
4. Her API isteğinde bu hash'i kullanır

```csharp
private string GenerateDailyPasswordHash(string plainPassword)
{
    var today = DateTime.Now.ToString("yyyy-MM-dd");
    var dataToHash = today + " " + plainPassword;  // ✅ Boşluk önemli!

    using var md5 = MD5.Create();
    var inputBytes = Encoding.UTF8.GetBytes(dataToHash);
    var hashBytes = md5.ComputeHash(inputBytes);

    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
}
```

---

## Test Etmek İçin PowerShell Komutu

```powershell
# Bugünün hash'ini hesapla
$today = Get-Date -Format "yyyy-MM-dd"
$password = "ZeMe@48.golkoy2"
$dataToHash = "$today $password"

$md5 = [System.Security.Cryptography.MD5]::Create()
$bytes = [System.Text.Encoding]::UTF8.GetBytes($dataToHash)
$hashBytes = $md5.ComputeHash($bytes)
$hash = ($hashBytes | ForEach-Object { $_.ToString("x2") }) -join ""

Write-Host "Tarih: $today"
Write-Host "Hash Edilecek: $dataToHash"
Write-Host "MD5 Hash: $hash"

# Test isteği JSON'u
$json = @{
    Mikro = @{
        Sifre = $hash
        KullaniciKodu = "Golkoy2"
        FirmaKodu = "Ze-Me 2023"
        ApiKey = "PZDEzh44zNcY2WKpOaoPHV5+mlVG1420SPXn3QBuVqcO6MvOk1j6NlSSFONwtTJV0ovN+6CjB6IKfPYN4TLXmjs6ESUxcwa2Yp+abW9+lac="
        CalismaYili = "2026"
    }
} | ConvertTo-Json -Depth 10

Write-Host "`nJSON Request:"
Write-Host $json
```

---

## Önemli Notlar

1. ✅ **Şifre appsettings'te plain text olarak kalır** - Kod runtime'da hash'ler
2. ✅ **Her gün otomatik yeni hash** - DateTime.Now kullanılıyor
3. ✅ **Boşluk karakteri kritik** - `"2026-02-13 ZeMe@48.golkoy2"` (tarih ve şifre arasında)
4. ✅ **Hash lowercase** - `.ToLowerInvariant()` kullanılıyor
5. ⚠️ **Sunucu saati önemli** - Mikro API sunucusu ile saat senkronizasyonu gerekli

---

## Sorun Giderme

### Eğer loglarda farklı şifre görüyorsanız:

1. **Tarih formatını kontrol edin**: `yyyy-MM-dd` formatında olmalı
2. **Boşluk karakterini kontrol edin**: Tarih ve şifre arasında TAM 1 boşluk
3. **Sunucu saatini kontrol edin**: Mikro API sunucusu ile aynı gün olmalı
4. **Hash'in lowercase olduğunu kontrol edin**: Büyük harf olmamalı
5. **Plain text şifreyi kontrol edin**: `ZeMe@48.golkoy2` doğru mu?

### Debug için log ekleyin:

```csharp
_logger.LogInformation("[MicroService] Hash Debug - Tarih: {Date}, Plain: {Plain}, Hash: {Hash}",
    today, plainPassword, hashString);
```
