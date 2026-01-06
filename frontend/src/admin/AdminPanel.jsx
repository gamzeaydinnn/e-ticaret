import React, { useState, useEffect } from "react";
import AdminDashboard from "../pages/Admin/Dashboard";
import AdminProducts from "../pages/Admin/AdminProducts";
import AdminCategories from "../pages/Admin/AdminCategories";
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
    { id: "dashboard", icon: "fas fa-chart-pie", label: "Dashboard" },
    { id: "products", icon: "fas fa-box", label: "Ürünler" },
    { id: "categories", icon: "fas fa-tags", label: "Kategoriler" },
    { id: "orders", icon: "fas fa-shopping-bag", label: "Siparişler" },
    { id: "users", icon: "fas fa-users", label: "Kullanıcılar" },
    { id: "coupons", icon: "fas fa-ticket-alt", label: "Kuponlar" },
    { id: "weights", icon: "fas fa-balance-scale", label: "Ağırlık Raporları", color: "purple" },
  ];

  return (
    <div className="d-flex admin-panel-root" style={{ minHeight: "100vh" }}>
      <style>{`
        .admin-sidebar {
          width: 240px;
          min-width: 240px;
          background: linear-gradient(180deg, #1e293b 0%, #0f172a 100%);
          color: white;
          display: flex;
          flex-direction: column;
          box-shadow: 2px 0 15px rgba(0,0,0,0.1);
        }
        .admin-logo {
          padding: 1.25rem;
          border-bottom: 1px solid rgba(255,255,255,0.08);
          display: flex;
          align-items: center;
          gap: 10px;
        }
        .admin-logo-icon {
          width: 36px;
          height: 36px;
          background: linear-gradient(135deg, #f97316, #fb923c);
          border-radius: 10px;
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 1.1rem;
        }
        .admin-logo-text {
          font-weight: 700;
          font-size: 1.1rem;
          color: #fff;
        }
        .admin-logo-sub {
          font-size: 0.7rem;
          color: rgba(255,255,255,0.5);
          margin-top: 2px;
        }
        .admin-user-info {
          padding: 1rem 1.25rem;
          border-bottom: 1px solid rgba(255,255,255,0.08);
          display: flex;
          align-items: center;
          gap: 10px;
        }
        .admin-user-avatar {
          width: 38px;
          height: 38px;
          background: linear-gradient(135deg, #f97316, #ea580c);
          border-radius: 50%;
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 1rem;
          font-weight: 600;
          color: white;
        }
        .admin-user-name {
          font-weight: 600;
          font-size: 0.9rem;
          color: #fff;
        }
        .admin-user-role {
          font-size: 0.7rem;
          color: rgba(255,255,255,0.5);
        }
        .admin-nav {
          flex: 1;
          padding: 0.75rem;
          overflow-y: auto;
        }
        .admin-nav-item {
          display: flex;
          align-items: center;
          gap: 10px;
          padding: 0.7rem 0.9rem;
          border-radius: 8px;
          border: none;
          background: transparent;
          color: rgba(255,255,255,0.7);
          font-size: 0.85rem;
          font-weight: 500;
          width: 100%;
          text-align: left;
          cursor: pointer;
          transition: all 0.2s ease;
          margin-bottom: 2px;
        }
        .admin-nav-item:hover {
          background: rgba(255,255,255,0.08);
          color: #fff;
        }
        .admin-nav-item.active {
          background: linear-gradient(135deg, #f97316, #ea580c);
          color: #fff;
          box-shadow: 0 4px 12px rgba(249, 115, 22, 0.3);
        }
        .admin-nav-item.active.purple {
          background: linear-gradient(135deg, #8b5cf6, #7c3aed);
          box-shadow: 0 4px 12px rgba(139, 92, 246, 0.3);
        }
        .admin-nav-icon {
          font-size: 1rem;
          width: 22px;
          text-align: center;
        }
        .admin-logout-section {
          padding: 1rem;
          border-top: 1px solid rgba(255,255,255,0.08);
        }
        .admin-logout-btn {
          width: 100%;
          padding: 0.6rem;
          border-radius: 8px;
          border: 1px solid rgba(255,255,255,0.2);
          background: transparent;
          color: rgba(255,255,255,0.7);
          font-size: 0.8rem;
          font-weight: 500;
          cursor: pointer;
          transition: all 0.2s ease;
          display: flex;
          align-items: center;
          justify-content: center;
          gap: 6px;
        }
        .admin-logout-btn:hover {
          background: rgba(239, 68, 68, 0.15);
          border-color: #ef4444;
          color: #ef4444;
        }
        .admin-content {
          flex: 1;
          background: #f1f5f9;
          overflow-y: auto;
        }
        .admin-header {
          background: #fff;
          padding: 1rem 1.5rem;
          border-bottom: 1px solid #e2e8f0;
          display: flex;
          align-items: center;
          justify-content: space-between;
        }
        .admin-header-title {
          font-weight: 600;
          font-size: 1.1rem;
          color: #1e293b;
        }
        .admin-header-time {
          font-size: 0.8rem;
          color: #64748b;
        }
        .admin-main {
          padding: 1.5rem;
        }
        @media (max-width: 768px) {
          .admin-panel-root {
            flex-direction: column;
          }
          .admin-sidebar {
            width: 100%;
            min-width: 0;
          }
          .admin-content {
            width: 100%;
          }
          .admin-header {
            flex-direction: column;
            align-items: flex-start;
            gap: 0.5rem;
          }
          .admin-main {
            padding: 1rem;
          }
          .admin-users-header h2 {
            font-size: 1.25rem;
          }
          .admin-users-actions,
          .admin-users-actions .btn {
            width: 100%;
          }
          .admin-users-page .table-responsive {
            overflow-x: visible;
          }
          .admin-users-page .admin-users-table thead {
            display: none;
          }
          .admin-users-page .admin-users-table tbody tr {
            display: block;
            margin-bottom: 0.75rem;
            border: 1px solid #e2e8f0;
            border-radius: 12px;
            padding: 0.75rem;
            background: #fff;
          }
          .admin-users-page .admin-users-table tbody td {
            display: flex;
            align-items: center;
            justify-content: space-between;
            padding: 0.35rem 0;
            border: 0;
          }
          .admin-users-page .admin-users-table tbody td::before {
            content: attr(data-label);
            font-weight: 600;
            color: #475569;
            padding-right: 1rem;
          }
          .admin-users-page .admin-users-action-btn {
            width: 100%;
          }
        }
      `}</style>
      
      {/* Sidebar */}
      <div className="admin-sidebar">
        {/* Logo */}
        <div className="admin-logo">
          <div className="admin-logo-icon"><i className="fas fa-shield-alt"></i></div>
          <div>
            <div className="admin-logo-text">Admin Panel</div>
            <div className="admin-logo-sub">Yönetim Merkezi</div>
          </div>
        </div>
        
        {/* User Info */}
        <div className="admin-user-info">
          <div className="admin-user-avatar">A</div>
          <div>
            <div className="admin-user-name">Admin User</div>
            <div className="admin-user-role">Yönetici</div>
          </div>
        </div>
        
        {/* Navigation */}
        <nav className="admin-nav">
          {sidebarItems.map((item) => (
            <button
              key={item.id}
              className={`admin-nav-item ${activeTab === item.id ? 'active' : ''} ${item.color === 'purple' ? 'purple' : ''}`}
              onClick={() => setActiveTab(item.id)}
            >
              <i className={`admin-nav-icon ${item.icon}`}></i>
              <span>{item.label}</span>
            </button>
          ))}
        </nav>
        
        {/* Logout */}
        <div className="admin-logout-section">
          <button className="admin-logout-btn" onClick={handleLogout}>
            <i className="fas fa-sign-out-alt"></i>
            <span>Çıkış Yap</span>
          </button>
        </div>
      </div>
      
      {/* Content */}
      <div className="admin-content">
        <div className="admin-header">
          <div className="admin-header-title">
            {sidebarItems.find(i => i.id === activeTab)?.label || 'Dashboard'}
          </div>
          <div className="admin-header-time">
            {new Date().toLocaleDateString('tr-TR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
          </div>
        </div>
        <div className="admin-main">
          {activeTab === "dashboard" && <AdminDashboard />}
          {activeTab === "products" && <AdminProducts />}
          {activeTab === "categories" && <AdminCategories />}
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
