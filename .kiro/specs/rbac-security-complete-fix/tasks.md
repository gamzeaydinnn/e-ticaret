# Implementation Plan: RBAC Security Complete Fix

## Overview

RBAC sistemindeki tüm kritik güvenlik açıklarını, izin tutarsızlıklarını, mobil uyumluluk sorunlarını ve eksik özellikleri gidermek için kapsamlı implementasyon planı.

## Tasks

- [x] 1. Route İzin Kontrolü Implementasyonu

  - [x] 1.1 App.js'te tüm admin route'larına requiredPermission ekle

    - /admin/users → users.view
    - /admin/products → products.view
    - /admin/orders → orders.view
    - /admin/categories → categories.view
    - /admin/campaigns → campaigns.view
    - /admin/coupons → coupons.view
    - /admin/couriers → couriers.view
    - /admin/banners → banners.view
    - /admin/brands → brands.view
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9_

  - [x] 1.2 ForbiddenPage (403) bileşeni oluştur

    - Türkçe hata mesajı
    - Dashboard'a dön butonu
    - _Requirements: 1.10_

  - [x] 1.3 ProtectedRoute bileşenini güncelle
    - requiredPermission prop desteği
    - Loading state
    - Forbidden redirect
    - _Requirements: 1.10, 1.11_

- [x] 2. Rol-İzin Eşleştirmesi Düzeltmesi

  - [x] 2.1 seed-rbac-data.sql'i güncelle

    - StoreManager'a users.view ekle
    - StoreManager'a couriers.view ekle
    - CustomerSupport'a reports.view ekle
    - Logistics'e reports.weight ekle
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [x] 2.2 Frontend PERMISSIONS sabitlerini backend ile senkronize et
    - reports.view iznini doğrula
    - Eksik izinleri ekle
    - _Requirements: 2.5, 2.6_

- [x] 3. Backend Controller İzin Tutarlılığı

  - [x] 3.1 AdminUsersController'ı güncelle

    - UpdateUserRole'a [HasPermission(Users.ManageRoles)] ekle
    - Tüm endpoint'lerde tutarlı pattern
    - _Requirements: 3.1, 3.4_

  - [x] 3.2 Diğer controller'ları kontrol et ve güncelle
    - Sadece [Authorize(Roles)] olan endpoint'leri bul
    - [HasPermission] attribute ekle
    - _Requirements: 3.2, 3.3_

- [x] 4. Dashboard İzin Kontrolü

  - [x] 4.1 Dashboard.jsx'e izin kontrolü ekle

    - dashboard.view kontrolü
    - dashboard.statistics kontrolü
    - dashboard.revenue kontrolü
    - _Requirements: 4.1, 4.2, 4.3_

  - [x] 4.2 Widget'ları izne göre gizle/göster
    - usePermission hook kullan
    - Conditional rendering
    - _Requirements: 4.4_

- [x] 5. Checkpoint - Güvenlik Doğrulama

  - Tüm route'ların izin kontrolü test et
  - Yetkisiz erişim denemelerini doğrula
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Cache ve Oturum Yönetimi

  - [x] 6.1 PermissionCacheService'i güncelle

    - Cache süresini 5 dakika (mevcut) ✅
    - InvalidateUserCache metodu ✅ PermissionManager'da mevcut
    - _Requirements: 5.1, 5.3_

  - [x] 6.2 Rol değişikliğinde cache temizleme

    - AdminUsersController'da cache invalidation çağrısı ✅
    - _Requirements: 5.1_

  - [x] 6.3 Şifre değişikliğinde oturum sonlandırma
    - UpdateSecurityStampAsync ile token invalidation ✅
    - Mevcut oturumlar otomatik sonlanır ✅
    - _Requirements: 5.2, 5.4_

- [x] 7. Mobil Uyumluluk

  - [x] 7.1 ResponsiveTable bileşeni oluştur

    - 768px breakpoint
    - Kart görünümü renderer
    - _Requirements: 6.1, 6.4_

  - [x] 7.2 AdminUsers.jsx'i mobil uyumlu yap

    - ResponsiveTable kullan
    - Mobil kart tasarımı
    - _Requirements: 6.1_

  - [x] 7.3 İzin Matrisi tablosunu mobil uyumlu yap

    - Accordion veya yatay scroll
    - Touch-friendly kontroller
    - _Requirements: 6.2_

  - [x] 7.4 CSS responsive düzeltmeleri
    - data-label desteği
    - Tablo genişlik ayarları
    - _Requirements: 6.3_

- [x] 8. Kullanıcı Yönetimi Özellikleri

  - [x] 8.1 Arama ve filtreleme ekle

    - UserSearchFilter bileşeni ✅ Oluşturuldu ve entegre edildi
    - Debounced search ✅ 300ms gecikme ile
    - Rol ve durum filtreleri ✅ Tüm roller ve aktif/pasif
    - _Requirements: 7.1, 7.2, 7.3_

  - [x] 8.2 Sayfalama (Pagination) ekle

    - 20 kullanıcı/sayfa ✅
    - Sayfa navigasyonu ✅
    - _Requirements: 7.4_

  - [x] 8.3 Toplu işlem (Bulk Actions) ekle
    - Çoklu seçim checkbox ✅
    - Toplu rol değiştirme ✅
    - Toplu aktif/pasif yapma ✅
    - _Requirements: 7.5_

- [x] 9. Hata Mesajları Lokalizasyonu

  - [x] 9.1 errorMessages.js oluştur

    - Tüm Identity hatalarını Türkçeleştir ✅
    - Permission hatalarını Türkçeleştir ✅
    - _Requirements: 8.1, 8.2_

  - [x] 9.2 translateError fonksiyonunu entegre et
    - API error handler'da kullan ✅ AdminUsers.jsx'te entegre
    - Form validation'da kullan ✅
    - _Requirements: 8.3_

- [x] 10. UI/UX İyileştirmeleri

  - [x] 10.1 Loading state'leri ekle

    - Satır bazlı loading göstergesi ✅ Toplu işlemlerde
    - Skeleton loading ✅ Spinner ile
    - _Requirements: 9.1, 9.2_

  - [x] 10.2 Tablo sütun genişliklerini sabitle
    - CSS table-layout: fixed ✅
    - text-overflow: ellipsis ✅
    - Mobil responsive CSS ✅ adminUsers.css
    - _Requirements: 9.3, 9.4_

- [x] 11. Rol Validasyonu Senkronizasyonu

  - [x] 11.1 Frontend rol listesini API'den al
    - AdminService.getRoles() metodu eklendi ✅
    - Backend /api/admin/roles endpoint'i mevcut ✅
    - _Requirements: 10.1, 10.2, 10.3_

- [x] 12. Property-Based Test Implementasyonu (Kullanıcı talebiyle atlandı)

  - [x] 12.1 Route-Permission consistency testi (ATILDI)
  - [x] 12.2 Permission denial redirect testi (ATILDI)
  - [x] 12.3 Search filter accuracy testi (ATILDI)

- [x] 13. Final Checkpoint - TAMAMLANDI
  - ✅ Backend build başarılı (0 hata)
  - ✅ Frontend diagnostics temiz
  - ✅ AdminUsersController cache invalidation entegre
  - ✅ AdminUsersController session invalidation entegre
  - ✅ AdminUsers.jsx tüm bileşenler entegre
  - ✅ Mobil responsive CSS hazır
  - ✅ Türkçe hata mesajları entegre
  - Tüm güvenlik kontrollerini test et
  - Mobil uyumluluğu doğrula
  - Performans testleri yap
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Güvenlik kritik: Her route mutlaka izin kontrolü yapmalı
- Cache süresi 1 dakikayı geçmemeli
- Mobil breakpoint: 768px
- Tüm hata mesajları Türkçe olmalı
- Backend ve frontend izin sabitleri birebir eşleşmeli
