# Docker Mikro VPN Setup

Bu kurulumda tum backend trafigini VPN'e tasimiyoruz. Sadece Mikro ERP HTTP ve Mikro ERP SQL istekleri `mikro-vpn` sidecar uzerinden geciyor.

## Mimari

- `api` normal Docker aginda kalir.
- `mikro-vpn` OpenVPN istemcisidir.
- `mikro-api-relay` ve `mikro-sql-relay`, `mikro-vpn` network namespace'ini paylasir.
- API, Mikro HTTP icin `https://mikro-vpn:8084` adresine gider.
- API, Mikro SQL icin `mikro-vpn:1433` adresine gider.

## Sunucuda Zorunlu Dosyalar

- `.env`
  Aciklama: Mikro hedef host/port ve SQL kullanici bilgileri burada tutulur.

## Sunucuda Kod Ile Ayni VPN Akisi

Sunucuda da lokal ile ayni mantik calisir:

- uygulama konteyneri Mikro HTTP icin `https://mikro-vpn:8084` adresine gider
- uygulama konteyneri Mikro SQL icin `mikro-vpn:1433` adresine gider
- bu iki trafik `mikro-vpn` sidecar uzerinden tunellenir
- yani urun cekme, fiyat cekme ve Mikro SQL sorgulari dogrudan hosttan degil VPN icinden akar

Bu davranisin kaynagi `docker-compose.prod.yml` icindeki environment override'lardir. Bu nedenle sunucuda `appsettings.Production.json` olmadan da sistem calisir; gerekli degerler `.env` ve compose environment ile verilir.

## VPN Dosyasi Nerede Durmali?

`vpn.ovpn` dosyasini repo icine koymak zorunda degilsiniz.

`.env` icinde su alan kullanilir:

```env
VPN_CONFIG_PATH=./vpn.ovpn
```

Bu profil `auth-user-pass` kullanmiyor. Sertifika, private key ve `tls-crypt-v2` anahtari dogrudan `.ovpn` dosyasinin icinde.

Isterseniz repo icindeki [vpn.ovpn](c:\Users\GAMZE\Desktop\eticaret\vpn.ovpn) dosyasini aynen sunucuya tasiyabilirsiniz. Repo disinda tutmak isterseniz yine absolute path verebilirsiniz:

```bash
mkdir -p /home/huseyinadm/secrets
cp /path/to/real-client.ovpn /home/huseyinadm/secrets/mikro.ovpn
chmod 600 /home/huseyinadm/secrets/mikro.ovpn
```

ve `.env` icinde:

```env
VPN_CONFIG_PATH=/home/huseyinadm/secrets/mikro.ovpn
```

Bu dosya yoksa veya bossa Docker icindeki `mikro-vpn` konteyniri baglanamaz.

## `.env` Zorunlu Alanlari

```env
MIKRO_API_KEY=CHANGE_ME
MIKRO_FIRMA_KODU=CHANGE_ME
MIKRO_KULLANICI_KODU=CHANGE_ME
MIKRO_SIFRE=CHANGE_ME
MIKRO_API_TARGET_HOST=10.0.0.3
MIKRO_API_TARGET_PORT=8084
MIKRO_API_RELAY_PORT=8084
MIKRO_SQL_TARGET_HOST=10.0.0.3
MIKRO_SQL_TARGET_PORT=1433
MIKRO_SQL_RELAY_PORT=1433
MIKRO_SQL_DATABASE=MikroDB_V16_2023
MIKRO_SQL_USER=CHANGE_ME
MIKRO_SQL_PASSWORD=CHANGE_ME
MIKRO_SQL_COMMAND_TIMEOUT_SECONDS=30
```

## Deploy

```bash
docker compose -f docker-compose.prod.yml build api mikro-api-relay mikro-sql-relay
docker compose -f docker-compose.prod.yml up -d
```

## Kontrol Komutlari

```bash
docker compose -f docker-compose.prod.yml ps
docker logs mikro-vpn
docker logs mikro-api-relay
docker logs mikro-sql-relay
docker exec ecommerce-api-prod curl -k https://mikro-vpn:8084/Api/APIMethods/HealthCheck
```

## Server Check List

- `/dev/net/tun` var.
- [vpn.ovpn](c:\Users\GAMZE\Desktop\eticaret\vpn.ovpn) bos degil ve verilen sertifika profiliyle dolu.
- `.env` icinde `MIKRO_SQL_USER` ve `MIKRO_SQL_PASSWORD` dolduruldu.
- Mikro hedef IP/port, VPN icinden erisilebilir.
- API icinde `MikroSettings__IgnoreSslErrors=true` aktif.

## Notlar

- `host.docker.internal` artik Mikro icin kullanilmiyor.
- Bu tasarim frontend, SQL Server ve diger harici servisleri normal Docker aginda birakir.
- Sertifika CN eslesmesi relay uzerinde korunmadigi icin Mikro HTTP icin SSL validation config uzerinden bypass ediliyor.
- Elinizde gercek `.ovpn` dosyasi yoksa server uzerinde VPN tuneli kuramazsiniz; bu dosyayi VPN saglayicisindan, Mikro agina bagli mevcut OpenVPN istemcisinden veya ag yoneticisinden almalisiniz.

## GitHub Icin Onerilen Dosya Politikasi

GitHub'a gondermeyin:

- `.env`
- `.env.production`
- `vpn.ovpn`
- `src/ECommerce.API/appsettings.Development.json`
- `src/ECommerce.API/appsettings.VpnTest.json`

GitHub'da kalabilir ama sadece temiz/sablon olarak kalmali:

- `.env.production.template`
- `src/ECommerce.API/appsettings.json`
- `src/ECommerce.API/appsettings.Production.json`

Yani production calismasi icin gerekli gizli degerlerin asil kaynagi GitHub degil sunucudaki `.env` ve VPN dosyasi olmali.
