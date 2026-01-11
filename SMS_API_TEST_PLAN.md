# 📱 SMS Doğrulama API Test Planı

Bu doküman, SMS doğrulama API'sinin test edilmesi için kullanılacak Swagger/Postman test senaryolarını içerir.

## 📋 Endpoint Listesi

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| POST | `/api/sms/send-otp` | OTP kodu gönderir |
| POST | `/api/sms/verify-otp` | OTP kodunu doğrular |
| POST | `/api/sms/resend-otp` | OTP kodunu tekrar gönderir |
| GET | `/api/sms/status/{phone}` | Doğrulama durumunu sorgular |
| GET | `/api/sms/can-send?phone=xxx` | OTP gönderilebilir mi kontrol eder |

---

## 🧪 Test Senaryoları

### 1. OTP Gönderme (send-otp)

#### ✅ Test 1.1: Başarılı OTP Gönderimi
```http
POST /api/sms/send-otp
Content-Type: application/json

{
    "phoneNumber": "5331234567",
    "purpose": 0
}
```

**Beklenen Yanıt (200 OK):**
```json
{
    "success": true,
    "message": "Doğrulama kodu telefonunuza gönderildi.",
    "expiresInSeconds": 180,
    "remainingDailyCount": 4
}
```

#### ❌ Test 1.2: Geçersiz Telefon Numarası
```http
POST /api/sms/send-otp
Content-Type: application/json

{
    "phoneNumber": "123",
    "purpose": 0
}
```

**Beklenen Yanıt (400 Bad Request):**
```json
{
    "success": false,
    "message": "Geçerli bir Türkiye cep telefonu numarası giriniz.",
    "errorCode": "INVALID_PHONE"
}
```

#### ⚠️ Test 1.3: Rate Limit (60 saniye içinde tekrar istek)
```http
POST /api/sms/send-otp
Content-Type: application/json

{
    "phoneNumber": "5331234567",
    "purpose": 0
}
```

**Beklenen Yanıt (429 Too Many Requests):**
```json
{
    "success": false,
    "message": "Çok fazla istek gönderildi. Lütfen 58 saniye bekleyin.",
    "errorCode": "RATE_LIMITED",
    "retryAfterSeconds": 58
}
```
**Header:** `Retry-After: 58`

---

### 2. OTP Doğrulama (verify-otp)

#### ✅ Test 2.1: Başarılı Doğrulama
```http
POST /api/sms/verify-otp
Content-Type: application/json

{
    "phoneNumber": "5331234567",
    "code": "123456",
    "purpose": 0
}
```

**Beklenen Yanıt (200 OK):**
```json
{
    "success": true,
    "message": "Telefon numaranız başarıyla doğrulandı."
}
```

#### ❌ Test 2.2: Yanlış Kod
```http
POST /api/sms/verify-otp
Content-Type: application/json

{
    "phoneNumber": "5331234567",
    "code": "000000",
    "purpose": 0
}
```

**Beklenen Yanıt (400 Bad Request):**
```json
{
    "success": false,
    "message": "Girdiğiniz kod hatalı. 2 deneme hakkınız kaldı.",
    "errorCode": "INVALID_CODE",
    "remainingAttempts": 2
}
```

#### ⏰ Test 2.3: Süresi Dolmuş Kod
```http
POST /api/sms/verify-otp
Content-Type: application/json

{
    "phoneNumber": "5331234567",
    "code": "123456",
    "purpose": 0
}
```

**Beklenen Yanıt (400 Bad Request):**
```json
{
    "success": false,
    "message": "Kodun süresi doldu. Lütfen yeni kod isteyin.",
    "errorCode": "CODE_EXPIRED"
}
```

#### 🚫 Test 2.4: Maksimum Deneme Aşıldı
**Beklenen Yanıt (400 Bad Request):**
```json
{
    "success": false,
    "message": "Maksimum deneme sayısına ulaştınız. Lütfen yeni kod isteyin.",
    "errorCode": "MAX_ATTEMPTS"
}
```

---

### 3. Tekrar Gönderme (resend-otp)

#### ✅ Test 3.1: Başarılı Tekrar Gönderim (60 saniye sonra)
```http
POST /api/sms/resend-otp
Content-Type: application/json

{
    "phoneNumber": "5331234567",
    "purpose": 0
}
```

**Beklenen Yanıt (200 OK):**
```json
{
    "success": true,
    "message": "Doğrulama kodu telefonunuza gönderildi.",
    "expiresInSeconds": 180
}
```

---

### 4. Durum Sorgulama (status)

#### Test 4.1: Aktif Doğrulama Var
```http
GET /api/sms/status/5331234567?purpose=0
```

**Beklenen Yanıt (200 OK):**
```json
{
    "hasActiveVerification": true,
    "status": "Pending",
    "remainingSeconds": 145,
    "remainingAttempts": 3,
    "resendAfterSeconds": 35,
    "canResend": false,
    "remainingDailyCount": 4
}
```

#### Test 4.2: Aktif Doğrulama Yok
```http
GET /api/sms/status/5339999999?purpose=0
```

**Beklenen Yanıt (200 OK):**
```json
{
    "hasActiveVerification": false,
    "status": "None",
    "canResend": true
}
```

---

### 5. Can-Send Kontrolü

#### Test 5.1: Gönderilebilir
```http
GET /api/sms/can-send?phone=5331234567
```

**Beklenen Yanıt (200 OK):**
```json
{
    "canSend": true,
    "remainingDailyCount": 5,
    "isBlocked": false
}
```

---

## 📊 Purpose Enum Değerleri

| Değer | Açıklama |
|-------|----------|
| 0 | Registration (Kayıt) |
| 1 | PasswordReset (Şifre Sıfırlama) |
| 2 | TwoFactorAuth (2FA) |
| 3 | PhoneChange (Telefon Değişikliği) |
| 4 | General (Genel) |

---

## 🔒 Güvenlik Testleri

### Test: Brute Force Koruması
1. Aynı numaraya 5 kez yanlış kod girin
2. Numara geçici olarak bloklanmalı

### Test: Rate Limiting
1. 60 saniye içinde 2. SMS isteği gönderin
2. 429 Too Many Requests almalısınız

### Test: Günlük Limit
1. Aynı numaraya 5 SMS gönderin
2. 6. istekte günlük limit hatası almalısınız

---

## 📝 Swagger URL
```
https://localhost:5001/swagger
```

## 🚀 Postman Collection
İmport edilecek Postman collection dosyası:
- `SMS_Verification_API.postman_collection.json`

---

## ✅ Checklist

- [ ] send-otp başarılı senaryo
- [ ] send-otp geçersiz telefon
- [ ] send-otp rate limit (60s cooldown)
- [ ] send-otp günlük limit (5 SMS)
- [ ] verify-otp başarılı doğrulama
- [ ] verify-otp yanlış kod
- [ ] verify-otp süresi dolmuş kod
- [ ] verify-otp maksimum deneme
- [ ] resend-otp başarılı
- [ ] status aktif doğrulama var
- [ ] status aktif doğrulama yok
- [ ] can-send kontrolü

---

**Not:** Migration'ı uygulamadan API çalışmayacaktır:
```bash
cd src/ECommerce.API
dotnet ef database update --project ../ECommerce.Data
```
