// ==========================================================================
// StoreAttendantLogin.jsx - Market Görevlisi Giriş Sayfası
// ==========================================================================
// Store Attendant rolü için login sayfası.
// Mobil uyumlu, modern UI tasarımı.
// ==========================================================================

import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useStoreAttendantAuth } from "../../contexts/StoreAttendantAuthContext";

export default function StoreAttendantLogin() {
  const navigate = useNavigate();
  const {
    login,
    isAuthenticated,
    loading: authLoading,
  } = useStoreAttendantAuth();

  // =========================================================================
  // STATE
  // =========================================================================
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  // =========================================================================
  // AUTH KONTROLÜ - Zaten giriş yapmışsa dashboard'a yönlendir
  // =========================================================================
  useEffect(() => {
    if (!authLoading && isAuthenticated) {
      navigate("/store/dashboard");
    }
  }, [authLoading, isAuthenticated, navigate]);

  // =========================================================================
  // FORM SUBMIT
  // =========================================================================
  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      const result = await login(email, password);

      if (result.success) {
        navigate("/store/dashboard");
      } else {
        setError(
          result.error || "Giriş başarısız. Lütfen bilgilerinizi kontrol edin.",
        );
      }
    } catch (err) {
      console.error("[StoreLogin] Giriş hatası:", err);
      setError("Bir hata oluştu. Lütfen tekrar deneyin.");
    } finally {
      setLoading(false);
    }
  };

  // =========================================================================
  // LOADING STATE
  // =========================================================================
  if (authLoading) {
    return (
      <div
        className="min-vh-100 d-flex align-items-center justify-content-center"
        style={{
          background: "linear-gradient(135deg, #2E7D32 0%, #66BB6A 100%)",
        }}
      >
        <div className="text-center text-white">
          <div
            className="spinner-border mb-3"
            style={{ width: "3rem", height: "3rem" }}
          ></div>
          <p>Yükleniyor...</p>
        </div>
      </div>
    );
  }

  // =========================================================================
  // RENDER
  // =========================================================================
  return (
    <div
      className="min-vh-100 d-flex align-items-center justify-content-center p-3"
      style={{
        background: "linear-gradient(135deg, #2E7D32 0%, #66BB6A 100%)",
      }}
    >
      <div
        className="card shadow-lg border-0"
        style={{
          maxWidth: "420px",
          width: "100%",
          borderRadius: "20px",
          overflow: "hidden",
        }}
      >
        {/* Üst Header */}
        <div
          className="text-center py-4 text-white"
          style={{
            background: "linear-gradient(135deg, #1B5E20 0%, #2E7D32 100%)",
          }}
        >
          <div
            className="mx-auto mb-3 d-flex align-items-center justify-content-center"
            style={{
              width: "70px",
              height: "70px",
              borderRadius: "20px",
              background: "rgba(255,255,255,0.2)",
              backdropFilter: "blur(10px)",
            }}
          >
            <i className="fas fa-store" style={{ fontSize: "2rem" }}></i>
          </div>
          <h4 className="fw-bold mb-1">Market Görevlisi</h4>
          <p className="mb-0 opacity-75" style={{ fontSize: "0.9rem" }}>
            Sipariş hazırlama paneli
          </p>
        </div>

        {/* Form Alanı */}
        <div className="card-body p-4">
          {/* Hata Mesajı */}
          {error && (
            <div
              className="alert alert-danger d-flex align-items-center py-2 mb-3"
              style={{ fontSize: "0.85rem", borderRadius: "10px" }}
            >
              <i className="fas fa-exclamation-circle me-2"></i>
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit}>
            {/* Email */}
            <div className="mb-3">
              <label className="form-label fw-semibold text-muted small mb-1">
                <i className="fas fa-envelope me-1"></i>
                E-posta
              </label>
              <input
                type="email"
                className="form-control form-control-lg"
                placeholder="ornek@email.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                disabled={loading}
                style={{
                  borderRadius: "12px",
                  fontSize: "1rem",
                  padding: "12px 16px",
                  border: "2px solid #e0e0e0",
                  transition: "all 0.2s",
                }}
              />
            </div>

            {/* Şifre */}
            <div className="mb-4">
              <label className="form-label fw-semibold text-muted small mb-1">
                <i className="fas fa-lock me-1"></i>
                Şifre
              </label>
              <div className="position-relative">
                <input
                  type={showPassword ? "text" : "password"}
                  className="form-control form-control-lg"
                  placeholder="••••••••"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  disabled={loading}
                  style={{
                    borderRadius: "12px",
                    fontSize: "1rem",
                    padding: "12px 50px 12px 16px",
                    border: "2px solid #e0e0e0",
                  }}
                />
                <button
                  type="button"
                  className="btn position-absolute top-50 end-0 translate-middle-y me-2 text-muted"
                  onClick={() => setShowPassword(!showPassword)}
                  style={{ background: "transparent", border: "none" }}
                >
                  <i
                    className={`fas fa-${showPassword ? "eye-slash" : "eye"}`}
                  ></i>
                </button>
              </div>
            </div>

            {/* Giriş Butonu */}
            <button
              type="submit"
              className="btn btn-lg w-100 text-white fw-semibold"
              disabled={loading}
              style={{
                background: "linear-gradient(135deg, #2E7D32 0%, #43A047 100%)",
                borderRadius: "12px",
                padding: "14px",
                border: "none",
                fontSize: "1rem",
                boxShadow: "0 4px 15px rgba(46, 125, 50, 0.3)",
              }}
            >
              {loading ? (
                <>
                  <span className="spinner-border spinner-border-sm me-2"></span>
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

          {/* Test Bilgileri - Geliştirme için */}
          <div className="mt-4 pt-3 border-top">
            <p className="text-center text-muted small mb-2">
              <i className="fas fa-info-circle me-1"></i>
              Test hesabı
            </p>
            <div
              className="bg-light rounded p-2 text-center"
              style={{ fontSize: "0.8rem", borderRadius: "10px" }}
            >
              <code className="text-dark">storeattendant@test.com</code>
              <br />
              <code className="text-dark">Test123!</code>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
