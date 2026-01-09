# SMS Doğrulama API Dokümantasyonu

## Genel Bakış

Bu API, NetGSM entegrasyonu ile SMS tabanlı OTP (One-Time Password) doğrulama hizmeti sağlar.

**Base URL:** `http://localhost:5153/api/sms`

**Güvenlik:** Rate limiting aktif (IP ve telefon bazlı)

---

## Endpoint'ler

### 1. OTP Kodu Gönder

**POST** `/api/sms/send-otp`

Belirtilen telefon numarasına 6 haneli doğrulama kodu gönderir.

#### Request Body

```json
{
  "phoneNumber": "05551234567",
  "purpose": "registration"
}
```

| Alan        | Tip    | Zorunlu | Açıklama                                                             |
| ----------- | ------ | ------- | -------------------------------------------------------------------- |
| phoneNumber | string | Evet    | Türkiye formatında telefon (05XXXXXXXXX)                             |
| purpose     | string | Evet    | `registration`, `password_reset`, `two_factor`, `phone_verification` |

#### Response (200 OK)

```json
{
  "success": true,
  "message": "Doğrulama kodu gönderildi",
  "expiresInSeconds": 180,
  "canResendAfter": 60,
  "attemptCount": 1,
  "errorCode": null
}
```

#### Response (429 Too Many Requests)

```json
{
  "success": false,
  "message": "Günlük SMS limitiniz doldu. Yarın tekrar deneyin",
  "retryAfterSeconds": 43200,
  "errorCode": "DAILY_LIMIT_EXCEEDED"
}
```

#### Hata Kodları

| Kod                     | Açıklama                                                   |
| ----------------------- | ---------------------------------------------------------- |
| `DAILY_LIMIT_EXCEEDED`  | Günlük 5 SMS limiti aşıldı                                 |
| `HOURLY_LIMIT_EXCEEDED` | Saatlik 3 SMS limiti aşıldı                                |
| `RESEND_COOLDOWN`       | 60 saniye beklemeden yeniden gönderim yapılamaz            |
| `PHONE_BLOCKED`         | Telefon numarası bloke edildi (çok fazla başarısız deneme) |
| `INVALID_PHONE`         | Geçersiz telefon numarası formatı                          |
| `SMS_PROVIDER_ERROR`    | NetGSM API hatası                                          |

---

### 2. OTP Kodu Doğrula

**POST** `/api/sms/verify-otp`

Gönderilen OTP kodunu doğrular.

#### Request Body

```json
{
  "phoneNumber": "05551234567",
  "code": "123456",
  "purpose": "registration"
}
```

#### Response (200 OK)

```json
{
  "success": true,
  "message": "Telefon numaranız doğrulandı",
  "verified": true,
  "remainingAttempts": null,
  "errorCode": null
}
```

#### Response (400 Bad Request - Yanlış Kod)

```json
{
  "success": false,
  "message": "Doğrulama kodu hatalı",
  "verified": false,
  "remainingAttempts": 2,
  "errorCode": "INVALID_CODE"
}
```

#### Response (400 Bad Request - Süresi Dolmuş)

```json
{
  "success": false,
  "message": "Doğrulama kodunun süresi doldu",
  "verified": false,
  "errorCode": "CODE_EXPIRED"
}
```

#### Hata Kodları

| Kod                     | Açıklama                                  |
| ----------------------- | ----------------------------------------- |
| `INVALID_CODE`          | Yanlış doğrulama kodu (3 deneme hakkı)    |
| `CODE_EXPIRED`          | Kod süresi dolmuş (3 dakika)              |
| `MAX_ATTEMPTS_EXCEEDED` | 3 yanlış deneme yapıldı                   |
| `NOT_FOUND`             | Bu telefon için aktif doğrulama kaydı yok |

---

### 3. OTP Yeniden Gönder

**POST** `/api/sms/resend-otp`

Aynı telefona yeniden OTP kodu gönderir.

#### Request Body

```json
{
  "phoneNumber": "05551234567",
  "purpose": "registration"
}
```

#### Response

`/api/sms/send-otp` ile aynı

**Not:** 60 saniye cooldown uygulanır.

---

### 4. Durum Sorgula

**GET** `/api/sms/status/{phoneNumber}?purpose=registration`

Telefon numarası için aktif doğrulama durumunu sorgular.

#### Response (200 OK)

```json
{
  "phoneNumber": "05551234567",
  "hasActiveVerification": true,
  "expiresAt": "2026-01-09T15:30:00Z",
  "remainingSeconds": 165,
  "failedAttempts": 1,
  "canResend": false,
  "resendAvailableAt": "2026-01-09T15:28:30Z"
}
```

---

### 5. Gönderim İzni Kontrol

**GET** `/api/sms/can-send?phoneNumber=05551234567`

Telefona OTP gönderilip gönderilemeyeceğini kontrol eder.

#### Response

```json
{
  "canSend": true,
  "reason": null,
  "retryAfterSeconds": null,
  "dailyRemaining": 3,
  "hourlyRemaining": 2
}
```

---

## Rate Limiting

### Limitler

| Kriter              | Limit     | Süre               |
| ------------------- | --------- | ------------------ |
| **Telefon başına**  | 5 SMS     | 24 saat            |
| **Telefon başına**  | 3 SMS     | 1 saat             |
| **Resend Cooldown** | 60 saniye | Her gönderim arası |
| **Yanlış Deneme**   | 3 kez     | Her OTP için       |
| **Kod Geçerliliği** | 3 dakika  | Her OTP            |

### Blokla

ma Koşulları

- 3 yanlış OTP denemesi → 1 saat bloke
- 5+ başarısız doğrulama → 24 saat bloke
- Şüpheli aktivite → Manuel inceleme

---

## Kullanım Örnekleri

### JavaScript/TypeScript

```javascript
// 1. OTP Gönder
const sendOtp = async (phoneNumber) => {
  const response = await fetch("http://localhost:5153/api/sms/send-otp", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      phoneNumber,
      purpose: "registration",
    }),
  });

  const result = await response.json();

  if (result.success) {
    console.log(`Kod gönderildi! ${result.expiresInSeconds}s geçerli`);
    return result;
  } else {
    console.error(`Hata: ${result.message}`);
    throw new Error(result.errorCode);
  }
};

// 2. OTP Doğrula
const verifyOtp = async (phoneNumber, code) => {
  const response = await fetch("http://localhost:5153/api/sms/verify-otp", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      phoneNumber,
      code,
      purpose: "registration",
    }),
  });

  const result = await response.json();

  if (result.success) {
    console.log("✅ Doğrulama başarılı!");
    return true;
  } else {
    console.error(`❌ Hata: ${result.message}`);
    if (result.remainingAttempts !== null) {
      console.warn(`⚠️ Kalan deneme: ${result.remainingAttempts}`);
    }
    return false;
  }
};

// 3. Kullanım
try {
  await sendOtp("05551234567");
  // Kullanıcı kodu girer...
  const verified = await verifyOtp("05551234567", "123456");
  if (verified) {
    // Kayıt işlemine devam et
  }
} catch (error) {
  console.error("OTP akışı hatası:", error);
}
```

### cURL Örnekleri

```bash
# OTP Gönder
curl -X POST http://localhost:5153/api/sms/send-otp \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "05551234567",
    "purpose": "registration"
  }'

# OTP Doğrula
curl -X POST http://localhost:5153/api/sms/verify-otp \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "05551234567",
    "code": "123456",
    "purpose": "registration"
  }'

# Durum Sorgula
curl -X GET "http://localhost:5153/api/sms/status/05551234567?purpose=registration"

# Gönderim İzni Kontrol
curl -X GET "http://localhost:5153/api/sms/can-send?phoneNumber=05551234567"
```

---

## Test Ortamı

### Mock SMS Servisi

Development ortamında gerçek SMS göndermeden test yapmak için:

```json
// appsettings.Development.json
{
  "NetGsm": {
    "Enabled": false,
    "UseMockService": true
  }
}
```

Mock servis aktif olduğunda:

- Gerçek SMS gönderilmez
- Tüm gönderimler başarılı sayılır
- Kodlar console'a yazılır
- Rate limiting çalışır

### Test Senaryoları

```bash
# 1. Başarılı Akış
POST /send-otp → 200 OK
POST /verify-otp → 200 OK (doğru kod)

# 2. Yanlış Kod
POST /send-otp → 200 OK
POST /verify-otp → 400 Bad Request (yanlış kod, 2 deneme kaldı)
POST /verify-otp → 400 Bad Request (yanlış kod, 1 deneme kaldı)
POST /verify-otp → 400 Bad Request (yanlış kod, 0 deneme kaldı - bloke)

# 3. Süresi Dolmuş Kod
POST /send-otp → 200 OK
# 3+ dakika bekle
POST /verify-otp → 400 Bad Request (kod süresi doldu)

# 4. Rate Limiting
POST /send-otp → 200 OK
POST /send-otp → 429 Too Many Requests (60s cooldown)
# 60 saniye bekle
POST /send-otp → 200 OK

# 5. Günlük Limit
# 5 kez başarılı gönderim yap
POST /send-otp (6. kez) → 429 Too Many Requests (günlük limit)
```

---

## Güvenlik Önerileri

### Frontend

1. ✅ OTP kodunu **asla** URL parametresinde gönderm eyin
2. ✅ HTTPS kullanın (production)
3. ✅ Rate limit mesajlarını kullanıcıya gösterin
4. ✅ Countdown timer ekleyin (resend butonu için)
5. ✅ Kalan deneme sayısını gösterin

### Backend

1. ✅ Rate limiting aktif (✓ Uygulanmış)
2. ✅ IP bazlı koruma (✓ Uygulanmış)
3. ✅ Telefon normalizasyonu (✓ Uygulanmış)
4. ✅ Code hashing (✓ Plain text, iyileştirilebilir)
5. ✅ Audit logging (✓ Uygulanmış)

### Production

1. ⚠️ User Secrets veya Environment Variables kullanın
2. ⚠️ NetGSM credentials'ları **asla** Git'e eklemeyin
3. ⚠️ HTTPS zorunlu yapın
4. ⚠️ CORS ayarlarını daraltın
5. ⚠️ IP whitelist (admin panel için)

---

## Swagger UI

API dokümantasyonunu tarayıcıda görüntülemek için:

```
http://localhost:5153/swagger
```

Burada tüm endpoint'leri test edebilirsiniz.

---

## Destek

Sorularınız için:

- **Email:** support@eticaret.com
- **Dokümantasyon:** https://docs.eticaret.com/sms-api
- **GitHub Issues:** https://github.com/yourusername/eticaret/issues
