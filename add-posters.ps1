# Backend API'ye posterler ekle

$apiUrl = "http://localhost:5000/api/banners"

# Slider posterler
$sliderPosters = @(
    @{
        title = "İlk Alışveriş İndirimi"
        imageUrl = "/images/ilk-alisveris-indirim-banner.png"
        linkUrl = "/kampanyalar/ilk-alisveris"
        type = "slider"
        displayOrder = 1
        isActive = $true
    },
    @{
        title = "Taze ve Doğal İndirim Reyonu"
        imageUrl = "/images/taze-dogal-indirim-banner.png"
        linkUrl = "/kategori/meyve-ve-sebze"
        type = "slider"
        displayOrder = 2
        isActive = $true
    },
    @{
        title = "Meyve Reyonumuz"
        imageUrl = "/images/meyve-reyonu-banner.png"
        linkUrl = "/kategori/meyve-ve-sebze"
        type = "slider"
        displayOrder = 3
        isActive = $true
    }
)

# Promo posterler
$promoPosters = @(
    @{
        title = "Özel Fiyat Köy Sütü"
        imageUrl = "/images/ozel-fiyat-koy-sutu.png"
        linkUrl = "/urun/koy-sutu"
        type = "promo"
        displayOrder = 1
        isActive = $true
    },
    @{
        title = "Temizlik Malzemeleri"
        imageUrl = "/images/temizlik-malzemeleri.png"
        linkUrl = "/kategori/temizlik"
        type = "promo"
        displayOrder = 2
        isActive = $true
    },
    @{
        title = "Taze Günlük Lezzetli"
        imageUrl = "/images/taze-gunluk-lezzetli.png"
        linkUrl = "/kategori/sut-ve-sut-urunleri"
        type = "promo"
        displayOrder = 3
        isActive = $true
    },
    @{
        title = "Gölköy Gurme Et"
        imageUrl = "/images/golkoy-banner-2.png"
        linkUrl = "/kategori/et-ve-et-urunleri"
        type = "promo"
        displayOrder = 4
        isActive = $true
    }
)

Write-Host "=== Slider Posterler Ekleniyor ===" -ForegroundColor Cyan
foreach ($poster in $sliderPosters) {
    $body = $poster | ConvertTo-Json
    Write-Host "Ekleniyor: $($poster.title)" -ForegroundColor Yellow
    try {
        $response = Invoke-WebRequest -Uri $apiUrl -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop
        Write-Host "✓ Başarılı" -ForegroundColor Green
    } catch {
        Write-Host "✗ Hata: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n=== Promo Posterler Ekleniyor ===" -ForegroundColor Cyan
foreach ($poster in $promoPosters) {
    $body = $poster | ConvertTo-Json
    Write-Host "Ekleniyor: $($poster.title)" -ForegroundColor Yellow
    try {
        $response = Invoke-WebRequest -Uri $apiUrl -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop
        Write-Host "✓ Başarılı" -ForegroundColor Green
    } catch {
        Write-Host "✗ Hata: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n=== Kontrol: Tüm Posterler ===" -ForegroundColor Cyan
try {
    $allPosters = Invoke-WebRequest -Uri $apiUrl -Method Get -ContentType "application/json" -ErrorAction Stop
    $data = $allPosters.Content | ConvertFrom-Json
    Write-Host "Toplam $($data.Count) poster veritabanında kayıtlı." -ForegroundColor Green
    $data | ForEach-Object {
        Write-Host "  - $($_.title) ($($_.type))"
    }
} catch {
    Write-Host "✗ Kontrol hatası: $($_.Exception.Message)" -ForegroundColor Red
}
