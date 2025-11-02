import React, { useState, useEffect } from "react";
import AdminDashboard from "../pages/Admin/Dashboard";
import AdminProducts from "../pages/Admin/AdminProducts";
import AdminOrders from "../pages/Admin/AdminOrders";
import AdminUsers from "../pages/Admin/AdminUsers";
import CouponManagement from "../pages/Admin/CouponManagement";
import WeightReportsPanel from "./WeightReportsPanel";

function AdminPanel() {
  const [activeTab, setActiveTab] = useState("dashboard");
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [loginData, setLoginData] = useState({ username: "", password: "" });

  useEffect(() => {
    const adminToken = localStorage.getItem("adminToken");
    if (adminToken) {
      setIsAuthenticated(true);
    }
  }, []);

  const handleLogin = async (e) => {
    e.preventDefault();
    if (loginData.username === "admin" && loginData.password === "admin123") {
      localStorage.setItem("adminToken", "demo-admin-token");
      setIsAuthenticated(true);
    } else {
      alert("Geçersiz kullanıcı adı veya şifre");
    }
  };

  const handleLogout = () => {
    localStorage.removeItem("adminToken");
    setIsAuthenticated(false);
    setActiveTab("dashboard");
  };

  if (!isAuthenticated) {
    return (
      <div
        className="min-vh-100 d-flex align-items-center justify-content-center"
        style={{
          background:
            "linear-gradient(135deg, #ff6f00 0%, #ff8f00 50%, #ffa000 100%)",
        }}
      >
        <div
          className="card shadow-lg"
          style={{ width: "400px", borderRadius: "20px" }}
        >
          <div className="card-body p-5">
            <div className="text-center mb-4">
              <h3 className="fw-bold text-dark">Admin Paneli</h3>
              <p className="text-muted">Yönetici girişi yapın</p>
            </div>
            <form onSubmit={handleLogin}>
              <div className="mb-3">
                <input
                  type="text"
                  className="form-control"
                  placeholder="Kullanıcı Adı"
                  value={loginData.username}
                  onChange={(e) =>
                    setLoginData({ ...loginData, username: e.target.value })
                  }
                  style={{ borderRadius: "10px" }}
                  required
                />
              </div>
              <div className="mb-4">
                <input
                  type="password"
                  className="form-control"
                  placeholder="Şifre"
                  value={loginData.password}
                  onChange={(e) =>
                    setLoginData({ ...loginData, password: e.target.value })
                  }
                  style={{ borderRadius: "10px" }}
                  required
                />
              </div>
              <button
                type="submit"
                className="btn w-100 text-white fw-bold"
                style={{
                  background: "linear-gradient(45deg, #ff6f00, #ff8f00)",
                  borderRadius: "10px",
                  border: "none",
                }}
              >
                Giriş Yap
              </button>
            </form>
            <div className="mt-3 text-center">
              <small className="text-muted">Demo: admin / admin123</small>
            </div>
          </div>
        </div>
      </div>
    );
  }

  const sidebarItems = [
    { id: "dashboard", icon: "📊", label: "Dashboard", color: "orange" },
    { id: "products", icon: "📦", label: "Ürünler", color: "orange" },
    { id: "orders", icon: "🛍️", label: "Siparişler", color: "orange" },
    { id: "users", icon: "👥", label: "Kullanıcılar", color: "orange" },
    { id: "coupons", icon: "🏷️", label: "Kuponlar", color: "orange" },
    {
      id: "weights",
      icon: "⚖️",
      label: "Ağırlık Raporları",
      color: "purple",
      badge: 3,
    },
  ];

  return (
    <div className="d-flex" style={{ minHeight: "100vh" }}>
      <style>{`
        @keyframes pulse {
          0%, 100% { transform: scale(1); box-shadow: 0 0 0 0 rgba(255, 107, 107, 0.7); }
          50% { transform: scale(1.05); box-shadow: 0 0 0 6px rgba(255, 107, 107, 0); }
        }
        @keyframes slideIn {
          from { transform: translateX(-10px); opacity: 0; }
          to { transform: translateX(0); opacity: 1; }
        }
        .menu-item-hover:hover {
          transform: translateX(5px);
          background: rgba(255, 255, 255, 0.15) !important;
        }
        .weight-menu-active {
          background: linear-gradient(135deg, #667eea 0%, #764ba2 100%) !important;
          box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4);
          border-left: 4px solid #fff !important;
        }
        .weight-menu-hover:hover {
          background: rgba(102, 126, 234, 0.2) !important;
        }
      `}</style>
      <div
        className="sidebar"
        style={{
          width: "260px",
          background: "linear-gradient(180deg, #2d3748 0%, #1a202c 100%)",
          color: "white",
          boxShadow: "4px 0 20px rgba(0,0,0,0.15)",
        }}
      >
        <div
          className="p-4 border-bottom"
          style={{ borderColor: "rgba(255,255,255,0.1)" }}
        >
          <h4 className="fw-bold mb-0 d-flex align-items-center">
            <span
              style={{
                background: "linear-gradient(135deg, #ff6f00, #ff9800)",
                padding: "8px 12px",
                borderRadius: "8px",
                marginRight: "10px",
              }}
            >
              🛡️
            </span>
            Admin Panel
          </h4>
        </div>
        <div className="p-3">
          <nav className="nav flex-column">
            {sidebarItems.map((item, index) => (
              <button
                key={item.id}
                className={`nav-link text-white border-0 rounded mb-2 p-3 position-relative menu-item-hover ${
                  item.color === "purple" && activeTab !== item.id
                    ? "weight-menu-hover"
                    : ""
                } ${
                  activeTab === item.id && item.color === "purple"
                    ? "weight-menu-active"
                    : ""
                }`}
                onClick={() => setActiveTab(item.id)}
                style={{
                  backgroundColor:
                    activeTab === item.id && item.color === "orange"
                      ? "rgba(255, 140, 0, 0.3)"
                      : "transparent",
                  textAlign: "left",
                  transition: "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
                  borderLeft:
                    activeTab === item.id && item.color === "orange"
                      ? "4px solid #ff8f00"
                      : "4px solid transparent",
                  fontWeight: activeTab === item.id ? "600" : "400",
                  animation: `slideIn 0.3s ease-out ${index * 0.05}s both`,
                }}
              >
                <span
                  className="me-2"
                  style={{
                    fontSize: "1.1rem",
                    filter:
                      activeTab === item.id
                        ? "drop-shadow(0 0 8px rgba(255,255,255,0.4))"
                        : "none",
                  }}
                >
                  {item.icon}
                </span>
                <span style={{ fontSize: "0.92rem" }}>{item.label}</span>
                {item.badge && (
                  <span
                    style={{
                      position: "absolute",
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
                      animation: "pulse 2s infinite",
                      minWidth: "24px",
                      textAlign: "center",
                    }}
                  >
                    {item.badge}
                  </span>
                )}
              </button>
            ))}
          </nav>
        </div>
        <div className="mt-auto p-4">
          <button
            className="btn btn-outline-light w-100 py-2"
            onClick={handleLogout}
            style={{
              borderRadius: "8px",
              fontWeight: "600",
              transition: "all 0.3s ease",
            }}
            onMouseEnter={(e) => {
              e.target.style.background = "rgba(255, 140, 0, 0.2)";
              e.target.style.borderColor = "#ff8f00";
              e.target.style.transform = "translateY(-2px)";
            }}
            onMouseLeave={(e) => {
              e.target.style.background = "transparent";
              e.target.style.borderColor = "rgba(255,255,255,0.5)";
              e.target.style.transform = "translateY(0)";
            }}
          >
            🚪 Çıkış Yap
          </button>
        </div>
      </div>
      <div className="flex-grow-1" style={{ background: "#f8f9fa" }}>
        <div className="p-4">
          {activeTab === "dashboard" && <AdminDashboard />}
          {activeTab === "products" && <AdminProducts />}
          {activeTab === "orders" && <AdminOrders />}
          {activeTab === "users" && <AdminUsers />}
          {activeTab === "coupons" && <CouponManagement />}
          {activeTab === "weights" && <WeightReportsPanel />}
        </div>
      </div>
    </div>
  );
}

export default AdminPanel;
