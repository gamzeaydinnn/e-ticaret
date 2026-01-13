# Requirements Document

## Introduction

Bu spec, e-ticaret admin panelindeki RBAC (Role-Based Access Control) izin sisteminin kritik sorunlarını düzeltmeyi hedefler. Mevcut sistemde route'larda izin kontrolü eksik ve menü ile route koruması uyumsuz durumda. Bu düzeltmeler ile her admin sayfası için spesifik izin kontrolü sağlanacak ve URL ile doğrudan erişim engellenecek.

## Glossary

- **RBAC**: Role-Based Access Control - Rol tabanlı erişim kontrolü sistemi
- **AdminGuard**: Admin route'larını koruyan React component
- **AdminLayout**: Admin paneli sidebar menüsünü ve layout'u sağlayan component
- **Permission**: Kullanıcının belirli bir işlemi yapabilme yetkisi (örn: "products.view")
- **Route**: React Router'da tanımlı sayfa yolu
- **requiredPermission**: AdminGuard'a geçirilen izin parametresi
- **SuperAdmin**: Tüm izinlere sahip en yetkili rol
- **StoreManager**: Ürün ve kategori yönetimi yetkili mağaza yöneticisi rolü
- **CustomerSupport**: Sipariş ve müşteri desteği yetkili rol
- **Logistics**: Kargo ve teslimat operasyonları yetkili rol

## Requirements

### Requirement 1: Route İzin Kontrolü

**User Story:** As a sistem yöneticisi, I want her admin route'unun spesifik izin kontrolü yapmasını, so that yetkisiz kullanıcılar URL ile doğrudan sayfalara erişemesin.

#### Acceptance Criteria

1. WHEN bir kullanıcı /admin/users sayfasına erişmeye çalıştığında, THE AdminGuard SHALL users.view izni kontrolü yapmalı
2. WHEN bir kullanıcı /admin/products sayfasına erişmeye çalıştığında, THE AdminGuard SHALL products.view izni kontrolü yapmalı
3. WHEN bir kullanıcı /admin/orders sayfasına erişmeye çalıştığında, THE AdminGuard SHALL orders.view izni kontrolü yapmalı
4. WHEN bir kullanıcı /admin/categories sayfasına erişmeye çalıştığında, THE AdminGuard SHALL categories.view izni kontrolü yapmalı
5. WHEN bir kullanıcı /admin/couriers sayfasına erişmeye çalıştığında, THE AdminGuard SHALL couriers.view izni kontrolü yapmalı
6. WHEN bir kullanıcı /admin/reports sayfasına erişmeye çalıştığında, THE AdminGuard SHALL reports.view veya reports.sales izni kontrolü yapmalı
7. WHEN bir kullanıcı /admin/posters sayfasına erişmeye çalıştığında, THE AdminGuard SHALL banners.view izni kontrolü yapmalı
8. WHEN bir kullanıcı /admin/weight-reports sayfasına erişmeye çalıştığında, THE AdminGuard SHALL reports.weight veya orders.view izni kontrolü yapmalı
9. WHEN bir kullanıcı /admin/campaigns sayfasına erişmeye çalıştığında, THE AdminGuard SHALL campaigns.view izni kontrolü yapmalı
10. WHEN bir kullanıcı /admin/micro sayfasına erişmeye çalıştığında, THE AdminGuard SHALL settings.system izni kontrolü yapmalı
11. WHEN bir kullanıcı /admin/dashboard sayfasına erişmeye çalıştığında, THE AdminGuard SHALL dashboard.view izni kontrolü yapmalı
12. WHEN bir kullanıcı /admin/logs/\* sayfalarına erişmeye çalıştığında, THE AdminGuard SHALL logs.view veya ilgili log izni kontrolü yapmalı

### Requirement 2: Yetkisiz Erişim Yönlendirmesi

**User Story:** As a yetkisiz kullanıcı, I want iznim olmayan sayfalara erişmeye çalıştığımda anlamlı bir hata sayfası görmek, so that neden erişemediğimi anlayabileyim.

#### Acceptance Criteria

1. WHEN bir kullanıcı izni olmayan bir sayfaya erişmeye çalıştığında, THE System SHALL kullanıcıyı /admin/access-denied sayfasına yönlendirmeli
2. WHEN kullanıcı access-denied sayfasına yönlendirildiğinde, THE System SHALL hangi izne ihtiyaç duyulduğunu göstermeli
3. WHEN kullanıcı access-denied sayfasına yönlendirildiğinde, THE System SHALL dashboard'a dönüş linki sunmalı

### Requirement 3: Menü-Route Tutarlılığı

**User Story:** As a admin kullanıcı, I want menüde gördüğüm sayfaların route korumasıyla tutarlı olmasını, so that menüde görmediğim sayfalara URL ile de erişemeyeyim.

#### Acceptance Criteria

1. THE AdminLayout menü izinleri SHALL App.js route izinleriyle birebir eşleşmeli
2. WHEN bir menü öğesi permission ile filtrelendiğinde, THE ilgili route da aynı permission ile korunmalı
3. WHEN adminOnly: true olan bir menü öğesi varsa, THE ilgili route da admin rolü kontrolü yapmalı

### Requirement 4: Seed Data İzin Güncellemesi

**User Story:** As a StoreManager rolündeki kullanıcı, I want gerekli izinlere sahip olmak, so that görevlerimi yerine getirebilmem için gereken sayfalara erişebileyim.

#### Acceptance Criteria

1. THE StoreManager rolü SHALL users.view iznine sahip olmalı (kullanıcı listesini görebilmesi için)
2. THE StoreManager rolü SHALL couriers.view iznine sahip olmalı (kurye listesini görebilmesi için)
3. THE CustomerSupport rolü SHALL reports.view iznine sahip olmalı (raporları görebilmesi için)
4. THE Logistics rolü SHALL reports.weight iznine sahip olmalı (ağırlık raporlarını görebilmesi için)

### Requirement 5: Backend İzin Kontrolü Tutarlılığı

**User Story:** As a sistem yöneticisi, I want backend controller'lardaki izin kontrollerinin tutarlı olmasını, so that frontend ve backend aynı izin kurallarını uygulasın.

#### Acceptance Criteria

1. THE AdminUsersController UpdateUserRole endpoint'i SHALL users.roles izni kontrolü yapmalı
2. WHEN bir endpoint [Authorize(Roles = Roles.AdminLike)] kullanıyorsa, THE endpoint ayrıca spesifik izin kontrolü de yapmalı
3. THE tüm admin controller'lar SHALL tutarlı izin attribute'ları kullanmalı

### Requirement 6: PERMISSIONS Sabitleri Senkronizasyonu

**User Story:** As a geliştirici, I want frontend PERMISSIONS sabitleri ile backend izinlerinin senkronize olmasını, so that yanlış izin kontrolü yapılmasın.

#### Acceptance Criteria

1. THE permissionService.js PERMISSIONS sabitleri SHALL backend Permissions.cs ile eşleşmeli
2. WHEN backend'de yeni bir izin eklendiğinde, THE frontend PERMISSIONS sabitleri de güncellenmeli
3. THE PERMISSIONS.REPORTS_VIEW sabiti SHALL backend'deki reports.view veya reports.sales ile eşleşmeli
