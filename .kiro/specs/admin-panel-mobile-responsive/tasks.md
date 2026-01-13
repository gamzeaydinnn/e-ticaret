# Implementation Plan: Admin Panel Mobile Responsive

## Overview

Bu plan, admin panelindeki tüm sekmelerin mobil uyumlu hale getirilmesi için gerekli implementasyon adımlarını içerir. Mevcut mobil-uyumlu sayfalar (Dashboard, AdminUsers) referans alınarak, diğer sayfalar aynı standartlara getirilecektir.

## Tasks

- [x] 1. Ortak mobil stil dosyası oluşturma

  - [x] 1.1 adminMobile.css dosyası oluştur
  - [x] 1.2 App.js'e adminMobile.css import et

- [x] 2. AdminMicro.js mobil uyumluluk

  - [x] 2.1 Header ve butonları responsive yap
  - [x] 2.2 Ürün ve stok tablolarını responsive yap

- [x] 3. CouponManagement.jsx mobil uyumluluk

  - [x] 3.1 Header ve arama bölümünü responsive yap
  - [x] 3.2 Kupon tablosunu responsive yap
  - [x] 3.3 Modal'ı mobilde full-width yap

- [x] 4. BannerManagement.jsx tam yeniden tasarım

  - [x] 4.1 Modern kart tabanlı layout oluştur
  - [x] 4.2 Form elemanlarını responsive yap
  - [x] 4.3 Banner listesini responsive grid yap

- [x] 5. AdminRoles.jsx mobil uyumluluk

  - [x] 5.1 Bootstrap Icons'ı Font Awesome'a çevir
  - [x] 5.2 Rol kartlarını responsive yap
  - [x] 5.3 Modal'ı mobilde full-width yap
  - [x] 5.4 İzin matrisi responsive yap

- [x] 6. Checkpoint - İlk 5 sayfa kontrolü

- [x] 7. AdminWeightReports ve WeightReportsPanel mobil uyumluluk

  - [x] 7.1 WeightReportsPanel responsive stiller ekle
  - [x] 7.2 İstatistik kartlarını 2x2 grid yap

- [x] 8. AdminCampaigns.jsx iyileştirmeleri

  - [x] 8.1 Form layout'u optimize et
  - [x] 8.2 Kampanya kartlarını responsive yap

- [x] 9. Log sayfaları mobil uyumluluk

  - [x] 9.1 AuditLogsPage.tsx responsive yap
  - [x] 9.2 SystemLogsPage.tsx responsive yap
  - [x] 9.3 InventoryLogsPage.tsx responsive yap

- [x] 10. Final checkpoint - Tüm sayfaları test et

## Notes

- Her sayfa için mevcut Dashboard.jsx ve AdminUsers.jsx referans alınacak
- Bootstrap 5 responsive utilities kullanılacak (col-_, d-none d-_-block, flex-\*)
- Minimum font size: 0.65rem
- Minimum touch target: 44px
- Breakpoints: 576px (xs), 768px (sm), 992px (md)
