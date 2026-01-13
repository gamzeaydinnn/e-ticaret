# Requirements Document

## Introduction

Bu spec, frontend'deki PERMISSIONS sabitleri ile backend'deki Permissions.cs arasındaki uyumsuzlukları giderir. Mevcut durumda:

- Frontend'de `PERMISSIONS.REPORTS_VIEW` tanımlı ama backend'de `reports.view` yok (sadece `reports.sales`, `reports.inventory` gibi granüler izinler var)
- Frontend'de `PERMISSIONS.LOGS_VIEW` tanımlı ama backend'de `logs.view` yok (sadece `logs.audit`, `logs.error` var)
- Frontend'de `PERMISSIONS.SETTINGS_SYSTEM` tanımlı ama backend'de karşılığı yok
- Bu uyumsuzluk nedeniyle frontend'de yanlış izin kontrolü yapılıyor

## Glossary

- **permissionService.js**: Frontend'de izin sabitlerini ve API çağrılarını içeren servis dosyası
- **Permissions.cs**: Backend'de tüm izinleri tanımlayan C# sınıfı
- **PERMISSIONS**: Frontend'deki izin sabitleri objesi
- **Permission Constant**: Kod içinde kullanılan izin string sabiti (örn: "reports.view")
- **Granüler İzin**: Modül içinde spesifik bir aksiyonu temsil eden izin (örn: reports.sales)
- **Genel İzin**: Modülün tamamına erişimi temsil eden izin (örn: reports.view)

## Requirements

### Requirement 1: Reports Modülü İzin Senkronizasyonu

**User Story:** As a developer, I want frontend and backend report permissions to match, so that permission checks work correctly.

#### Acceptance Criteria

1. WHEN backend Permissions.cs is updated, THE System SHALL add `reports.view` as a general view permission
2. WHEN frontend checks `PERMISSIONS.REPORTS_VIEW`, THE System SHALL correctly validate against backend `reports.view`
3. WHEN a user has `reports.view` permission, THE System SHALL allow access to the reports module
4. WHEN a user lacks `reports.view` but has `reports.sales`, THE System SHALL allow access only to sales reports

### Requirement 2: Logs Modülü İzin Senkronizasyonu

**User Story:** As a developer, I want frontend and backend log permissions to match, so that log access control works correctly.

#### Acceptance Criteria

1. WHEN backend Permissions.cs is updated, THE System SHALL add `logs.view` as a general view permission
2. WHEN frontend checks `PERMISSIONS.LOGS_VIEW`, THE System SHALL correctly validate against backend `logs.view`
3. WHEN a user has `logs.view` permission, THE System SHALL allow access to the logs module
4. WHEN a user lacks `logs.view` but has `logs.audit`, THE System SHALL allow access only to audit logs

### Requirement 3: Settings Modülü İzin Senkronizasyonu

**User Story:** As a developer, I want frontend and backend settings permissions to match, so that system settings access works correctly.

#### Acceptance Criteria

1. WHEN backend Permissions.cs is updated, THE System SHALL add `settings.system` for system-level settings
2. WHEN frontend checks `PERMISSIONS.SETTINGS_SYSTEM`, THE System SHALL correctly validate against backend `settings.system`
3. WHEN a user has `settings.system` permission, THE System SHALL allow access to system configuration
4. IF a user lacks `settings.system` permission, THEN THE System SHALL hide system settings menu items

### Requirement 4: İzin Sabitleri Otomatik Senkronizasyon Mekanizması

**User Story:** As a developer, I want a mechanism to detect permission mismatches, so that I can fix them before deployment.

#### Acceptance Criteria

1. THE System SHALL provide a validation endpoint that compares frontend and backend permissions
2. WHEN a mismatch is detected, THE System SHALL log a warning with the mismatched permission names
3. WHEN frontend requests a permission not defined in backend, THE System SHALL return false (fail-safe)
4. THE System SHALL document all permission constants in both frontend and backend with matching names

### Requirement 5: Shipping Modülü İzin Eklenmesi

**User Story:** As a developer, I want shipping permissions to be properly defined, so that logistics operations work correctly.

#### Acceptance Criteria

1. WHEN frontend permissionService.js is updated, THE System SHALL add shipping-related permissions
2. THE System SHALL define `SHIPPING_VIEW`, `SHIPPING_UPDATE_STATUS`, `SHIPPING_TRACK`, `SHIPPING_WEIGHT_APPROVAL` in frontend
3. WHEN backend Permissions.cs is checked, THE System SHALL verify shipping permissions exist
4. WHEN a Logistics user accesses shipping features, THE System SHALL validate against shipping permissions

### Requirement 6: Permission Naming Convention Standardization

**User Story:** As a developer, I want consistent permission naming, so that the codebase is maintainable.

#### Acceptance Criteria

1. THE System SHALL use lowercase with dots for permission names (e.g., "module.action")
2. THE System SHALL use SCREAMING_SNAKE_CASE for frontend constants (e.g., PRODUCTS_VIEW)
3. THE System SHALL use PascalCase for backend class names (e.g., Products.View)
4. WHEN a new permission is added, THE System SHALL follow the established naming convention
5. THE System SHALL maintain a mapping document between frontend constants and backend values
