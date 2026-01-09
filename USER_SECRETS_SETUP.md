# User Secrets Kurulum Rehberi

## Development Ortamı için Güvenli Yapılandırma

### 1. User Secrets Başlatma

```bash
cd src/ECommerce.API
dotnet user-secrets init
```

### 2. NetGSM Credentials Ekleme

```bash
# NetGSM kullanıcı adı
dotnet user-secrets set "NetGsm:UserCode" "YOUR_NETGSM_USERNAME"

# NetGSM şifre
dotnet user-secrets set "NetGsm:Password" "YOUR_NETGSM_PASSWORD"

# SMS başlığı (NetGSM'den onaylı)
dotnet user-secrets set "NetGsm:MsgHeader" "YOUR_SMS_HEADER"

# Uygulama adı
dotnet user-secrets set "NetGsm:AppName" "E-Ticaret"
```

### 3. JWT Secret Key Ekleme

```bash
# Güçlü bir secret key oluştur (min 32 karakter)
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 32)"
```

### 4. Veritabanı Şifresi (Opsiyonel)

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1435;Database=ECommerceDb;User Id=sa;Password=YOUR_SECURE_PASSWORD;TrustServerCertificate=True;"
```

### 5. User Secrets Kontrol

```bash
# Tüm secret'ları listele
dotnet user-secrets list

# Belirli bir secret'ı göster
dotnet user-secrets list | grep NetGsm
```

### 6. User Secrets Temizleme

```bash
# Tüm secret'ları sil
dotnet user-secrets clear

# Belirli bir secret'ı sil
dotnet user-secrets remove "NetGsm:Password"
```

## Güvenlik Notları

⚠️ **ASLA** `secrets.json` veya gerçek credential'ları Git'e eklemeyin!

✅ `.gitignore` dosyasında şunlar olmalı:

```
**/secrets.json
**/.user-secrets/
appsettings.Production.json
```

✅ User Secrets'ın avantajları:

- Kod deposunda saklanmaz
- Geliştirici bazında farklı credential'lar
- Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- Linux/macOS: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

## Production Ortamı

Production'da User Secrets kullanmayın. Bunun yerine:

- **Azure**: Key Vault
- **AWS**: Secrets Manager
- **Docker**: Environment Variables
- **Kubernetes**: Secrets

## Test Ortamı

Test ortamında SMS göndermemek için:

```bash
dotnet user-secrets set "NetGsm:Enabled" "false"
dotnet user-secrets set "NetGsm:UseMockService" "true"
```

Bu durumda `MockSmsService` devreye girer ve gerçek SMS gönderilmez.
