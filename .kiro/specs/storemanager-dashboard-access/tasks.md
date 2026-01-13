# Implementation Plan: StoreManager Dashboard Access

## Overview

StoreManager rolünün admin paneline tam erişim sağlaması için veritabanı seed güncellemesi, AdminLayout menü filtreleme iyileştirmesi ve route koruması implementasyonu.

## Tasks

- [ ] 1. Veritabanı Seed Güncellemesi

  - [ ] 1.1 seed-rbac-data.sql dosyasında StoreManager izinlerini güncelle

    - `users.view` iznini StoreManager'a ekle
    - `couriers.view` iznini StoreManager'a ekle
    - `reports.view` iznini StoreManager'a ekle
    - _Requirements: 1.1, 2.1, 4.1, 4.2_

  - [ ] 1.2 IdentitySeeder.cs dosyasında StoreManager izinlerini güncelle
    - StoreManagerPermissions dizisine yeni izinleri ekle
    - SQL script ile senkronize olduğundan emin ol
    - _Requirements: 4.4_

- [ ] 2. AdminLayout Menü Filtreleme İyileştirmesi

  - [ ] 2.1 AdminLayout.jsx'te adminOnly flag'lerini kaldır

    - Kullanıcılar menüsünden adminOnly: true kaldır
    - Kuryeler menüsünden adminOnly: true kaldır
    - İzin kontrolü yeterli olacak
    - _Requirements: 3.1, 3.2_

  - [ ] 2.2 Menü görünürlük mantığını sadeleştir
    - checkPermission fonksiyonunu tek kaynak olarak kullan
    - isAdminLike kontrolünü gereksiz yerlerde kaldır
    - _Requirements: 3.3_

- [ ] 3. Route Koruması Güncellemesi

  - [ ] 3.1 App.js'te admin route'larına requiredPermission ekle

    - /admin/users route'una PERMISSIONS.USERS_VIEW ekle
    - /admin/couriers route'una PERMISSIONS.COURIERS_VIEW ekle
    - /admin/reports route'una PERMISSIONS.REPORTS_VIEW ekle
    - _Requirements: 3.4_

  - [ ] 3.2 AdminGuard.js'te izin kontrolü iyileştirmesi
    - requiredPermission prop'u ile izin kontrolü yap
    - İzin yoksa dashboard'a yönlendir
    - Toast mesajı göster
    - _Requirements: 3.4_

- [ ] 4. Checkpoint - Veritabanı ve Frontend Senkronizasyonu

  - Seed script'i çalıştır ve izinleri doğrula
  - StoreManager ile giriş yapıp menü görünürlüğünü test et
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Property-Based Test Implementasyonu

  - [ ] 5.1 StoreManager read-only user access property testi

    - **Property 1: StoreManager Read-Only User Access**
    - **Validates: Requirements 1.3, 1.4**

  - [ ] 5.2 StoreManager read-only courier access property testi

    - **Property 2: StoreManager Read-Only Courier Access**
    - **Validates: Requirements 2.3, 2.4**

  - [ ] 5.3 Menu visibility matches permissions property testi
    - **Property 3: Menu Visibility Matches Permissions**
    - **Validates: Requirements 3.1, 3.2, 3.3**

- [ ] 6. Final Checkpoint
  - Tüm testlerin geçtiğinden emin ol
  - StoreManager ile end-to-end test yap
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Seed script değişiklikleri production'a deploy edilmeden önce test ortamında doğrulanmalı
- AdminLayout değişiklikleri tüm admin rollerini etkileyebilir, dikkatli test edilmeli
