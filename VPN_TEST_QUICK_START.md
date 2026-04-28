# 📦 VPN Mikro API Test Ortamı - BAŞLANGIC KILAVUZU

## 🎯 TL;DR (Hızlı Başla)

```powershell
# Windows
$env:ASPNETCORE_ENVIRONMENT = "VpnTest"
dotnet run --project src\ECommerce.API\ECommerce.API.csproj --launch-profile VpnTest
```

Ardından: `http://localhost:5153/api/mikroapitest/config` ziyaret et

Not: Uygulama calisirken ayni komutu tekrar verirseniz `address already in use (5153)` hatasi alirsiniz. Bu durumda eski sureci durdurup tekrar baslatin.

---

## 📦 OLUŞTURULAN DOSYALAR

### ✅ Yapılandırma (1 dosya)
- **`appsettings.VpnTest.json`** ← Tüm başlangıç config burada!
  - API URL: `http://10.0.0.3:8084`
  - Firma, Kullanıcı, Şifre hazır
  - Sadece `ASPNETCORE_ENVIRONMENT=VpnTest` ile yüklenir

### ✅ Test İçeriği (1 dosya)
- **`Postman-VPN-MikroAPI-Test.json`**
  - 4 hazır test isteği (Login, Stok, Müşteri, Sistem)
  - Postman'da import et ve çalıştır

### ✅ Dokümantasyon (2 dosya)
- **`VPN_TEST_SETUP.md`** - Detaylı talimatlar
- **`VPN_MIKRO_API_SETUP_SUMMARY.md`** - Hızlı referans

### ✅ C# Kod (2 dosya)
- **`MikroApiVpnTestService.cs`** ← Tüm API isteklerini buradan gönder
- **`MikroApiTestController.cs`** ← Web endpoint'eri test et
- Program.cs Otomatik kayıtlı ✓

### ✅ Başlama Script'leri (3 dosya)
- **`vpn-test-start.bat`** - Windows (Çift tıkla ve çalış)
- **`vpn-test-setup.ps1`** - PowerShell komut satırı
- **`vpn-test-setup.sh`** - Linux/Mac bash

---

## 🚀 BAŞLATMA SEÇENEKLERI

### **Seçenek 1: Windows - Batch File (En Kolay) ⭐**
```batch
Çift tıkla: vpn-test-start.bat
```
Hepsi otomatik olur!

### **Seçenek 2: PowerShell (Windows)**
```powershell
$env:ASPNETCORE_ENVIRONMENT = "VpnTest"
dotnet run --project src\ECommerce.API\ECommerce.API.csproj --launch-profile VpnTest
```

### **Seçenek 3: Visual Studio**
1. Proje özelliği (`Properties`)
2. Debug → Environment Variables
3. `ASPNETCORE_ENVIRONMENT` = `VpnTest`
4. F5 çalıştır

### **Seçenek 4: Linux/Mac (Bash)**
```bash
export ASPNETCORE_ENVIRONMENT=VpnTest
dotnet run --project src/ECommerce.API/ECommerce.API.csproj
```

---

## ✅ TEST ETME

### **1. Config Kontrol**
```
GET http://localhost:5153/api/mikroapitest/config
```
Çıktı:
```json
{
  "apiUrl": "http://10.0.0.3:8084",
  "firmaKodu": "Ze-Me 2023",
  "isVpnTest": true,
  "message": "✅ Konfigürasyon başarıyla yüklendi"
}
```

### **2. Giriş Yap**
```
POST http://localhost:5153/api/mikroapitest/login

Response: {
  "isError": false,
  "data": { /* Mikro API yanıtı */ }
}
```

### **3. Postman ile Test**
1. Postman aç
2. **Import** → `Postman-VPN-MikroAPI-Test.json`
3. Requests tab'ına tıkla
4. Her test isteğini çalıştır

---

## 🔐 KİMLİK BİLGİLERİ

| Parametre | Değer |
|-----------|-------|
| **API URL** | `http://10.0.0.3:8084` |
| **FirmaKodu** | `Ze-Me 2023` |
| **KullaniciKodu** | `Golkoy2` |
| **Sifre** | `ZeMe@48.golkoy2` |
| **CalismaYili** | `2026` |
| **ApiKey** | `PZDEzh44zNcY2WKpOaoPHV5+m...` (Otomatik) |

**NOT:** Şifre MD5 format: `YYYY-MM-DD şifre`

---

## 🛠️ KOD ÖRNEKLERI

### **Servis Kullanma (Dependency Injection)**
```csharp
public class OrderService
{
    private readonly MikroApiVpnTestService _service;

    public OrderService(MikroApiVpnTestService service)
    {
        _service = service;  // DI'dan otomatik
    }

    public async Task TestMikro()
    {
        // Giriş yap
        var login = await _service.LoginAsync();
        
        // Ürün sorgula
        var product = await _service.GetProductAsync("URUN123");
        
        // Müşteri sorgula
        var customer = await _service.GetCustomerAsync("WEB-001");
    }
}
```

### **HTTP istek gönder (Postman gibi)**
```bash
curl -X POST http://localhost:5153/api/mikroapitest/login \
  -H "Content-Type: application/json" \
  -d '{}'
```

### **Port Çakışması (5153 veya 7221 already in use)**
```powershell
Get-NetTCPConnection -LocalPort 5153 -State Listen | Select-Object OwningProcess
Get-NetTCPConnection -LocalPort 7221 -State Listen | Select-Object OwningProcess
Stop-Process -Id <PID> -Force
```

---

## 📊 ENDPOINT'LER

| Yöntem | Endpoint | Açıklama | Test |
|--------|----------|----------|------|
| `GET` | `/api/mikroapitest/config` | Aktif config | ✅ |
| `POST` | `/api/mikroapitest/login` | API Login | ✅ |
| `GET` | `/api/mikroapitest/product/{key}` | Ürün sorgula | ✅ |
| `GET` | `/api/mikroapitest/customer/{key}` | Müşteri sorgula | ✅ |
| `GET` | `/api/mikroapitest/system-info` | Sistem info | ✅ |
| `GET` | `/api/mikroapitest/health-check` | Bağlantı test | ✅ |

---

## 🐛 SORUN GIDERME

### Problem: "Bağlantı Reddedildi"
```
System.Net.Http.HttpRequestException: Connection refused (10.0.0.3:8084)
```
**Çözüm:**
1. VPN'ye bağlı mısın? `ping 10.0.0.3`
2. Sunucu 8084 portunda dinliyor mu?
3. Firewall engel miyor?

### Problem: "Config yüklenmedi"
```json
{
  "apiUrl": "http://localhost:8094"  // ← VpnTest değil!
}
```
**Çözüm:**
- `ASPNETCORE_ENVIRONMENT=VpnTest` ayarını kontrol et
- Application restart et
- `$env:ASPNETCORE_ENVIRONMENT` ile doğrula

### Problem: "Authentication hatası"
```json
{
  "isError": true,
  "errorMessage": "Invalid credentials"
}
```
**Çözüm:**
- Şifre formatını kontrol et: `YYYY-MM-DD şifre`
- Bugünün tarihini kullan (her gün değişir)
- appsettings.VpnTest.json'da kimlik bilgilerini doğrula

---

## 🔄 ORTAM YÖNETİMİ

### Mevcut Ortamlar

```
appsettings.json (BASE)
├─ appsettings.Development.json   → localhost:8094
├─ appsettings.VpnTest.json       → 10.0.0.3:8084 ← BUNA KULLAN
├─ appsettings.Production.json    → Canlı sunucu
└─ Ortam değişkeni override eder
```

### Ortam Değişkeninin Kontrolü
```csharp
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
Console.WriteLine($"Aktif Ortam: {env}");  // "VpnTest" çıkmalı
```

---

## 📚 DOSYA HIYERARŞISI

```
eticaret/
├── 📄 appsettings.VpnTest.json              ← CONFIG
├── 📄 Postman-VPN-MikroAPI-Test.json        ← POSTMAN
├── 📄 VPN_TEST_SETUP.md                     ← DETAYLI KLAVE
├── 📄 VPN_MIKRO_API_SETUP_SUMMARY.md        ← HIZLI REF.
├── 📄 vpn-test-start.bat                    ← WINDOWS
├── 📄 vpn-test-setup.ps1                    ← POWERSHELL
├── 📄 vpn-test-setup.sh                     ← LINUX/MAC
│
├── src/ECommerce.API/
│   ├── Program.cs                           ← (DI kayıtlı ✓)
│   └── Controllers/VpnTest/
│       └── MikroApiTestController.cs        ← WEB ENDPOINT'LERİ
│
└── src/ECommerce.Infrastructure/
    └── Services/MicroServices/
        └── MikroApiVpnTestService.cs        ← TÜM İSTEKLER
```

---

## ✨ AVANTAJLAR

✅ **TEK CONFIG DOSYASI** - `appsettings.VpnTest.json`'da IP değiştir, tüm isteklerde otomatik güncellenir
✅ **DI HAZIR** - Servisi inject et ve kullan
✅ **POSTMAN READY** - Test koleksiyonu hazır
✅ **LOGGING** - Her isteği loglar
✅ **MD5 OTOMATIK** - Şifre otomatik hash'lenir
✅ **ENVIRONMENT BASED** - Production'dan bağımsız

---

## ❓ SORULAR?

- `VPN_TEST_SETUP.md` → Detaylı talimatlar
- `VPN_MIKRO_API_SETUP_SUMMARY.md` → Tam teknik referans
- `MikroApiVpnTestService.cs` → Kod örneği
- `MikroApiTestController.cs` → Web API örneği

---

## 🎉 KONTROL LİSTESİ

- [ ] VPN'ye bağlıyım (`ping 10.0.0.3` ✓)
- [ ] `appsettings.VpnTest.json` mevcut
- [ ] `ASPNETCORE_ENVIRONMENT=VpnTest` ayarlandı
- [ ] `dotnet run` başlatıldı
- [ ] `http://localhost:5153/api/mikroapitest/config` erişilebilir
- [ ] `/login` endpoint'i yanıt veriyor
- [ ] Postman Collection import edildikişi
- [ ] Test isteklerinin başarılı olduğu görüldü

Tamamlanan kontrol leri `✅` işaretle!
