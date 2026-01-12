// =============================================================================
// PermissionWrapper.jsx - İzin Bazlı Conditional Render Component
// =============================================================================
// Bu component, kullanıcı izinlerine göre içerik göstermek için kullanılır.
// Sayfa içi butonlar, linkler veya bölümler için idealdir.
//
// Kullanım:
// <PermissionWrapper permission="products.create">
//   <button>Ürün Ekle</button>
// </PermissionWrapper>
// =============================================================================

import React from "react";
import { useAuth } from "../contexts/AuthContext";

/**
 * İzin bazlı conditional render wrapper
 *
 * @param {Object} props
 * @param {string|string[]} props.permission - Gerekli izin(ler)
 * @param {boolean} props.requireAll - Tüm izinler mi gerekli? (default: false)
 * @param {React.ReactNode} props.children - Yetkili ise gösterilecek içerik
 * @param {React.ReactNode} props.fallback - Yetkisiz ise gösterilecek içerik (opsiyonel)
 * @param {boolean} props.hide - true ise yetkisiz durumda tamamen gizle (default: true)
 * @param {boolean} props.disable - true ise yetkisiz durumda disable et
 * @param {string} props.disabledClassName - Disable durumunda eklenecek class
 * @param {string} props.disabledStyle - Disable durumunda eklenecek style
 */
export const PermissionWrapper = ({
  permission,
  requireAll = false,
  children,
  fallback = null,
  hide = true,
  disable = false,
  disabledClassName = "disabled opacity-50",
  disabledStyle = {},
}) => {
  const { user, hasPermission, hasAnyPermission, hasAllPermissions } =
    useAuth();

  // Kullanıcı yoksa veya user null ise
  if (!user) {
    return hide ? null : fallback;
  }

  // SuperAdmin her şeyi görebilir
  if (user.role === "SuperAdmin") {
    return children;
  }

  // İzin kontrolü
  const permissions = Array.isArray(permission) ? permission : [permission];

  let hasAccess = false;
  if (requireAll) {
    hasAccess = hasAllPermissions?.(...permissions) ?? false;
  } else {
    if (permissions.length === 1) {
      hasAccess = hasPermission?.(permissions[0]) ?? false;
    } else {
      hasAccess = hasAnyPermission?.(...permissions) ?? false;
    }
  }

  // Erişim varsa içeriği göster
  if (hasAccess) {
    return children;
  }

  // Erişim yok - disable modunda
  if (disable && !hide) {
    // Children'ı clone edip disabled prop ekle
    if (React.isValidElement(children)) {
      return React.cloneElement(children, {
        disabled: true,
        className: `${
          children.props.className || ""
        } ${disabledClassName}`.trim(),
        style: {
          ...children.props.style,
          ...disabledStyle,
          pointerEvents: "none",
        },
        title: "Bu işlem için yetkiniz bulunmamaktadır.",
        "aria-disabled": true,
      });
    }
  }

  // Erişim yok - gizle veya fallback göster
  return hide ? null : fallback;
};

/**
 * Sadece SuperAdmin için içerik gösteren wrapper
 */
export const SuperAdminOnly = ({ children, fallback = null }) => {
  const { user } = useAuth();

  if (user?.role === "SuperAdmin") {
    return children;
  }

  return fallback;
};

/**
 * Belirli roller için içerik gösteren wrapper
 *
 * @param {Object} props
 * @param {string|string[]} props.roles - İzin verilen roller
 * @param {React.ReactNode} props.children - Yetkili ise gösterilecek içerik
 * @param {React.ReactNode} props.fallback - Yetkisiz ise gösterilecek içerik
 */
export const RoleWrapper = ({ roles, children, fallback = null }) => {
  const { user } = useAuth();

  if (!user) return fallback;

  // SuperAdmin her role erişebilir
  if (user.role === "SuperAdmin") return children;

  const allowedRoles = Array.isArray(roles) ? roles : [roles];

  if (allowedRoles.includes(user.role)) {
    return children;
  }

  return fallback;
};

/**
 * İzin bazlı buton - Yetkisiz ise disabled
 *
 * @param {Object} props
 * @param {string|string[]} props.permission - Gerekli izin(ler)
 * @param {boolean} props.requireAll - Tüm izinler mi gerekli?
 * @param {string} props.className - Buton class'ı
 * @param {Function} props.onClick - Click handler
 * @param {React.ReactNode} props.children - Buton içeriği
 * @param {Object} props.rest - Diğer buton props'ları
 */
export const PermissionButton = ({
  permission,
  requireAll = false,
  className = "btn btn-primary",
  onClick,
  children,
  ...rest
}) => {
  const { user, hasPermission, hasAnyPermission, hasAllPermissions } =
    useAuth();

  // İzin kontrolü
  let hasAccess = false;

  if (user?.role === "SuperAdmin") {
    hasAccess = true;
  } else if (user && permission) {
    const permissions = Array.isArray(permission) ? permission : [permission];

    if (requireAll) {
      hasAccess = hasAllPermissions?.(...permissions) ?? false;
    } else {
      hasAccess =
        permissions.length === 1
          ? hasPermission?.(permissions[0]) ?? false
          : hasAnyPermission?.(...permissions) ?? false;
    }
  }

  return (
    <button
      className={`${className} ${
        !hasAccess ? "disabled opacity-50" : ""
      }`.trim()}
      onClick={hasAccess ? onClick : undefined}
      disabled={!hasAccess}
      title={!hasAccess ? "Bu işlem için yetkiniz bulunmamaktadır." : undefined}
      {...rest}
    >
      {children}
    </button>
  );
};

/**
 * İzin bazlı link - Yetkisiz ise gizli veya disabled
 *
 * @param {Object} props
 * @param {string|string[]} props.permission - Gerekli izin(ler)
 * @param {boolean} props.requireAll - Tüm izinler mi gerekli?
 * @param {string} props.to - Link hedefi
 * @param {string} props.className - Link class'ı
 * @param {boolean} props.hideIfNoPermission - Yetkisiz ise gizle
 * @param {React.ReactNode} props.children - Link içeriği
 */
export const PermissionLink = ({
  permission,
  requireAll = false,
  to,
  className = "",
  hideIfNoPermission = true,
  children,
  ...rest
}) => {
  const { user, hasPermission, hasAnyPermission, hasAllPermissions } =
    useAuth();
  const { Link } = require("react-router-dom");

  // İzin kontrolü
  let hasAccess = false;

  if (user?.role === "SuperAdmin") {
    hasAccess = true;
  } else if (user && permission) {
    const permissions = Array.isArray(permission) ? permission : [permission];

    if (requireAll) {
      hasAccess = hasAllPermissions?.(...permissions) ?? false;
    } else {
      hasAccess =
        permissions.length === 1
          ? hasPermission?.(permissions[0]) ?? false
          : hasAnyPermission?.(...permissions) ?? false;
    }
  }

  // Gizle
  if (!hasAccess && hideIfNoPermission) {
    return null;
  }

  // Disabled link
  if (!hasAccess) {
    return (
      <span
        className={`${className} disabled opacity-50`.trim()}
        style={{ pointerEvents: "none", cursor: "not-allowed" }}
        title="Bu sayfaya erişim yetkiniz bulunmamaktadır."
        {...rest}
      >
        {children}
      </span>
    );
  }

  return (
    <Link to={to} className={className} {...rest}>
      {children}
    </Link>
  );
};

export default PermissionWrapper;
