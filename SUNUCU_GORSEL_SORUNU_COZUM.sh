#!/bin/bash
# ======================================================================
# SUNUCU BANNER GÖRSEL SORUNU - KÖK NEDEN ANALİZİ VE ÇÖZÜM
# ======================================================================
# Hazırlayan: Senior Developer
# Tarih: 2026-01-12
# Proje: E-Ticaret Platform
# ======================================================================

echo "🔍 SORUN ANALİZİ"
echo "================"
echo ""
echo "1️⃣ DATABASE'DE DOSYA ADLARI DOĞRU:"
echo "   ✅ banner_taze-dogal-indirim-banner_20260112111459_a4b5151d.png"
echo "   ✅ banner_gemini_generated_image_r09nenr09nenr09n_20260112103231_01dc07c7.png"
echo ""
echo "2️⃣ NGINX PROXY ÇALIŞIYOR:"
echo "   ✅ curl test 200 OK döndü"
echo ""
echo "3️⃣ CONTAINER'DA DOSYALAR VAR:"
echo "   ✅ 5 dosya /app/uploads/banners/ içinde mevcut"
echo ""
echo "4️⃣ AMA BROWSER'DA 404 HATALARI!"
echo "   ❌ İstenen dosyalar container'da YOK"
echo ""
echo "📋 KÖK NEDEN:"
echo "============="
echo "Dockerfile: COPY --from=build /src/ECommerce.API/uploads ./uploads"
echo "            ⬇️"
echo "            LOCAL uploads/ klasörü BOŞ!"
echo "            ⬇️"
echo "docker-compose.prod.yml: volumes: ./uploads:/app/uploads"
echo "            ⬇️"
echo "            SUNUCU'daki ~/eticaret/uploads/ klasörü BOŞ!"
echo "            ⬇️"
echo "            Volume mount, container içindeki uploads'ı override ediyor"
echo "            ⬇️"
echo "            Sonuç: Container'daki görseller kayboldu!"
echo ""
echo "🔧 ÇÖZÜM ADIMLARI"
echo "================="
echo ""

# ======================================================================
# ADIM 1: Mevcut container'daki dosyaları sunucuya kopyala
# ======================================================================
echo "📦 ADIM 1: Container'daki mevcut görselleri sunucuya kopyala"
echo "--------------------------------------------------------------"
echo ""
echo "# Container'dan host'a dosya kopyala"
echo "docker cp ecommerce-api-prod:/app/uploads/banners/. ~/eticaret/uploads/banners/"
echo ""

# ======================================================================
# ADIM 2: Dockerfile'ı güncelle (COPY uploads satırını kaldır)
# ======================================================================
echo "📝 ADIM 2: Dockerfile'ı güncelle (zaten yapıldı)"
echo "--------------------------------------------------------------"
echo "Dockerfile'dan COPY uploads satırı kaldırıldı."
echo "Artık uploads klasörü SADECE volume mount ile yönetilecek."
echo ""

# ======================================================================
# ADIM 3: Değişiklikleri commit ve push
# ======================================================================
echo "💾 ADIM 3: Git commit ve push"
echo "--------------------------------------------------------------"
echo "cd ~/eticaret"
echo "git add src/ECommerce.API/Dockerfile"
echo "git commit -m 'fix: Remove uploads COPY from Dockerfile - use volume mount only'"
echo "git push origin main"
echo ""

# ======================================================================
# ADIM 4: Sunucuya deploy
# ======================================================================
echo "🚀 ADIM 4: Sunucuda rebuild ve deploy"
echo "--------------------------------------------------------------"
echo "# Sunucuya SSH"
echo "ssh huseyinadm@31.186.24.78"
echo ""
echo "# Git pull"
echo "cd ~/eticaret"
echo "git pull origin main"
echo ""
echo "# Uploads klasörü oluştur (yoksa)"
echo "mkdir -p ~/eticaret/uploads/banners"
echo "mkdir -p ~/eticaret/uploads/products"
echo "mkdir -p ~/eticaret/uploads/categories"
echo ""
echo "# API container'ı yeniden build et"
echo "docker-compose -f docker-compose.prod.yml build api"
echo ""
echo "# Container'ı yeniden başlat"
echo "docker-compose -f docker-compose.prod.yml up -d api"
echo ""
echo "# Sağlık kontrolü"
echo "docker ps | grep ecommerce-api-prod"
echo "docker logs ecommerce-api-prod --tail 20"
echo ""

# ======================================================================
# ADIM 5: Admin panel'den görselleri yeniden yükle
# ======================================================================
echo "🖼️ ADIM 5: Admin panel'den banner görsellerini yükle"
echo "--------------------------------------------------------------"
echo "1. Tarayıcıda: https://golkoygurme.com.tr/admin/posters"
echo "2. Her banner için 'Düzenle' butonuna tıkla"
echo "3. Doğru görsel dosyasını seç ve yükle"
echo "4. Kaydet"
echo ""
echo "Bu işlem görselleri sunucudaki ~/eticaret/uploads/banners/ klasörüne kaydedecek."
echo "Volume mount sayesinde container restart sonrası görseller KAYBOLMAYACAK."
echo ""

# ======================================================================
# ADIM 6: Test ve doğrulama
# ======================================================================
echo "✅ ADIM 6: Test ve doğrulama"
echo "--------------------------------------------------------------"
echo ""
echo "# 1. Sunucuda dosya kontrolü"
echo "ls -la ~/eticaret/uploads/banners/"
echo ""
echo "# 2. Container'da dosya kontrolü"
echo "docker exec ecommerce-api-prod ls -la /app/uploads/banners/"
echo ""
echo "# 3. Nginx üzerinden test"
echo "curl -I http://127.0.0.1/uploads/banners/[DOSYA_ADI].png"
echo ""
echo "# 4. Browser'da test"
echo "https://golkoygurme.com.tr/"
echo ""
echo "# 5. Hard refresh (cache temizle)"
echo "Ctrl+Shift+R"
echo ""

# ======================================================================
# SONUÇ
# ======================================================================
echo "📊 BEKLENTİLER"
echo "=============="
echo ""
echo "✅ Görseller ~/eticaret/uploads/banners/ içinde kalıcı olarak saklanacak"
echo "✅ Container restart sonrası görseller KAYBOLMAYACAK"
echo "✅ Admin panel upload → sunucuya kaydedilecek → container ile paylaşılacak"
echo "✅ Frontend banner görsellerini hatasız görebilecek"
echo ""
echo "⚠️ ÖNEMLİ NOT:"
echo "=============="
echo "Bu çözüm ile:"
echo "- Dockerfile artık uploads kopyalamıyor (gereksiz)"
echo "- Volume mount uploads'ı yönetiyor (kalıcılık sağlanıyor)"
echo "- Görseller host'ta saklanıyor (backup kolaylığı)"
echo ""
echo "🎯 ÖZET: Volume mount stratejisi ile uploads yönetimi doğru şekilde yapılandırıldı."
echo ""
