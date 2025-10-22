// src/pages/Admin/AdminLogin.jsx
import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../contexts/AuthContext";

export default function AdminLogin() {
  const [credentials, setCredentials] = useState({
    email: "",
    password: "",
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const { login, setUser } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    try {
      // Admin veya Demo Admin girişi
      const isRealAdmin =
        credentials.email === "admin@admin.com" &&
        credentials.password === "admin123";
      const isDemoAdmin =
        credentials.email === "demo@example.com" &&
        credentials.password === "123456";

      if (isRealAdmin || isDemoAdmin) {
        const adminUser = {
          id: isRealAdmin ? "admin-1" : "demo-admin-1",
          name: isRealAdmin ? "Admin User" : "Demo User",
          email: credentials.email,
          role: "Admin",
          isAdmin: true,
        };

        // localStorage'a admin bilgilerini kaydet
        localStorage.setItem("user", JSON.stringify(adminUser));
        localStorage.setItem("userId", adminUser.id);
        localStorage.setItem("authToken", "admin_token_" + Date.now());

        // Auth context'i güncelle
        setUser?.(adminUser);

        navigate("/admin/dashboard");
      } else {
        setError("Geçersiz email veya şifre");
      }
    } catch (err) {
      setError("Giriş yapılırken bir hata oluştu");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      className="min-vh-100 d-flex align-items-center justify-content-center position-relative"
      style={{
        background:
          "linear-gradient(135deg, #f57c00 0%, #ff9800 50%, #ff5722 100%)",
        overflow: "hidden",
      }}
    >
      {/* Animated Background Elements */}
      <div className="position-absolute w-100 h-100">
        <div
          className="position-absolute rounded-circle opacity-10"
          style={{
            width: "300px",
            height: "300px",
            background: "rgba(255,255,255,0.1)",
            top: "-150px",
            right: "-150px",
            animation: "float 6s ease-in-out infinite",
          }}
        ></div>
        <div
          className="position-absolute rounded-circle opacity-10"
          style={{
            width: "200px",
            height: "200px",
            background: "rgba(255,255,255,0.1)",
            bottom: "-100px",
            left: "-100px",
            animation: "float 8s ease-in-out infinite reverse",
          }}
        ></div>
      </div>

      <div className="container position-relative">
        <div className="row justify-content-center">
          <div className="col-md-6 col-lg-4">
            <div
              className="card border-0 shadow-2xl"
              style={{
                background: "rgba(255, 255, 255, 0.95)",
                backdropFilter: "blur(20px)",
                borderRadius: "20px",
              }}
            >
              <div className="card-body p-5">
                <div className="text-center mb-4">
                  <div
                    className="d-inline-flex align-items-center justify-content-center rounded-circle mb-4"
                    style={{
                      width: "80px",
                      height: "80px",
                      background: "linear-gradient(135deg, #f57c00, #ff9800)",
                      boxShadow: "0 10px 30px rgba(255, 152, 0, 0.4)",
                    }}
                  >
                    <i className="fas fa-shield-alt fa-2x text-white"></i>
                  </div>
                  <h2 className="fw-bold mb-2" style={{ color: "#2d3748" }}>
                    Admin Paneli
                  </h2>
                  <p className="text-muted mb-0">
                    Yönetim sistemine güvenli giriş
                  </p>
                </div>

                {error && (
                  <div
                    className="alert border-0 mb-4"
                    style={{
                      background: "linear-gradient(135deg, #ff6b6b, #ee5a24)",
                      color: "white",
                      borderRadius: "12px",
                      boxShadow: "0 5px 15px rgba(255, 107, 107, 0.3)",
                    }}
                    role="alert"
                  >
                    <i className="fas fa-exclamation-circle me-2"></i>
                    {error}
                  </div>
                )}

                <form onSubmit={handleSubmit}>
                  <div className="mb-4">
                    <label
                      htmlFor="email"
                      className="form-label fw-semibold text-dark mb-2"
                    >
                      <i
                        className="fas fa-envelope me-2"
                        style={{ color: "#f57c00" }}
                      ></i>
                      Email Adresi
                    </label>
                    <input
                      type="email"
                      className="form-control border-0 py-3 px-4"
                      style={{
                        background: "rgba(245, 124, 0, 0.05)",
                        borderRadius: "12px",
                        fontSize: "16px",
                        boxShadow: "inset 0 2px 4px rgba(245, 124, 0, 0.1)",
                      }}
                      id="email"
                      value={credentials.email}
                      onChange={(e) =>
                        setCredentials({
                          ...credentials,
                          email: e.target.value,
                        })
                      }
                      required
                      placeholder="Yönetici email adresinizi girin"
                    />
                  </div>

                  <div className="mb-4">
                    <label
                      htmlFor="password"
                      className="form-label fw-semibold text-dark mb-2"
                    >
                      <i
                        className="fas fa-lock me-2"
                        style={{ color: "#f57c00" }}
                      ></i>
                      Şifre
                    </label>
                    <input
                      type="password"
                      className="form-control border-0 py-3 px-4"
                      style={{
                        background: "rgba(245, 124, 0, 0.05)",
                        borderRadius: "12px",
                        fontSize: "16px",
                        boxShadow: "inset 0 2px 4px rgba(245, 124, 0, 0.1)",
                      }}
                      id="password"
                      value={credentials.password}
                      onChange={(e) =>
                        setCredentials({
                          ...credentials,
                          password: e.target.value,
                        })
                      }
                      required
                      placeholder="Şifrenizi girin"
                    />
                  </div>

                  <button
                    type="submit"
                    className="btn w-100 fw-semibold py-3 border-0 text-white position-relative overflow-hidden"
                    style={{
                      background: "linear-gradient(135deg, #f57c00, #ff9800)",
                      borderRadius: "12px",
                      fontSize: "16px",
                      boxShadow: "0 8px 25px rgba(245, 124, 0, 0.4)",
                      transition: "all 0.3s ease",
                    }}
                    disabled={loading}
                    onMouseEnter={(e) => {
                      e.target.style.transform = "translateY(-2px)";
                      e.target.style.boxShadow =
                        "0 12px 35px rgba(245, 124, 0, 0.5)";
                    }}
                    onMouseLeave={(e) => {
                      e.target.style.transform = "translateY(0)";
                      e.target.style.boxShadow =
                        "0 8px 25px rgba(245, 124, 0, 0.4)";
                    }}
                  >
                    {loading ? (
                      <>
                        <span
                          className="spinner-border spinner-border-sm me-2"
                          role="status"
                        ></span>
                        Giriş Yapılıyor...
                      </>
                    ) : (
                      <>
                        <i className="fas fa-sign-in-alt me-2"></i>
                        Güvenli Giriş
                      </>
                    )}
                  </button>
                </form>

                <div className="text-center mt-4">
                  <small style={{ color: "#64748b" }}>
                    <i className="fas fa-shield-check me-2"></i>
                    Güvenli ve yetkili erişim gereklidir
                  </small>
                </div>

                {/* Test bilgileri - modern tasarım */}
                <div
                  className="mt-4 p-3 border-0"
                  style={{
                    background: "linear-gradient(135deg, #fff3e0, #ffe0b2)",
                    borderRadius: "12px",
                    border: "1px solid rgba(245, 124, 0, 0.2)",
                  }}
                >
                  <div className="row text-center">
                    <div className="col-12 mb-2">
                      <small className="fw-bold" style={{ color: "#f57c00" }}>
                        <i className="fas fa-key me-1"></i>
                        Test Bilgileri
                      </small>
                    </div>
                    <div className="col-6">
                      <small className="d-block text-muted mb-1">Email:</small>
                      <small
                        className="fw-semibold"
                        style={{ color: "#2d3748" }}
                      >
                        admin@admin.com
                      </small>
                    </div>
                    <div className="col-6">
                      <small className="d-block text-muted mb-1">Şifre:</small>
                      <small
                        className="fw-semibold"
                        style={{ color: "#2d3748" }}
                      >
                        admin123
                      </small>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* CSS Animations */}
      <style jsx>{`
        @keyframes float {
          0%,
          100% {
            transform: translateY(0px) rotate(0deg);
          }
          50% {
            transform: translateY(-20px) rotate(180deg);
          }
        }

        .card {
          animation: slideInUp 0.6s ease-out;
        }

        @keyframes slideInUp {
          from {
            opacity: 0;
            transform: translateY(30px);
          }
          to {
            opacity: 1;
            transform: translateY(0);
          }
        }

        .form-control:focus {
          outline: none !important;
          box-shadow: 0 0 0 3px rgba(245, 124, 0, 0.2) !important;
          background: rgba(245, 124, 0, 0.08) !important;
        }
      `}</style>
    </div>
  );
}
