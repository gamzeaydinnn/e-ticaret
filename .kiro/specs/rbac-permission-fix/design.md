# Design Document: RBAC Permission Fix

## Overview

Bu tasarım, e-ticaret admin panelindeki RBAC izin sisteminin kritik sorunlarını çözmek için gerekli değişiklikleri tanımlar. Ana hedefler:

1. **App.js Route Koruması**: Tüm admin route'larına `requiredPermission` parametresi eklenmesi
2. **Menü-Route Tutarlılığı**: AdminLayout menü izinleri ile route izinlerinin senkronize edilmesi
3. **Seed Data Güncellemesi**: Eksik rol-izin eşleştirmelerinin eklenmesi
4. **Backend Tutarlılığı**: Controller'lardaki izin kontrollerinin düzeltilmesi

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Frontend (React)                          │
├─────────────────────────────────────────────────────────────────┤
│  App.js                                                          │
│  ├── Route: /admin/users → AdminGuard(requiredPermission)       │
│  ├── Route: /admin/products → AdminGuard(requiredPermission)    │
│  └── ... (tüm admin route'ları)                                 │
├─────────────────────────────────────────────────────────────────┤
│  AdminGuard.js                                                   │
│  ├── hasPermission() kontrolü                                   │
│  ├── hasAnyPermission() kontrolü (OR logic)                     │
│  └── Access Denied yönlendirmesi                                │
├─────────────────────────────────────────────────────────────────┤
│  AdminLayout.jsx                                                 │
│  ├── Menü filtreleme (permission bazlı)                         │
│  └── checkPermission() helper                                   │
├─────────────────────────────────────────────────────────────────┤
│  permissionService.js                                            │
│  └── PERMISSIONS sabitleri (backend ile senkron)                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Backend (ASP.NET)                         │
├─────────────────────────────────────────────────────────────────┤
│  AdminUsersController.cs                                         │
│  ├── [HasPermission(Permissions.Users.View)]                    │
│  ├── [HasPermission(Permissions.Users.Roles)] ← YENİ            │
│  └── Tutarlı izin attribute'ları                                │
├─────────────────────────────────────────────────────────────────┤
│  seed-rbac-data.sql                                              │
│  ├── StoreManager → users.view, couriers.view                   │
│  ├── CustomerSupport → reports.view                             │
│  └── Logistics → reports.weight                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### 1. App.js Route Güncellemeleri

Her admin route'una `requiredPermission` parametresi eklenecek:

```jsx
// Mevcut (YANLIŞ)
<Route
  path="/admin/users"
  element={
    <AdminGuard>
      <AdminLayout><AdminUsers /></AdminLayout>
    </AdminGuard>
  }
/>

// Düzeltilmiş (DOĞRU)
<Route
  path="/admin/users"
  element={
    <AdminGuard requiredPermission="users.view">
      <AdminLayout><AdminUsers /></AdminLayout>
    </AdminGuard>
  }
/>
```

### 2. Route-Permission Eşleştirme Tablosu

| Route                 | Permission                        | Açıklama               |
| --------------------- | --------------------------------- | ---------------------- |
| /admin/dashboard      | dashboard.view                    | Dashboard görüntüleme  |
| /admin/users          | users.view                        | Kullanıcı listesi      |
| /admin/products       | products.view                     | Ürün listesi           |
| /admin/categories     | categories.view                   | Kategori listesi       |
| /admin/orders         | orders.view                       | Sipariş listesi        |
| /admin/couriers       | couriers.view                     | Kurye listesi          |
| /admin/reports        | ["reports.view", "reports.sales"] | Raporlar (OR)          |
| /admin/posters        | banners.view                      | Poster/Banner yönetimi |
| /admin/weight-reports | ["reports.weight", "orders.view"] | Ağırlık raporları (OR) |
| /admin/campaigns      | campaigns.view                    | Kampanya yönetimi      |
| /admin/micro          | settings.system                   | ERP/Mikro entegrasyonu |
| /admin/logs/audit     | logs.audit                        | Audit logları          |
| /admin/logs/errors    | logs.error                        | Hata logları           |
| /admin/logs/system    | logs.view                         | Sistem logları         |
| /admin/logs/inventory | logs.view                         | Envanter logları       |
| /admin/roles          | roles.view                        | Rol yönetimi           |
| /admin/permissions    | roles.permissions                 | İzin yönetimi          |

### 3. AdminGuard Davranışı

```
┌─────────────────────────────────────────┐
│           AdminGuard Flow               │
├─────────────────────────────────────────┤
│ 1. User authenticated?                  │
│    NO → Redirect to /admin/login        │
│                                         │
│ 2. User has admin role?                 │
│    NO → Redirect to /                   │
│                                         │
│ 3. requiredPermission specified?        │
│    NO → Allow access                    │
│                                         │
│ 4. User is SuperAdmin?                  │
│    YES → Allow access                   │
│                                         │
│ 5. User has required permission(s)?     │
│    YES → Allow access                   │
│    NO → Redirect to /admin/access-denied│
└─────────────────────────────────────────┘
```

### 4. Seed Data Güncellemeleri

```sql
-- StoreManager için eklenmesi gereken izinler
'users.view',      -- Kullanıcı listesini görebilmesi için
'couriers.view'    -- Kurye listesini görebilmesi için

-- CustomerSupport için eklenmesi gereken izinler
'reports.view'     -- Raporları görebilmesi için

-- Logistics için eklenmesi gereken izinler
'reports.weight'   -- Ağırlık raporlarını görebilmesi için
```

### 5. Backend Controller Güncellemesi

```csharp
// Mevcut (YANLIŞ)
[HttpPut("{id}/role")]
[Authorize(Roles = Roles.AdminLike)]
public async Task<IActionResult> UpdateUserRole(...)

// Düzeltilmiş (DOĞRU)
[HttpPut("{id}/role")]
[HasPermission(Permissions.Users.Roles)]
public async Task<IActionResult> UpdateUserRole(...)
```

## Data Models

### Permission Mapping Interface

```typescript
interface RoutePermissionMapping {
  path: string;
  permission: string | string[]; // Tek izin veya OR logic için array
  requireAll?: boolean; // Array ise AND logic için true
}
```

### AdminGuard Props

```typescript
interface AdminGuardProps {
  children: React.ReactNode;
  requiredPermission?: string | string[];
  requireAll?: boolean; // default: false (OR logic)
  fallbackPath?: string; // default: "/admin/access-denied"
}
```

## Correctness Properties

_A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees._

### Property 1: Route Permission Enforcement

_For any_ admin route with a `requiredPermission` parameter, and _for any_ user attempting to access that route:

- If the user has the required permission(s), access SHALL be granted
- If the user does NOT have the required permission(s) AND is NOT SuperAdmin, access SHALL be denied and user SHALL be redirected to /admin/access-denied

**Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9, 1.10, 1.11, 1.12, 2.1**

### Property 2: SuperAdmin Bypass

_For any_ admin route with a `requiredPermission` parameter, and _for any_ user with role "SuperAdmin":

- Access SHALL always be granted regardless of specific permissions

**Validates: Requirements 1.1-1.12**

### Property 3: OR Logic Permission Check

_For any_ admin route with multiple permissions in array format (e.g., `["reports.view", "reports.sales"]`), and _for any_ user:

- If the user has AT LEAST ONE of the specified permissions, access SHALL be granted
- If the user has NONE of the specified permissions, access SHALL be denied

**Validates: Requirements 1.6, 1.8**

### Property 4: Role-Permission Seed Data Integrity

_For any_ role defined in the system (StoreManager, CustomerSupport, Logistics):

- The role SHALL have all permissions specified in the seed data
- StoreManager SHALL have users.view AND couriers.view permissions
- CustomerSupport SHALL have reports.view permission
- Logistics SHALL have reports.weight permission

**Validates: Requirements 4.1, 4.2, 4.3, 4.4**

## Error Handling

### Yetkisiz Erişim Durumları

1. **Kullanıcı giriş yapmamış**: `/admin/login` sayfasına yönlendir
2. **Kullanıcı admin değil**: Ana sayfaya (`/`) yönlendir
3. **Kullanıcı izni yok**: `/admin/access-denied` sayfasına yönlendir
   - State'te `requiredPermission` bilgisi geçirilir
   - Kullanıcıya hangi izne ihtiyacı olduğu gösterilir

### Access Denied Sayfası İçeriği

```jsx
// /admin/access-denied sayfası gösterecek:
{
  title: "Erişim Reddedildi",
  message: "Bu sayfaya erişim yetkiniz bulunmamaktadır.",
  requiredPermission: location.state?.requiredPermission,
  actions: [
    { label: "Dashboard'a Dön", path: "/admin/dashboard" }
  ]
}
```

## Testing Strategy

### Unit Tests

1. **AdminGuard Component Tests**

   - requiredPermission olmadan render
   - requiredPermission ile izni olan kullanıcı
   - requiredPermission ile izni olmayan kullanıcı
   - SuperAdmin bypass testi
   - Array permission (OR logic) testi

2. **Permission Helper Tests**
   - hasPermission() fonksiyonu
   - hasAnyPermission() fonksiyonu
   - hasAllPermissions() fonksiyonu

### Property-Based Tests

Property-based testing için `fast-check` kütüphanesi kullanılacak.

1. **Route Permission Property Test**

   - Rastgele route ve permission kombinasyonları
   - Rastgele kullanıcı rolleri ve izinleri
   - Erişim kararının tutarlılığı kontrolü

2. **Seed Data Integrity Test**
   - Tüm roller için beklenen izinlerin varlığı kontrolü

### Integration Tests

1. **Backend Controller Tests**
   - UpdateUserRole endpoint'inin users.roles izni kontrolü
   - Yetkisiz erişim denemelerinde 403 dönüşü

### Test Configuration

- Minimum 100 iterasyon per property test
- Test tag format: **Feature: rbac-permission-fix, Property {number}: {property_text}**
