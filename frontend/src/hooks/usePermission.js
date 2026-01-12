// =============================================================================
// usePermission.js - İzin Kontrol Hook'u
// =============================================================================
// Bu hook, componentler içinde kolayca izin kontrolü yapmayı sağlar.
// AuthContext'i wrap eder ve ek utility fonksiyonları sunar.
//
// Kullanım:
// const { hasPermission, can, cannot } = usePermission();
// const canCreateProduct = can('products.create');
// =============================================================================

import { useCallback, useMemo } from "react";
import { useAuth } from "../contexts/AuthContext";
import { PERMISSIONS } from "../services/permissionService";

/**
 * İzin kontrol hook'u
 * Component içinde kolayca izin kontrolü yapmayı sağlar.
 *
 * @returns {Object} - İzin kontrol fonksiyonları ve state
 */
export const usePermission = () => {
  const {
    user,
    permissions,
    permissionsLoading: loading,
    permissionsError: error,
    hasPermission,
    hasAnyPermission,
    hasAllPermissions,
    canAccessAdminPanel,
    refreshPermissions,
  } = useAuth();

  // Derived state
  const role = user?.role || null;
  const isSuperAdmin = role === "SuperAdmin";

  /**
   * Modül erişim kontrolü
   */
  const canAccessModule = useCallback(
    (moduleName) => {
      if (isSuperAdmin) return true;
      if (!permissions || permissions.length === 0) return false;
      return permissions.some((p) => p.startsWith(`${moduleName}.`));
    },
    [permissions, isSuperAdmin]
  );

  /**
   * Modül izinlerini getir
   */
  const getModulePermissions = useCallback(
    (moduleName) => {
      if (!permissions || permissions.length === 0) return [];
      return permissions.filter((p) => p.startsWith(`${moduleName}.`));
    },
    [permissions]
  );

  /**
   * Kısa syntax için 'can' alias'ı
   *
   * @param {string} permission - Kontrol edilecek izin
   * @returns {boolean} - İzin durumu
   */
  const can = useCallback(
    (permission) => {
      return hasPermission?.(permission) ?? false;
    },
    [hasPermission]
  );

  /**
   * Negatif kontrol için 'cannot' fonksiyonu
   *
   * @param {string} permission - Kontrol edilecek izin
   * @returns {boolean} - İzin yoksa true
   */
  const cannot = useCallback(
    (permission) => {
      return !hasPermission?.(permission);
    },
    [hasPermission]
  );

  /**
   * Birden fazla izinden herhangi birine sahip mi?
   *
   * @param {string[]} permissionList - İzin listesi
   * @returns {boolean} - Herhangi birine sahipse true
   */
  const canAny = useCallback(
    (permissionList) => {
      return hasAnyPermission?.(...permissionList) ?? false;
    },
    [hasAnyPermission]
  );

  /**
   * Tüm izinlere sahip mi?
   *
   * @param {string[]} permissionList - İzin listesi
   * @returns {boolean} - Tümüne sahipse true
   */
  const canAll = useCallback(
    (permissionList) => {
      return hasAllPermissions?.(...permissionList) ?? false;
    },
    [hasAllPermissions]
  );

  // =========================================================================
  // Modül bazlı kısa yollar (Sık kullanılan kontroller)
  // =========================================================================

  /**
   * Dashboard erişim kontrolleri
   */
  const dashboardPermissions = useMemo(
    () => ({
      canView: hasPermission?.(PERMISSIONS.DASHBOARD_VIEW) ?? false,
      canViewStats: hasPermission?.(PERMISSIONS.DASHBOARD_STATISTICS) ?? false,
      canViewRevenue: hasPermission?.(PERMISSIONS.DASHBOARD_REVENUE) ?? false,
    }),
    [hasPermission]
  );

  /**
   * Ürün yönetimi izin kontrolleri
   */
  const productPermissions = useMemo(
    () => ({
      canView: hasPermission?.(PERMISSIONS.PRODUCTS_VIEW) ?? false,
      canCreate: hasPermission?.(PERMISSIONS.PRODUCTS_CREATE) ?? false,
      canUpdate: hasPermission?.(PERMISSIONS.PRODUCTS_UPDATE) ?? false,
      canDelete: hasPermission?.(PERMISSIONS.PRODUCTS_DELETE) ?? false,
      canManageStock: hasPermission?.(PERMISSIONS.PRODUCTS_STOCK) ?? false,
      canManagePricing: hasPermission?.(PERMISSIONS.PRODUCTS_PRICING) ?? false,
      canImport: hasPermission?.(PERMISSIONS.PRODUCTS_IMPORT) ?? false,
      canExport: hasPermission?.(PERMISSIONS.PRODUCTS_EXPORT) ?? false,
      // Herhangi bir ürün yetkisi var mı?
      hasAny: canAccessModule("products"),
    }),
    [hasPermission, canAccessModule]
  );

  /**
   * Kategori yönetimi izin kontrolleri
   */
  const categoryPermissions = useMemo(
    () => ({
      canView: hasPermission?.(PERMISSIONS.CATEGORIES_VIEW) ?? false,
      canCreate: hasPermission?.(PERMISSIONS.CATEGORIES_CREATE) ?? false,
      canUpdate: hasPermission?.(PERMISSIONS.CATEGORIES_UPDATE) ?? false,
      canDelete: hasPermission?.(PERMISSIONS.CATEGORIES_DELETE) ?? false,
      hasAny: canAccessModule("categories"),
    }),
    [hasPermission, canAccessModule]
  );

  /**
   * Sipariş yönetimi izin kontrolleri
   */
  const orderPermissions = useMemo(
    () => ({
      canView: hasPermission?.(PERMISSIONS.ORDERS_VIEW) ?? false,
      canViewDetails: hasPermission?.(PERMISSIONS.ORDERS_DETAILS) ?? false,
      canUpdateStatus: hasPermission?.(PERMISSIONS.ORDERS_STATUS) ?? false,
      canCancel: hasPermission?.(PERMISSIONS.ORDERS_CANCEL) ?? false,
      canRefund: hasPermission?.(PERMISSIONS.ORDERS_REFUND) ?? false,
      canAssignCourier:
        hasPermission?.(PERMISSIONS.ORDERS_ASSIGN_COURIER) ?? false,
      canViewCustomerInfo:
        hasPermission?.(PERMISSIONS.ORDERS_CUSTOMER_INFO) ?? false,
      canExport: hasPermission?.(PERMISSIONS.ORDERS_EXPORT) ?? false,
      hasAny: canAccessModule("orders"),
    }),
    [hasPermission, canAccessModule]
  );

  /**
   * Kullanıcı yönetimi izin kontrolleri
   */
  const userPermissions = useMemo(
    () => ({
      canView: hasPermission?.(PERMISSIONS.USERS_VIEW) ?? false,
      canCreate: hasPermission?.(PERMISSIONS.USERS_CREATE) ?? false,
      canUpdate: hasPermission?.(PERMISSIONS.USERS_UPDATE) ?? false,
      canDelete: hasPermission?.(PERMISSIONS.USERS_DELETE) ?? false,
      canManageRoles: hasPermission?.(PERMISSIONS.USERS_ROLES) ?? false,
      canViewSensitive: hasPermission?.(PERMISSIONS.USERS_SENSITIVE) ?? false,
      canExport: hasPermission?.(PERMISSIONS.USERS_EXPORT) ?? false,
      hasAny: canAccessModule("users"),
    }),
    [hasPermission, canAccessModule]
  );

  /**
   * Rol yönetimi izin kontrolleri
   */
  const rolePermissions = useMemo(
    () => ({
      canView: hasPermission?.(PERMISSIONS.ROLES_VIEW) ?? false,
      canCreate: hasPermission?.(PERMISSIONS.ROLES_CREATE) ?? false,
      canUpdate: hasPermission?.(PERMISSIONS.ROLES_UPDATE) ?? false,
      canDelete: hasPermission?.(PERMISSIONS.ROLES_DELETE) ?? false,
      canManagePermissions:
        hasPermission?.(PERMISSIONS.ROLES_PERMISSIONS) ?? false,
      hasAny: canAccessModule("roles"),
    }),
    [hasPermission, canAccessModule]
  );

  /**
   * Kampanya yönetimi izin kontrolleri
   */
  const campaignPermissions = useMemo(
    () => ({
      canView: hasPermission?.(PERMISSIONS.CAMPAIGNS_VIEW) ?? false,
      canCreate: hasPermission?.(PERMISSIONS.CAMPAIGNS_CREATE) ?? false,
      canUpdate: hasPermission?.(PERMISSIONS.CAMPAIGNS_UPDATE) ?? false,
      canDelete: hasPermission?.(PERMISSIONS.CAMPAIGNS_DELETE) ?? false,
      hasAny: canAccessModule("campaigns"),
    }),
    [hasPermission, canAccessModule]
  );

  /**
   * Kurye yönetimi izin kontrolleri
   */
  const courierPermissions = useMemo(
    () => ({
      canView: hasPermission?.(PERMISSIONS.COURIERS_VIEW) ?? false,
      canCreate: hasPermission?.(PERMISSIONS.COURIERS_CREATE) ?? false,
      canUpdate: hasPermission?.(PERMISSIONS.COURIERS_UPDATE) ?? false,
      canDelete: hasPermission?.(PERMISSIONS.COURIERS_DELETE) ?? false,
      canAssign: hasPermission?.(PERMISSIONS.COURIERS_ASSIGN) ?? false,
      hasAny: canAccessModule("couriers"),
    }),
    [hasPermission, canAccessModule]
  );

  /**
   * Rapor izin kontrolleri
   */
  const reportPermissions = useMemo(
    () => ({
      canView: hasPermission?.(PERMISSIONS.REPORTS_VIEW) ?? false,
      canViewSales: hasPermission?.(PERMISSIONS.REPORTS_SALES) ?? false,
      canViewInventory: hasPermission?.(PERMISSIONS.REPORTS_INVENTORY) ?? false,
      canViewCustomers: hasPermission?.(PERMISSIONS.REPORTS_CUSTOMERS) ?? false,
      canExport: hasPermission?.(PERMISSIONS.REPORTS_EXPORT) ?? false,
      hasAny: canAccessModule("reports"),
    }),
    [hasPermission, canAccessModule]
  );

  /**
   * Banner yönetimi izin kontrolleri
   */
  const bannerPermissions = useMemo(
    () => ({
      canView: hasPermission?.(PERMISSIONS.BANNERS_VIEW) ?? false,
      canCreate: hasPermission?.(PERMISSIONS.BANNERS_CREATE) ?? false,
      canUpdate: hasPermission?.(PERMISSIONS.BANNERS_UPDATE) ?? false,
      canDelete: hasPermission?.(PERMISSIONS.BANNERS_DELETE) ?? false,
      hasAny: canAccessModule("banners"),
    }),
    [hasPermission, canAccessModule]
  );

  /**
   * Ayarlar izin kontrolleri
   */
  const settingsPermissions = useMemo(
    () => ({
      canView: hasPermission?.(PERMISSIONS.SETTINGS_VIEW) ?? false,
      canUpdate: hasPermission?.(PERMISSIONS.SETTINGS_UPDATE) ?? false,
      canManageSystem: hasPermission?.(PERMISSIONS.SETTINGS_SYSTEM) ?? false,
      hasAny: canAccessModule("settings"),
    }),
    [hasPermission, canAccessModule]
  );

  /**
   * Log izin kontrolleri
   */
  const logPermissions = useMemo(
    () => ({
      canView: hasPermission?.(PERMISSIONS.LOGS_VIEW) ?? false,
      canViewAudit: hasPermission?.(PERMISSIONS.LOGS_AUDIT) ?? false,
      canViewError: hasPermission?.(PERMISSIONS.LOGS_ERROR) ?? false,
      canExport: hasPermission?.(PERMISSIONS.LOGS_EXPORT) ?? false,
      hasAny: canAccessModule("logs"),
    }),
    [hasPermission, canAccessModule]
  );

  return {
    // Core state
    permissions,
    role,
    isSuperAdmin,
    canAccessAdminPanel,
    loading,
    error,

    // Core functions
    hasPermission,
    can,
    cannot,
    canAny,
    canAll,
    canAccessModule,
    getModulePermissions,

    // Module-specific permissions (memoized)
    dashboardPermissions,
    productPermissions,
    categoryPermissions,
    orderPermissions,
    userPermissions,
    rolePermissions,
    campaignPermissions,
    courierPermissions,
    reportPermissions,
    bannerPermissions,
    settingsPermissions,
    logPermissions,

    // Permission constants for easy access
    PERMISSIONS,
  };
};

export default usePermission;
