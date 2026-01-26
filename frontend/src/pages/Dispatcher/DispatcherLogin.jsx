// ==========================================================================
// DispatcherLogin.jsx - Sevkiyat Görevlisi Giriş Sayfası
// ==========================================================================
// E-posta + şifre ile giriş, "Beni hatırla" özelliği.
// DispatcherAuthContext ile entegre çalışır.
// NEDEN: Dispatcher rolü için ayrı giriş sayfası, güvenlik ve modülerlik sağlar.
// ==========================================================================

import React, { useState, useEffect } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { useDispatcherAuth } from "../../contexts/DispatcherAuthContext";

export default function DispatcherLogin() {
  // Form state
  const [formData, setFormData] = useState({
    email: "",
    password: "",
    rememberMe: false,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  const navigate = useNavigate();
  const location = useLocation();
  const { login, isAuthenticated, loading: authLoading } = useDispatcherAuth();

  // Zaten giriş yapmışsa dashboard'a yönlendir
  useEffect(() => {
    if (!authLoading && isAuthenticated) {
      const from = location.state?.from?.pathname || "/dispatch/dashboard";
      navigate(from);
    }
  }, [isAuthenticated, authLoading, navigate, location]);

  // Location state'den hata mesajı varsa göster
  useEffect(() => {
    if (location.state?.error) {
      setError(location.state.error);
    }
  }, [location.state]);

  // =========================================================================
  // FORM SUBMIT
  // =========================================================================
  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    const email = formData.email?.trim();
    const password = formData.password;

    // Validasyon
    if (!email) {
      setError("E-posta adresi gerekli");
      setLoading(false);
      return;
    }
    if (!isValidEmail(email)) {
      setError("Geçerli bir e-posta adresi girin");
      setLoading(false);
      return;
    }
    if (!password) {
      setError("Şifre gerekli");
      setLoading(false);
      return;
    }
    if (password.length < 6) {
      setError("Şifre en az 6 karakter olmalı");
      setLoading(false);
      return;
    }

    try {
      // DispatcherAuthContext login fonksiyonunu kullan
      const result = await login(email, password, formData.rememberMe);

      if (result.success) {
        // Başarılı giriş - Dashboard'a yönlendir
        const from = location.state?.from?.pathname || "/dispatch/dashboard";
        navigate(from);
      } else {
        // Hata mesajını göster
        setError(result.error || "Giriş başarısız");
      }
    } catch (err) {
      console.error("[DispatcherLogin] Giriş hatası:", err);
      setError("Giriş yapılırken bir hata oluştu");
    } finally {
      setLoading(false);
    }
  };

  // =========================================================================
  // E-POSTA VALİDASYONU
  // =========================================================================
  const isValidEmail = (email) => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  };

  // =========================================================================
  // INPUT DEĞİŞİKLİĞİ
  // =========================================================================
  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
    // Yazarken hata mesajını temizle
    if (error) setError("");
  };

  // Auth yüklenirken bekle
  if (authLoading) {
    return (
      <div
        className="min-vh-100 d-flex align-items-center justify-content-center"
        style={{
          background: "linear-gradient(135deg, #1a1a2e 0%, #16213e 100%)",
        }}
      >
        <div className="text-center">
          <div
            className="spinner-border text-info mb-3"
            role="status"
            style={{ width: "3rem", height: "3rem" }}
          >
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
          <p className="text-white-50">Oturum kontrol ediliyor...</p>
        </div>
      </div>
    );
  }

  return (
    <div
      className="min-vh-100 d-flex align-items-center justify-content-center py-5"
      style={{
        background: "linear-gradient(135deg, #1a1a2e 0%, #16213e 100%)",
      }}
    >
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-11 col-sm-10 col-md-8 col-lg-6 col-xl-5">
            {/* Login Card */}
            <div
              className="card border-0 shadow-lg"
              style={{
                borderRadius: "20px",
                background: "rgba(255, 255, 255, 0.95)",
                backdropFilter: "blur(10px)",
              }}
            >
              <div className="card-body p-4 p-md-5">
                {/* Header */}
                <div className="text-center mb-4">
                  {/* Sevkiyat İkonu */}
                  <div
                    className="d-inline-flex align-items-center justify-content-center mb-3"
                    style={{
                      width: "80px",
                      height: "80px",
                      borderRadius: "20px",
                      background:
                        "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
                      boxShadow: "0 10px 30px rgba(102, 126, 234, 0.4)",
                    }}
                  >
                    <i
                      className="fas fa-shipping-fast text-white"
                      style={{ fontSize: "2rem" }}
                    ></i>
                  </div>

                  <h2 className="fw-bold mb-1" style={{ color: "#1a1a2e" }}>
                    Sevkiyat Paneli
                  </h2>
                  <p className="text-muted mb-0">
                    Giriş yaparak siparişleri yönetin
                  </p>
                </div>

                {/* Hata Mesajı */}
                {error && (
                  <div
                    className="alert alert-danger d-flex align-items-center py-2 mb-4"
                    role="alert"
                    style={{ borderRadius: "12px" }}
                  >
                    <i className="fas fa-exclamation-circle me-2"></i>
                    <span>{error}</span>
                  </div>
                )}

                {/* Login Form */}
                <form onSubmit={handleSubmit}>
                  {/* E-posta */}
                  <div className="mb-3">
                    <label
                      htmlFor="email"
                      className="form-label fw-semibold text-muted small"
                    >
                      E-POSTA
                    </label>
                    <div className="input-group">
                      <span
                        className="input-group-text border-0"
                        style={{
                          background: "#f8f9fa",
                          borderRadius: "12px 0 0 12px",
                        }}
                      >
                        <i className="fas fa-envelope text-muted"></i>
                      </span>
                      <input
                        type="email"
                        className="form-control border-0 py-3"
                        id="email"
                        name="email"
                        placeholder="ornek@sirket.com"
                        value={formData.email}
                        onChange={handleChange}
                        disabled={loading}
                        autoComplete="email"
                        style={{
                          background: "#f8f9fa",
                          borderRadius: "0 12px 12px 0",
                        }}
                      />
                    </div>
                  </div>

                  {/* Şifre */}
                  <div className="mb-3">
                    <label
                      htmlFor="password"
                      className="form-label fw-semibold text-muted small"
                    >
                      ŞİFRE
                    </label>
                    <div className="input-group">
                      <span
                        className="input-group-text border-0"
                        style={{
                          background: "#f8f9fa",
                          borderRadius: "12px 0 0 12px",
                        }}
                      >
                        <i className="fas fa-lock text-muted"></i>
                      </span>
                      <input
                        type={showPassword ? "text" : "password"}
                        className="form-control border-0 py-3"
                        id="password"
                        name="password"
                        placeholder="••••••••"
                        value={formData.password}
                        onChange={handleChange}
                        disabled={loading}
                        autoComplete="current-password"
                        style={{
                          background: "#f8f9fa",
                          borderRadius: "0",
                        }}
                      />
                      <button
                        type="button"
                        className="input-group-text border-0"
                        onClick={() => setShowPassword(!showPassword)}
                        style={{
                          background: "#f8f9fa",
                          borderRadius: "0 12px 12px 0",
                          cursor: "pointer",
                        }}
                      >
                        <i
                          className={`fas ${showPassword ? "fa-eye-slash" : "fa-eye"} text-muted`}
                        ></i>
                      </button>
                    </div>
                  </div>

                  {/* Beni Hatırla */}
                  <div className="d-flex justify-content-between align-items-center mb-4">
                    <div className="form-check">
                      <input
                        type="checkbox"
                        className="form-check-input"
                        id="rememberMe"
                        name="rememberMe"
                        checked={formData.rememberMe}
                        onChange={handleChange}
                        disabled={loading}
                        style={{
                          width: "18px",
                          height: "18px",
                          cursor: "pointer",
                        }}
                      />
                      <label
                        className="form-check-label text-muted ms-1"
                        htmlFor="rememberMe"
                        style={{ cursor: "pointer" }}
                      >
                        Beni hatırla
                      </label>
                    </div>
                  </div>

                  {/* Giriş Butonu */}
                  <button
                    type="submit"
                    className="btn btn-lg w-100 text-white fw-semibold py-3"
                    disabled={loading}
                    style={{
                      background:
                        "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
                      borderRadius: "12px",
                      border: "none",
                      boxShadow: "0 5px 20px rgba(102, 126, 234, 0.4)",
                      transition: "all 0.3s ease",
                    }}
                    onMouseEnter={(e) => {
                      if (!loading) {
                        e.target.style.transform = "translateY(-2px)";
                        e.target.style.boxShadow =
                          "0 8px 25px rgba(102, 126, 234, 0.5)";
                      }
                    }}
                    onMouseLeave={(e) => {
                      e.target.style.transform = "translateY(0)";
                      e.target.style.boxShadow =
                        "0 5px 20px rgba(102, 126, 234, 0.4)";
                    }}
                  >
                    {loading ? (
                      <>
                        <span
                          className="spinner-border spinner-border-sm me-2"
                          role="status"
                          aria-hidden="true"
                        ></span>
                        Giriş yapılıyor...
                      </>
                    ) : (
                      <>
                        <i className="fas fa-sign-in-alt me-2"></i>
                        Giriş Yap
                      </>
                    )}
                  </button>
                </form>

                {/* Alt Bilgi */}
                <div className="text-center mt-4">
                  <p className="text-muted small mb-0">
                    <i className="fas fa-shield-alt me-1"></i>
                    Güvenli bağlantı ile korunmaktasınız
                  </p>
                </div>
              </div>
            </div>

            {/* Yardım Linki */}
            <div className="text-center mt-4">
              <p className="text-white-50 small mb-0">
                Sorun mu yaşıyorsunuz?{" "}
                <a
                  href="/yardim"
                  className="text-white text-decoration-none"
                  style={{ fontWeight: "500" }}
                >
                  Yardım Alın
                </a>
              </p>
            </div>

            {/* Ana Sayfa Linki */}
            <div className="text-center mt-3">
              <a
                href="/"
                className="text-white-50 small text-decoration-none d-inline-flex align-items-center"
              >
                <i className="fas fa-arrow-left me-2"></i>
                Ana Sayfaya Dön
              </a>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
