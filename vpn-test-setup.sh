#!/bin/bash
# VPN Mikro API Test - Installation & Verification Script
# Bu script, VPN test ortamı için gerekli dosyaların varlığını ve konfigürasyonunu kontrol eder.

echo "═════════════════════════════════════════════════════════════════"
echo "🔍 VPN Mikro API Test - Kurulum Kontrolü"
echo "═════════════════════════════════════════════════════════════════"
echo ""

# Renk kodları
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Dosya kontrol fonksiyonu
check_file() {
    if [ -f "$1" ]; then
        echo -e "${GREEN}✅${NC} $1"
        return 0
    else
        echo -e "${RED}❌${NC} $1 (BULUNAMADI)"
        return 1
    fi
}

# Dizin kontrol fonksiyonu
check_dir() {
    if [ -d "$1" ]; then
        echo -e "${GREEN}✅${NC} $1"
        return 0
    else
        echo -e "${RED}❌${NC} $1 (BULUNAMADI)"
        return 1
    fi
}

# ═════════════════════════════════════════════════════════════════════
echo ""
echo "📋 YAPILAN İŞLEMLER:"
echo "─────────────────────────────────────────────────────────────────"

# 1. Yapılandırma Dosyaları
echo ""
echo "🔧 1. Yapılandırma Dosyaları:"
check_file "src/ECommerce.API/appsettings.VpnTest.json"
check_file "Postman-VPN-MikroAPI-Test.json"

# 2. Dokümantasyon
echo ""
echo "📖 2. Dokümantasyon Dosyaları:"
check_file "VPN_TEST_SETUP.md"
check_file "VPN_MIKRO_API_SETUP_SUMMARY.md"

# 3. Kod Dosyaları
echo ""
echo "💻 3. C# Kod Dosyaları:"
check_file "src/ECommerce.Infrastructure/Services/MicroServices/MikroApiVpnTestService.cs"
check_file "src/ECommerce.API/Controllers/VpnTest/MikroApiTestController.cs"

# 4. Program.cs güncellemesi
echo ""
echo "⚙️ 4. Program.cs Güncellemesi:"
if grep -q "MikroApiVpnTestService" "src/ECommerce.API/Program.cs"; then
    echo -e "${GREEN}✅${NC} Program.cs - MikroApiVpnTestService kaydı"
else
    echo -e "${RED}❌${NC} Program.cs - MikroApiVpnTestService kaydı (GÜNCELLENMESİ GEREKEBILIR)"
fi

# ═════════════════════════════════════════════════════════════════════
echo ""
echo "═════════════════════════════════════════════════════════════════"
echo "📝 ÖNEMLİ NOTLAR:"
echo "─────────────────────────────────────────────────────────────────"

echo ""
echo "1️⃣ ORTAM DEĞİŞKENİNİ AYARLA:"
echo "   Bash/Linux/Mac:"
echo "   ${YELLOW}export ASPNETCORE_ENVIRONMENT=VpnTest${NC}"
echo ""
echo "   PowerShell (Windows):"
echo "   ${YELLOW}\$env:ASPNETCORE_ENVIRONMENT = 'VpnTest'${NC}"
echo ""

echo "2️⃣ UYGULAMAYI BAŞLAT:"
echo "   ${YELLOW}dotnet run --project src/ECommerce.API/ECommerce.API.csproj${NC}"
echo ""

echo "3️⃣ TEST ET:"
echo "   API Endpoint: http://10.0.0.3:8084"
echo "   Test Controller: /api/mikroapitest/"
echo "   Postman Collection: Postman-VPN-MikroAPI-Test.json"
echo ""

echo "4️⃣ KİMLİK BİLGİLERİ:"
echo "   Firma: Ze-Me 2023"
echo "   Kullanıcı: Golkoy2"
echo "   Çalışma Yılı: 2026"
echo ""

# ═════════════════════════════════════════════════════════════════════
echo ""
echo "🔗 BAŞLANGIC ENDPOINT'LERİ:"
echo "─────────────────────────────────────────────────────────────────"
echo ""
echo "GET    /api/mikroapitest/config          → Aktif konfigürasyonu görüntüle"
echo "POST   /api/mikroapitest/login            → API'ye giriş yap"
echo "GET    /api/mikroapitest/product/{key}   → Ürün bilgisi sorgula"
echo "GET    /api/mikroapitest/customer/{key}  → Müşteri bilgisi sorgula"
echo "GET    /api/mikroapitest/system-info     → Sistem bilgisi al"
echo "GET    /api/mikroapitest/health-check    → Bağlantı kontrolü"
echo ""

# ═════════════════════════════════════════════════════════════════════
echo ""
echo "🚀 HAZIRLIKLARıN TESPİT EDİLMESİ:"
echo "─────────────────────────────────────────────────────────────────"

files_ok=true

if ! check_file "src/ECommerce.API/appsettings.VpnTest.json" 2>/dev/null; then files_ok=false; fi
if ! check_file "Postman-VPN-MikroAPI-Test.json" 2>/dev/null; then files_ok=false; fi
if ! check_file "VPN_TEST_SETUP.md" 2>/dev/null; then files_ok=false; fi
if ! check_file "VPN_MIKRO_API_SETUP_SUMMARY.md" 2>/dev/null; then files_ok=false; fi
if ! check_file "src/ECommerce.Infrastructure/Services/MicroServices/MikroApiVpnTestService.cs" 2>/dev/null; then files_ok=false; fi
if ! check_file "src/ECommerce.API/Controllers/VpnTest/MikroApiTestController.cs" 2>/dev/null; then files_ok=false; fi

echo ""
if [ "$files_ok" = true ]; then
    echo -e "${GREEN}✅ TÜM DOSYALAR BAŞARIYLA OLUŞTURULDU!${NC}"
    echo ""
    echo "Sonraki adım: ASPNETCORE_ENVIRONMENT = VpnTest ile çalıştır"
else
    echo -e "${YELLOW}⚠️ BAZI DOSYALAR EKSİK OLABİLİR${NC}"
    echo ""
    echo "Lütfen manuel olarak kontrol et:"
    echo "  - appsettings.VpnTest.json"
    echo "  - MikroApiVpnTestService.cs"
    echo "  - MikroApiTestController.cs"
fi

echo ""
echo "═════════════════════════════════════════════════════════════════"
echo "✨ Kurulum tamamlandı! VPN test ortamı hazır."
echo "═════════════════════════════════════════════════════════════════"
