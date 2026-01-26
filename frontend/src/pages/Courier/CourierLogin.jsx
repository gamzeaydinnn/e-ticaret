// ==========================================================================
// CourierLogin.jsx - Kurye Giriş Sayfası
// ==========================================================================
// Telefon/e-posta + şifre ile giriş, "Beni hatırla" özelliği.
// CourierAuthContext ile entegre çalışır.
// ==========================================================================

import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useCourierAuth } from "../../contexts/CourierAuthContext";

export default function CourierLogin() {
  // Form state
  const [formData, setFormData] = useState({
    emailOrPhone: "",
    password: "",
    rememberMe: false,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  const navigate = useNavigate();
  const { login, isAuthenticated, loading: authLoading } = useCourierAuth();

  // Zaten giriş yapmışsa dashboard'a yönlendir
  useEffect(() => {
    if (!authLoading && isAuthenticated) {
      navigate("/courier/dashboard");
    }
  }, [isAuthenticated, authLoading, navigate]);

  // =========================================================================
  // FORM SUBMIT
  // =========================================================================
  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    const identifier = normalizeIdentifier(formData.emailOrPhone);

    // Validasyon
    if (!identifier) {
      setError("E-posta veya telefon numarası gerekli");
      setLoading(false);
      return;
    }
    if (!formData.password) {
      setError("Şifre gerekli");
      setLoading(false);
      return;
    }
    if (formData.password.length < 6) {
      setError("Şifre en az 6 karakter olmalı");
      setLoading(false);
      return;
    }

    try {
      // CourierAuthContext login fonksiyonunu kullan
      const result = await login(
        identifier,
        formData.password,
        formData.rememberMe,
      );

      if (result.success) {
        // Başarılı giriş - Dashboard'a yönlendir
        navigate("/courier/dashboard");
      } else {
        // Hata mesajını Türkçeleştir
        setError(translateError(result.error));
      }
    } catch (err) {
      console.error("Giriş hatası:", err);
      setError(translateError(err.message));
    } finally {
      setLoading(false);
    }
  };

  // =========================================================================
  // HATA MESAJLARINI TÜRKÇELEŞTİR
  // =========================================================================
  const translateError = (errorMsg) => {
    const errorMap = {
      "Invalid credentials": "Geçersiz e-posta/telefon veya şifre",
      "User not found": "Kullanıcı bulunamadı",
      "Account locked": "Hesabınız kilitlendi. Yönetici ile iletişime geçin",
      "Account disabled": "Hesabınız devre dışı bırakıldı",
      "Too many attempts": "Çok fazla deneme. Lütfen 5 dakika bekleyin",
      "Network error": "Bağlantı hatası. İnternet bağlantınızı kontrol edin",
      "Server error": "Sunucu hatası. Lütfen daha sonra tekrar deneyin",
    };

    // Tam eşleşme kontrolü
    if (errorMap[errorMsg]) {
      return errorMap[errorMsg];
    }

    // Kısmi eşleşme kontrolü
    for (const [key, value] of Object.entries(errorMap)) {
      if (errorMsg?.toLowerCase().includes(key.toLowerCase())) {
        return value;
      }
    }

    return (
      errorMsg || "Giriş yapılırken bir hata oluştu. Lütfen tekrar deneyin."
    );
  };

  // =========================================================================
  // E-POSTA/TELEFON NORMALİZE
  // =========================================================================
  const normalizeIdentifier = (value) => {
    const trimmed = value?.trim() || "";
    if (!trimmed) return "";

    // Telefon ise sadece rakamları gönder (başta + varsa koru)
    const isLikelyPhone = /^[\d\s()+-]+$/.test(trimmed);
    if (!isLikelyPhone) return trimmed;

    const hasPlus = trimmed.startsWith("+");
    const digits = trimmed.replace(/\D/g, "");
    return hasPlus ? `+${digits}` : digits;
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
      <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
      </div>
    );
  }

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div className="container-fluid px-3 px-sm-4">
        <div className="row justify-content-center">
          <div className="col-12 col-sm-10 col-md-7 col-lg-4 d-flex justify-content-center">
            <div
              className="card shadow-lg border-0 w-100"
              style={{ borderRadius: "16px", maxWidth: "420px" }}
            >
              <div className="card-body p-4 p-md-5">
                {/* Logo ve Başlık */}
                <div className="text-center mb-4">
                  <div
                    className="mx-auto mb-3 d-flex align-items-center justify-content-center"
                    style={{
                      width: "70px",
                      height: "70px",
                      background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                      borderRadius: "18px",
                      boxShadow: "0 8px 25px rgba(255, 107, 53, 0.3)",
                    }}
                  >
                    <i className="fas fa-motorcycle text-white fs-3"></i>
                  </div>
                  <h3 className="fw-bold text-dark mb-1">Kurye Paneli</h3>
                  <p className="text-muted mb-0">Hesabınızla giriş yapın</p>
                </div>

                {/* Hata Mesajı */}
                {error && (
                  <div
                    className="alert alert-danger py-2 d-flex align-items-center"
                    role="alert"
                    style={{ borderRadius: "10px", fontSize: "0.9rem" }}
                  >
                    <i className="fas fa-exclamation-circle me-2"></i>
                    <span>{error}</span>
                  </div>
                )}

                {/* Giriş Formu */}
                <form onSubmit={handleSubmit}>
                  {/* E-posta veya Telefon */}
                  <div className="mb-3">
                    <label
                      className="form-label fw-semibold"
                      style={{ fontSize: "0.9rem" }}
                    >
                      <i className="fas fa-user me-2 text-muted"></i>
                      E-posta veya Telefon
                    </label>
                    <input
                      type="text"
                      name="emailOrPhone"
                      className="form-control form-control-lg"
                      value={formData.emailOrPhone}
                      onChange={handleChange}
                      placeholder="ornek@email.com veya 05xx xxx xxxx"
                      required
                      autoComplete="username"
                      style={{ borderRadius: "10px", fontSize: "1rem" }}
                    />
                  </div>

                  {/* Şifre */}
                  <div className="mb-3">
                    <label
                      className="form-label fw-semibold"
                      style={{ fontSize: "0.9rem" }}
                    >
                      <i className="fas fa-lock me-2 text-muted"></i>
                      Şifre
                    </label>
                    <div className="input-group">
                      <input
                        type={showPassword ? "text" : "password"}
                        name="password"
                        className="form-control form-control-lg"
                        value={formData.password}
                        onChange={handleChange}
                        placeholder="••••••••"
                        required
                        autoComplete="current-password"
                        style={{
                          borderRadius: "10px 0 0 10px",
                          fontSize: "1rem",
                          borderRight: "none",
                        }}
                      />
                      <button
                        type="button"
                        className="btn btn-outline-secondary"
                        onClick={() => setShowPassword(!showPassword)}
                        style={{
                          borderRadius: "0 10px 10px 0",
                          borderLeft: "none",
                          borderColor: "#dee2e6",
                        }}
                        tabIndex={-1}
                      >
                        <i
                          className={`fas fa-eye${showPassword ? "-slash" : ""}`}
                        ></i>
                      </button>
                    </div>
                  </div>

                  {/* Beni Hatırla */}
                  <div className="mb-4 d-flex justify-content-between align-items-center">
                    <div className="form-check">
                      <input
                        type="checkbox"
                        name="rememberMe"
                        id="rememberMe"
                        className="form-check-input"
                        checked={formData.rememberMe}
                        onChange={handleChange}
                        style={{ cursor: "pointer" }}
                      />
                      <label
                        className="form-check-label text-muted"
                        htmlFor="rememberMe"
                        style={{ cursor: "pointer", fontSize: "0.9rem" }}
                      >
                        Beni hatırla
                      </label>
                    </div>
                    <a
                      href="#forgot"
                      className="text-primary text-decoration-none"
                      style={{ fontSize: "0.9rem" }}
                      onClick={(e) => {
                        e.preventDefault();
                        alert(
                          "Şifre sıfırlama için yöneticinizle iletişime geçin.",
                        );
                      }}
                    >
                      Şifremi unuttum
                    </a>
                  </div>

                  {/* Giriş Butonu */}
                  <button
                    type="submit"
                    disabled={loading}
                    className="btn btn-lg w-100 text-white fw-bold"
                    style={{
                      background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                      border: "none",
                      borderRadius: "10px",
                      boxShadow: "0 4px 15px rgba(255, 107, 53, 0.3)",
                      padding: "12px",
                      fontSize: "1rem",
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

                {/* Demo Bilgisi */}
                <div className="text-center mt-4 pt-3 border-top">
                  <small className="text-muted d-block mb-2">
                    <i className="fas fa-info-circle me-1"></i>
                    Demo hesap bilgileri:
                  </small>
                  <div className="d-flex justify-content-center gap-2 flex-wrap">
                    <span
                      className="badge bg-light text-dark"
                      style={{ cursor: "pointer", fontSize: "0.75rem" }}
                      onClick={() =>
                        setFormData((prev) => ({
                          ...prev,
                          emailOrPhone: "ahmett@courier.com",
                          password: "Ahmet.123",
                        }))
                      }
                    >
                      <i className="fas fa-envelope me-1"></i>
                      ahmett@courier.com
                    </span>
                    <span
                      className="badge bg-light text-dark"
                      style={{ cursor: "pointer", fontSize: "0.75rem" }}
                      onClick={() =>
                        setFormData((prev) => ({
                          ...prev,
                          emailOrPhone: "0532 123 4567",
                          password: "Ahmet.123",
                        }))
                      }
                    >
                      <i className="fas fa-phone me-1"></i>
                      0532 123 4567
                    </span>
                  </div>
                  <small
                    className="text-muted d-block mt-1"
                    style={{ fontSize: "0.7rem" }}
                  >
                    Şifre: Ahmet.123
                  </small>
                </div>
              </div>
            </div>

            {/* Alt Bilgi */}
            <div className="text-center mt-3">
              <small className="text-muted">
                © 2026 E-Ticaret Kurye Sistemi
              </small>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
