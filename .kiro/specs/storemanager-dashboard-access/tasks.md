# Implementation Plan: StoreManager Dashboard Access

## Overview

StoreManager rolünün admin paneline tam erişim sağlaması için veritabanı seed güncellemesi, AdminLayout menü filtreleme iyileştirmesi ve route koruması implementasyonu.

## Tasks

- [x] 1. Veritabanı Seed Güncellemesi

  - [x] 1.1 seed-rbac-data.sql dosyasında StoreManager izinlerini güncelle

    - `users.view` iznini StoreManager'a ekle
    - `couriers.view` iznini StoreManager'a ekle
    - `reports.view` iznini StoreManager'a ekle
    - _Requirements: 1.1, 2.1, 4.1, 4.2_

  - [x] 1.2 IdentitySeeder.cs dosyasında StoreManager izinlerini güncelle
    - StoreManagerPermissions dizisine yeni izinleri ekle
    - SQL script ile senkronize olduğundan emin ol
    - NOT: StoreManager kullanıcı yönetimine erişmemeli (tasarım gereği)
    - _Requirements: 4.4_

- [x] 2. AdminLayout Menü Filtreleme İyileştirmesi

  - [x] 2.1 AdminLayout.jsx'te adminOnly flag'lerini kaldır

    - Kullanıcılar menüsünden adminOnly: true kaldırıldı
    - Kuryeler menüsünden adminOnly: true kaldırıldı
    - Kampanyalar menüsünden adminOnly: true kaldırıldı
    - Loglar menüsünden adminOnly: true kaldırıldı
    - ERP/Mikro menüsünden adminOnly: true kaldırıldı
    - İzin kontrolü yeterli olacak
    - _Requirements: 3.1, 3.2_

  - [x] 2.2 Menü görünürlük mantığını sadeleştir
    - checkPermission fonksiyonu tek kaynak olarak kullanılıyor
    - isAdminLike ve ADMIN_ROLES kontrolü kaldırıldı
    - _Requirements: 3.3_

- [x] 3. Route Koruması Güncellemesi

  - [x] 3.1 App.js'te admin route'larına requiredPermission ekle

    - /admin/users route'una PERMISSIONS.USERS_VIEW mevcut
    - /admin/couriers route'una PERMISSIONS.COURIERS_VIEW mevcut
    - /admin/reports route'una PERMISSIONS.REPORTS_VIEW mevcut
    - _Requirements: 3.4_

  - [x] 3.2 AdminGuard.js'te izin kontrolü iyileştirmesi
    - requiredPermission prop'u ile izin kontrolü yapılıyor
    - İzin yoksa dashboard'a yönlendirme mevcut
    - Toast mesajı gösteriliyor
    - _Requirements: 3.4_

- [x] 4. Checkpoint - Veritabanı ve Frontend Senkronizasyonu

  - Seed script'i çalıştır ve izinleri doğrula
  - StoreManager ile giriş yapıp menü görünürlüğünü test et
  - Tüm testler geçiyor

- [ ]\* 5. Property-Based Test Implementasyonu

  - [ ]\* 5.1 StoreManager read-only user access property testi

    - **Property 1: StoreManager Read-Only User Access**
    - **Validates: Requirements 1.3, 1.4**

  - [ ]\* 5.2 StoreManager read-only courier access property testi

    - **Property 2: StoreManager Read-Only Courier Access**
    - **Validates: Requirements 2.3, 2.4**

  - [ ]\* 5.3 Menu visibility matches permissions property testi
    - **Property 3: Menu Visibility Matches Permissions**
    - **Validates: Requirements 3.1, 3.2, 3.3**

- [x] 6. Final Checkpoint
  - Tüm ana görevler tamamlandı
  - StoreManager izin bazlı menü filtreleme çalışıyor
  - Property testleri opsiyonel

## Notes

- Seed script değişiklikleri production'a deploy edilmeden önce test ortamında doğrulanmalı
- AdminLayout değişiklikleri tüm admin rollerini etkileyebilir, dikkatli test edilmeli
- Property testleri opsiyonel olarak işaretlendi ([ ]\*)
