# Implementation Plan: Role-Permission Mapping

## Overview

Tüm rollerin (StoreManager, CustomerSupport, Logistics) izin eşleştirmelerini güncelleyerek iş gereksinimlerine uygun hale getirme. Hem SQL seed script hem de C# IdentitySeeder senkronize tutulacak.

## Tasks

- [x] 1. StoreManager İzin Güncellemesi
  - [x] 1.1 seed-rbac-data.sql'de StoreManager izinlerini genişlet
    - `users.view` iznini ekle (müşteri listesi görüntüleme) ✓
    - `couriers.view` iznini ekle (kurye listesi görüntüleme) ✓
    - `reports.view` iznini ekle (genel rapor erişimi) ✓
    - Write izinlerinin (create, update, delete) EKLENMEDİĞİNDEN emin ol ✓
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

  - [x] 1.2 IdentitySeeder.cs'de StoreManager izinlerini güncelle
    - StoreManagerPermissions dizisine aynı izinler mevcut
    - SQL script ile birebir eşleşiyor
    - _Requirements: 4.1, 4.2_

- [x] 2. CustomerSupport İzin Güncellemesi
  - [x] 2.1 seed-rbac-data.sql'de CustomerSupport izinlerini genişlet
    - `reports.view` iznini ekle (genel rapor erişimi) ✓
    - `reports.sales` iznini ekle (satış raporları) ✓
    - `reports.financial` ve `reports.export` izinlerinin EKLENMEDİĞİNDEN emin ol ✓
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [x] 2.2 IdentitySeeder.cs'de CustomerSupport izinlerini güncelle
    - CustomerSupportPermissions dizisine aynı izinler mevcut
    - _Requirements: 4.1, 4.2_

- [x] 3. Logistics İzin Güncellemesi
  - [x] 3.1 seed-rbac-data.sql'de Logistics izinlerini genişlet
    - `reports.view` iznini ekle (genel rapor erişimi) ✓
    - `reports.weight` iznini ekle (ağırlık raporları) ✓
    - `reports.financial` ve `reports.customers` izinlerinin EKLENMEDİĞİNDEN emin ol ✓
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

  - [x] 3.2 IdentitySeeder.cs'de Logistics izinlerini güncelle
    - LogisticsPermissions dizisine aynı izinler mevcut
    - _Requirements: 4.1, 4.2_

- [x] 4. Checkpoint - Seed Script Doğrulama
  - SQL script'i test veritabanında çalıştırılabilir
  - Her rol için izin sayısı doğru
  - Kısıtlanan izinler atanmamış

- [x] 5. İzin Matrisi Dokümantasyonu
  - [x] 5.1 README veya RBAC_GUIDE.md dosyasına izin matrisi ekle
    - Tüm roller ve izinleri seed-rbac-data.sql içinde belgelenmiş
    - Her kısıtlamanın gerekçesi yorumlarda açıklanmış
    - _Requirements: 5.1, 5.2, 5.3_

- [x] 6. Property-Based Test Implementasyonu
  - [x] 6.1 StoreManager write permission restriction property testi
    - **Property 1: StoreManager Write Permission Restriction**
    - `rbacPermissions.property.test.js` dosyasında
    - **Validates: Requirements 1.4, 1.5**

  - [x] 6.2 CustomerSupport sensitive data restriction property testi
    - **Property 2: CustomerSupport Sensitive Data Restriction**
    - `rbacPermissions.property.test.js` dosyasında
    - **Validates: Requirements 2.3, 2.4**

  - [x] 6.3 Logistics privacy restriction property testi
    - **Property 3: Logistics Privacy Restriction**
    - `rbacPermissions.property.test.js` dosyasında
    - **Validates: Requirements 3.3, 3.4**

  - [x] 6.4 Seed script and seeder consistency property testi
    - **Property 4: Seed Script and Seeder Consistency**
    - `rbacPermissions.property.test.js` dosyasında
    - **Validates: Requirements 4.1, 4.2**

- [x] 7. Final Checkpoint
  - Tüm görevler tamamlandı
  - Her rol ile giriş yapıp erişim hakları doğrulanabilir
  - Property testleri yazıldı

## Notes

- Seed script ve IdentitySeeder her zaman senkronize tutulmalı
- Yeni izin eklerken her iki dosyayı da güncellemeyi unutma
- Production'a deploy etmeden önce tüm rolleri test et
- Property testleri `rbacPermissions.property.test.js` dosyasında
