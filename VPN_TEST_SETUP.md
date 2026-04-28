# VPN Mikro API Test Konfigürasyonu

## 📋 Özet
Localhost yerine VPN sunucusuna (10.0.0.3:8084) bağlanarak test yapmak için hazırlanmış yapılandırma.

---

## 🔧 1. Yapılandırma Dosyası

**Dosya:** `src/ECommerce.API/appsettings.VpnTest.json`

Bu dosya, Mikro API's base URL'ini VPN sunucusuna işaret etmektedir:
```json
"MikroSettings": {
    "ApiUrl": "http://10.0.0.3:8084",
    ...
}
```

---

## 🚀 2. Uygulamayı VPN Test Modunda Başlatma

### **Windows (PowerShell)**
```powershell
$env:ASPNETCORE_ENVIRONMENT = "VpnTest"
dotnet run --project src/ECommerce.API/ECommerce.API.csproj
```

### **Linux/Mac (Bash)**
```bash
export ASPNETCORE_ENVIRONMENT=VpnTest
dotnet run --project src/ECommerce.API/ECommerce.API.csproj
```

### **Visual Studio**
1. Çözümü sağ-tıkla → Properties
2. Debug → `Environment variables` → `ASPNETCORE_ENVIRONMENT` = `VpnTest`

### **VS Code (launch.json)**
```json
{
    "name": "VPN Test",
    "type": "coreclr",
    "request": "launch",
    "program": "${workspaceFolder}/src/ECommerce.API/bin/Debug/net6.0/ECommerce.API.dll",
    "args": [],
    "cwd": "${workspaceFolder}/src/ECommerce.API",
    "env": {
        "ASPNETCORE_ENVIRONMENT": "VpnTest"
    }
}
```

---

## 📡 3. Test Etme (Postman)

**Postman Collection:** `Postman-VPN-MikroAPI-Test.json`

### **İmport Etme:**
1. Postman'i aç
2. **File** → **Import**
3. `Postman-VPN-MikroAPI-Test.json` dosyasını seç

### **Testler:**

#### **1. API Login (Giriş Yapma)**
```
POST http://10.0.0.3:8084/Api/APILogin

Body:
{
  "ApiKey": "PZDEzh44zNcY2WKpOaoPHV5+mlVG1420SPXn3QBuVqcO6MvOk1j6NlSSFONwtTJV0ovN+6CjB6IKfPYN4TLXmjs6ESUxcwa2Yp+abW9+lac=",
  "CalismaYili": "2026",
  "FirmaKodu": "Ze-Me 2023",
  "KullaniciKodu": "Golkoy2",
  "Sifre": "2026-03-27 ZeMe@48.golkoy2",
  "SubeNo": 0
}
```

**NOT:** Şifre formatı = `YYYY-MM-DD şifre` (tarih güncellenmelidir!)

#### **2. Stok Sorgusu**
```
GET http://10.0.0.3:8084/Api/AnaUrun?anahtar=test
```

#### **3. Müşteri Sorgula**
```
GET http://10.0.0.3:8084/Api/AnaMusteri?musteriAnahtari=WEB-001
```

#### **4. Sistem Bilgisi**
```
GET http://10.0.0.3:8084/Api/AnaBilgisayar
```

---

## 🔐 4. Kimlik Bilgileri

| Alan | Değer |
|------|-------|
| **API Key** | `PZDEzh44zNcY2WKpOaoPHV5+mlVG1420SPXn3QBuVqcO6MvOk1j6NlSSFONwtTJV0ovN+6CjB6IKfPYN4TLXmjs6ESUxcwa2Yp+abW9+lac=` |
| **Firma Kodu** | `Ze-Me 2023` |
| **Kullanıcı Kodu** | `Golkoy2` |
| **Şifre** | `ZeMe@48.golkoy2` |
| **Çalışma Yılı** | `2026` |

---

## 📝 5. Kodda Konfigürasyonun Kullanılması

MikroSettings otomatik olarak yüklenir. Dependency Injection aracılığıyla kullanın:

```csharp
public class MikroApiService
{
    private readonly IOptions<MikroSettings> _mikroSettings;

    public MikroApiService(IOptions<MikroSettings> mikroSettings)
    {
        _mikroSettings = mikroSettings.Value;
    }

    public async Task<IResult> CallMikroApi()
    {
        var baseUrl = _mikroSettings.ApiUrl;  // "http://10.0.0.3:8084"
        var apiKey = _mikroSettings.ApiKey;
        var firma = _mikroSettings.FirmaKodu;
        
        // İstek gönder...
    }
}
```

---

## 🐛 Sorun Giderme

### Problem: "Bağlantı Reddedildi"
```
System.Net.Http.HttpRequestException: Connection refused (10.0.0.3:8084)
```
**Çözüm:** 
- VPN'ye bağlı olduğundan emin ol
- `ping 10.0.0.3` ile bağlantıyı test et
- Sunucu 8084 portunda dinliyor mu kontrol et

### Problem: "İçerik Boş"
```json
{"result":"[\"StatusCode\":200]","Data":null,"ErrorMessage":"(JSON parse hatası)"}
```
**Çözüm:** 
- Postman'da `Content-Type: application/json` header'ı kontrol et
- Kimlik bilgileri doğru mu kontrol et

### Problem: "Authentication Hatası"
**Çözüm:** 
- Şifre formatını kontrol et: `YYYY-MM-DD şifre` (boşluk önemli!)
- Bugün tarihini kullan (şifre her gün değişir)

---

## 📌 Hızlı Komutlar

### Test Başlat:
```powershell
$env:ASPNETCORE_ENVIRONMENT = "VpnTest"; dotnet run --project src/ECommerce.API/ECommerce.API.csproj
```

### Tüm Ortamlar (Local):
- **Development:** `set ASPNETCORE_ENVIRONMENT=Development`
- **VPN Test:** `set ASPNETCORE_ENVIRONMENT=VpnTest`
- **Production:** `set ASPNETCORE_ENVIRONMENT=Production`

---

## ✅ Kontrol Listesi

- [ ] VPN'ye bağlıyım
- [ ] `appsettings.VpnTest.json` dosyası mevcut
- [ ] `ASPNETCORE_ENVIRONMENT = VpnTest` ortam değişkeni ayarlandı
- [ ] Uygulama başlatıldı ve 5001 portunda çalışıyor
- [ ] Postman Collection import edildikişi
- [ ] APILogin isteği başarıyla yanıt veriyor
