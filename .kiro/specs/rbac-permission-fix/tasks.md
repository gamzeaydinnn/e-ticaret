# Implementation Plan: RBAC Permission Fix

## Overview

Bu plan, RBAC izin sistemindeki kritik sorunları düzeltmek için gerekli kod değişikliklerini içerir. Öncelik sırası: Frontend route koruması → Seed data → Backend tutarlılığı.

## Tasks

- [x] 1. App.js Route İzin Kontrollerini Ekle

  - [x] 1.1 Dashboard route'una requiredPermission ekle
    - `/admin/dashboard` için `requiredPermission="dashboard.view"` ekle
    - _Requirements: 1.11_
  - [x] 1.2 Users route'una requiredPermission ekle
    - `/admin/users` için `requiredPermission="users.view"` ekle
    - _Requirements: 1.1_
  - [x] 1.3 Products route'una requiredPermission ekle
    - `/admin/products` için `requiredPermission="products.view"` ekle
    - _Requirements: 1.2_
  - [x] 1.4 Orders route'una requiredPermission ekle
    - `/admin/orders` için `requiredPermission="orders.view"` ekle
    - _Requirements: 1.3_
  - [x] 1.5 Categories route'una requiredPermission ekle
    - `/admin/categories` için `requiredPermission="categories.view"` ekle
    - _Requirements: 1.4_
  - [x] 1.6 Couriers route'una requiredPermission ekle
    - `/admin/couriers` için `requiredPermission="couriers.view"` ekle
    - _Requirements: 1.5_
  - [x] 1.7 Reports route'una requiredPermission ekle
    - `/admin/reports` için `requiredPermission={["reports.view", "reports.sales"]}` ekle (OR logic)
    - _Requirements: 1.6_
  - [x] 1.8 Posters route'una requiredPermission ekle
    - `/admin/posters` için `requiredPermission="banners.view"` ekle
    - _Requirements: 1.7_
  - [x] 1.9 Weight Reports route'una requiredPermission ekle
    - `/admin/weight-reports` için `requiredPermission={["reports.weight", "orders.view"]}` ekle (OR logic)
    - _Requirements: 1.8_
  - [x] 1.10 Campaigns route'una requiredPermission ekle
    - `/admin/campaigns` için `requiredPermission="campaigns.view"` ekle
    - _Requirements: 1.9_
  - [x] 1.11 Micro route'una requiredPermission ekle
    - `/admin/micro` için `requiredPermission="settings.system"` ekle
    - _Requirements: 1.10_
  - [x] 1.12 Log route'larına requiredPermission ekle
    - `/admin/logs/audit` için `requiredPermission="logs.audit"` ekle
    - `/admin/logs/errors` için `requiredPermission="logs.error"` ekle
    - `/admin/logs/system` için `requiredPermission="logs.view"` ekle
    - `/admin/logs/inventory` için `requiredPermission="logs.view"` ekle
    - _Requirements: 1.12_

- [x] 2. Checkpoint - Frontend Route Kontrollerini Doğrula

  - Tüm admin route'larının requiredPermission parametresi aldığını doğrula
  - Syntax hatası olmadığını kontrol et
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Seed Data İzin Güncellemelerini Ekle

  - [x] 3.1 StoreManager rolüne eksik izinleri ekle
    - `users.view` iznini StoreManager'a ekle
    - `couriers.view` iznini StoreManager'a ekle
    - _Requirements: 4.1, 4.2_
  - [x] 3.2 CustomerSupport rolüne eksik izinleri ekle
    - `reports.view` iznini CustomerSupport'a ekle
    - _Requirements: 4.3_
  - [x] 3.3 Logistics rolüne eksik izinleri ekle
    - `reports.weight` iznini Logistics'e ekle
    - _Requirements: 4.4_

- [x] 4. Backend Controller İzin Kontrollerini Düzelt

  - [x] 4.1 AdminUsersController UpdateUserRole endpoint'ini düzelt
    - `[Authorize(Roles = Roles.AdminLike)]` yerine `[HasPermission(Permissions.Users.Roles)]` kullan
    - _Requirements: 5.1_

- [x] 5. permissionService.js PERMISSIONS Sabitlerini Güncelle

  - [x] 5.1 Eksik PERMISSIONS sabitlerini ekle
    - `REPORTS_WEIGHT: "reports.weight"` sabitini ekle (varsa kontrol et)
    - `LOGS_VIEW: "logs.view"` sabitini ekle (varsa kontrol et)
    - _Requirements: 6.1, 6.3_

- [x] 6. Final Checkpoint - Tüm Değişiklikleri Doğrula
  - Frontend route izinlerinin çalıştığını doğrula
  - Seed data'nın doğru olduğunu kontrol et
  - Backend controller'ın düzgün çalıştığını doğrula
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tüm route'lar AdminLayout menüsündeki permission değerleriyle eşleşmeli
- SuperAdmin her zaman tüm sayfalara erişebilmeli
- Array permission'lar OR logic ile çalışır (herhangi biri yeterli)
- Seed data değişiklikleri veritabanına manuel uygulanmalı veya migration ile
