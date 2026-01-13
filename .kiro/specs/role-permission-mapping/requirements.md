# Requirements Document

## Introduction

Bu spec, seed-rbac-data.sql dosyasındaki rol-izin eşleştirme eksikliklerini giderir. Mevcut durumda:

- StoreManager'a `users.view` izni verilmemiş → Kullanıcı listesini göremiyor
- StoreManager'a `couriers.view` izni verilmemiş → Kurye listesini göremiyor
- CustomerSupport'a `reports.view` izni verilmemiş → Raporları göremiyor
- Logistics'e `reports.weight` izni verilmemiş → Ağırlık raporlarını göremiyor

## Glossary

- **RolePermissions**: Veritabanında rol ve izin ilişkisini tutan tablo
- **StoreManager**: Mağaza yöneticisi rolü - ürün, kategori, kampanya yönetimi
- **CustomerSupport**: Müşteri destek rolü - sipariş yönetimi, müşteri iletişimi
- **Logistics**: Lojistik rolü - kargo ve teslimat operasyonları
- **SuperAdmin**: Tüm izinlere sahip en yetkili rol
- **seed-rbac-data.sql**: Veritabanına RBAC verilerini ekleyen SQL script
- **IdentitySeeder**: Uygulama başlangıcında RBAC verilerini oluşturan C# sınıfı

## Requirements

### Requirement 1: StoreManager İzin Genişletmesi

**User Story:** As a StoreManager, I want to have appropriate view permissions, so that I can effectively manage the store operations.

#### Acceptance Criteria

1. WHEN StoreManager role is seeded, THE System SHALL grant `users.view` permission for viewing customer list
2. WHEN StoreManager role is seeded, THE System SHALL grant `couriers.view` permission for monitoring deliveries
3. WHEN StoreManager role is seeded, THE System SHALL grant `reports.view` permission for accessing general reports
4. WHEN StoreManager role is seeded, THE System SHALL NOT grant `users.create`, `users.update`, `users.delete` permissions
5. WHEN StoreManager role is seeded, THE System SHALL NOT grant `couriers.create`, `couriers.update`, `couriers.delete` permissions

### Requirement 2: CustomerSupport İzin Genişletmesi

**User Story:** As a CustomerSupport agent, I want to view reports, so that I can analyze customer issues and order patterns.

#### Acceptance Criteria

1. WHEN CustomerSupport role is seeded, THE System SHALL grant `reports.view` permission for general report access
2. WHEN CustomerSupport role is seeded, THE System SHALL grant `reports.sales` permission for sales report access
3. WHEN CustomerSupport role is seeded, THE System SHALL NOT grant `reports.financial` permission (sensitive data)
4. WHEN CustomerSupport role is seeded, THE System SHALL NOT grant `reports.export` permission (data export restriction)

### Requirement 3: Logistics İzin Genişletmesi

**User Story:** As a Logistics operator, I want to view weight reports, so that I can manage shipping weight discrepancies.

#### Acceptance Criteria

1. WHEN Logistics role is seeded, THE System SHALL grant `reports.weight` permission for weight report access
2. WHEN Logistics role is seeded, THE System SHALL grant `reports.view` permission for general report access
3. WHEN Logistics role is seeded, THE System SHALL NOT grant `reports.financial` permission (sensitive data)
4. WHEN Logistics role is seeded, THE System SHALL NOT grant `reports.customers` permission (customer privacy)

### Requirement 4: Seed Script ve IdentitySeeder Senkronizasyonu

**User Story:** As a DevOps engineer, I want seed scripts and IdentitySeeder to be synchronized, so that deployments are consistent.

#### Acceptance Criteria

1. WHEN seed-rbac-data.sql is updated, THE IdentitySeeder SHALL be updated with the same permission mappings
2. WHEN IdentitySeeder runs, THE System SHALL apply identical permissions as seed-rbac-data.sql
3. WHEN a new permission is added, THE System SHALL update both seed-rbac-data.sql and IdentitySeeder
4. WHEN permissions are modified, THE System SHALL log the changes for audit purposes

### Requirement 5: İzin Matrisi Dokümantasyonu

**User Story:** As a system administrator, I want a clear permission matrix, so that I can understand which role has which permissions.

#### Acceptance Criteria

1. THE System SHALL maintain a permission matrix showing all roles and their permissions
2. THE System SHALL document the rationale for each permission assignment
3. WHEN a permission is added or removed, THE System SHALL update the permission matrix documentation
4. THE System SHALL provide a way to export the current permission matrix from the database
