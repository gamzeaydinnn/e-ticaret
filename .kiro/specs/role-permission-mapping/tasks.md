# Implementation Plan: Role-Permission Mapping

## Overview

Tüm rollerin (StoreManager, CustomerSupport, Logistics) izin eşleştirmelerini güncelleyerek iş gereksinimlerine uygun hale getirme. Hem SQL seed script hem de C# IdentitySeeder senkronize tutulacak.

## Tasks

- [ ] 1. StoreManager İzin Güncellemesi

  - [ ] 1.1 seed-rbac-data.sql'de StoreManager izinlerini genişlet

    - `users.view` iznini ekle (müşteri listesi görüntüleme)
    - `couriers.view` iznini ekle (kurye listesi görüntüleme)
    - `reports.view` iznini ekle (genel rapor erişimi)
    - Write izinlerinin (create, update, delete) EKLENMEDİĞİNDEN emin ol
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

  - [ ] 1.2 IdentitySeeder.cs'de StoreManager izinlerini güncelle
    - StoreManagerPermissions dizisine aynı izinleri ekle
    - SQL script ile birebir eşleştiğinden emin ol
    - _Requirements: 4.1, 4.2_

- [ ] 2. CustomerSupport İzin Güncellemesi

  - [ ] 2.1 seed-rbac-data.sql'de CustomerSupport izinlerini genişlet

    - `reports.view` iznini ekle (genel rapor erişimi)
    - `reports.sales` iznini ekle (satış raporları)
    - `reports.financial` ve `reports.export` izinlerinin EKLENMEDİĞİNDEN emin ol
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [ ] 2.2 IdentitySeeder.cs'de CustomerSupport izinlerini güncelle
    - CustomerSupportPermissions dizisine aynı izinleri ekle
    - _Requirements: 4.1, 4.2_

- [ ] 3. Logistics İzin Güncellemesi

  - [ ] 3.1 seed-rbac-data.sql'de Logistics izinlerini genişlet

    - `reports.view` iznini ekle (genel rapor erişimi)
    - `reports.weight` iznini ekle (ağırlık raporları)
    - `reports.financial` ve `reports.customers` izinlerinin EKLENMEDİĞİNDEN emin ol
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

  - [ ] 3.2 IdentitySeeder.cs'de Logistics izinlerini güncelle
    - LogisticsPermissions dizisine aynı izinleri ekle
    - _Requirements: 4.1, 4.2_

- [ ] 4. Checkpoint - Seed Script Doğrulama

  - SQL script'i test veritabanında çalıştır
  - Her rol için izin sayısını doğrula
  - Kısıtlanan izinlerin atanmadığını kontrol et
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. İzin Matrisi Dokümantasyonu

  - [ ] 5.1 README veya RBAC_GUIDE.md dosyasına izin matrisi ekle
    - Tüm roller ve izinleri tablo formatında listele
    - Her kısıtlamanın gerekçesini açıkla
    - _Requirements: 5.1, 5.2, 5.3_

- [ ] 6. Property-Based Test Implementasyonu

  - [ ] 6.1 StoreManager write permission restriction property testi

    - **Property 1: StoreManager Write Permission Restriction**
    - **Validates: Requirements 1.4, 1.5**

  - [ ] 6.2 CustomerSupport sensitive data restriction property testi

    - **Property 2: CustomerSupport Sensitive Data Restriction**
    - **Validates: Requirements 2.3, 2.4**

  - [ ] 6.3 Logistics privacy restriction property testi

    - **Property 3: Logistics Privacy Restriction**
    - **Validates: Requirements 3.3, 3.4**

  - [ ] 6.4 Seed script and seeder consistency property testi
    - **Property 4: Seed Script and Seeder Consistency**
    - **Validates: Requirements 4.1, 4.2**

- [ ] 7. Final Checkpoint
  - Tüm testlerin geçtiğinden emin ol
  - Her rol ile giriş yapıp erişim haklarını doğrula
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Seed script ve IdentitySeeder her zaman senkronize tutulmalı
- Yeni izin eklerken her iki dosyayı da güncellemeyi unutma
- Production'a deploy etmeden önce tüm rolleri test et
