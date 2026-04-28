# 🚀 VPN Mikro API Test Sistemi - ÖZETİ

## ✅ Oluşturulan Dosyalar

### 1. **appsettings.VpnTest.json** (Yapılandırma)
📍 Konum: `src/ECommerce.API/appsettings.VpnTest.json`

- **Amaç:** VPN sunucusu (10.0.0.3:8084) için test konfigürasyonu
- **Mikro API URL:** `http://10.0.0.3:8084`
- **Otomatik Yükleme:** `ASPNETCORE_ENVIRONMENT=VpnTest` ayarlandığında devreye girer

```bash
# Kullanım
$env:ASPNETCORE_ENVIRONMENT = "VpnTest"
dotnet run --project src/ECommerce.API/ECommerce.API.csproj
```

---

### 2. **Postman-VPN-MikroAPI-Test.json** (Test Collection)
📍 Konum: `Postman-VPN-MikroAPI-Test.json`

- **İçerik:** 4 hazır test isteği
  - ✅ **APILogin** - API'ye kimlik doğrulama
  - ✅ **Stok Sorgusu** - Ürün bilgisi alma
  - ✅ **Müşteri Sorgula** - Müşteri bilgisi alma
  - ✅ **Sistem Bilgisi** - Sunucunun durumunu kontrol etme

**Import:** Postman → File → Import → `Postman-VPN-MikroAPI-Test.json`

---

### 3. **VPN_TEST_SETUP.md** (Detaylı Kılavuz)
📍 Konum: `VPN_TEST_SETUP.md`

- 📖 Tam talimatlar ve örnekler
- 🔐 Kimlik bilgileri tablosu
- 🐛 Sorun giderme rehberi
- ✅ Kontrol listesi

---

### 4. **MikroApiVpnTestService.cs** (C# Servis Sınıfı)
📍 Konum: `src/ECommerce.Infrastructure/Services/MicroServices/MikroApiVpnTestService.cs`

- **Dependency Injection Hazır**
- **Otomatik Logging** - Her isteği loglar
- **MD5 Hash Otomatiği** - Şifre otomatik hash'lenir
- **4 Ana Method:**
  ```csharp
  await _service.LoginAsync();           // Giriş yapma
  await _service.GetProductAsync(key);   // Ürün sorgusu
  await _service.GetCustomerAsync(key);  // Müşteri sorgusu
  await _service.GetSystemInfoAsync();   // Sistem bilgisi
  ```

---

## 🎯 Kullanım Akışı

### **1️⃣ Config Seç**
```powershell
# Windows PowerShell
$env:ASPNETCORE_ENVIRONMENT = "VpnTest"

# Veya Linux/Mac
export ASPNETCORE_ENVIRONMENT=VpnTest
```

### **2️⃣ Uygulamayı Başlat**
```bash
dotnet run --project src/ECommerce.API/ECommerce.API.csproj
```
→ Program otomatik olarak `appsettings.VpnTest.json`'u yükler  
→ Mikro API URL: `http://10.0.0.3:8084`

### **3️⃣ Test Et (Postman)**
- Postman Collection'ı import et
- **APILogin** isteğini çalıştır
- Yanıt başarılı mı kontrol et

### **4️⃣ Kodda Kullan (C#)**
```csharp
public class OrderService
{
    private readonly MikroApiVpnTestService _mikroService;

    public OrderService(MikroApiVpnTestService mikroService)
    {
        _mikroService = mikroService;  // DI'dan otomatik gelir
    }

    public async Task ProcessOrder()
    {
        // Müşteri sorgula
        var customer = await _mikroService.GetCustomerAsync("WEB-001");
        
        // Ürün sorgula
        var product = await _mikroService.GetProductAsync("URUN123");
        
        // İşleme devam et...
    }
}
```

---

## 🔐 Kimlik Bilgileri

| Parametre | Değer |
|-----------|-------|
| **API URL** | `http://10.0.0.3:8084` |
| **API Key** | `PZDEzh44zNcY2WKpOaoPHV5+mlVG1420SPXn3QBuVqcO6MvOk1j6NlSSFONwtTJV0ovN+6CjB6IKfPYN4TLXmjs6ESUxcwa2Yp+abW9+lac=` |
| **Firma Kodu** | `Ze-Me 2023` |
| **Kullanıcı Kodu** | `Golkoy2` |
| **Şifre** | `ZeMe@48.golkoy2` |
| **Çalışma Yılı** | `2026` |

**⚠️ ÖNEMLİ:** Şifre **MD5 hash** formatında gönderilmelidir:
```
Hash = MD5(YYYY-MM-DD şifre)
Örnek: MD5("2026-03-27 ZeMe@48.golkoy2")
```

---

## 🔄 Config Seçimi (Ortamlar)

| Ortam | Environment | Config Dosyası | ApiUrl |
|-------|-------------|----------------|--------|
| 🖥️ Local Development | `Development` | `appsettings.Development.json` | `http://localhost:8094` |
| 🌐 VPN Test | `VpnTest` | `appsettings.VpnTest.json` | `http://10.0.0.3:8084` |
| 🚀 Production | `Production` | `appsettings.Production.json` | Sunucu URL'i |

---

## ⚡ Hızlı Komutlar

```powershell
# VPN Test Başlat
$env:ASPNETCORE_ENVIRONMENT = "VpnTest"; dotnet run --project src/ECommerce.API

# Development Başlat
$env:ASPNETCORE_ENVIRONMENT = "Development"; dotnet run --project src/ECommerce.API

# Ortamı Kontrol Et (C# içinde)
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
Console.WriteLine($"Ortam: {env}");  // Output: "VpnTest"
```

---

## 📊 Yapılandırma Hiyerarşisi

```
appsettings.json (Base)
    ↓
appsettings.{ASPNETCORE_ENVIRONMENT}.json (Override)
    ↓
Environment Variables (Override)
    ↓
Program.cs Configure (Override)
```

**Örnek:**
1. `appsettings.json` → `MikroSettings:ApiUrl = "http://localhost:8094"`
2. `appsettings.VpnTest.json` → `MikroSettings:ApiUrl = "http://10.0.0.3:8084"` ✅ (Override!)

---

## ✨ Avantajlar

✅ **Tek yerden IP değiştir** - Tüm isteklerde otomatik güncelleme  
✅ **Production-ready** - Güvenli konfigürasyon yapısı  
✅ **Logging entegre** - Her isteği loglar  
✅ **Dependency Injection** - ASP.NET Core standartı  
✅ **Test kolaylığı** - Postman Collection hazır  
✅ **Seçenekli ortamlar** - Local/VPN/Production arasında geçiş  

---

## 🆘 Sorun Giderme

### Bağlantı Reddediliyorsa
```
System.Net.Http.HttpRequestException: Connection refused
```
→ VPN bağlıyım, `ping 10.0.0.3` test et

### Config yüklenmiyorsa
```csharp
// Program.cs'de kontrol et
var env = builder.Environment.EnvironmentName;
Console.WriteLine($"Ortam: {env}");  // "VpnTest" olmalı
```

### JSON Parse hatası
→ Postman'da `Content-Type: application/json` header'ını ekle

---

## 📝 Sonraki Adımlar

1. ✅ VPN'ye bağlan
2. ✅ `ASPNETCORE_ENVIRONMENT=VpnTest` ayarla
3. ✅ Uygulamayı başlat
4. ✅ Postman'da APILogin'i test et
5. ✅ Kod'da `MikroApiVpnTestService`'i kullan

