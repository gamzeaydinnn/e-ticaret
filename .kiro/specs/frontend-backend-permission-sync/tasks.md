# Implementation Plan: Frontend-Backend Permission Sync

## Overview

Frontend PERMISSIONS sabitleri ile backend Permissions.cs arasındaki uyumsuzlukları gidererek tutarlı bir izin sistemi oluşturma. Eksik izinler eklenir ve adlandırma standardı uygulanır.

## Tasks

- [x] 1. Backend Permissions.cs Güncellemesi

  - [x] 1.1 Reports modülüne genel view izni ekle

    - `public const string View = "reports.view";` ekle
    - All dizisine View'ı ekle
    - _Requirements: 1.1_

  - [x] 1.2 Logs modülüne genel view izni ekle

    - `public const string View = "logs.view";` ekle
    - All dizisine View'ı ekle
    - _Requirements: 2.1_

  - [x] 1.3 Settings modülüne system izni ekle
    - `public const string System = "settings.system";` ekle
    - All dizisine System'ı ekle
    - _Requirements: 3.1_

- [x] 2. Veritabanı Seed Güncellemesi

  - [x] 2.1 seed-rbac-data.sql'e yeni izinleri ekle

    - `reports.view` iznini Permissions tablosuna ekle
    - `logs.view` iznini Permissions tablosuna ekle
    - `settings.system` iznini Permissions tablosuna ekle
    - _Requirements: 1.1, 2.1, 3.1_

  - [x] 2.2 Yeni izinleri uygun rollere ata
    - SuperAdmin'e tüm yeni izinleri ata
    - StoreManager'a reports.view ata
    - CustomerSupport'a reports.view ata
    - Logistics'e reports.view ata
    - _Requirements: 1.3, 2.3, 3.3_

- [x] 3. Frontend permissionService.js Güncellemesi

  - [x] 3.1 PERMISSIONS objesini backend ile senkronize et

    - REPORTS_VIEW değerinin "reports.view" olduğunu doğrula
    - LOGS_VIEW değerinin "logs.view" olduğunu doğrula
    - SETTINGS_SYSTEM değerinin "settings.system" olduğunu doğrula
    - _Requirements: 1.2, 2.2, 3.2_

  - [x] 3.2 Shipping izinlerini ekle/doğrula

    - SHIPPING_VIEW, SHIPPING_UPDATE_STATUS, SHIPPING_TRACK, SHIPPING_WEIGHT_APPROVAL ekle
    - Backend Shipping sınıfı ile eşleştir
    - _Requirements: 5.1, 5.2, 5.3_

  - [x] 3.3 PERMISSION_MODULES objesini güncelle
    - Reports modülüne REPORTS_VIEW ekle
    - Logs modülüne LOGS_VIEW ekle
    - Settings modülüne SETTINGS_SYSTEM ekle
    - _Requirements: 1.2, 2.2, 3.2_

- [x] 4. Checkpoint - Senkronizasyon Doğrulama

  - Frontend PERMISSIONS ile backend Permissions.cs karşılaştır
  - Tüm değerlerin eşleştiğini doğrula
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. AdminLayout ve Route Güncellemesi

  - [x] 5.1 AdminLayout.jsx'te yeni izinleri kullan

    - Raporlar menüsünde PERMISSIONS.REPORTS_VIEW kullan
    - Loglar menüsünde PERMISSIONS.LOGS_VIEW kullan
    - ERP/Mikro menüsünde PERMISSIONS.SETTINGS_SYSTEM kullan
    - _Requirements: 3.4_

  - [x] 5.2 App.js'te route izinlerini güncelle
    - /admin/reports route'una PERMISSIONS.REPORTS_VIEW ekle
    - /admin/logs/\* route'larına PERMISSIONS.LOGS_VIEW ekle
    - /admin/micro route'una PERMISSIONS.SETTINGS_SYSTEM ekle
    - _Requirements: 3.4_

- [x] 6. Property-Based Test Implementasyonu

  - [x] 6.1 Frontend-backend permission value match property testi

    - **Property 1: Frontend-Backend Permission Value Match**
    - **Validates: Requirements 1.2, 2.2, 3.2**

  - [x] 6.2 Unknown permission fail-safe property testi

    - **Property 2: Unknown Permission Fail-Safe**
    - **Validates: Requirements 4.3**

  - [x] 6.3 Permission naming convention compliance property testi
    - **Property 3: Permission Naming Convention Compliance**
    - **Validates: Requirements 6.1, 6.2, 6.3**

- [x] 7. İzin Eşleştirme Dokümantasyonu

  - [x] 7.1 İzin eşleştirme tablosu oluştur
    - Frontend constant → Backend property → Database value eşleştirmesi
    - Tüm izinleri listele
    - _Requirements: 4.4, 6.5_

- [x] 8. Final Checkpoint
  - Tüm testlerin geçtiğinden emin ol
  - Her rol ile izin kontrollerini test et
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Frontend ve backend izin değerleri birebir eşleşmeli (case-sensitive)
- Yeni izin eklerken her iki tarafı da güncellemeyi unutma
- İzin adlandırma standardına uy: lowercase.action (database), SCREAMING_SNAKE_CASE (frontend), PascalCase (backend)
