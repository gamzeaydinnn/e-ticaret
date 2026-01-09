# 📱 SMS Doğrulama Sistemi - Kurulum ve Kullanım Kılavuzu

## 🎯 Genel Bakış

E-Ticaret projesi için NetGSM entegrasyonu ile geliştirilmiş profesyonel SMS doğrulama sistemi.

### ✨ Özellikler

- ✅ **OTP (One-Time Password)** doğrulama
- ✅ **NetGSM** SMS API entegrasyonu
- ✅ **Rate limiting** (IP + Telefon bazlı)
- ✅ **Database tabanlı** kayıt sistemi
- ✅ **Mock SMS servisi** (test ortamı)
- ✅ **RESTful API** endpoint'leri
- ✅ **React frontend** entegrasyonu
- ✅ **Güvenlik** özellikleri (günlük limit, blokla ma, cooldown)

### 📋 Kullanım Senaryoları

1. **Kullanıcı Kaydı:** Telefon numarası ile kayıt + SMS doğrulama
2. **Şifre Sıfırlama:** SMS ile güvenli şifre sıfırlama
3. **İki Faktörlü Doğrulama (2FA):** Giriş sonrası ek güvenlik
4. **Telefon Doğrulama:** Profil bilgilerini güncelleme

---

## 🚀 Hızlı Başlangıç

### 1. Gereksinimler

- **.NET 9.0** SDK
- **Node.js 18+** (Frontend için)
- **SQL Server** (Docker veya lokal)
- **NetGSM Hesabı** (production için)

### 2. Backend Kurulum

```bash
# Projeyi klonla
git clone https://github.com/yourusername/eticaret.git
cd eticaret

# Backend'e geç
cd src/ECommerce.API

# User Secrets başlat
dotnet user-secrets init

# NetGSM credentials ekle
dotnet user-secrets set "NetGsm:UserCode" "YOUR_NETGSM_USERNAME"
dotnet user-secrets set "NetGsm:Password" "YOUR_NETGSM_PASSWORD"
dotnet user-secrets set "NetGsm:MsgHeader" "YOUR_SMS_HEADER"

# Veritabanı migration
dotnet ef database update

# Uygulamayı çalıştır
dotnet run
```

Backend: `http://localhost:5153`  
Swagger: `http://localhost:5153/swagger`

### 3. Frontend Kurulum

```bash
# Frontend dizinine geç
cd frontend

# Bağımlılıkları yükle
npm install

# Development sunucusunu başlat
npm start
```

Frontend: `http://localhost:3000`

---

## ⚙️ Yapılandırma

### Development Ortamı (Mock SMS)

```json
// appsettings.Development.json
{
  "NetGsm": {
    "Enabled": false,
    "UseMockService": true
  },
  "SmsVerification": {
    "ExpirationSeconds": 300,
    "ResendCooldownSeconds": 30,
    "DailyMaxOtpCount": 10,
    "MaxWrongAttempts": 5
  }
}
```

**Avantajlar:**

- ✅ Gerçek SMS gönderilmez (maliyet yok)
- ✅ Kodlar console'a yazılır
- ✅ Rate limiting test edilebilir
- ✅ Hızlı development

### Production Ortamı

```bash
# Environment variables (.env dosyası)
NETGSM__USERCODE=your_username
NETGSM__PASSWORD=your_password
NETGSM__MSGHEADER=YOUR_SMS_HEADER
NETGSM__ENABLED=true
NETGSM__USEMOCKSERVICE=false

# SMS limitleri (önerilen production değerleri)
SMSVERIFICATION__EXPIRATIONSECONDS=180
SMSVERIFICATION__RESENDCOOLDOWNSECONDS=60
SMSVERIFICATION__DAILYMAXOTPCOUNT=5
SMSVERIFICATION__HOURLYMAXOTPCOUNT=3
SMSVERIFICATION__MAXWRONGATTEMPTS=3
```

### Docker Deployment

```bash
# .env dosyasını oluştur
cp .env.template .env
# Credential'ları düzenle
nano .env

# Docker compose ile başlat
docker-compose -f docker-compose.prod.yml up -d

# Logları izle
docker-compose -f docker-compose.prod.yml logs -f api
```

---

## 🔧 API Kullanımı

### Backend API

```csharp
// SmsVerificationController kullanımı

// 1. OTP Gönder
POST /api/sms/send-otp
{
  "phoneNumber": "05551234567",
  "purpose": "registration"
}

// 2. OTP Doğrula
POST /api/sms/verify-otp
{
  "phoneNumber": "05551234567",
  "code": "123456",
  "purpose": "registration"
}

// 3. Durum Sorgula
GET /api/sms/status/05551234567?purpose=registration

// 4. Gönderim İzni Kontrol
GET /api/sms/can-send?phoneNumber=05551234567
```

### Frontend Entegrasyonu

```javascript
// React - otpService kullanımı
import otpService from "./services/otpService";

// 1. OTP Gönder
const handleSendOtp = async (phoneNumber) => {
  const result = await otpService.sendOtp(phoneNumber, "registration");

  if (result.success) {
    alert(`Kod gönderildi! ${result.expiresInSeconds}s geçerli`);
  } else {
    alert(`Hata: ${result.message}`);
  }
};

// 2. OTP Doğrula
const handleVerifyOtp = async (phoneNumber, code) => {
  const result = await otpService.verifyOtp(phoneNumber, code, "registration");

  if (result.success) {
    // Kayıt işlemine devam
    console.log("✅ Doğrulama başarılı!");
  } else {
    alert(`❌ ${result.message}`);
    if (result.remainingAttempts !== undefined) {
      console.warn(`Kalan deneme: ${result.remainingAttempts}`);
    }
  }
};
```

### Auth Flow ile Entegrasyon

```javascript
// React - AuthContext ile SMS kayıt
import { useAuth } from "./contexts/AuthContext";

function RegisterForm() {
  const { registerWithPhone, verifyPhoneRegistration } = useAuth();

  // 1. Telefon ile kayıt başlat
  const handleRegister = async () => {
    const result = await registerWithPhone({
      phoneNumber: "05551234567",
      firstName: "Ahmet",
      lastName: "Yılmaz",
      password: "Secure123!",
    });

    if (result.success) {
      // SMS gönderildi, OTP modal'ı aç
      setShowOtpModal(true);
    }
  };

  // 2. OTP ile doğrula ve kaydı tamamla
  const handleVerify = async (code) => {
    const result = await verifyPhoneRegistration("05551234567", code);

    if (result.success) {
      // Kullanıcı giriş yaptı, token alındı
      navigate("/dashboard");
    }
  };
}
```

---

## 🔒 Güvenlik

### Rate Limiting

| Kriter          | Limit     | Süre         |
| --------------- | --------- | ------------ |
| Telefon başına  | 5 SMS     | 24 saat      |
| Telefon başına  | 3 SMS     | 1 saat       |
| Resend cooldown | 60 saniye | Her gönderim |
| Yanlış deneme   | 3 kez     | Her OTP      |
| Kod geçerliliği | 3 dakika  | -            |

### Bloke Koşulları

- **3 yanlış OTP:** 1 saat bloke
- **5+ başarısız doğrulama:** 24 saat bloke
- **Şüpheli aktivite:** Manuel inceleme

### Güvenlik Checklist

- [x] Rate limiting (IP + Telefon)
- [x] OTP expiration (3 dakika)
- [x] Maksimum deneme sayısı (3)
- [x] Resend cooldown (60 saniye)
- [x] Günlük SMS limiti (5)
- [x] Telefon normalizasyonu
- [x] IP tracking
- [x] Audit logging
- [ ] OTP code hashing (iyileştirme önerisi)
- [ ] CAPTCHA entegrasyonu (opsiyonel)

---

## 🧪 Test

### Mock SMS ile Test

```bash
# Development modda çalıştır
dotnet run --environment Development

# Console çıktısı:
# 📱 MOCK SMS: 055****67 -> Doğrulama kodunuz: 123456
```

### Unit Test

```bash
cd src/ECommerce.Tests
dotnet test --filter "FullyQualifiedName~SmsVerificationManagerTests"
```

### Integration Test

```bash
# Postman/Thunder Client ile test
POST http://localhost:5153/api/sms/send-otp
Content-Type: application/json

{
  "phoneNumber": "05551234567",
  "purpose": "registration"
}
```

### Frontend Test

```bash
cd frontend
npm test
```

---

## 📊 Database Schema

### SmsVerification Tablosu

```sql
CREATE TABLE SmsVerifications (
    Id BIGINT PRIMARY KEY IDENTITY,
    PhoneNumber NVARCHAR(20) NOT NULL,
    Code NVARCHAR(10) NOT NULL,
    Purpose INT NOT NULL, -- Enum: Registration=0, PasswordReset=1, etc.
    ExpiresAt DATETIME2 NOT NULL,
    VerifiedAt DATETIME2 NULL,
    IpAddress NVARCHAR(45) NULL,
    UserAgent NVARCHAR(500) NULL,
    UserId INT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);

CREATE INDEX IX_SmsVerifications_Phone_Purpose
    ON SmsVerifications(PhoneNumber, Purpose);
CREATE INDEX IX_SmsVerifications_ExpiresAt
    ON SmsVerifications(ExpiresAt);
```

### SmsRateLimit Tablosu

```sql
CREATE TABLE SmsRateLimits (
    Id BIGINT PRIMARY KEY IDENTITY,
    PhoneNumber NVARCHAR(20) NOT NULL,
    IpAddress NVARCHAR(45) NULL,
    DailyCount INT NOT NULL DEFAULT 0,
    HourlyCount INT NOT NULL DEFAULT 0,
    DailyResetAt DATETIME2 NOT NULL,
    HourlyResetAt DATETIME2 NOT NULL,
    LastSentAt DATETIME2 NULL,
    FailedAttempts INT NOT NULL DEFAULT 0,
    IsBlocked BIT NOT NULL DEFAULT 0,
    BlockedUntil DATETIME2 NULL,
    BlockReason NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NULL
);

CREATE INDEX IX_SmsRateLimits_PhoneNumber
    ON SmsRateLimits(PhoneNumber);
CREATE INDEX IX_SmsRateLimits_IpAddress
    ON SmsRateLimits(IpAddress);
```

---

## 🐛 Sorun Giderme

### SMS Gönderilmiyor

```bash
# 1. NetGSM credentials kontrol
dotnet user-secrets list

# 2. NetGSM enabled mi?
# appsettings.json → NetGsm:Enabled = true

# 3. Mock service kapalı mı?
# appsettings.json → NetGsm:UseMockService = false

# 4. NetGSM API durumu kontrol
curl https://api.netgsm.com.tr/sms/rest/v2/health
```

### Rate Limit Hatası

```sql
-- Database'deki rate limit kaydını temizle
DELETE FROM SmsRateLimits WHERE PhoneNumber = '05551234567';

-- Veya reset yap
UPDATE SmsRateLimits
SET DailyCount = 0, HourlyCount = 0, IsBlocked = 0
WHERE PhoneNumber = '05551234567';
```

### OTP Doğrulanmıyor

```csharp
// Kod geçerliliğini kontrol et
SELECT * FROM SmsVerifications
WHERE PhoneNumber = '05551234567'
  AND ExpiresAt > GETUTCDATE()
  AND VerifiedAt IS NULL
ORDER BY CreatedAt DESC;
```

### Development Mock SMS Çalışmıyor

```json
// appsettings.Development.json kontrol
{
  "NetGsm": {
    "Enabled": false, // ← false olmalı
    "UseMockService": true // ← true olmalı
  }
}
```

---

## 📚 İleri Seviye Kullanım

### Custom SMS Template

```csharp
// NetGsmService.cs - SMS içeriğini özelleştir
private string FormatOtpMessage(string code, SmsVerificationPurpose purpose)
{
    return purpose switch
    {
        SmsVerificationPurpose.Registration =>
            $"Hoş geldiniz! Doğrulama kodunuz: {code}. 3 dakika geçerlidir.",

        SmsVerificationPurpose.PasswordReset =>
            $"Şifre sıfırlama kodunuz: {code}. Kimseyle paylaşmayın.",

        SmsVerificationPurpose.TwoFactor =>
            $"Giriş doğrulama kodunuz: {code}.",

        _ => $"Doğrulama kodunuz: {code}"
    };
}
```

### Background Job - Expired Records Cleanup

```csharp
// Süresi dolmuş OTP kayıtlarını temizle (cronjob)
public class SmsCleanupJob : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupExpiredRecords();
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CleanupExpiredRecords()
    {
        // 7 günden eski kayıtları sil
        var cutoffDate = DateTime.UtcNow.AddDays(-7);
        await _smsRepo.DeleteExpiredAsync(cutoffDate);
    }
}
```

### Analytics Dashboard

```sql
-- SMS istatistikleri
SELECT
    Purpose,
    COUNT(*) as TotalSent,
    SUM(CASE WHEN VerifiedAt IS NOT NULL THEN 1 ELSE 0 END) as Verified,
    AVG(DATEDIFF(SECOND, CreatedAt, VerifiedAt)) as AvgVerificationTime
FROM SmsVerifications
WHERE CreatedAt >= DATEADD(DAY, -30, GETUTCDATE())
GROUP BY Purpose;
```

---

## 📞 Destek ve Katkıda Bulunma

### Destek

- **Email:** support@eticaret.com
- **Dokümantasyon:** [SMS_API_DOCUMENTATION.md](./SMS_API_DOCUMENTATION.md)
- **GitHub Issues:** https://github.com/yourusername/eticaret/issues

### Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Commit yapın (`git commit -m 'Add amazing feature'`)
4. Push yapın (`git push origin feature/amazing-feature`)
5. Pull Request açın

### Kod Standartları

- ✅ SOLID prensipleri
- ✅ Clean Code
- ✅ Türkçe yorumlar
- ✅ Unit test coverage
- ✅ XML dokümantasyonu

---

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır.

---

## 🙏 Teşekkürler

- **NetGSM** - SMS API sağlayıcısı
- **Microsoft** - .NET 9.0 framework
- **React Community** - Frontend kütüphaneleri

---

**Son Güncelleme:** 9 Ocak 2026  
**Versiyon:** 1.0.0  
**Yazar:** E-Ticaret Geliştirme Ekibi
