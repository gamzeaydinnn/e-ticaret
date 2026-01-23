# 🚀 YAPI KREDİ POSNET PRODUCTION DEPLOYMENT CHECKLIST

## 📋 Genel Bakış

Bu doküman, Yapı Kredi POSNET ödeme sisteminin production ortamına geçişi için gerekli tüm adımları içerir.

---

## ✅ 1. BANKA TARAFINDAN SAĞLANACAK BİLGİLER

Aşağıdaki bilgiler Yapı Kredi Bankası tarafından sağlanmalıdır:

| Bilgi              | Açıklama                                   | Durum |
| ------------------ | ------------------------------------------ | ----- |
| `MerchantId`       | 10 haneli üye işyeri numarası              | ⬜    |
| `TerminalId`       | 8 haneli terminal numarası                 | ⬜    |
| `PosnetId`         | POSNET numarası (16 haneye kadar)          | ⬜    |
| `EncKey`           | 3D Secure şifreleme anahtarı (32 karakter) | ⬜    |
| Production URL'ler | Canlı ortam endpoint'leri                  | ⬜    |

### 🔑 Credential Kontrolü

```bash
# Environment variable'ları kontrol et
echo $POSNET_MERCHANT_ID
echo $POSNET_TERMINAL_ID
echo $POSNET_ID
# EncKey asla ekrana yazdırılmamalı!
```

---

## ✅ 2. SUNUCU KONFİGÜRASYONU

### 2.1 Statik IP Bildirimi

- [ ] Sunucu statik IP adresi belirlendi
- [ ] IP adresi Yapı Kredi'ye bildirildi
- [ ] Banka tarafından whitelist'e eklendi

**Sunucu IP:** `31.186.24.78`

### 2.2 SSL/TLS Sertifikası

- [ ] Domain için SSL sertifikası kurulu (`golkoygurme.com.tr`)
- [ ] TLS 1.2 veya üzeri aktif
- [ ] HTTP Strict Transport Security (HSTS) aktif

```bash
# SSL kontrolü
openssl s_client -connect golkoygurme.com.tr:443 -tls1_2
```

### 2.3 Firewall Kuralları

- [ ] Outbound 443 portu açık (POSNET endpoint'leri için)
- [ ] Inbound callback URL'i erişilebilir

---

## ✅ 3. UYGULAMA KONFİGÜRASYONU

### 3.1 appsettings.Production.json

```json
{
  "PaymentSettings": {
    "PosnetMerchantId": "${POSNET_MERCHANT_ID}",
    "PosnetTerminalId": "${POSNET_TERMINAL_ID}",
    "PosnetId": "${POSNET_ID}",
    "PosnetEncKey": "${POSNET_ENC_KEY}",
    "PosnetXmlServiceUrl": "https://posnet.yapikredi.com.tr/PosnetWebService/XML",
    "Posnet3DServiceUrl": "https://posnet.yapikredi.com.tr/3DSWebService/YKBPaymentService",
    "PosnetCallbackUrl": "https://golkoygurme.com.tr/api/payments/posnet/3d-callback",
    "PosnetIsTestEnvironment": false,
    "PosnetTimeoutSeconds": 60,
    "PosnetWorldPointEnabled": true
  }
}
```

### 3.2 Environment Variables (Önerilen)

```bash
# Docker Compose veya Kubernetes secrets kullanılabilir
POSNET_MERCHANT_ID=<banka_tarafından_sağlanan>
POSNET_TERMINAL_ID=<banka_tarafından_sağlanan>
POSNET_ID=<banka_tarafından_sağlanan>
POSNET_ENC_KEY=<banka_tarafından_sağlanan>
```

### 3.3 User Secrets (Geliştirme için)

```bash
dotnet user-secrets set "PaymentSettings:PosnetMerchantId" "XXX"
dotnet user-secrets set "PaymentSettings:PosnetTerminalId" "XXX"
dotnet user-secrets set "PaymentSettings:PosnetId" "XXX"
dotnet user-secrets set "PaymentSettings:PosnetEncKey" "XXX"
```

---

## ✅ 4. GÜVENLİK KONTROL LİSTESİ

### 4.1 PCI-DSS Uyumluluğu

- [ ] Kart numaraları düz metin olarak loglanmıyor
- [ ] CVV asla veritabanına kaydedilmiyor
- [ ] Tüm iletişim TLS ile şifreli
- [ ] Kart verileri maskelenmiş olarak saklanıyor

### 4.2 Kod Güvenliği

- [ ] EncKey ve credential'lar source code'da yok
- [ ] `.gitignore` dosyasında secrets ignore ediliyor
- [ ] Güvenli rastgele transaction ID üretimi aktif

### 4.3 Rate Limiting

- [ ] Rate limiting aktif (30 istek/dakika, 200 istek/saat)
- [ ] Fraud detection mekanizması aktif
- [ ] Brute force koruması aktif

### 4.4 Loglama

- [ ] Audit loglama aktif
- [ ] Hassas veriler maskelenmiş olarak loglanıyor
- [ ] Log dosyaları güvenli depolanıyor

---

## ✅ 5. TEST SENARYOları

### 5.1 Sandbox Test (Canlı Öncesi)

```bash
# Test kartları ile sandbox'ta test
# Başarılı kart: 4506349116543211
# Yetersiz bakiye: 4111111111111111

curl -X POST https://staging.golkoygurme.com.tr/api/payments/posnet/test \
  -H "Content-Type: application/json" \
  -d '{"amount": 100, "cardNumber": "4506349116543211", "installment": 1}'
```

### 5.2 Production Test (Küçük Tutar)

- [ ] 1 TL ile test ödeme yapıldı
- [ ] İptal işlemi test edildi
- [ ] İade işlemi test edildi
- [ ] 3D Secure akışı test edildi

### 5.3 Taksit Testleri

- [ ] Tek çekim test edildi
- [ ] 3 taksit test edildi
- [ ] 6 taksit test edildi
- [ ] Desteklenmeyen taksit sayısı için hata kontrolü

---

## ✅ 6. MONİTORİNG & ALERTING

### 6.1 Health Check Endpoint

```bash
# Health check
curl https://golkoygurme.com.tr/health/posnet
```

### 6.2 Prometheus Metrics (Opsiyonel)

```yaml
# prometheus.yml
scrape_configs:
  - job_name: "ecommerce-posnet"
    static_configs:
      - targets: ["golkoygurme.com.tr:5000"]
    metrics_path: "/metrics"
```

### 6.3 Alert Kuralları

- [ ] POSNET timeout alert'i ayarlandı (>5 saniye)
- [ ] Hata oranı alert'i ayarlandı (>%5)
- [ ] Rate limit aşımı alert'i ayarlandı

---

## ✅ 7. ROLLBACK PLANI

### 7.1 Önceki Sürüme Dönüş

```bash
# Docker ile rollback
docker-compose down
docker-compose -f docker-compose.prod.yml up -d --build
```

### 7.2 Yedekleme

- [ ] Veritabanı yedeği alındı
- [ ] Konfigürasyon dosyaları yedeklendi
- [ ] Mevcut çalışan versiyon tag'lendi

---

## ✅ 8. DEPLOYMENT ADIMLARI

### Adım 1: Hazırlık

```bash
# Son değişiklikleri çek
git pull origin main

# Environment variables'ları ayarla
export POSNET_MERCHANT_ID="xxx"
export POSNET_TERMINAL_ID="xxx"
export POSNET_ID="xxx"
export POSNET_ENC_KEY="xxx"
```

### Adım 2: Build & Deploy

```bash
# Backend build
cd src/ECommerce.API
dotnet publish -c Release -o ./publish

# Frontend build
cd frontend
npm run build

# Docker deployment
docker-compose -f docker-compose.prod.yml up -d --build
```

### Adım 3: Doğrulama

```bash
# Backend health check
curl https://golkoygurme.com.tr/health

# POSNET özel health check
curl https://golkoygurme.com.tr/api/payments/posnet/health

# Test ödeme (1 TL)
curl -X POST https://golkoygurme.com.tr/api/payments/posnet/test \
  -H "Authorization: Bearer <admin_token>" \
  -H "Content-Type: application/json" \
  -d '{"amount": 1}'
```

---

## ✅ 9. CANLIYA GEÇIŞ ONAYLARI

| Onay                         | Sorumlu  | Tarih | İmza |
| ---------------------------- | -------- | ----- | ---- |
| Teknik Test Tamamlandı       | DevOps   |       | ⬜   |
| Güvenlik Kontrolü Tamamlandı | Security |       | ⬜   |
| Banka Onayı Alındı           | Finans   |       | ⬜   |
| Proje Yöneticisi Onayı       | PM       |       | ⬜   |

---

## 📞 ACİL DURUM İLETİŞİM

| Konu                      | İletişim                 |
| ------------------------- | ------------------------ |
| Yapı Kredi Teknik Destek  | 0850 XXX XX XX           |
| Yapı Kredi İşyeri Servisi | 0212 XXX XX XX           |
| Sistem Yöneticisi         | admin@golkoygurme.com.tr |
| Proje Yöneticisi          | pm@golkoygurme.com.tr    |

---

## 📝 NOTLAR

1. **Test ortamı URL'leri:**
   - XML: `https://setmpos.ykb.com/PosnetWebService/XML`
   - 3D Secure: `https://setmpos.ykb.com/3DSWebService/YKBPaymentService`

2. **Production URL'leri:**
   - XML: `https://posnet.yapikredi.com.tr/PosnetWebService/XML`
   - 3D Secure: `https://posnet.yapikredi.com.tr/3DSWebService/YKBPaymentService`

3. **Test Kart Numaraları:**
   - Başarılı Visa: `4506349116543211`
   - Başarılı Mastercard: `5406675406675403`
   - Yetersiz Bakiye: `4111111111111111`

4. **MAC Hesaplama:**
   - SHA-256 kullanılır
   - EncKey Base64 encoded olmalı

---

**Son Güncelleme:** 2026-01-19
**Versiyon:** 1.0.0
**Hazırlayan:** DevOps Team
