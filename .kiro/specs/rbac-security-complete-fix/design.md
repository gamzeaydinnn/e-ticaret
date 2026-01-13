# Design Document

## Overview

Bu tasarım dokümanı, RBAC sistemindeki kritik güvenlik açıklarını, izin tutarsızlıklarını ve UI/UX sorunlarını çözmek için kapsamlı bir mimari sunar. Çözüm, frontend route koruması, backend controller tutarlılığı, cache yönetimi ve mobil uyumluluk bileşenlerini içerir.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Frontend Layer                            │
├─────────────────────────────────────────────────────────────────┤
│  App.js (Routes)                                                 │
│  ├── ProtectedRoute (requiredPermission prop)                   │
│  ├── AdminGuard (base protection)                               │
│  └── Permission-based rendering                                  │
├─────────────────────────────────────────────────────────────────┤
│  PermissionContext                                               │
│  ├── Real-time permission sync                                  │
│  ├── Cache invalidation on role change                          │
│  └── WebSocket listener for forced logout                       │
├─────────────────────────────────────────────────────────────────┤
│  UI Components                                                   │
│  ├── ResponsiveTable (mobile card view)                         │
│  ├── SearchableUserList                                         │
│  ├── PaginatedList                                              │
│  └── BulkActionToolbar                                          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Backend Layer                             │
├─────────────────────────────────────────────────────────────────┤
│  Controllers                                                     │
│  ├── [HasPermission] attribute on ALL endpoints                 │
│  ├── Consistent authorization pattern                           │
│  └── Turkish error messages                                      │
├─────────────────────────────────────────────────────────────────┤
│  Services                                                        │
│  ├── PermissionCacheService (1-min TTL)                         │
│  ├── SessionInvalidationService                                 │
│  └── RoleChangeNotificationService                              │
├─────────────────────────────────────────────────────────────────┤
│  Database                                                        │
│  ├── seed-rbac-data.sql (corrected mappings)                    │
│  └── Role-Permission junction table                             │
└─────────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### 1. ProtectedRoute Component (Enhanced)

```jsx
// frontend/src/components/ProtectedRoute.jsx
/**
 * Geliştirilmiş route koruma bileşeni
 * - requiredPermission: Gerekli izin string'i
 * - fallbackPath: İzin yoksa yönlendirilecek sayfa
 * - showForbidden: 403 sayfası göster/gösterme
 */
const ProtectedRoute = ({
  children,
  requiredPermission,
  fallbackPath = "/admin/forbidden",
  showForbidden = true,
}) => {
  const { hasPermission, loading } = usePermission();

  if (loading) return <LoadingSpinner />;

  if (requiredPermission && !hasPermission(requiredPermission)) {
    return showForbidden ? <ForbiddenPage /> : <Navigate to={fallbackPath} />;
  }

  return children;
};
```

### 2. Route Configuration Map

```javascript
// frontend/src/config/routePermissions.js
/**
 * Tüm admin route'ları ve gerekli izinleri
 * Merkezi yönetim için tek kaynak
 */
export const ROUTE_PERMISSIONS = {
  "/admin/dashboard": "dashboard.view",
  "/admin/users": "users.view",
  "/admin/products": "products.view",
  "/admin/orders": "orders.view",
  "/admin/categories": "categories.view",
  "/admin/campaigns": "campaigns.view",
  "/admin/coupons": "coupons.view",
  "/admin/couriers": "couriers.view",
  "/admin/banners": "banners.view",
  "/admin/brands": "brands.view",
  "/admin/reports": "reports.view",
  "/admin/logs": "logs.view",
  "/admin/settings": "settings.view",
  "/admin/roles": "roles.view",
  "/admin/permissions": "roles.permissions",
};
```

### 3. Cache Invalidation Service

```csharp
// Backend: PermissionCacheService.cs
public interface IPermissionCacheService
{
    Task InvalidateUserCacheAsync(int userId);
    Task InvalidateAllUserCachesAsync();
    Task<List<string>> GetUserPermissionsAsync(int userId);
}

public class PermissionCacheService : IPermissionCacheService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(1); // 5 dakikadan 1 dakikaya düşürüldü

    public async Task InvalidateUserCacheAsync(int userId)
    {
        var cacheKey = $"user_permissions_{userId}";
        _cache.Remove(cacheKey);

        // WebSocket ile frontend'e bildir
        await _hubContext.Clients.User(userId.ToString())
            .SendAsync("PermissionsChanged");
    }
}
```

### 4. Session Invalidation on Password Change

```csharp
// Backend: SessionInvalidationService.cs
public class SessionInvalidationService
{
    public async Task InvalidateUserSessionsAsync(int userId)
    {
        // Kullanıcının tüm refresh token'larını iptal et
        await _tokenRepository.RevokeAllUserTokensAsync(userId);

        // WebSocket ile zorla logout bildirimi
        await _hubContext.Clients.User(userId.ToString())
            .SendAsync("ForceLogout", "Şifreniz değiştirildi. Lütfen tekrar giriş yapın.");
    }
}
```

### 5. Responsive Table Component

```jsx
// frontend/src/components/ResponsiveTable.jsx
/**
 * Mobil uyumlu tablo bileşeni
 * 768px altında kart görünümüne geçer
 */
const ResponsiveTable = ({ columns, data, mobileCardRenderer }) => {
  const isMobile = useMediaQuery("(max-width: 768px)");

  if (isMobile && mobileCardRenderer) {
    return (
      <div className="card-list">
        {data.map((item, index) => mobileCardRenderer(item, index))}
      </div>
    );
  }

  return (
    <table className="table table-responsive">
      {/* Normal tablo render */}
    </table>
  );
};
```

### 6. User Search and Filter Component

```jsx
// frontend/src/components/UserSearchFilter.jsx
const UserSearchFilter = ({ onFilterChange }) => {
  const [search, setSearch] = useState("");
  const [roleFilter, setRoleFilter] = useState("all");
  const [statusFilter, setStatusFilter] = useState("all");

  // Debounced search
  const debouncedSearch = useDebounce(search, 300);

  useEffect(() => {
    onFilterChange({
      search: debouncedSearch,
      role: roleFilter,
      status: statusFilter,
    });
  }, [debouncedSearch, roleFilter, statusFilter]);

  return (
    <div className="filter-toolbar">
      <input
        type="text"
        placeholder="Ad, soyad veya email ara..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />
      <select
        value={roleFilter}
        onChange={(e) => setRoleFilter(e.target.value)}
      >
        <option value="all">Tüm Roller</option>
        {/* Rol seçenekleri */}
      </select>
      <select
        value={statusFilter}
        onChange={(e) => setStatusFilter(e.target.value)}
      >
        <option value="all">Tüm Durumlar</option>
        <option value="active">Aktif</option>
        <option value="inactive">Pasif</option>
      </select>
    </div>
  );
};
```

## Data Models

### Updated Seed Data Structure

```sql
-- Düzeltilmiş rol-izin eşleştirmeleri
-- StoreManager için eklenen izinler
INSERT INTO RolePermissions (RoleId, PermissionId) VALUES
  (@StoreManagerRoleId, (SELECT Id FROM Permissions WHERE Name = 'users.view')),
  (@StoreManagerRoleId, (SELECT Id FROM Permissions WHERE Name = 'couriers.view'));

-- CustomerSupport için eklenen izinler
INSERT INTO RolePermissions (RoleId, PermissionId) VALUES
  (@CustomerSupportRoleId, (SELECT Id FROM Permissions WHERE Name = 'reports.view'));

-- Logistics için eklenen izinler
INSERT INTO RolePermissions (RoleId, PermissionId) VALUES
  (@LogisticsRoleId, (SELECT Id FROM Permissions WHERE Name = 'reports.weight'));
```

## Correctness Properties

_A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees._

### Property 1: Route-Permission Consistency

_For any_ admin route defined in App.js, the route SHALL have a corresponding requiredPermission that matches the permission checked in AdminLayout menu visibility.

**Validates: Requirements 1.11**

### Property 2: Permission Denial Redirect

_For any_ user without the required permission attempting to access a protected route, the system SHALL redirect to the forbidden page or fallback path within 100ms.

**Validates: Requirements 1.10**

### Property 3: Cache Invalidation Immediacy

_For any_ role change operation, the affected user's permission cache SHALL be invalidated within 1 second of the change.

**Validates: Requirements 5.1**

### Property 4: Session Termination on Password Change

_For any_ password change operation performed by an admin, all active sessions of the affected user SHALL be terminated immediately.

**Validates: Requirements 5.2, 5.4**

### Property 5: Mobile Responsive Breakpoint

_For any_ screen width below 768px, table components SHALL render in card/list view instead of traditional table layout.

**Validates: Requirements 6.4**

### Property 6: Search Filter Accuracy

_For any_ search query in the user list, the results SHALL contain only users whose name or email contains the search term (case-insensitive).

**Validates: Requirements 7.1**

### Property 7: Pagination Threshold

_For any_ user list with more than 20 items, the UI SHALL display pagination controls and limit visible items per page.

**Validates: Requirements 7.4**

### Property 8: Backend Permission Attribute Coverage

_For any_ admin controller endpoint, there SHALL exist both [Authorize] and [HasPermission] attributes ensuring dual-layer security.

**Validates: Requirements 3.4**

## Error Handling

### Turkish Error Messages Map

```javascript
// frontend/src/utils/errorMessages.js
export const ERROR_MESSAGES = {
  // Identity Errors
  PasswordRequiresNonAlphanumeric: "Şifre en az bir özel karakter içermelidir",
  PasswordRequiresDigit: "Şifre en az bir rakam içermelidir",
  PasswordRequiresUpper: "Şifre en az bir büyük harf içermelidir",
  PasswordRequiresLower: "Şifre en az bir küçük harf içermelidir",
  PasswordTooShort: "Şifre en az 6 karakter olmalıdır",
  DuplicateEmail: "Bu email adresi zaten kullanılıyor",
  DuplicateUserName: "Bu kullanıcı adı zaten kullanılıyor",
  InvalidToken: "Geçersiz veya süresi dolmuş token",
  UserNotFound: "Kullanıcı bulunamadı",
  InvalidCredentials: "Geçersiz email veya şifre",

  // Permission Errors
  AccessDenied: "Bu işlem için yetkiniz bulunmamaktadır",
  Forbidden: "Erişim reddedildi",
  Unauthorized: "Oturum açmanız gerekmektedir",

  // Generic Errors
  ServerError: "Sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin",
  NetworkError: "Bağlantı hatası. İnternet bağlantınızı kontrol edin",
  ValidationError: "Girilen bilgilerde hata var",
};

export const translateError = (error) => {
  if (typeof error === "string") {
    return ERROR_MESSAGES[error] || error;
  }
  if (error?.code) {
    return ERROR_MESSAGES[error.code] || error.message || "Bilinmeyen hata";
  }
  return "Bilinmeyen hata oluştu";
};
```

### Error Boundary Component

```jsx
// frontend/src/components/ErrorBoundary.jsx
class AdminErrorBoundary extends React.Component {
  state = { hasError: false, error: null };

  static getDerivedStateFromError(error) {
    return { hasError: true, error };
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="error-container">
          <h2>Bir hata oluştu</h2>
          <p>{translateError(this.state.error)}</p>
          <button onClick={() => window.location.reload()}>
            Sayfayı Yenile
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}
```

## Testing Strategy

### Unit Tests

1. **Route Permission Tests**: Her route için izin kontrolü doğrulaması
2. **Cache Invalidation Tests**: Rol değişikliğinde cache temizleme testi
3. **Error Translation Tests**: Tüm hata mesajlarının Türkçe çevirisi
4. **Responsive Breakpoint Tests**: 768px altında kart görünümü testi

### Property-Based Tests

1. **Route-Menu Consistency**: Tüm route'ların menü ile tutarlılığı
2. **Permission Denial**: İzinsiz erişim denemelerinin engellenmesi
3. **Search Accuracy**: Arama sonuçlarının doğruluğu
4. **Pagination Correctness**: Sayfalama mantığının doğruluğu

### Integration Tests

1. **End-to-End Permission Flow**: Login → Permission Check → Page Access
2. **Role Change Flow**: Admin rol değiştirir → Cache temizlenir → Kullanıcı yeni izinleri görür
3. **Password Change Flow**: Admin şifre değiştirir → Kullanıcı logout olur

### Test Framework

- Frontend: Jest + React Testing Library + fast-check (PBT)
- Backend: xUnit + Moq + FluentAssertions
- E2E: Playwright veya Cypress
