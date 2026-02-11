// src/pages/Admin/AdminLogin.jsx
// Admin paneli giriş sayfası - Backend API entegrasyonu ile
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../contexts/AuthContext";
import { AuthService } from "../../services/authService";

export default function AdminLogin() {
  const [credentials, setCredentials] = useState({
    email: "",
    password: "",
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const { setUser, loadUserPermissions } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    try {
      // ÖNCELİK 1: Backend API ile giriş dene
      const resp = await AuthService.login({
        email: credentials.email,
        password: credentials.password,
      });

      // Backend başarılı yanıt verdi
      const data = resp && resp.data === undefined ? resp : resp.data;

      if (data && (data.success || data.token || data.Token)) {
        const token = data.token || data.Token;
        const userData = data.user ||
          data.User || {
            id: data.id,
            email: credentials.email,
            firstName: data.firstName,
            lastName: data.lastName,
            name:
              data.name ||
              `${data.firstName ?? ""} ${data.lastName ?? ""}`.trim() ||
              "Admin User",
            role: data.role || "Admin",
            isAdmin:
              data.isAdmin ??
              (data.role === "Admin" || data.role === "SuperAdmin"),
          };

        // ============================================================================
        // Admin Paneline Erişim Kontrolü
        // Backend'deki Roles.GetAdminPanelRoles() ile senkronize tutulmalı
        // Tüm yönetici rolleri: SuperAdmin, Admin, StoreManager, CustomerSupport, Logistics
        // YENİ: StoreAttendant (Market Görevlisi), Dispatcher (Sevkiyat Görevlisi)
        // ============================================================================
        const ADMIN_PANEL_ROLES = [
          "SuperAdmin",
          "Admin",
          "StoreManager",
          "CustomerSupport",
          "Logistics",
          "StoreAttendant", // Market Görevlisi
          "Dispatcher", // Sevkiyat Görevlisi
        ];

        const userRole = userData.role || "";
        const hasAdminAccess =
          userData.isAdmin === true ||
          ADMIN_PANEL_ROLES.some(
            (role) => role.toLowerCase() === userRole.toLowerCase(),
          );

        if (!hasAdminAccess) {
          setError("Bu hesap admin paneline erişim yetkisine sahip değil!");
          setLoading(false);
          return;
        }

        // Token ve kullanıcı bilgilerini kaydet (tüm key'lere yaz - uyumluluk için)
        AuthService.saveToken(token); // 'token' key'ine yazar ve axios header'a ekler
        localStorage.setItem("authToken", token); // AdminGuard uyumluluğu için
        localStorage.setItem("user", JSON.stringify(userData));
        if (userData?.id != null) {
          localStorage.setItem("userId", String(userData.id));
        }

        // Auth context'i güncelle
        setUser?.(userData);

        // ============================================================================
        // KRİTİK: İzinleri yükle - Navigate etmeden önce
        // Bu işlem olmadan dashboard'a gidildiğinde 401/403 hataları alınır
        // loadUserPermissions artık izinleri döndürüyor, state güncelini bekle
        // ============================================================================
        console.log("⏳ İzinler yükleniyor...");
        let permissionsReady = false;
        try {
          if (loadUserPermissions) {
            // İzinleri yükle ve dönen değeri al
            const loadedPermissions = await loadUserPermissions(userData, true); // forceRefresh=true

            // İzinler başarıyla yüklendi mi kontrol et
            if (
              Array.isArray(loadedPermissions) &&
              loadedPermissions.length > 0
            ) {
              permissionsReady = true;
              console.log(
                "✅ İzinler yüklendi:",
                loadedPermissions.length,
                "adet",
              );
            } else {
              // İzin bulunamadı - localStorage'a varsayılan dashboard.view ekle (fallback)
              const fallbackPerms = ["dashboard.view"];
              localStorage.setItem(
                "userPermissions",
                JSON.stringify(fallbackPerms),
              );
              localStorage.setItem(
                "permissionsCacheTime",
                Date.now().toString(),
              );
              localStorage.setItem("permissionsCacheRole", userData.role);
              console.warn("⚠️ İzin bulunamadı, fallback kullanılıyor");
              permissionsReady = true;
            }

            // State güncelinin React tarafından işlenmesi için bekle
            await new Promise((resolve) => setTimeout(resolve, 150));
          }
        } catch (permError) {
          console.warn("⚠️ İzin yükleme hatası:", permError);
          // İzin hatası olsa bile devam et - PermissionManager'da fallback var
          permissionsReady = true;
        }

        // Token'ın axios'a set edildiğinden emin ol
        console.log("✅ Token kaydedildi:", token.substring(0, 20) + "...");
        console.log("✅ User data:", userData);
        console.log("✅ Permissions ready:", permissionsReady);

        // ============================================================================
        // ROL BAZLI YÖNLENDİRME
        // StoreAttendant ve Dispatcher için özel sayfalar
        // ============================================================================
        let targetPage = "/admin/dashboard";

        if (userData.role === "StoreAttendant") {
          targetPage = "/admin/orders"; // Sipariş hazırlama sayfasına yönlendir
        } else if (userData.role === "Dispatcher") {
          targetPage = "/admin/orders"; // Sevkiyat için sipariş sayfasına yönlendir
        }

        // ============================================================================
        // SAYFA YENİLEME - State senkronizasyonu için
        // navigate yerine window.location kullan (tam sayfa yenileme)
        // ============================================================================
        window.location.href = targetPage;
        return;
      }

      // Backend başarısız yanıt
      setError(data?.message || data?.error || "Giriş başarısız!");
    } catch (err) {
      // Sadece development ortamında detaylı log
      if (process.env.NODE_ENV !== "production") {
        console.error("Admin login error:", err);
      }

      // ÖNCELİK 2: Backend bağlantısı yoksa fallback demo login
      // GÜVENLİK: Demo login sadece development ortamında aktif
      const isDemoAdmin =
        process.env.NODE_ENV !== "production" &&
        credentials.email === "demo@example.com" &&
        credentials.password === "123456";

      if (isDemoAdmin) {
        const demoToken = "demo_admin_token_" + Date.now();
        const adminUser = {
          id: "demo-admin-1",
          name: "Demo Admin",
          email: credentials.email,
          role: "Admin",
          isAdmin: true,
        };

        // Token ve kullanıcı bilgilerini kaydet
        AuthService.saveToken(demoToken);
        localStorage.setItem("authToken", demoToken);
        localStorage.setItem("user", JSON.stringify(adminUser));
        localStorage.setItem("userId", adminUser.id);

        setUser?.(adminUser);
        navigate("/admin/dashboard");
      } else {
        // Hata mesajını kullanıcıya göster
        setError(err.message || "Geçersiz email veya şifre");
      }
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
