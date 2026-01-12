// src/components/AdminLayout.jsx
// =============================================================================
// Admin Layout - İzin Bazlı Menü Sistemi
// =============================================================================
// Bu component admin paneli için sidebar ve layout sağlar.
// Menü öğeleri kullanıcının izinlerine göre filtrelenir.
// =============================================================================

import { useEffect, useMemo, useState, useCallback } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import { PERMISSIONS } from "../services/permissionService";

export default function AdminLayout({ children }) {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [openMenus, setOpenMenus] = useState({});
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout, hasPermission, hasAnyPermission } = useAuth();

  // Admin rolleri
  const ADMIN_ROLES = [
    "Admin",
    "SuperAdmin",
    "StoreManager",
    "CustomerSupport",
    "Logistics",
  ];

  const isAdminLike = user?.role === "SuperAdmin" || user?.isAdmin;
  const isSuperAdmin = user?.role === "SuperAdmin";

  // İzin kontrolü helper fonksiyonu
  const checkPermission = useCallback(
    (permission) => {
      if (isSuperAdmin) return true;
      if (!permission) return true;

      if (Array.isArray(permission)) {
        return hasAnyPermission?.(...permission) ?? false;
      }
      return hasPermission?.(permission) ?? false;
    },
    [isSuperAdmin, hasPermission, hasAnyPermission]
  );

  // Menü öğeleri - İzin bazlı filtreleme
  const menuItems = useMemo(
    () => [
      {
        path: "/admin/dashboard",
        icon: "fas fa-tachometer-alt",
        label: "Dashboard",
        permission: PERMISSIONS.DASHBOARD_VIEW,
      },
      {
        path: "/admin/products",
        icon: "fas fa-box",
        label: "Ürünler",
        permission: PERMISSIONS.PRODUCTS_VIEW,
      },
      {
        path: "/admin/categories",
        icon: "fas fa-tags",
        label: "Kategoriler",
        permission: PERMISSIONS.CATEGORIES_VIEW,
      },
      {
        path: "/admin/orders",
        icon: "fas fa-shopping-cart",
        label: "Siparişler",
        permission: PERMISSIONS.ORDERS_VIEW,
      },
      {
        path: "/admin/users",
        icon: "fas fa-users",
        label: "Kullanıcılar",
        permission: PERMISSIONS.USERS_VIEW,
        adminOnly: true,
      },
      {
        path: "/admin/couriers",
        icon: "fas fa-motorcycle",
        label: "Kuryeler",
        permission: PERMISSIONS.COURIERS_VIEW,
      },
      {
        path: "/admin/reports",
        icon: "fas fa-chart-bar",
        label: "Raporlar",
        permission: PERMISSIONS.REPORTS_VIEW,
      },
      {
        path: "/admin/micro",
        icon: "fas fa-plug",
        label: "ERP / Mikro",
        permission: PERMISSIONS.SETTINGS_SYSTEM,
        adminOnly: true,
      },
      {
        path: "/admin/posters",
        icon: "fas fa-image",
        label: "Poster Yönetimi",
        permission: PERMISSIONS.BANNERS_VIEW,
      },
      {
        path: "/admin/weight-reports",
        icon: "fas fa-weight",
        label: "Ağırlık Raporları",
        permission: PERMISSIONS.ORDERS_VIEW,
      },
      {
        path: "/admin/campaigns",
        icon: "fas fa-gift",
        label: "Kampanya Yönetimi",
        permission: PERMISSIONS.CAMPAIGNS_VIEW,
        adminOnly: true,
      },
      // Rol ve İzin Yönetimi - Sadece SuperAdmin
      {
        label: "Yetki Yönetimi",
        icon: "fas fa-user-shield",
        superAdminOnly: true,
        children: [
          {
            path: "/admin/roles",
            label: "Rol Yönetimi",
            permission: PERMISSIONS.ROLES_VIEW,
          },
          {
            path: "/admin/permissions",
            label: "İzin Yönetimi",
            permission: PERMISSIONS.ROLES_PERMISSIONS,
          },
        ],
      },
      {
        label: "Loglar",
        icon: "fas fa-clipboard-list",
        permission: PERMISSIONS.LOGS_VIEW,
        adminOnly: true,
        children: [
          {
            path: "/admin/logs/audit",
            label: "Audit Logs",
            permission: PERMISSIONS.LOGS_AUDIT,
          },
          {
            path: "/admin/logs/errors",
            label: "Error Logs",
            permission: PERMISSIONS.LOGS_ERROR,
          },
          {
            path: "/admin/logs/system",
            label: "System Logs",
            permission: PERMISSIONS.LOGS_VIEW,
          },
          {
            path: "/admin/logs/inventory",
            label: "Inventory Logs",
            permission: PERMISSIONS.LOGS_VIEW,
          },
        ],
      },
    ],
    []
  );

  // Filtrelenmiş menü öğeleri
  const filteredMenuItems = useMemo(() => {
    return menuItems.filter((item) => {
      // SuperAdmin kontrolü
      if (item.superAdminOnly && !isSuperAdmin) return false;

      // Admin-only kontrolü
      if (item.adminOnly && !isAdminLike && !ADMIN_ROLES.includes(user?.role))
        return false;

      // İzin kontrolü
      if (item.permission && !checkPermission(item.permission)) return false;

      // Alt menüleri filtrele
      if (item.children) {
        const filteredChildren = item.children.filter((child) => {
          if (child.permission && !checkPermission(child.permission))
            return false;
          return true;
        });

        // Hiç görünür alt menü yoksa ana menüyü gösterme
        if (filteredChildren.length === 0) return false;

        // Filtrelenmiş children'ı güncelle
        item.filteredChildren = filteredChildren;
      }

      return true;
    });
  }, [menuItems, isSuperAdmin, isAdminLike, user?.role, checkPermission]);

  useEffect(() => {
    const parentWithChild = filteredMenuItems.find(
      (item) =>
        (item.children || item.filteredChildren) &&
        (item.filteredChildren || item.children).some((child) =>
          location.pathname.startsWith(child.path)
        )
    );
    if (parentWithChild) {
      setOpenMenus((prev) => ({
        ...prev,
        [parentWithChild.label]: true,
      }));
    }
  }, [location.pathname, filteredMenuItems]);

  const toggleMenu = (label) => {
    setOpenMenus((prev) => ({
      ...prev,
      [label]: !prev[label],
    }));
  };

  const handleLogout = () => {
    logout();
    localStorage.removeItem("authUser");
    navigate("/admin/login");
  };

  return (
    <div className="d-flex admin-layout-root">
      {/* Sidebar */}
      <div
        className={`text-white ${sidebarOpen ? "" : "d-none d-lg-block"}`}
        style={{
          minHeight: "100vh",
          width: "240px",
          background: "linear-gradient(180deg, #2d3748 0%, #1a202c 100%)",
          boxShadow: "2px 0 10px rgba(0,0,0,0.1)",
          position: "relative",
        }}
      >
        <div
          className="p-3 border-bottom"
          style={{ borderColor: "rgba(255,255,255,0.1) !important" }}
        >
          <h5 className="mb-0 fw-bold">
            <i
              className="fas fa-shield-alt me-2"
              style={{ color: "#f57c00" }}
            ></i>
            Admin Panel
          </h5>
        </div>

        <div className="p-3">
          <div className="d-flex align-items-center mb-3">
            <div
              className="rounded-circle d-flex align-items-center justify-content-center me-3"
              style={{
                width: "36px",
                height: "36px",
                background: "linear-gradient(135deg, #f57c00, #ff9800)",
              }}
            >
              <i
                className="fas fa-user text-white"
                style={{ fontSize: "0.85rem" }}
              ></i>
            </div>
            <div>
              <div className="fw-semibold" style={{ fontSize: "0.9rem" }}>
                {user?.name || "Admin"}
              </div>
              <small
                style={{ color: "rgba(255,255,255,0.7)", fontSize: "0.75rem" }}
              >
                {user?.role || "Yönetici"}
              </small>
            </div>
          </div>
        </div>

        <nav className="mt-2">
          {filteredMenuItems.map((item, index) => {
            // Children varsa (alt menü)
            const children = item.filteredChildren || item.children;
            if (children && children.length > 0) {
              const isActiveChild = children.some((child) =>
                location.pathname.startsWith(child.path)
              );
              return (
                <div key={item.label}>
                  <button
                    type="button"
                    className="d-flex align-items-center w-100 border-0 bg-transparent px-3 py-3 text-white"
                    style={{
                      transition: "all 0.2s",
                      background: isActiveChild
                        ? "linear-gradient(135deg, #f57c00, #ff9800)"
                        : "transparent",
                      borderLeft: isActiveChild
                        ? "3px solid #fff"
                        : "3px solid transparent",
                    }}
                    onClick={() => toggleMenu(item.label)}
                  >
                    <i
                      className={`${item.icon} me-3`}
                      style={{ width: "20px", fontSize: "0.9rem" }}
                    ></i>
                    <span style={{ fontSize: "0.9rem" }}>{item.label}</span>
                    <i
                      className={`fas ms-auto ${
                        openMenus[item.label]
                          ? "fa-chevron-up"
                          : "fa-chevron-down"
                      }`}
                      style={{ fontSize: "0.75rem" }}
                    ></i>
                  </button>
                  {openMenus[item.label] && (
                    <div className="ms-4">
                      {children.map((child) => (
                        <Link
                          key={child.path}
                          to={child.path}
                          className="d-block text-decoration-none px-3 py-2 text-white"
                          style={{
                            borderLeft:
                              location.pathname === child.path
                                ? "3px solid #fff"
                                : "3px solid transparent",
                            background:
                              location.pathname === child.path
                                ? "rgba(245, 124, 0, 0.2)"
                                : "transparent",
                            fontSize: "0.85rem",
                          }}
                        >
                          {child.label}
                        </Link>
                      ))}
                    </div>
                  )}
                </div>
              );
            }
            return (
              <Link
                key={item.path}
                to={item.path}
                className="d-block text-decoration-none px-3 py-3 text-white position-relative"
                style={{
                  transition: "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
                  background:
                    location.pathname === item.path
                      ? "linear-gradient(135deg, #f57c00, #ff9800)"
                      : "transparent",
                  borderLeft:
                    location.pathname === item.path
                      ? "4px solid #fff"
                      : "4px solid transparent",
                  boxShadow:
                    location.pathname === item.path
                      ? "0 4px 15px rgba(245, 124, 0, 0.3)"
                      : "none",
                  transform:
                    location.pathname === item.path
                      ? "translateX(3px)"
                      : "translateX(0)",
                  animation: `slideIn 0.4s ease-out ${index * 0.05}s both`,
                }}
                onMouseEnter={(e) => {
                  if (location.pathname !== item.path) {
                    e.target.style.backgroundColor = "rgba(245, 124, 0, 0.1)";
                    e.target.style.borderLeft =
                      "4px solid rgba(245, 124, 0, 0.3)";
                    e.target.style.transform = "translateX(5px)";
                  }
                }}
                onMouseLeave={(e) => {
                  if (location.pathname !== item.path) {
                    e.target.style.backgroundColor = "transparent";
                    e.target.style.borderLeft = "4px solid transparent";
                    e.target.style.transform = "translateX(0)";
                  }
                }}
              >
                <i
                  className={`${item.icon} me-3`}
                  style={{
                    width: "20px",
                    fontSize: "0.9rem",
                    filter:
                      location.pathname === item.path
                        ? "drop-shadow(0 0 8px rgba(255,255,255,0.5))"
                        : "none",
                    transition: "filter 0.3s ease",
                  }}
                ></i>
                <span
                  style={{
                    fontSize: "0.9rem",
                    fontWeight: location.pathname === item.path ? "600" : "400",
                  }}
                >
                  {item.label}
                </span>
                {item.path === "/admin/weight-reports" && (
                  <span
                    className="position-absolute"
                    style={{
                      right: "12px",
                      top: "50%",
                      transform: "translateY(-50%)",
                      background:
                        "linear-gradient(135deg, #ff6b6b 0%, #ee5a6f 100%)",
                      color: "white",
                      fontSize: "0.7rem",
                      fontWeight: "700",
                      padding: "3px 7px",
                      borderRadius: "12px",
                      boxShadow: "0 2px 8px rgba(255, 107, 107, 0.4)",
                      animation: "pulse 2s infinite",
                      minWidth: "22px",
                      textAlign: "center",
                    }}
                  >
                    3
                  </span>
                )}
              </Link>
            );
          })}
        </nav>
      </div>

      {/* Main Content */}
      <div className="flex-grow-1">
        {/* Top Navigation */}
        <nav
          className="navbar navbar-expand-lg bg-white shadow-sm"
          style={{ borderBottom: "1px solid #e5e7eb" }}
        >
          <div className="container-fluid">
            <button
              className="btn btn-outline-secondary d-lg-none me-2"
              onClick={() => setSidebarOpen(!sidebarOpen)}
            >
              <i className="fas fa-bars"></i>
            </button>

            <div className="navbar-nav ms-auto">
              <div className="nav-item dropdown">
                <button
                  className="nav-link dropdown-toggle d-flex align-items-center text-dark btn btn-link border-0"
                  type="button"
                  data-bs-toggle="dropdown"
                  style={{ fontSize: "0.9rem", textDecoration: "none" }}
                >
                  <div
                    className="rounded-circle d-flex align-items-center justify-content-center me-2"
                    style={{
                      width: "32px",
                      height: "32px",
                      background: "linear-gradient(135deg, #f57c00, #ff9800)",
                    }}
                  >
                    <i
                      className="fas fa-user text-white"
                      style={{ fontSize: "0.75rem" }}
                    ></i>
                  </div>
                  {user?.name || "Admin"}
                </button>
                <ul className="dropdown-menu dropdown-menu-end shadow">
                  <li>
                    <button className="dropdown-item" type="button">
                      <i
                        className="fas fa-user me-2"
                        style={{ color: "#f57c00" }}
                      ></i>
                      Profil
                    </button>
                  </li>
                  <li>
                    <button className="dropdown-item" type="button">
                      <i
                        className="fas fa-cog me-2"
                        style={{ color: "#f57c00" }}
                      ></i>
                      Ayarlar
                    </button>
                  </li>
                  <li>
                    <hr className="dropdown-divider" />
                  </li>
                  <li>
                    <button
                      className="dropdown-item text-danger"
                      onClick={handleLogout}
                    >
                      <i className="fas fa-sign-out-alt me-2"></i>Çıkış Yap
                    </button>
                  </li>
                </ul>
              </div>
            </div>
          </div>
        </nav>

        {/* Page Content */}
        <main className="p-4 admin-layout-main">{children}</main>
      </div>
    </div>
  );
}

// CSS için inline styles
const styles = `
.dropdown-menu {
  border: none;
  border-radius: 12px;
  box-shadow: 0 4px 20px rgba(0,0,0,0.1);
}
.dropdown-item:hover {
  background-color: rgba(245, 124, 0, 0.1);
  color: #f57c00;
  transform: translateX(3px);
  transition: all 0.2s ease;
}
@keyframes pulse {
  0%, 100% {
    transform: translateY(-50%) scale(1);
    box-shadow: 0 2px 8px rgba(255, 107, 107, 0.4);
  }
  50% {
    transform: translateY(-50%) scale(1.08);
    box-shadow: 0 4px 15px rgba(255, 107, 107, 0.6);
  }
}
@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateX(-20px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}
`;

// Styles'ı head'e ekle
if (typeof document !== "undefined") {
  const styleElement = document.createElement("style");
  styleElement.textContent = styles;
  document.head.appendChild(styleElement);
}
