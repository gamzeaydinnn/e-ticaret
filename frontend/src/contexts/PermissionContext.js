// =============================================================================
// PermissionContext.js - RBAC İzin Yönetimi Context
// =============================================================================
// Bu context, kullanıcı izinlerini uygulama genelinde yönetir.
// Login sonrası izinler yüklenir ve tüm componentler tarafından kullanılabilir.
//
// Kullanım:
// const { hasPermission, hasAnyPermission, permissions } = usePermissions();
// if (hasPermission('products.create')) { ... }
// =============================================================================

import {
  createContext,
  useContext,
  useState,
  useCallback,
  useEffect,
  useMemo,
} from "react";
import { permissionService } from "../services/permissionService";
import { useAuth } from "./AuthContext";

// Context oluştur
const PermissionContext = createContext(null);

/**
 * Permission Context Hook
 * İzin kontrolü için kullanılır.
 *
 * @returns {Object} - Permission context değerleri
 * @throws {Error} - Provider dışında kullanılırsa hata fırlatır
 */
export const usePermissions = () => {
  const context = useContext(PermissionContext);
  if (!context) {
    throw new Error("usePermissions must be used within a PermissionProvider");
  }
  return context;
};

/**
 * Permission Provider Component
 * Uygulama genelinde izin state'ini yönetir.
 */
export const PermissionProvider = ({ children }) => {
  // State tanımlamaları
  const [permissions, setPermissions] = useState([]);
  const [role, setRole] = useState(null);
  const [isSuperAdmin, setIsSuperAdmin] = useState(false);
  const [canAccessAdminPanel, setCanAccessAdminPanel] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Auth context'ten kullanıcı bilgisini al
  const { user } = useAuth();

  /**
   * Kullanıcı izinlerini backend'den yükler.
   * Login sonrası veya sayfa yenilemede çağrılır.
   */
  const loadPermissions = useCallback(async () => {
    // Kullanıcı yoksa izinleri temizle
    if (!user) {
      setPermissions([]);
      setRole(null);
      setIsSuperAdmin(false);
      setCanAccessAdminPanel(false);
      setLoading(false);
      setError(null);
      return;
    }

    try {
      setLoading(true);
      setError(null);

      const data = await permissionService.getMyPermissions();

      setPermissions(data.permissions || []);
      setRole(data.role || user.role);
      setIsSuperAdmin(data.isSuperAdmin || false);
      setCanAccessAdminPanel(data.canAccessAdminPanel || false);

      // LocalStorage'a da kaydet (offline/hızlı erişim için)
      try {
        localStorage.setItem(
          "userPermissions",
          JSON.stringify({
            permissions: data.permissions || [],
            role: data.role || user.role,
            isSuperAdmin: data.isSuperAdmin || false,
            canAccessAdminPanel: data.canAccessAdminPanel || false,
            timestamp: Date.now(),
          })
        );
      } catch (e) {
        console.warn("[PermissionContext] localStorage kaydetme hatası:", e);
      }
    } catch (err) {
      console.error("[PermissionContext] İzinler yüklenirken hata:", err);
      setError("İzinler yüklenirken bir hata oluştu.");

      // Hata durumunda localStorage'dan yükle (fallback)
      try {
        const cached = localStorage.getItem("userPermissions");
        if (cached) {
          const parsed = JSON.parse(cached);
          // 24 saatten eski değilse kullan
          if (parsed.timestamp && Date.now() - parsed.timestamp < 86400000) {
            setPermissions(parsed.permissions || []);
            setRole(parsed.role);
            setIsSuperAdmin(parsed.isSuperAdmin || false);
            setCanAccessAdminPanel(parsed.canAccessAdminPanel || false);
          }
        }
      } catch (e) {
        console.warn("[PermissionContext] localStorage okuma hatası:", e);
      }
    } finally {
      setLoading(false);
    }
  }, [user]);

  /**
   * Kullanıcı değiştiğinde izinleri yeniden yükle
   */
  useEffect(() => {
    loadPermissions();
  }, [loadPermissions]);

  /**
   * İzinleri temizler (logout için)
   */
  const clearPermissions = useCallback(() => {
    setPermissions([]);
    setRole(null);
    setIsSuperAdmin(false);
    setCanAccessAdminPanel(false);
    setError(null);

    try {
      localStorage.removeItem("userPermissions");
    } catch (e) {
      console.warn("[PermissionContext] localStorage temizleme hatası:", e);
    }
  }, []);

  /**
   * Kullanıcının belirli bir izne sahip olup olmadığını kontrol eder.
   * SuperAdmin her zaman true döner.
   *
   * @param {string} permission - Kontrol edilecek izin (örn: "products.create")
   * @returns {boolean} - İzin durumu
   */
  const hasPermission = useCallback(
    (permission) => {
      // SuperAdmin her şeye erişebilir
      if (isSuperAdmin) return true;

      // İzin kontrolü
      return permissions.includes(permission);
    },
    [permissions, isSuperAdmin]
  );

  /**
   * Kullanıcının belirtilen izinlerden herhangi birine sahip olup olmadığını kontrol eder.
   * OR mantığı ile çalışır.
   *
   * @param {...string} permissionList - Kontrol edilecek izinler
   * @returns {boolean} - Herhangi birine sahipse true
   */
  const hasAnyPermission = useCallback(
    (...permissionList) => {
      // SuperAdmin her şeye erişebilir
      if (isSuperAdmin) return true;

      // En az birine sahip mi?
      return permissionList.some((p) => permissions.includes(p));
    },
    [permissions, isSuperAdmin]
  );

  /**
   * Kullanıcının belirtilen tüm izinlere sahip olup olmadığını kontrol eder.
   * AND mantığı ile çalışır.
   *
   * @param {...string} permissionList - Kontrol edilecek izinler
   * @returns {boolean} - Tümüne sahipse true
   */
  const hasAllPermissions = useCallback(
    (...permissionList) => {
      // SuperAdmin her şeye erişebilir
      if (isSuperAdmin) return true;

      // Hepsine sahip mi?
      return permissionList.every((p) => permissions.includes(p));
    },
    [permissions, isSuperAdmin]
  );

  /**
   * Belirli bir modüldeki izinleri filtreler.
   *
   * @param {string} module - Modül adı (örn: "products", "orders")
   * @returns {string[]} - Modüldeki izinler
   */
  const getModulePermissions = useCallback(
    (module) => {
      const prefix = `${module}.`;
      return permissions.filter((p) => p.startsWith(prefix));
    },
    [permissions]
  );

  /**
   * Kullanıcının bir modüle erişip erişemeyeceğini kontrol eder.
   * Modüldeki herhangi bir izne sahipse true döner.
   *
   * @param {string} module - Modül adı
   * @returns {boolean} - Erişim durumu
   */
  const canAccessModule = useCallback(
    (module) => {
      if (isSuperAdmin) return true;

      const prefix = `${module}.`;
      return permissions.some((p) => p.startsWith(prefix));
    },
    [permissions, isSuperAdmin]
  );

  // Context değeri (memoized performans için)
  const contextValue = useMemo(
    () => ({
      // State
      permissions,
      role,
      isSuperAdmin,
      canAccessAdminPanel,
      loading,
      error,

      // Actions
      loadPermissions,
      clearPermissions,

      // Permission checks
      hasPermission,
      hasAnyPermission,
      hasAllPermissions,
      getModulePermissions,
      canAccessModule,
    }),
    [
      permissions,
      role,
      isSuperAdmin,
      canAccessAdminPanel,
      loading,
      error,
      loadPermissions,
      clearPermissions,
      hasPermission,
      hasAnyPermission,
      hasAllPermissions,
      getModulePermissions,
      canAccessModule,
    ]
  );

  return (
    <PermissionContext.Provider value={contextValue}>
      {children}
    </PermissionContext.Provider>
  );
};

export default PermissionContext;
