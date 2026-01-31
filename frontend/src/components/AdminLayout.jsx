// src/components/AdminLayout.jsx
// =============================================================================
// Admin Layout - İzin Bazlı Menü Sistemi + Oturum Zaman Aşımı
// =============================================================================
// Bu component admin paneli için sidebar ve layout sağlar.
// Menü öğeleri kullanıcının izinlerine göre filtrelenir.
// GÜVENLİK: 30 dakika hareketsizlik sonrası otomatik çıkış yapılır.
// =============================================================================

import { useEffect, useMemo, useState, useCallback } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import { PERMISSIONS } from "../services/permissionService";
import useSessionTimeout from "../hooks/useSessionTimeout";
import SessionWarningModal from "./SessionWarningModal";

export default function AdminLayout({ children }) {
  // ============================================================================
  // MOBİL RESPONSIVE: Başlangıçta sidebar kapalı (mobilde)
  // window.innerWidth kontrolü ile masaüstünde açık, mobilde kapalı başlar
  // ============================================================================
  const [sidebarOpen, setSidebarOpen] = useState(() => {
    if (typeof window !== "undefined") {
      return window.innerWidth >= 992; // lg breakpoint
    }
    return true;
  });
  const [openMenus, setOpenMenus] = useState({});
  const [showSessionWarning, setShowSessionWarning] = useState(false);
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout, hasPermission, hasAnyPermission } = useAuth();

  // ============================================================================
  // OTURUM ZAMAN AŞIMI (SESSION TIMEOUT) - 30 dakika
  // Kullanıcı 30 dakika hareketsiz kalırsa otomatik çıkış yapılır.
  // Son 5 dakikada uyarı modalı gösterilir.
  // ============================================================================
  const handleSessionTimeout = useCallback(() => {
    // Oturum sona erdi - çıkış yap ve login sayfasına yönlendir
    logout();
    localStorage.removeItem("authUser");
    // Mesaj göstermek için sessionStorage kullan
    sessionStorage.setItem("sessionExpired", "true");
    navigate("/admin/login");
  }, [logout, navigate]);

  const handleSessionWarning = useCallback(() => {
    // Uyarı modalını göster
    setShowSessionWarning(true);
  }, []);

  const { remainingTimeFormatted, isWarning, extendSession } =
    useSessionTimeout({
      timeoutMinutes: 30, // 30 dakika hareketsizlik sonrası çıkış
      warningMinutes: 5, // Son 5 dakikada uyarı göster
      onTimeout: handleSessionTimeout,
      onWarning: handleSessionWarning,
    });

  // Oturumu uzat butonuna basıldığında
  const handleExtendSession = useCallback(() => {
    extendSession();
    setShowSessionWarning(false);
  }, [extendSession]);

  // ============================================================================
  // TOUCH SWIPE DESTEĞİ - Mobilde kaydırarak sidebar açma/kapama
  // ============================================================================
  const [touchStart, setTouchStart] = useState(null);
  const [touchEnd, setTouchEnd] = useState(null);
  const minSwipeDistance = 50; // Minimum kaydırma mesafesi (px)

  const onTouchStart = useCallback((e) => {
    setTouchEnd(null);
    setTouchStart(e.targetTouches[0].clientX);
  }, []);

  const onTouchMove = useCallback((e) => {
    setTouchEnd(e.targetTouches[0].clientX);
  }, []);

  const onTouchEnd = useCallback(() => {
    if (!touchStart || !touchEnd) return;

    const distance = touchStart - touchEnd;
    const isLeftSwipe = distance > minSwipeDistance;
    const isRightSwipe = distance < -minSwipeDistance;

    // Sadece mobilde çalışsın
    if (typeof window !== "undefined" && window.innerWidth < 992) {
      if (isRightSwipe && !sidebarOpen && touchStart < 50) {
        // Ekranın sol kenarından sağa kaydırma - sidebar aç
        setSidebarOpen(true);
      } else if (isLeftSwipe && sidebarOpen) {
        // Sola kaydırma - sidebar kapat
        setSidebarOpen(false);
      }
    }
  }, [touchStart, touchEnd, sidebarOpen]);

  // Touch event listener'ları ekle
  useEffect(() => {
    const handleTouchStart = (e) => onTouchStart(e);
    const handleTouchMove = (e) => onTouchMove(e);
    const handleTouchEnd = () => onTouchEnd();

    document.addEventListener("touchstart", handleTouchStart, {
      passive: true,
    });
    document.addEventListener("touchmove", handleTouchMove, { passive: true });
    document.addEventListener("touchend", handleTouchEnd, { passive: true });

    return () => {
      document.removeEventListener("touchstart", handleTouchStart);
      document.removeEventListener("touchmove", handleTouchMove);
      document.removeEventListener("touchend", handleTouchEnd);
    };
  }, [onTouchStart, onTouchMove, onTouchEnd]);

  // Ekran boyutu değiştiğinde sidebar durumunu güncelle
  useEffect(() => {
    const handleResize = () => {
      if (window.innerWidth >= 992) {
        setSidebarOpen(true);
      }
    };
    window.addEventListener("resize", handleResize);
    return () => window.removeEventListener("resize", handleResize);
  }, []);

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
    [isSuperAdmin, hasPermission, hasAnyPermission],
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
      },
      {
        path: "/admin/posters",
        icon: "fas fa-image",
        label: "Poster Yönetimi",
        permission: PERMISSIONS.BANNERS_VIEW,
      },
      {
        path: "/admin/home-blocks",
        icon: "fas fa-th-large",
        label: "Ana Sayfa Blokları",
        permission: PERMISSIONS.BANNERS_VIEW,
      },
      {
        path: "/admin/weight-management",
        icon: "fas fa-weight",
        label: "Ağırlık Raporları",
        permission: PERMISSIONS.ORDERS_VIEW,
      },
      {
        path: "/admin/campaigns",
        icon: "fas fa-gift",
        label: "Kampanya Yönetimi",
        permission: PERMISSIONS.CAMPAIGNS_VIEW,
      },
      // =============================================================================
      // Kargo Ayarları - Araç tipi bazlı kargo ücretleri yönetimi
      // =============================================================================
      {
        path: "/admin/shipping-settings",
        icon: "fas fa-shipping-fast",
        label: "Kargo Ayarları",
        permission: PERMISSIONS.SETTINGS_SYSTEM,
      },
      // =============================================================================
      // Kupon Yönetimi - İndirim kuponları için admin sayfası
      // =============================================================================
      {
        path: "/admin/coupons",
        icon: "fas fa-ticket-alt",
        label: "Kupon Yönetimi",
        permission: PERMISSIONS.COUPONS_VIEW,
      },
      // =============================================================================
      // Bülten Yönetimi - Newsletter aboneleri ve toplu e-posta
      // =============================================================================
      {
        path: "/admin/newsletter",
        icon: "fas fa-envelope-open-text",
        label: "Bülten Yönetimi",
        permission: PERMISSIONS.NEWSLETTER_VIEW,
      },
      {
        label: "Loglar",
        icon: "fas fa-clipboard-list",
        permission: PERMISSIONS.LOGS_VIEW,
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
    [],
  );

  // StoreAttendant rolü kontrolü
  const isStoreAttendant = user?.role === "StoreAttendant";

  // Filtrelenmiş menü öğeleri
  const filteredMenuItems = useMemo(() => {
    return menuItems.filter((item) => {
      // StoreAttendant için sadece Dashboard ve Siparişler göster
      if (isStoreAttendant) {
        const allowedPaths = ["/admin/dashboard", "/admin/orders"];
        if (!allowedPaths.includes(item.path)) return false;
      }

      // SuperAdmin kontrolü
      if (item.superAdminOnly && !isSuperAdmin) return false;

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
  }, [menuItems, isSuperAdmin, isStoreAttendant, checkPermission]);

  // Mobilde menü öğesine tıklandığında sidebar'ı kapat
  const handleMenuClick = useCallback(() => {
    if (typeof window !== "undefined" && window.innerWidth < 992) {
      setSidebarOpen(false);
    }
  }, []);

  useEffect(() => {
    const parentWithChild = filteredMenuItems.find(
      (item) =>
        (item.children || item.filteredChildren) &&
        (item.filteredChildren || item.children).some((child) =>
          location.pathname.startsWith(child.path),
        ),
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
      {/* ====================================================================
          MOBİL OVERLAY - Sidebar açıkken arkaplanı karartan katman
          Mobilde sidebar açıkken tıklanınca kapanmasını sağlar
          ==================================================================== */}
      {sidebarOpen && (
        <div
          className="d-lg-none position-fixed top-0 start-0 w-100 h-100"
          style={{
            backgroundColor: "rgba(0,0,0,0.5)",
            zIndex: 1040,
          }}
          onClick={() => setSidebarOpen(false)}
        />
      )}

      {/* ====================================================================
          SIDEBAR - Scroll özellikli, mobil uyumlu
          Header sabit, menüler scrollable, logout butonu altta sabit
          ==================================================================== */}
      <div
        className={`text-white sidebar-container ${
          sidebarOpen ? "sidebar-open" : "sidebar-closed"
        }`}
        style={{
          height: "100vh", // vh kullanarak tam ekran yüksekliği
          width: "240px",
          background: "linear-gradient(180deg, #2d3748 0%, #1a202c 100%)",
          boxShadow: "2px 0 10px rgba(0,0,0,0.1)",
          position: "fixed",
          left: 0,
          top: 0,
          zIndex: 1050,
          transform: sidebarOpen ? "translateX(0)" : "translateX(-100%)",
          transition: "transform 0.3s ease-in-out",
          display: "flex",
          flexDirection: "column", // Flex column yapısı ile header-content-footer ayrımı
          overflow: "hidden", // Ana container scroll etmemeli
        }}
      >
        {/* ================================================================
            SIDEBAR HEADER - Sabit üstte
            ================================================================ */}
        <div
          className="p-3 border-bottom d-flex align-items-center justify-content-between flex-shrink-0"
          style={{ borderColor: "rgba(255,255,255,0.1) !important" }}
        >
          <h5 className="mb-0 fw-bold">
            <i
              className="fas fa-shield-alt me-2"
              style={{ color: "#f57c00" }}
            ></i>
            {user?.role === "StoreAttendant"
              ? "Sipariş Hazırlık"
              : "Admin Panel"}
          </h5>
          {/* Mobilde sidebar kapatma butonu */}
          <button
            className="btn btn-link text-white d-lg-none p-0"
            onClick={() => setSidebarOpen(false)}
            style={{ fontSize: "1.25rem" }}
          >
            <i className="fas fa-times"></i>
          </button>
        </div>

        {/* ================================================================
            KULLANICI BİLGİSİ - Sabit
            ================================================================ */}
        <div className="p-3 flex-shrink-0">
          <div className="d-flex align-items-center mb-3">
            <div
              className="rounded-circle d-flex align-items-center justify-content-center me-3"
              style={{
                width: "36px",
                height: "36px",
                background:
                  user?.role === "StoreAttendant"
                    ? "linear-gradient(135deg, #3b82f6, #60a5fa)"
                    : "linear-gradient(135deg, #f57c00, #ff9800)",
              }}
            >
              <i
                className={`fas ${user?.role === "StoreAttendant" ? "fa-box" : "fa-user"} text-white`}
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
                {user?.role === "StoreAttendant"
                  ? "Market Görevlisi"
                  : user?.role || "Yönetici"}
              </small>
            </div>
          </div>
        </div>

        {/* ================================================================
            NAVİGASYON MENÜSÜ - SCROLLABLE ALAN
            flex-grow-1 ile kalan alanı kaplar, overflow-y-auto ile scroll
            ================================================================ */}
        <nav
          className="sidebar-nav-scroll flex-grow-1"
          style={{
            overflowY: "auto",
            overflowX: "hidden",
            paddingBottom: "80px", // Logout butonu için alan
          }}
        >
          {filteredMenuItems.map((item, index) => {
            // Children varsa (alt menü)
            const children = item.filteredChildren || item.children;
            if (children && children.length > 0) {
              const isActiveChild = children.some((child) =>
                location.pathname.startsWith(child.path),
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
                          onClick={handleMenuClick}
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
                onClick={handleMenuClick}
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
                {item.path === "/admin/weight-management" && (
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

        {/* ================================================================
            SIDEBAR FOOTER - Çıkış Butonu (Sabit Altta)
            position: absolute ile her zaman sidebar'ın altında görünür
            ================================================================ */}
        <div
          className="sidebar-footer flex-shrink-0"
          style={{
            position: "absolute",
            bottom: 0,
            left: 0,
            right: 0,
            padding: "16px",
            background: "linear-gradient(180deg, transparent 0%, #1a202c 30%)",
            borderTop: "1px solid rgba(255,255,255,0.1)",
          }}
        >
          <button
            className="btn w-100 d-flex align-items-center justify-content-center gap-2"
            onClick={handleLogout}
            style={{
              background: "rgba(220, 38, 38, 0.15)",
              color: "#fca5a5",
              border: "1px solid rgba(220, 38, 38, 0.3)",
              borderRadius: "8px",
              padding: "10px 16px",
              fontSize: "0.9rem",
              fontWeight: "500",
              transition: "all 0.2s",
            }}
            onMouseEnter={(e) => {
              e.target.style.background = "rgba(220, 38, 38, 0.25)";
              e.target.style.borderColor = "rgba(220, 38, 38, 0.5)";
            }}
            onMouseLeave={(e) => {
              e.target.style.background = "rgba(220, 38, 38, 0.15)";
              e.target.style.borderColor = "rgba(220, 38, 38, 0.3)";
            }}
          >
            <i className="fas fa-sign-out-alt"></i>
            <span>Çıkış Yap</span>
          </button>
        </div>
      </div>

      {/* Main Content */}
      <div
        className="flex-grow-1 main-content-wrapper"
        style={{
          // Mobilde margin ve width CSS @media query ile override edilir
          marginLeft: sidebarOpen ? "240px" : "0",
          width: sidebarOpen ? "calc(100% - 240px)" : "100%",
          minHeight: "100vh",
          transition: "margin-left 0.3s ease-in-out, width 0.3s ease-in-out",
        }}
      >
        {/* Top Navigation */}
        <nav
          className="navbar navbar-expand-lg bg-white shadow-sm sticky-top"
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
        <main
          className="p-4 p-md-4 p-sm-2 admin-layout-main"
          style={{
            maxWidth: "100%",
            overflowX: "hidden",
          }}
        >
          {children}
        </main>
      </div>

      {/* ================================================================
          SESSION WARNING MODAL
          Oturum süresi dolmak üzereyken uyarı gösterir
          ================================================================ */}
      <SessionWarningModal
        isOpen={showSessionWarning && isWarning}
        remainingTime={remainingTimeFormatted}
        onExtend={handleExtendSession}
        onLogout={handleLogout}
      />
    </div>
  );
}

// CSS için inline styles
const styles = `
/* ====================================================================
   MOBİL RESPONSİVE DÜZELTMELERİ
   Admin paneli mobilde düzgün görünmesi için
   ==================================================================== */

/* Tüm cihazlar için temel stil */
.admin-layout-root {
  overflow-x: hidden;
}

/* ====================================================================
   SIDEBAR SCROLL STİLLERİ
   Menü scroll edilebilir, modern görünümlü scrollbar
   ==================================================================== */
.sidebar-nav-scroll {
  scrollbar-width: thin; /* Firefox */
  scrollbar-color: rgba(255,255,255,0.3) transparent; /* Firefox */
}

/* Webkit (Chrome, Safari, Edge) scrollbar stilleri */
.sidebar-nav-scroll::-webkit-scrollbar {
  width: 6px;
}

.sidebar-nav-scroll::-webkit-scrollbar-track {
  background: transparent;
}

.sidebar-nav-scroll::-webkit-scrollbar-thumb {
  background: rgba(255,255,255,0.2);
  border-radius: 3px;
}

.sidebar-nav-scroll::-webkit-scrollbar-thumb:hover {
  background: rgba(255,255,255,0.35);
}

/* Scroll yapıldığında scrollbar görünsün */
.sidebar-nav-scroll:hover::-webkit-scrollbar-thumb {
  background: rgba(255,255,255,0.3);
}

/* Mobil swipe indicator - sol kenardan kaydırma ipucu */
@media (max-width: 991.98px) {
  .admin-layout-root::before {
    content: '';
    position: fixed;
    left: 0;
    top: 50%;
    transform: translateY(-50%);
    width: 4px;
    height: 60px;
    background: linear-gradient(180deg, transparent, rgba(245, 124, 0, 0.3), transparent);
    border-radius: 0 4px 4px 0;
    z-index: 1000;
    opacity: 0.6;
    animation: swipeHint 2s ease-in-out infinite;
  }
}

@keyframes swipeHint {
  0%, 100% { opacity: 0.3; transform: translateY(-50%) translateX(0); }
  50% { opacity: 0.7; transform: translateY(-50%) translateX(3px); }
}

/* Mobil cihazlar için (992px altı) */
@media (max-width: 991.98px) {
  .main-content-wrapper {
    margin-left: 0 !important;
    width: 100% !important;
    padding-left: 0 !important;
    padding-right: 0 !important;
  }
  
  .sidebar-container {
    position: fixed !important;
    z-index: 1050 !important;
    width: 260px !important;
    box-shadow: 4px 0 25px rgba(0,0,0,0.3) !important;
  }
  
  .sidebar-closed {
    transform: translateX(-100%) !important;
  }
  
  .sidebar-open {
    transform: translateX(0) !important;
  }
  
  .admin-layout-main {
    padding: 0.75rem !important;
  }
  
  /* Tablo responsive */
  .table-responsive {
    font-size: 0.8rem;
  }
  
  .table-responsive th,
  .table-responsive td {
    padding: 0.5rem 0.4rem !important;
    white-space: nowrap;
  }
  
  /* Kart başlıkları */
  .card-header h5,
  .card-header h6 {
    font-size: 0.9rem !important;
  }
  
  /* Navbar padding */
  .navbar .container-fluid {
    padding-left: 0.5rem !important;
    padding-right: 0.5rem !important;
  }
  
  /* Form elemanları */
  .form-control,
  .form-select {
    font-size: 0.85rem !important;
    padding: 0.4rem 0.6rem !important;
  }
  
  /* Butonlar */
  .btn {
    font-size: 0.8rem !important;
    padding: 0.35rem 0.6rem !important;
  }
  
  .btn-sm {
    font-size: 0.75rem !important;
    padding: 0.25rem 0.5rem !important;
  }
}

/* Çok küçük ekranlar için (576px altı) */
@media (max-width: 575.98px) {
  .sidebar-container {
    width: 200px !important;
  }
  
  .admin-layout-main {
    padding: 0.5rem !important;
  }
  
  .card {
    margin-bottom: 0.75rem !important;
  }
  
  .card-body {
    padding: 0.75rem !important;
  }
  
  h1, .h1 { font-size: 1.5rem !important; }
  h2, .h2 { font-size: 1.25rem !important; }
  h3, .h3 { font-size: 1.1rem !important; }
  h4, .h4 { font-size: 1rem !important; }
  h5, .h5 { font-size: 0.9rem !important; }
}

/* Desktop için (992px ve üstü) */
@media (min-width: 992px) {
  .sidebar-container {
    transform: translateX(0) !important;
  }
  
  .main-content-wrapper {
    margin-left: 240px !important;
    width: calc(100% - 240px) !important;
  }
}

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
