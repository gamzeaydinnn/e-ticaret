# Requirements Document

## Introduction

Bu doküman, RBAC (Role-Based Access Control) sistemindeki kritik güvenlik açıklarını, izin tutarsızlıklarını, mobil uyumluluk sorunlarını ve eksik özellikleri gidermek için gereksinimleri tanımlar. Tüm admin route'larında izin kontrolü, rol-izin eşleştirmesi düzeltmeleri, cache yönetimi ve UI/UX iyileştirmeleri kapsamındadır.

## Glossary

- **RBAC_System**: Role-Based Access Control sistemi, kullanıcı yetkilerini roller üzerinden yöneten güvenlik altyapısı
- **Route_Guard**: Frontend'de sayfa erişimini kontrol eden koruma bileşeni
- **Permission_Check**: Kullanıcının belirli bir izne sahip olup olmadığını doğrulayan kontrol mekanizması
- **AdminGuard**: Admin paneline erişimi kontrol eden temel koruma bileşeni
- **Cache_Manager**: İzin bilgilerini önbellekte tutan ve yöneten servis
- **Seed_Data**: Veritabanına başlangıç verilerini ekleyen SQL betikleri

## Requirements

### Requirement 1: Route İzin Kontrolü

**User Story:** As a sistem yöneticisi, I want tüm admin route'larında izin kontrolü yapılmasını, so that yetkisiz kullanıcılar URL ile doğrudan sayfalara erişemesin.

#### Acceptance Criteria

1. WHEN bir kullanıcı /admin/users sayfasına erişmeye çalıştığında, THE Route_Guard SHALL users.view iznini kontrol etmeli
2. WHEN bir kullanıcı /admin/products sayfasına erişmeye çalıştığında, THE Route_Guard SHALL products.view iznini kontrol etmeli
3. WHEN bir kullanıcı /admin/orders sayfasına erişmeye çalıştığında, THE Route_Guard SHALL orders.view iznini kontrol etmeli
4. WHEN bir kullanıcı /admin/categories sayfasına erişmeye çalıştığında, THE Route_Guard SHALL categories.view iznini kontrol etmeli
5. WHEN bir kullanıcı /admin/campaigns sayfasına erişmeye çalıştığında, THE Route_Guard SHALL campaigns.view iznini kontrol etmeli
6. WHEN bir kullanıcı /admin/coupons sayfasına erişmeye çalıştığında, THE Route_Guard SHALL coupons.view iznini kontrol etmeli
7. WHEN bir kullanıcı /admin/couriers sayfasına erişmeye çalıştığında, THE Route_Guard SHALL couriers.view iznini kontrol etmeli
8. WHEN bir kullanıcı /admin/banners sayfasına erişmeye çalıştığında, THE Route_Guard SHALL banners.view iznini kontrol etmeli
9. WHEN bir kullanıcı /admin/brands sayfasına erişmeye çalıştığında, THE Route_Guard SHALL brands.view iznini kontrol etmeli
10. WHEN bir kullanıcı gerekli izne sahip değilse, THE Route_Guard SHALL 403 Forbidden sayfasına yönlendirmeli
11. THE RBAC_System SHALL AdminLayout menüsü ile route koruması arasında tutarlılık sağlamalı

### Requirement 2: Rol-İzin Eşleştirmesi Düzeltmesi

**User Story:** As a veritabanı yöneticisi, I want rol-izin eşleştirmelerinin doğru yapılmasını, so that her rol sadece yetkili olduğu işlemleri yapabilsin.

#### Acceptance Criteria

1. WHEN StoreManager rolü tanımlandığında, THE Seed_Data SHALL users.view iznini atamalı
2. WHEN StoreManager rolü tanımlandığında, THE Seed_Data SHALL couriers.view iznini atamalı
3. WHEN CustomerSupport rolü tanımlandığında, THE Seed_Data SHALL reports.view iznini atamalı
4. WHEN Logistics rolü tanımlandığında, THE Seed_Data SHALL reports.weight iznini atamalı
5. THE Seed_Data SHALL frontend PERMISSIONS sabitleri ile backend izinleri arasında tam uyum sağlamalı
6. WHEN reports.view izni kontrol edildiğinde, THE RBAC_System SHALL backend'de tanımlı reports.view iznini kullanmalı

### Requirement 3: Backend Controller İzin Tutarlılığı

**User Story:** As a güvenlik uzmanı, I want tüm backend endpoint'lerinde tutarlı izin kontrolü yapılmasını, so that güvenlik açıkları oluşmasın.

#### Acceptance Criteria

1. WHEN UpdateUserRole endpoint'i çağrıldığında, THE AdminUsersController SHALL users.roles iznini kontrol etmeli
2. WHEN herhangi bir admin endpoint'i çağrıldığında, THE Controller SHALL hem rol hem de izin kontrolü yapmalı
3. IF bir endpoint sadece [Authorize(Roles)] kullanıyorsa, THEN THE Controller SHALL [HasPermission] attribute'u da eklemeli
4. THE RBAC_System SHALL tüm controller'larda tutarlı izin kontrol pattern'i kullanmalı

### Requirement 4: Dashboard İzin Kontrolü

**User Story:** As a admin kullanıcı, I want dashboard'da sadece yetkili olduğum istatistikleri görmek, so that hassas verilere erişimim kısıtlansın.

#### Acceptance Criteria

1. WHEN bir kullanıcı dashboard'a eriştiğinde, THE Dashboard SHALL dashboard.view iznini kontrol etmeli
2. WHEN bir kullanıcı istatistikleri görüntülemek istediğinde, THE Dashboard SHALL dashboard.statistics iznini kontrol etmeli
3. WHEN bir kullanıcı gelir grafiğini görüntülemek istediğinde, THE Dashboard SHALL dashboard.revenue iznini kontrol etmeli
4. IF kullanıcı gerekli izne sahip değilse, THEN THE Dashboard SHALL ilgili widget'ı gizlemeli

### Requirement 5: Cache ve Oturum Yönetimi

**User Story:** As a sistem yöneticisi, I want rol değişikliklerinin anında yansımasını, so that güvenlik açıkları oluşmasın.

#### Acceptance Criteria

1. WHEN bir kullanıcının rolü değiştirildiğinde, THE Cache_Manager SHALL o kullanıcının izin cache'ini hemen temizlemeli
2. WHEN bir kullanıcının şifresi değiştirildiğinde, THE RBAC_System SHALL o kullanıcının aktif oturumlarını sonlandırmalı
3. THE Cache_Manager SHALL izin cache süresini 5 dakikadan 1 dakikaya düşürmeli veya anlık güncelleme mekanizması sağlamalı
4. WHEN admin bir kullanıcının şifresini değiştirdiğinde, THE RBAC_System SHALL o kullanıcıyı zorla logout yapmalı

### Requirement 6: Mobil Uyumluluk

**User Story:** As a mobil kullanıcı, I want admin panelini mobil cihazda rahatça kullanmak, so that her yerden yönetim yapabileyim.

#### Acceptance Criteria

1. WHEN AdminUsers tablosu mobilde görüntülendiğinde, THE UI SHALL responsive card layout'a geçmeli
2. WHEN İzin Matrisi tablosu mobilde görüntülendiğinde, THE UI SHALL yatay scroll veya accordion layout kullanmalı
3. THE UI SHALL tüm tablolarda mobil için data-label CSS desteği sağlamalı
4. WHEN ekran genişliği 768px altında olduğunda, THE UI SHALL tablo yerine kart görünümü kullanmalı

### Requirement 7: Kullanıcı Yönetimi Özellikleri

**User Story:** As a admin kullanıcı, I want kullanıcıları arayıp filtrelemek, so that büyük kullanıcı listelerinde hızlıca bulabileyim.

#### Acceptance Criteria

1. THE AdminUsers SHALL kullanıcı adı ve email ile arama özelliği sağlamalı
2. THE AdminUsers SHALL rol bazlı filtreleme özelliği sağlamalı
3. THE AdminUsers SHALL aktif/pasif durum filtreleme özelliği sağlamalı
4. WHEN kullanıcı sayısı 20'yi aştığında, THE AdminUsers SHALL sayfalama (pagination) kullanmalı
5. THE AdminUsers SHALL toplu seçim ve toplu işlem (bulk actions) özelliği sağlamalı

### Requirement 8: Hata Mesajları Lokalizasyonu

**User Story:** As a Türk kullanıcı, I want tüm hata mesajlarını Türkçe görmek, so that sorunları anlayabileyim.

#### Acceptance Criteria

1. WHEN backend'den hata mesajı geldiğinde, THE UI SHALL Türkçe çeviri göstermeli
2. THE RBAC_System SHALL Identity hatalarını (password requirements vb.) Türkçe göstermeli
3. THE UI SHALL tüm validation mesajlarını Türkçe göstermeli

### Requirement 9: Loading ve UI State Yönetimi

**User Story:** As a kullanıcı, I want işlem sırasında görsel geri bildirim almak, so that sistemin çalıştığını anlayabileyim.

#### Acceptance Criteria

1. WHEN rol değiştirme işlemi yapılırken, THE UI SHALL tablo satırında loading göstergesi göstermeli
2. WHEN veri yüklenirken, THE UI SHALL skeleton loading göstermeli
3. THE UI SHALL tablo sütun genişliklerini sabit tutmalı
4. WHEN uzun email adresleri görüntülendiğinde, THE UI SHALL text-overflow: ellipsis kullanmalı

### Requirement 10: Rol Validasyonu Senkronizasyonu

**User Story:** As a geliştirici, I want frontend ve backend rol listelerinin senkronize olmasını, so that tutarsızlık hataları oluşmasın.

#### Acceptance Criteria

1. THE RBAC_System SHALL frontend ASSIGNABLE_ROLES listesini backend AllowedRoles ile senkronize tutmalı
2. WHEN yeni rol eklendiğinde, THE RBAC_System SHALL her iki tarafı da güncellemeli
3. THE RBAC_System SHALL rol listesini tek bir kaynaktan (backend API) almalı
