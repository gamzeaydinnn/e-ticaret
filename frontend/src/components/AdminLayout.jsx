// src/components/AdminLayout.jsx
import React, { useState, useEffect } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";

export default function AdminLayout({ children }) {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [openMenus, setOpenMenus] = useState({});
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  const isAdminLike =
    user?.role === "Admin" || user?.role === "SuperAdmin" || user?.isAdmin;

  const menuItems = [
    {
      path: "/admin/dashboard",
      icon: "fas fa-tachometer-alt",
      label: "Dashboard",
    },
    { path: "/admin/products", icon: "fas fa-box", label: "Ürünler" },
    { path: "/admin/categories", icon: "fas fa-tags", label: "Kategoriler" },
    {
      path: "/admin/orders",
      icon: "fas fa-shopping-cart",
      label: "Siparişler",
    },
    {
      path: "/admin/users",
      icon: "fas fa-users",
      label: "Kullanıcılar",
      adminOnly: true,
    },
    { path: "/admin/couriers", icon: "fas fa-motorcycle", label: "Kuryeler" },
    { path: "/admin/reports", icon: "fas fa-chart-bar", label: "Raporlar" },
    { path: "/admin/micro", icon: "fas fa-plug", label: "ERP / Mikro" },
    { path: "/admin/coupons", icon: "fas fa-ticket-alt", label: "Kuponlar" },
    {
      path: "/admin/campaigns",
      icon: "fas fa-gift",
      label: "Kampanya Yönetimi",
      adminOnly: true,
    },
    {
      label: "Loglar",
      icon: "fas fa-clipboard-list",
      adminOnly: true,
      children: [
        { path: "/admin/logs/audit", label: "Audit Logs" },
        { path: "/admin/logs/errors", label: "Error Logs" },
        { path: "/admin/logs/system", label: "System Logs" },
      ],
    },
  ];

  useEffect(() => {
    const parentWithChild = menuItems.find(
      (item) =>
        item.children &&
        item.children.some((child) => location.pathname.startsWith(child.path))
    );
    if (parentWithChild) {
      setOpenMenus((prev) => ({
        ...prev,
        [parentWithChild.label]: true,
      }));
    }
  }, [location.pathname]);

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
    <div className="d-flex">
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
          {menuItems.map((item) => {
            if (item.adminOnly && !isAdminLike) {
              return null;
            }
            if (item.children && item.children.length > 0) {
              const isActiveChild = item.children.some((child) =>
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
                        openMenus[item.label] ? "fa-chevron-up" : "fa-chevron-down"
                      }`}
                      style={{ fontSize: "0.75rem" }}
                    ></i>
                  </button>
                  {openMenus[item.label] && (
                    <div className="ms-4">
                      {item.children.map((child) => (
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
                className="d-block text-decoration-none px-3 py-3 text-white"
                style={{
                  transition: "all 0.2s",
                  background:
                    location.pathname === item.path
                      ? "linear-gradient(135deg, #f57c00, #ff9800)"
                      : "transparent",
                  borderLeft:
                    location.pathname === item.path
                      ? "3px solid #fff"
                      : "3px solid transparent",
                }}
                onMouseEnter={(e) => {
                  if (location.pathname !== item.path) {
                    e.target.style.backgroundColor = "rgba(245, 124, 0, 0.1)";
                  }
                }}
                onMouseLeave={(e) => {
                  if (location.pathname !== item.path) {
                    e.target.style.backgroundColor = "transparent";
                  }
                }}
              >
                <i
                  className={`${item.icon} me-3`}
                  style={{ width: "20px", fontSize: "0.9rem" }}
                ></i>
                <span style={{ fontSize: "0.9rem" }}>{item.label}</span>
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
                <a
                  className="nav-link dropdown-toggle d-flex align-items-center text-dark"
                  href="#"
                  role="button"
                  data-bs-toggle="dropdown"
                  style={{ fontSize: "0.9rem" }}
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
                </a>
                <ul className="dropdown-menu dropdown-menu-end shadow">
                  <li>
                    <a className="dropdown-item" href="#">
                      <i
                        className="fas fa-user me-2"
                        style={{ color: "#f57c00" }}
                      ></i>
                      Profil
                    </a>
                  </li>
                  <li>
                    <a className="dropdown-item" href="#">
                      <i
                        className="fas fa-cog me-2"
                        style={{ color: "#f57c00" }}
                      ></i>
                      Ayarlar
                    </a>
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
        <main className="p-4">{children}</main>
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
}
`;

// Styles'ı head'e ekle
if (typeof document !== "undefined") {
  const styleElement = document.createElement("style");
  styleElement.textContent = styles;
  document.head.appendChild(styleElement);
}
