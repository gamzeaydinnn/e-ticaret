# Requirements Document

## Introduction

Bu spec, StoreManager rolünün admin paneline erişim sorunlarını çözer. Mevcut durumda StoreManager rolü sadece Dashboard'a erişebiliyor çünkü veritabanında gerekli izinler (users.view, couriers.view vb.) atanmamış. AdminLayout.jsx'te `adminOnly: true` olan menüler `isAdminLike` kontrolü ile gösteriliyor ancak backend'de izin eksikliği nedeniyle API 403 hatası dönüyor.

## Glossary

- **StoreManager**: Mağaza yöneticisi rolü, ürün ve kategori yönetimi yapabilmeli
- **AdminLayout**: Admin paneli sidebar ve menü yapısını sağlayan React component
- **isAdminLike**: SuperAdmin, Admin veya StoreManager rollerinden birine sahip kullanıcıları tanımlayan kontrol
- **adminOnly**: Sadece admin benzeri rollerin görebileceği menü öğelerini işaretleyen flag
- **RBAC**: Role-Based Access Control - Rol tabanlı erişim kontrolü sistemi
- **seed-rbac-data.sql**: Veritabanına rol-izin eşleştirmelerini ekleyen SQL script

## Requirements

### Requirement 1: StoreManager Kullanıcı Listesi Erişimi

**User Story:** As a StoreManager, I want to view the user list, so that I can see customer information for order management.

#### Acceptance Criteria

1. WHEN a StoreManager accesses the admin panel, THE System SHALL grant `users.view` permission to StoreManager role in the database
2. WHEN a StoreManager navigates to /admin/users, THE System SHALL display the user list without 403 error
3. WHEN a StoreManager views user list, THE System SHALL NOT allow user creation, update, or deletion operations
4. IF a StoreManager attempts to modify user data, THEN THE System SHALL return 403 Forbidden response

### Requirement 2: StoreManager Kurye Listesi Erişimi

**User Story:** As a StoreManager, I want to view the courier list, so that I can monitor delivery operations.

#### Acceptance Criteria

1. WHEN a StoreManager accesses the admin panel, THE System SHALL grant `couriers.view` permission to StoreManager role in the database
2. WHEN a StoreManager navigates to /admin/couriers, THE System SHALL display the courier list without 403 error
3. WHEN a StoreManager views courier list, THE System SHALL NOT allow courier creation, update, or deletion operations
4. IF a StoreManager attempts to modify courier data, THEN THE System SHALL return 403 Forbidden response

### Requirement 3: AdminLayout Menü ve Route Tutarlılığı

**User Story:** As a system architect, I want menu visibility to match actual permissions, so that users don't see inaccessible menu items.

#### Acceptance Criteria

1. WHEN AdminLayout renders menu items, THE System SHALL check actual user permissions from PermissionContext
2. WHEN a menu item has `adminOnly: true` flag, THE System SHALL verify the user has the corresponding permission before displaying
3. WHEN a user lacks permission for a menu item, THE System SHALL hide that menu item from sidebar
4. WHEN a user directly navigates to a URL without permission, THE System SHALL redirect to dashboard with access denied message

### Requirement 4: Veritabanı İzin Seed Güncellemesi

**User Story:** As a database administrator, I want seed scripts to include all necessary permissions, so that roles work correctly after deployment.

#### Acceptance Criteria

1. WHEN seed-rbac-data.sql is executed, THE System SHALL insert `users.view` permission for StoreManager role
2. WHEN seed-rbac-data.sql is executed, THE System SHALL insert `couriers.view` permission for StoreManager role
3. WHEN seed-rbac-data.sql is executed, THE System SHALL NOT grant write permissions (create, update, delete) to StoreManager for users and couriers
4. WHEN IdentitySeeder runs, THE System SHALL apply the same permission mappings as seed-rbac-data.sql
