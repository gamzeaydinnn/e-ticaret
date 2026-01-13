// src/guards/AdminGuard.js
// =============================================================================
// Admin Guard - Yetki Kontrollü Route Koruması
// =============================================================================
// Bu component, admin rotalarını korur ve izin bazlı erişim kontrolü sağlar.
// Hem rol hem de spesifik izin kontrolleri yapılabilir.
// =============================================================================

import React from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";

// Admin rolleri listesi
const ADMIN_ROLES = [
  "Admin",
  "SuperAdmin",
  "StoreManager",
  "CustomerSupport",
  "Logistics",
];

/**
 * Admin yetkisi kontrolü yapan guard component
 *
 * @param {Object} props
 * @param {React.ReactNode} props.children - Korunan içerik
 * @param {string|string[]} props.requiredPermission - Gerekli izin(ler)
 * @param {boolean} props.requireAll - Tüm izinler mi gerekli? (default: false = herhangi biri)
 * @param {string} props.fallbackPath - Yetkisiz durumda yönlendirilecek yol
 */
export const AdminGuard = ({
  children,
  requiredPermission = null,
  requireAll = false,
  fallbackPath = null,
}) => {
  const {
    user,
    loading,
    setUser,
    hasPermission,
    hasAnyPermission,
    hasAllPermissions,
    canAccessAdminPanel,
    permissionsLoading, // İzinler yüklenirken
    permissions: contextPermissions, // Context'teki izin listesi
    loadUserPermissions, // İzin yükleme fonksiyonu
  } = useAuth();
  const location = useLocation();

  // ============================================================================
  // YÜKLEME DURUMU - Hem user hem de permissions yüklenene kadar bekle
  // Race condition düzeltmesi: İzinler yüklenmeden izin kontrolü yapılmamalı
  // ============================================================================
  if (loading || permissionsLoading) {
    return (
      <div
        className="d-flex justify-content-center align-items-center"
        style={{ minHeight: "100vh" }}
      >
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
      </div>
    );
  }

  // Kullanıcı yoksa localStorage'dan admin oturumu toparlamayı dene
  if (!user) {
    try {
      const storedUser = localStorage.getItem("user");
      const token =
        localStorage.getItem("authToken") ||
        localStorage.getItem("adminToken") ||
        localStorage.getItem("token");
      if (storedUser && token) {
        const parsed = JSON.parse(storedUser);
        if (parsed && (parsed.isAdmin || ADMIN_ROLES.includes(parsed.role))) {
          setUser?.(parsed);
          return children;
        }
      }
    } catch (e) {
      console.error("User restore error:", e);
    }
    return <Navigate to="/admin/login" state={{ from: location }} replace />;
  }

  // Admin yetkisi yoksa ana sayfaya yönlendir
  const isAdmin = user.isAdmin || ADMIN_ROLES.includes(user.role);
  if (!isAdmin && !canAccessAdminPanel?.()) {
    return <Navigate to="/" replace />;
  }

  // Spesifik izin kontrolü
  if (requiredPermission) {
    const reqPerms = Array.isArray(requiredPermission)
      ? requiredPermission
      : [requiredPermission];

    // SuperAdmin her şeye erişebilir
    if (user.role !== "SuperAdmin") {
      let hasAccess = false;

      // ============================================================================
      // İZİN KONTROLÜ - Önce context, sonra localStorage fallback
      // Context permissions güncelse onu kullan, yoksa cache'den kontrol et
      // ============================================================================

      // Context'teki izinler var mı kontrol et
      const hasContextPermissions =
        Array.isArray(contextPermissions) && contextPermissions.length > 0;

      if (hasContextPermissions) {
        // Context'teki helper fonksiyonları kullan
        if (requireAll) {
          hasAccess = hasAllPermissions?.(...reqPerms) ?? false;
        } else {
          hasAccess = hasAnyPermission?.(...reqPerms) ?? false;
        }
      }

      // Context'te izin yoksa veya bulunamadıysa, localStorage'dan kontrol et
      if (!hasAccess) {
        try {
          const cachedPermissions = localStorage.getItem("userPermissions");
          const cachedRole = localStorage.getItem("permissionsCacheRole");

          // Sadece aynı rol için cache'i kullan
          if (cachedPermissions && cachedRole === user.role) {
            const cached = JSON.parse(cachedPermissions);
            if (Array.isArray(cached) && cached.length > 0) {
              if (requireAll) {
                hasAccess = reqPerms.every((p) => cached.includes(p));
              } else {
                hasAccess = reqPerms.some((p) => cached.includes(p));
              }

              // Cache'de bulunduysa, context'e de yükle (sync için)
              // NOT: Bu sadece fallback, normalde login'de yüklenmeli
              console.log(
                "[AdminGuard] Cache'den izin bulundu, context sync bekleniyor"
              );
            }
          }
        } catch (e) {
          console.warn("[AdminGuard] Cache okuma hatası:", e);
        }
      }

      // ============================================================================
      // DEBUG LOG - İzin kontrolü sonucunu logla
      // ============================================================================
      console.log("[AdminGuard] İzin kontrolü:", {
        user: user.email || user.name,
        role: user.role,
        requiredPermission: reqPerms,
        requireAll,
        hasAccess,
        contextPermsCount: contextPermissions?.length || 0,
      });

      if (!hasAccess) {
        // Yetkisiz - fallback path'e veya access denied sayfasına yönlendir
        const redirectPath = fallbackPath || "/admin/access-denied";
        return (
          <Navigate
            to={redirectPath}
            state={{
              from: location,
              requiredPermission: reqPerms,
              message: "Bu sayfaya erişim yetkiniz bulunmamaktadır.",
            }}
            replace
          />
        );
      }
    }
  }

  // Admin yetkisi varsa içeriği göster
  return children;
};

/**
 * İzin bazlı conditional render wrapper
 *
 * @param {Object} props
 * @param {string|string[]} props.permission - Gerekli izin(ler)
 * @param {boolean} props.requireAll - Tüm izinler mi gerekli?
 * @param {React.ReactNode} props.children - Yetkili ise gösterilecek içerik
 * @param {React.ReactNode} props.fallback - Yetkisiz ise gösterilecek içerik (opsiyonel)
 */
export const PermissionGuard = ({
  permission,
  requireAll = false,
  children,
  fallback = null,
}) => {
  const { user, hasPermission, hasAnyPermission, hasAllPermissions } =
    useAuth();

  if (!user) return fallback;

  // SuperAdmin her şeyi görebilir
  if (user.role === "SuperAdmin") return children;

  const permissions = Array.isArray(permission) ? permission : [permission];

  let hasAccess = false;
  if (requireAll) {
    hasAccess = hasAllPermissions?.(...permissions) ?? false;
  } else {
    // Tek izin için hasPermission, birden fazla için hasAnyPermission
    if (permissions.length === 1) {
      hasAccess = hasPermission?.(permissions[0]) ?? false;
    } else {
      hasAccess = hasAnyPermission?.(...permissions) ?? false;
    }
  }

  return hasAccess ? children : fallback;
};

/**
 * Admin login kontrolü - giriş yapmışsa admin paneline yönlendir
 */
export const AdminLoginGuard = ({ children }) => {
  const { user, loading } = useAuth();

  if (loading) {
    return (
      <div
        className="d-flex justify-content-center align-items-center"
        style={{ minHeight: "100vh" }}
      >
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
      </div>
    );
  }

  // Admin girişi yapmışsa dashboard'a yönlendir
  if (user && (user.isAdmin || ADMIN_ROLES.includes(user.role))) {
    return <Navigate to="/admin/dashboard" replace />;
  }

  return children;
};

/**
 * Rol bazlı guard - Belirli roller için erişim kontrolü
 *
 * @param {Object} props
 * @param {string|string[]} props.allowedRoles - İzin verilen roller
 * @param {React.ReactNode} props.children - Korunan içerik
 * @param {string} props.fallbackPath - Yetkisiz durumda yönlendirilecek yol
 */
export const RoleGuard = ({
  allowedRoles,
  children,
  fallbackPath = "/admin/access-denied",
}) => {
  const { user, loading } = useAuth();
  const location = useLocation();

  if (loading) {
    return (
      <div
        className="d-flex justify-content-center align-items-center"
        style={{ minHeight: "100vh" }}
      >
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
      </div>
    );
  }

  if (!user) {
    return <Navigate to="/admin/login" state={{ from: location }} replace />;
  }

  const roles = Array.isArray(allowedRoles) ? allowedRoles : [allowedRoles];

  // SuperAdmin her role erişebilir
  if (user.role === "SuperAdmin" || roles.includes(user.role)) {
    return children;
  }

  return (
    <Navigate
      to={fallbackPath}
      state={{
        from: location,
        message: `Bu sayfaya erişmek için şu rollerden birine sahip olmanız gerekmektedir: ${roles.join(
          ", "
        )}`,
      }}
      replace
    />
  );
};

export default AdminGuard;
