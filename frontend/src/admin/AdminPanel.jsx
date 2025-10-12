import React, { useState, useEffect } from "react";
import AdminDashboard from "../pages/Admin/Dashboard";
import AdminProducts from "../pages/Admin/AdminProducts";
import AdminOrders from "../pages/Admin/AdminOrders";
import AdminUsers from "../pages/Admin/AdminUsers";

const AdminPanel = () => {
  const [activeTab, setActiveTab] = useState("dashboard");
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [loginData, setLoginData] = useState({ username: "", password: "" });

  useEffect(() => {
    // Admin kimlik doğrulama kontrolü
    const adminToken = localStorage.getItem("adminToken");
    if (adminToken) {
      setIsAuthenticated(true);
    }
  }, []);

  const handleLogin = async (e) => {
    e.preventDefault();
    // Demo için basit authentication
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

  return (
    <div className="d-flex" style={{ minHeight: "100vh" }}>
      {/* Sidebar */}
      <div
        className="sidebar"
        style={{
          width: "250px",
          background: "linear-gradient(180deg, #ff6f00 0%, #ff8f00 100%)",
          color: "white",
        }}
      >
        <div className="p-4">
          <h4 className="fw-bold mb-4">Admin Panel</h4>
          <nav className="nav flex-column">
            {[
              { id: "dashboard", icon: "📊", label: "Dashboard" },
              { id: "products", icon: "📦", label: "Ürünler" },
              { id: "orders", icon: "🛍️", label: "Siparişler" },
              { id: "users", icon: "👥", label: "Kullanıcılar" },
            ].map((item) => (
              <button
                key={item.id}
                className={`nav-link text-white border-0 rounded mb-2 p-3 ${
                  activeTab === item.id ? "bg-white bg-opacity-25" : ""
                }`}
                onClick={() => setActiveTab(item.id)}
                style={{ backgroundColor: "transparent", textAlign: "left" }}
              >
                <span className="me-2">{item.icon}</span>
                {item.label}
              </button>
            ))}
          </nav>
        </div>
        <div className="mt-auto p-4">
          <button
            className="btn btn-outline-light w-100"
            onClick={handleLogout}
          >
            Çıkış Yap
          </button>
        </div>
      </div>

      {/* Main Content */}
      <div className="flex-grow-1" style={{ background: "#f8f9fa" }}>
        <div className="p-4">
          {activeTab === "dashboard" && <AdminDashboard />}
          {activeTab === "products" && <AdminProducts />}
          {activeTab === "orders" && <AdminOrders />}
          {activeTab === "users" && <AdminUsers />}
        </div>
      </div>
    </div>
  );
};

export default AdminPanel;
