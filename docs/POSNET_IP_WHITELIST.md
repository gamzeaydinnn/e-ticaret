# 🔒 YAPI KREDİ POSNET IP WHİTELİST REHBERİ

## 📋 Genel Bilgi

Yapı Kredi POSNET entegrasyonu için, bankanın güvenlik politikası gereği sunucu IP adresinizin banka tarafında whitelist'e eklenmesi gerekmektedir.

---

## 🌐 SUNUCU BİLGİLERİ

### Production Sunucusu

| Bilgi            | Değer                                                        |
| ---------------- | ------------------------------------------------------------ |
| **Sunucu IP**    | `31.186.24.78`                                               |
| **Domain**       | `golkoygurme.com.tr`                                         |
| **Callback URL** | `https://golkoygurme.com.tr/api/payments/posnet/3d-callback` |
| **Port**         | `443` (HTTPS)                                                |

### Yedek/Staging Sunucusu (Opsiyonel)

| Bilgi            | Değer                                                                |
| ---------------- | -------------------------------------------------------------------- |
| **Sunucu IP**    | `TBD`                                                                |
| **Domain**       | `staging.golkoygurme.com.tr`                                         |
| **Callback URL** | `https://staging.golkoygurme.com.tr/api/payments/posnet/3d-callback` |

---

## 📝 BANKA'YA BİLDİRİLECEK BİLGİLER

### 1. İşyeri Bilgileri

```
İşyeri Adı: Gölköy Gürme E-Ticaret
Vergi No: XXXXXXXXXX
Ticari Sicil No: XXXXXX
Yetkili Kişi: [AD SOYAD]
E-posta: [EMAIL]
Telefon: [TELEFON]
```

### 2. Teknik Bilgiler

```
Sunucu IP Adresi: 31.186.24.78
Domain Adı: golkoygurme.com.tr
SSL Sertifikası: Var (Let's Encrypt / veya ticari sertifika)
TLS Versiyonu: 1.2 ve üzeri

3D Secure Callback URL:
- Production: https://golkoygurme.com.tr/api/payments/posnet/3d-callback
- Test: http://localhost:5153/api/payments/posnet/3d-callback

API Erişim Noktaları:
- Production: https://golkoygurme.com.tr/api/payments/
- Test: http://localhost:5153/api/payments/
```

### 3. Kullanılacak POSNET Özellikleri

```
[X] Direkt Satış (Sale)
[X] 3D Secure (OOS-TDS)
[X] İptal (Reverse)
[X] İade (Return)
[X] World Puan Sorgulama
[ ] Joker Vadaa
[ ] VFT (Vade Farklı İşlemler)
```

---

## 📧 BANKA İLETİŞİM ŞABLONU

### Konu: POSNET Entegrasyonu IP Whitelist Talebi

```
Sayın Yapı Kredi POSNET Teknik Destek,

Firmamiız [FİRMA ADI] olarak POSNET entegrasyonu gerçekleştirmekteyiz.
Aşağıdaki sunucu bilgilerimizin whitelist'e eklenmesini talep ediyoruz.

İŞYERİ BİLGİLERİ:
- MerchantId: [MERCHANT_ID]
- TerminalId: [TERMINAL_ID]
- Firma Adı: [FİRMA ADI]

SUNUCU BİLGİLERİ:
- Sunucu IP: 31.186.24.78
- Domain: golkoygurme.com.tr
- Callback URL: https://golkoygurme.com.tr/api/payments/posnet/3d-callback

İSTENEN HİZMETLER:
- 3D Secure (OOS-TDS)
- Satış (Sale)
- İptal (Reverse)
- İade (Return)
- World Puan Entegrasyonu

SSL sertifikamız aktiftir ve TLS 1.2+ desteklenmektedir.

Gereğini arz ederiz.

Saygılarımızla,
[AD SOYAD]
[POZİSYON]
[FİRMA ADI]
[TELEFON]
[E-POSTA]
```

---

## 🔧 TEKNİK KONTROLLER

### IP Adresi Doğrulama

```bash
# Sunucu public IP kontrolü
curl ifconfig.me

# DNS kontrolü
nslookup golkoygurme.com.tr

# SSL sertifika kontrolü
openssl s_client -connect golkoygurme.com.tr:443 -servername golkoygurme.com.tr
```

### Callback URL Erişilebilirlik Testi

```bash
# Callback endpoint erişim testi
curl -X POST https://golkoygurme.com.tr/api/payments/posnet/3d-callback \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "test=1"

# Beklenen yanıt: 400 Bad Request veya özel hata mesajı (endpoint çalışıyor demek)
```

---

## ⚠️ ÖNEMLİ NOTLAR

1. **Statik IP Gereksinimi:**
   - POSNET entegrasyonu için sunucunuzun statik IP adresine sahip olması zorunludur.
   - Dinamik IP adresleri kabul edilmez.

2. **IP Değişikliği:**
   - Sunucu IP adresi değişirse, HEMEN bankaya bildirilmelidir.
   - Yeni IP whitelist'e eklenene kadar ödemeler çalışmaz.

3. **Çoklu Sunucu:**
   - Load balancer kullanıyorsanız, TÜM sunucu IP'leri bildirilmelidir.
   - Outbound NAT IP'si kullanılıyorsa, bu IP bildirilmelidir.

4. **Test ve Production Ayrımı:**
   - Test ortamı (sandbox) için IP bildirimi genellikle gerekmez.
   - Production geçişte kesinlikle IP bildirimi yapılmalıdır.

5. **Firewall Kuralları:**
   - Outbound 443 portu açık olmalıdır.
   - POSNET endpoint'lerine erişim engellenemez.

---

## 📞 ACİL DURUMDA

IP değişikliği veya erişim sorunlarında:

| İletişim                 | Bilgi                   |
| ------------------------ | ----------------------- |
| Yapı Kredi POSNET Destek | 0850 XXX XX XX          |
| E-posta                  | posnet@yapikredi.com.tr |
| Çalışma Saatleri         | Hafta içi 09:00 - 18:00 |

---

## 📄 İLGİLİ BELGELER

- POSNET Entegrasyon Dokümanı v2.1.1.3
- Production Checklist: `docs/POSNET_PRODUCTION_CHECKLIST.md`
- Güvenlik Gereksinimleri: PCI-DSS Compliance Guide

---

**Son Güncelleme:** 2026-01-19
**Hazırlayan:** DevOps Team
