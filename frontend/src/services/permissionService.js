// =============================================================================
// permissionService.js - RBAC İzin Yönetimi API Servisi
// =============================================================================
// Bu servis, backend'deki RBAC (Role-Based Access Control) sistemine
// API çağrıları yapar. Kullanıcı izinleri, rol yönetimi ve izin
// kontrolleri için kullanılır.
// =============================================================================

import api from "./api";

/**
 * Permission API Servisi
 * Tüm izin ve rol yönetimi işlemlerini merkezi olarak yönetir.
 */
export const permissionService = {
  // =========================================================================
  // KULLANICI İZİN İŞLEMLERİ
  // =========================================================================

  /**
   * Mevcut kullanıcının tüm izinlerini getirir.
   * Login sonrası çağrılmalı ve PermissionContext'e yüklenmelidir.
   *
   * @returns {Promise<Object>} - { permissions: string[], role: string, isSuperAdmin: boolean }
   */
  getMyPermissions: async () => {
    try {
      const response = await api.get("/api/auth/permissions");
      return (
        response.data?.data ||
        response.data || {
          permissions: [],
          role: null,
          isSuperAdmin: false,
          canAccessAdminPanel: false,
        }
      );
    } catch (error) {
      console.error("[PermissionService] getMyPermissions error:", error);
      // Hata durumunda boş izin seti döndür - kullanıcı hiçbir şeye erişemez
      return {
        permissions: [],
        role: null,
        isSuperAdmin: false,
        canAccessAdminPanel: false,
      };
    }
  },

  /**
   * Kullanıcının belirli bir izne sahip olup olmadığını kontrol eder.
   * Bu method backend'e sorgu yapar - local kontrol için usePermission hook'u kullanın.
   *
   * @param {string} permission - Kontrol edilecek izin (örn: "products.create")
   * @returns {Promise<boolean>} - İzin durumu
   */
  checkPermission: async (permission) => {
    try {
      const response = await api.get(`/api/auth/check-permission`, {
        params: { permission },
      });
      return response.data?.hasPermission || false;
    } catch (error) {
      console.error("[PermissionService] checkPermission error:", error);
      return false;
    }
  },

  // =========================================================================
  // ADMIN - İZİN YÖNETİMİ (SuperAdmin Only)
  // =========================================================================

  /**
   * Sistemdeki tüm izinleri listeler.
   *
   * @param {boolean} groupByModule - True ise modüle göre gruplar
   * @returns {Promise<Array>} - İzin listesi
   */
  getAllPermissions: async (groupByModule = false) => {
    try {
      const response = await api.get("/api/admin/permissions", {
        params: { groupByModule },
      });
      return response.data?.data || [];
    } catch (error) {
      console.error("[PermissionService] getAllPermissions error:", error);
      throw error;
    }
  },

  /**
   * Belirli bir iznin detaylarını getirir.
   *
   * @param {number} id - İzin ID
   * @returns {Promise<Object>} - İzin detayları
   */
  getPermissionById: async (id) => {
    try {
      const response = await api.get(`/api/admin/permissions/${id}`);
      return response.data?.data || null;
    } catch (error) {
      console.error("[PermissionService] getPermissionById error:", error);
      throw error;
    }
  },

  /**
   * İzin durumunu aktif/pasif yapar.
   *
   * @param {number} id - İzin ID
   * @returns {Promise<Object>} - Güncellenmiş izin
   */
  togglePermissionStatus: async (id) => {
    try {
      const response = await api.put(
        `/api/admin/permissions/${id}/toggle-status`
      );
      return response.data;
    } catch (error) {
      console.error("[PermissionService] togglePermissionStatus error:", error);
      throw error;
    }
  },

  /**
   * Tüm modülleri listeler.
   *
   * @returns {Promise<Array>} - Modül listesi
   */
  getModules: async () => {
    try {
      const response = await api.get("/api/admin/permissions/modules");
      return response.data?.data || [];
    } catch (error) {
      console.error("[PermissionService] getModules error:", error);
      throw error;
    }
  },

  /**
   * İzin istatistiklerini getirir.
   *
   * @returns {Promise<Object>} - İstatistikler
   */
  getStatistics: async () => {
    try {
      const response = await api.get("/api/admin/permissions/statistics");
      return response.data?.data || {};
    } catch (error) {
      console.error("[PermissionService] getStatistics error:", error);
      throw error;
    }
  },

  /**
   * Belirli bir kullanıcının izinlerini getirir.
   *
   * @param {number} userId - Kullanıcı ID
   * @returns {Promise<Object>} - Kullanıcı izinleri
   */
  getUserPermissions: async (userId) => {
    try {
      const response = await api.get(`/api/admin/permissions/user/${userId}`);
      return response.data?.data || [];
    } catch (error) {
      console.error("[PermissionService] getUserPermissions error:", error);
      throw error;
    }
  },

  // =========================================================================
  // ADMIN - ROL YÖNETİMİ
  // =========================================================================

  /**
   * Sistemdeki tüm rolleri listeler.
   *
   * @returns {Promise<Array>} - Rol listesi
   */
  getAllRoles: async () => {
    try {
      const response = await api.get("/api/admin/roles");
      return response.data?.data || [];
    } catch (error) {
      console.error("[PermissionService] getAllRoles error:", error);
      throw error;
    }
  },

  /**
   * Belirli bir rolün detaylarını ve izinlerini getirir.
   *
   * @param {number} roleId - Rol ID
   * @returns {Promise<Object>} - Rol detayları ve izinleri
   */
  getRoleById: async (roleId) => {
    try {
      const response = await api.get(`/api/admin/roles/${roleId}`);
      return response.data?.data || null;
    } catch (error) {
      console.error("[PermissionService] getRoleById error:", error);
      throw error;
    }
  },

  /**
   * Rol izinlerini günceller (toplu atama).
   *
   * @param {number} roleId - Rol ID
   * @param {Array<number>} permissionIds - Yeni izin ID'leri
   * @returns {Promise<Object>} - Güncelleme sonucu
   */
  updateRolePermissions: async (roleId, permissionIds) => {
    try {
      const response = await api.put(`/api/admin/roles/${roleId}/permissions`, {
        permissionIds,
      });
      return response.data;
    } catch (error) {
      console.error("[PermissionService] updateRolePermissions error:", error);
      throw error;
    }
  },

  /**
   * Role tek bir izin ekler.
   *
   * @param {number} roleId - Rol ID
   * @param {number} permissionId - İzin ID
   * @returns {Promise<Object>} - Ekleme sonucu
   */
  addPermissionToRole: async (roleId, permissionId) => {
    try {
      const response = await api.post(
        `/api/admin/roles/${roleId}/permissions/${permissionId}`
      );
      return response.data;
    } catch (error) {
      console.error("[PermissionService] addPermissionToRole error:", error);
      throw error;
    }
  },

  /**
   * Rolden tek bir izni kaldırır.
   *
   * @param {number} roleId - Rol ID
   * @param {number} permissionId - İzin ID
   * @returns {Promise<Object>} - Kaldırma sonucu
   */
  removePermissionFromRole: async (roleId, permissionId) => {
    try {
      const response = await api.delete(
        `/api/admin/roles/${roleId}/permissions/${permissionId}`
      );
      return response.data;
    } catch (error) {
      console.error(
        "[PermissionService] removePermissionFromRole error:",
        error
      );
      throw error;
    }
  },

  /**
   * Role atanabilecek izinleri getirir (henüz atanmamış olanlar).
   *
   * @param {number} roleId - Rol ID
   * @returns {Promise<Array>} - Atanabilir izinler
   */
  getAvailablePermissionsForRole: async (roleId) => {
    try {
      const response = await api.get(
        `/api/admin/roles/${roleId}/available-permissions`
      );
      return response.data?.data || [];
    } catch (error) {
      console.error(
        "[PermissionService] getAvailablePermissionsForRole error:",
        error
      );
      throw error;
    }
  },

  /**
   * Rol-izin matrisini getirir.
   * Admin panelinde checkbox grid için kullanılır.
   *
   * @returns {Promise<Object>} - { PermissionHeaders: [], RoleMatrix: [] }
   */
  getRolePermissionMatrix: async () => {
    try {
      const response = await api.get("/api/admin/roles/matrix");
      return response.data?.data || { PermissionHeaders: [], RoleMatrix: [] };
    } catch (error) {
      console.error(
        "[PermissionService] getRolePermissionMatrix error:",
        error
      );
      throw error;
    }
  },

  /**
   * Rolleri karşılaştırır.
   *
   * @param {Array<number>} roleIds - Karşılaştırılacak rol ID'leri
   * @returns {Promise<Object>} - Karşılaştırma sonucu
   */
  compareRoles: async (roleIds) => {
    try {
      const response = await api.get("/api/admin/roles/comparison", {
        params: { roleIds: roleIds.join(",") },
      });
      return response.data?.data || {};
    } catch (error) {
      console.error("[PermissionService] compareRoles error:", error);
      throw error;
    }
  },
};

// =========================================================================
// İZİN SABİTLERİ (Frontend için kopyası)
// Backend'deki Permissions.cs ile senkronize olmalı
// =========================================================================
export const PERMISSIONS = {
  // Dashboard
  DASHBOARD_VIEW: "dashboard.view",
  DASHBOARD_STATISTICS: "dashboard.statistics",
  DASHBOARD_REVENUE: "dashboard.revenue",

  // Ürünler
  PRODUCTS_VIEW: "products.view",
  PRODUCTS_CREATE: "products.create",
  PRODUCTS_UPDATE: "products.update",
  PRODUCTS_DELETE: "products.delete",
  PRODUCTS_STOCK: "products.stock",
  PRODUCTS_PRICING: "products.pricing",
  PRODUCTS_IMPORT: "products.import",
  PRODUCTS_EXPORT: "products.export",

  // Kategoriler
  CATEGORIES_VIEW: "categories.view",
  CATEGORIES_CREATE: "categories.create",
  CATEGORIES_UPDATE: "categories.update",
  CATEGORIES_DELETE: "categories.delete",

  // Siparişler
  ORDERS_VIEW: "orders.view",
  ORDERS_DETAILS: "orders.details",
  ORDERS_STATUS: "orders.status",
  ORDERS_CANCEL: "orders.cancel",
  ORDERS_REFUND: "orders.refund",
  ORDERS_ASSIGN_COURIER: "orders.assign_courier",
  ORDERS_CUSTOMER_INFO: "orders.customer_info",
  ORDERS_EXPORT: "orders.export",

  // Kullanıcılar
  USERS_VIEW: "users.view",
  USERS_CREATE: "users.create",
  USERS_UPDATE: "users.update",
  USERS_DELETE: "users.delete",
  USERS_ROLES: "users.roles",
  USERS_SENSITIVE: "users.sensitive",
  USERS_EXPORT: "users.export",

  // Roller
  ROLES_VIEW: "roles.view",
  ROLES_CREATE: "roles.create",
  ROLES_UPDATE: "roles.update",
  ROLES_DELETE: "roles.delete",
  ROLES_PERMISSIONS: "roles.permissions",

  // Kampanyalar
  CAMPAIGNS_VIEW: "campaigns.view",
  CAMPAIGNS_CREATE: "campaigns.create",
  CAMPAIGNS_UPDATE: "campaigns.update",
  CAMPAIGNS_DELETE: "campaigns.delete",

  // Kuponlar
  COUPONS_VIEW: "coupons.view",
  COUPONS_CREATE: "coupons.create",
  COUPONS_UPDATE: "coupons.update",
  COUPONS_DELETE: "coupons.delete",

  // Kargolar
  COURIERS_VIEW: "couriers.view",
  COURIERS_CREATE: "couriers.create",
  COURIERS_UPDATE: "couriers.update",
  COURIERS_DELETE: "couriers.delete",
  COURIERS_ASSIGN: "couriers.assign",

  // Teslimat - Backend Shipping sınıfı ile senkronize
  SHIPPING_VIEW: "shipping.pending",
  SHIPPING_UPDATE_STATUS: "shipping.tracking",
  SHIPPING_TRACK: "shipping.ship",
  SHIPPING_WEIGHT_APPROVAL: "shipping.deliver",

  // Raporlar
  REPORTS_VIEW: "reports.view",
  REPORTS_SALES: "reports.sales",
  REPORTS_INVENTORY: "reports.inventory",
  REPORTS_CUSTOMERS: "reports.customers",
  REPORTS_EXPORT: "reports.export",
  REPORTS_WEIGHT: "reports.weight", // Ağırlık raporları için (YENİ)

  // Bannerlar
  BANNERS_VIEW: "banners.view",
  BANNERS_CREATE: "banners.create",
  BANNERS_UPDATE: "banners.update",
  BANNERS_DELETE: "banners.delete",

  // Markalar
  BRANDS_VIEW: "brands.view",
  BRANDS_CREATE: "brands.create",
  BRANDS_UPDATE: "brands.update",
  BRANDS_DELETE: "brands.delete",

  // Ayarlar
  SETTINGS_VIEW: "settings.view",
  SETTINGS_UPDATE: "settings.update",
  SETTINGS_SYSTEM: "settings.system",

  // Loglar
  LOGS_VIEW: "logs.view",
  LOGS_AUDIT: "logs.audit",
  LOGS_ERROR: "logs.error",
  LOGS_EXPORT: "logs.export",
};

// Modül bazlı izin grupları (UI için)
export const PERMISSION_MODULES = {
  Dashboard: [
    PERMISSIONS.DASHBOARD_VIEW,
    PERMISSIONS.DASHBOARD_STATISTICS,
    PERMISSIONS.DASHBOARD_REVENUE,
  ],
  Products: [
    PERMISSIONS.PRODUCTS_VIEW,
    PERMISSIONS.PRODUCTS_CREATE,
    PERMISSIONS.PRODUCTS_UPDATE,
    PERMISSIONS.PRODUCTS_DELETE,
    PERMISSIONS.PRODUCTS_STOCK,
    PERMISSIONS.PRODUCTS_PRICING,
  ],
  Categories: [
    PERMISSIONS.CATEGORIES_VIEW,
    PERMISSIONS.CATEGORIES_CREATE,
    PERMISSIONS.CATEGORIES_UPDATE,
    PERMISSIONS.CATEGORIES_DELETE,
  ],
  Orders: [
    PERMISSIONS.ORDERS_VIEW,
    PERMISSIONS.ORDERS_DETAILS,
    PERMISSIONS.ORDERS_STATUS,
    PERMISSIONS.ORDERS_CANCEL,
    PERMISSIONS.ORDERS_REFUND,
  ],
  Users: [
    PERMISSIONS.USERS_VIEW,
    PERMISSIONS.USERS_CREATE,
    PERMISSIONS.USERS_UPDATE,
    PERMISSIONS.USERS_DELETE,
    PERMISSIONS.USERS_ROLES,
  ],
  Roles: [
    PERMISSIONS.ROLES_VIEW,
    PERMISSIONS.ROLES_CREATE,
    PERMISSIONS.ROLES_UPDATE,
    PERMISSIONS.ROLES_DELETE,
    PERMISSIONS.ROLES_PERMISSIONS,
  ],
  Campaigns: [
    PERMISSIONS.CAMPAIGNS_VIEW,
    PERMISSIONS.CAMPAIGNS_CREATE,
    PERMISSIONS.CAMPAIGNS_UPDATE,
    PERMISSIONS.CAMPAIGNS_DELETE,
  ],
  Couriers: [
    PERMISSIONS.COURIERS_VIEW,
    PERMISSIONS.COURIERS_CREATE,
    PERMISSIONS.COURIERS_UPDATE,
    PERMISSIONS.COURIERS_DELETE,
  ],
  Shipping: [
    PERMISSIONS.SHIPPING_VIEW,
    PERMISSIONS.SHIPPING_UPDATE_STATUS,
    PERMISSIONS.SHIPPING_TRACK,
    PERMISSIONS.SHIPPING_WEIGHT_APPROVAL,
  ],
  Reports: [
    PERMISSIONS.REPORTS_VIEW,
    PERMISSIONS.REPORTS_SALES,
    PERMISSIONS.REPORTS_INVENTORY,
    PERMISSIONS.REPORTS_EXPORT,
    PERMISSIONS.REPORTS_WEIGHT, // Ağırlık raporları (YENİ)
  ],
  Banners: [
    PERMISSIONS.BANNERS_VIEW,
    PERMISSIONS.BANNERS_CREATE,
    PERMISSIONS.BANNERS_UPDATE,
    PERMISSIONS.BANNERS_DELETE,
  ],
  Settings: [
    PERMISSIONS.SETTINGS_VIEW,
    PERMISSIONS.SETTINGS_UPDATE,
    PERMISSIONS.SETTINGS_SYSTEM,
  ],
  Logs: [
    PERMISSIONS.LOGS_VIEW,
    PERMISSIONS.LOGS_AUDIT,
    PERMISSIONS.LOGS_ERROR,
    PERMISSIONS.LOGS_EXPORT,
  ],
};

export default permissionService;
