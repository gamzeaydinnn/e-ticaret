import { PERMISSIONS } from "../services/permissionService";

/**
 * Admin panel giriş / yönlendirme önceliği.
 * İlk erişilebilir rota varsayılan landing page olur.
 */
export const ADMIN_LANDING_ROUTES = [
  { path: "/admin/dashboard", permission: PERMISSIONS.DASHBOARD_VIEW },
  { path: "/admin/orders", permission: PERMISSIONS.ORDERS_VIEW },
  { path: "/admin/products", permission: PERMISSIONS.PRODUCTS_VIEW },
  { path: "/admin/categories", permission: PERMISSIONS.CATEGORIES_VIEW },
  { path: "/admin/couriers", permission: PERMISSIONS.COURIERS_VIEW },
  { path: "/admin/reports", permission: [PERMISSIONS.REPORTS_VIEW, PERMISSIONS.REPORTS_SALES] },
  { path: "/admin/weight-management", permission: [PERMISSIONS.REPORTS_WEIGHT, PERMISSIONS.ORDERS_VIEW] },
  { path: "/admin/campaigns", permission: PERMISSIONS.CAMPAIGNS_VIEW },
  { path: "/admin/coupons", permission: PERMISSIONS.COUPONS_VIEW },
  { path: "/admin/newsletter", permission: PERMISSIONS.NEWSLETTER_VIEW },
  { path: "/admin/users", permission: PERMISSIONS.USERS_VIEW },
];

const ROLE_LANDING_FALLBACK = {
  StoreAttendant: "/admin/orders",
  Dispatcher: "/admin/orders",
};

const readCachedPermissions = (role) => {
  if (!role) return [];
  try {
    const cachedRole = localStorage.getItem("permissionsCacheRole");
    if (cachedRole !== role) return [];
    const raw = localStorage.getItem("userPermissions");
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed : parsed?.permissions || [];
  } catch {
    return [];
  }
};

const hasRoutePermission = (permission, permSet, hasPermission, hasAnyPermission) => {
  if (!permission) return true;
  if (Array.isArray(permission)) {
    if (hasAnyPermission) {
      return hasAnyPermission(...permission);
    }
    return permission.some((code) => permSet.has(code));
  }
  if (hasPermission) {
    return hasPermission(permission);
  }
  return permSet.has(permission);
};

/**
 * Kullanıcının erişebildiği ilk admin sayfasını döndürür.
 */
export function resolveAdminLandingPath({
  user,
  permissions = [],
  hasPermission,
  hasAnyPermission,
} = {}) {
  if (!user) return "/admin/login";
  if (user.role === "SuperAdmin") return "/admin/dashboard";

  const permList =
    (Array.isArray(permissions) && permissions.length > 0
      ? permissions
      : readCachedPermissions(user.role)) || [];
  const permSet = new Set(permList);

  for (const route of ADMIN_LANDING_ROUTES) {
    if (
      hasRoutePermission(route.permission, permSet, hasPermission, hasAnyPermission)
    ) {
      return route.path;
    }
  }

  if (ROLE_LANDING_FALLBACK[user.role]) {
    return ROLE_LANDING_FALLBACK[user.role];
  }

  return "/admin/access-denied";
}
