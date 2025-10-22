// src/components/LoginModal.js
import React, { useState } from "react";
import { useAuth } from "../contexts/AuthContext";

const LoginModal = ({ show, onHide, onLoginSuccess }) => {
  const [isLogin, setIsLogin] = useState(true);
  const [isForgotPassword, setIsForgotPassword] = useState(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [loading, setLoading] = useState(false);

  const { login, register, loginWithSocial } = useAuth();

  const handleForgotPassword = async (e) => {
    e.preventDefault();
    setError("");
    setSuccess("");
    setLoading(true);

    try {
      const response = await fetch(
        `${
          process.env.REACT_APP_API_URL || "http://localhost:5153"
        }/api/auth/forgot-password`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email }),
        }
      );

      if (response.ok) {
        setSuccess("Şifre sıfırlama bağlantısı e-posta adresinize gönderildi.");
        setTimeout(() => {
          setIsForgotPassword(false);
          setSuccess("");
        }, 3000);
      } else {
        setError("E-posta adresi bulunamadı.");
      }
    } catch (error) {
      setError("Bir hata oluştu. Lütfen tekrar deneyin.");
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      let result;
      if (isLogin) {
        result = await login(email, password);
      } else {
        result = await register(email, password, firstName, lastName);
      }

      if (result.success) {
        onLoginSuccess && onLoginSuccess();
        onHide();
        // Formu temizle
        setEmail("");
        setPassword("");
        setFirstName("");
        setLastName("");
      } else {
        setError(result.error);
      }
    } catch (error) {
      setError("Bir hata oluştu. Lütfen tekrar deneyin.");
    } finally {
      setLoading(false);
    }
  };

  const handleSocialLogin = async (provider) => {
    setError("");
    setLoading(true);
    try {
      // Dev basit senaryo: email/name bilgisi olmadan backend dev fallback'i kullan
      const result = await loginWithSocial(provider, { email });
      if (result.success) {
        onLoginSuccess && onLoginSuccess();
        onHide();
      } else {
        setError(result.error || "Sosyal giriş başarısız");
      }
    } catch (e) {
      setError("Sosyal giriş başarısız");
    } finally {
      setLoading(false);
    }
  };

  const handleModalClick = (e) => {
    e.stopPropagation();
  };

  const fillDemoCredentials = () => {
    setEmail("demo@example.com");
    setPassword("123456");
  };

  if (!show) return null;

  return (
    <div
      className="modal fade show d-block"
      style={{
        backgroundColor: "rgba(0,0,0,0.6)",
        backdropFilter: "blur(4px)",
      }}
      onClick={onHide}
    >
      <div
        className="modal-dialog modal-dialog-centered"
        onClick={handleModalClick}
        style={{ maxWidth: "440px" }}
      >
        <div
          className="modal-content border-0"
          style={{
            borderRadius: "24px",
            overflow: "hidden",
            boxShadow: "0 20px 60px rgba(255, 107, 53, 0.25)",
          }}
        >
          <div
            className="modal-header border-0"
            style={{
              background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
              padding: "24px 32px",
            }}
          >
            <h5 className="modal-title text-white fw-bold">
              {isForgotPassword
                ? "Şifremi Unuttum"
                : isLogin
                ? "Giriş Yap"
                : "Hesap Oluştur"}
            </h5>
            <button
              type="button"
              className="btn-close btn-close-white"
              onClick={onHide}
            ></button>
          </div>

          <div
            className="modal-body p-4"
            style={{ padding: "32px !important" }}
          >
            {/* Şifre Sıfırlama Formu */}
            {isForgotPassword ? (
              <>
                {success && (
                  <div className="alert alert-success">
                    <i className="fas fa-check-circle me-2"></i>
                    {success}
                  </div>
                )}
                {error && (
                  <div className="alert alert-danger">
                    <i className="fas fa-exclamation-triangle me-2"></i>
                    {error}
                  </div>
                )}
                <form onSubmit={handleForgotPassword}>
                  <div className="mb-3">
                    <label htmlFor="email" className="form-label">
                      <i className="fas fa-envelope me-2"></i>E-posta
                    </label>
                    <input
                      type="email"
                      className="form-control form-control-lg"
                      id="email"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      required
                      placeholder="E-posta adresinizi girin"
                      style={{
                        borderRadius: "12px",
                        border: "2px solid #e0e0e0",
                        padding: "12px 16px",
                      }}
                      onFocus={(e) => (e.target.style.borderColor = "#ff6b35")}
                      onBlur={(e) => (e.target.style.borderColor = "#e0e0e0")}
                    />
                  </div>
                  <button
                    type="submit"
                    className="btn btn-lg w-100 text-white fw-bold mb-3"
                    style={{
                      background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                      border: "none",
                      borderRadius: "12px",
                      padding: "14px",
                    }}
                    disabled={loading}
                  >
                    {loading
                      ? "Gönderiliyor..."
                      : "Şifre Sıfırlama Bağlantısı Gönder"}
                  </button>
                </form>
                <div className="text-center">
                  <button
                    className="btn btn-link p-0 text-decoration-none"
                    onClick={() => {
                      setIsForgotPassword(false);
                      setError("");
                      setSuccess("");
                    }}
                    style={{ color: "#ff6b35" }}
                  >
                    ← Giriş Sayfasına Dön
                  </button>
                </div>
              </>
            ) : (
              <>
                {/* Demo Bilgilendirme */}
                <div
                  className="alert d-flex align-items-center mb-3"
                  style={{
                    background:
                      "linear-gradient(135deg, #e3f2fd 0%, #bbdefb 100%)",
                    border: "none",
                    borderRadius: "16px",
                    padding: "16px",
                  }}
                >
                  <i className="fas fa-info-circle me-2"></i>
                  <div>
                    <strong>Demo Hesap:</strong> demo@example.com / 123456
                    <button
                      className="btn btn-sm btn-outline-primary ms-2"
                      type="button"
                      onClick={fillDemoCredentials}
                    >
                      Otomatik Doldur
                    </button>
                  </div>
                </div>

                {error && (
                  <div className="alert alert-danger">
                    <i className="fas fa-exclamation-triangle me-2"></i>
                    {error}
                  </div>
                )}

                <form onSubmit={handleSubmit}>
                  {/* Sosyal Giriş */}
                  <div className="d-grid gap-2 mb-3">
                    <button
                      type="button"
                      className="btn btn-light border d-flex align-items-center justify-content-center"
                      onClick={() => handleSocialLogin("google")}
                      disabled={loading}
                    >
                      <i className="fab fa-google me-2" style={{ color: "#DB4437" }}></i>
                      Google ile devam et
                    </button>
                    <button
                      type="button"
                      className="btn btn-light border d-flex align-items-center justify-content-center"
                      onClick={() => handleSocialLogin("facebook")}
                      disabled={loading}
                    >
                      <i className="fab fa-facebook me-2" style={{ color: "#4267B2" }}></i>
                      Facebook ile devam et
                    </button>
                  </div>
                  {!isLogin && (
                    <>
                      <div className="mb-3">
                        <label htmlFor="firstName" className="form-label">
                          <i className="fas fa-user me-2"></i>Ad
                        </label>
                        <input
                          type="text"
                          className="form-control form-control-lg"
                          id="firstName"
                          value={firstName}
                          onChange={(e) => setFirstName(e.target.value)}
                          required={!isLogin}
                          placeholder="Adınızı girin"
                          style={{
                            borderRadius: "12px",
                            border: "2px solid #e0e0e0",
                            padding: "12px 16px",
                            transition: "all 0.3s ease",
                          }}
                          onFocus={(e) =>
                            (e.target.style.borderColor = "#ff6b35")
                          }
                          onBlur={(e) =>
                            (e.target.style.borderColor = "#e0e0e0")
                          }
                        />
                      </div>
                      <div className="mb-3">
                        <label htmlFor="lastName" className="form-label">
                          <i className="fas fa-user me-2"></i>Soyad
                        </label>
                        <input
                          type="text"
                          className="form-control form-control-lg"
                          id="lastName"
                          value={lastName}
                          onChange={(e) => setLastName(e.target.value)}
                          required={!isLogin}
                          placeholder="Soyadınızı girin"
                          style={{
                            borderRadius: "12px",
                            border: "2px solid #e0e0e0",
                            padding: "12px 16px",
                          }}
                          onFocus={(e) =>
                            (e.target.style.borderColor = "#ff6b35")
                          }
                          onBlur={(e) =>
                            (e.target.style.borderColor = "#e0e0e0")
                          }
                        />
                      </div>
                    </>
                  )}

                  <div className="mb-3">
                    <label htmlFor="email" className="form-label">
                      <i className="fas fa-envelope me-2"></i>E-posta
                    </label>
                    <input
                      type="email"
                      className="form-control form-control-lg"
                      id="email"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      required
                      placeholder="E-posta adresinizi girin"
                      style={{
                        borderRadius: "12px",
                        border: "2px solid #e0e0e0",
                        padding: "12px 16px",
                      }}
                      onFocus={(e) => (e.target.style.borderColor = "#ff6b35")}
                      onBlur={(e) => (e.target.style.borderColor = "#e0e0e0")}
                    />
                  </div>

                  <div className="mb-4">
                    <label htmlFor="password" className="form-label">
                      <i className="fas fa-lock me-2"></i>Şifre
                    </label>
                    <input
                      type="password"
                      className="form-control form-control-lg"
                      id="password"
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      required
                      placeholder="Şifrenizi girin"
                      style={{
                        borderRadius: "12px",
                        border: "2px solid #e0e0e0",
                        padding: "12px 16px",
                      }}
                      onFocus={(e) => (e.target.style.borderColor = "#ff6b35")}
                      onBlur={(e) => (e.target.style.borderColor = "#e0e0e0")}
                    />
                  </div>

                  <button
                    type="submit"
                    className="btn btn-lg w-100 text-white fw-bold mb-3"
                    style={{
                      background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                      border: "none",
                      borderRadius: "12px",
                      padding: "14px",
                      transition: "all 0.3s ease",
                      boxShadow: "0 4px 15px rgba(255, 107, 53, 0.3)",
                    }}
                    onMouseEnter={(e) => {
                      e.target.style.transform = "translateY(-2px)";
                      e.target.style.boxShadow =
                        "0 6px 20px rgba(255, 107, 53, 0.4)";
                    }}
                    onMouseLeave={(e) => {
                      e.target.style.transform = "translateY(0)";
                      e.target.style.boxShadow =
                        "0 4px 15px rgba(255, 107, 53, 0.3)";
                    }}
                    disabled={loading}
                  >
                    {loading ? (
                      <>
                        <i className="fas fa-spinner fa-spin me-2"></i>Lütfen
                        bekleyin...
                      </>
                    ) : (
                      <>{isLogin ? "Giriş Yap" : "Hesap Oluştur"}</>
                    )}
                  </button>
                </form>

                {isLogin && (
                  <div className="text-center mb-3">
                    <button
                      className="btn btn-link p-0 text-decoration-none"
                      onClick={() => {
                        setIsForgotPassword(true);
                        setError("");
                      }}
                      style={{ color: "#ff6b35", fontSize: "14px" }}
                    >
                      Şifremi Unuttum
                    </button>
                  </div>
                )}

                <div className="text-center">
                  <p className="text-muted mb-0">
                    {isLogin ? "Hesabınız yok mu?" : "Zaten hesabınız var mı?"}{" "}
                    <button
                      className="btn btn-link p-0 text-decoration-none"
                      onClick={() => {
                        setIsLogin(!isLogin);
                        setError("");
                      }}
                      style={{ color: "#ff6b35" }}
                    >
                      {isLogin ? "Hesap Oluştur" : "Giriş Yap"}
                    </button>
                  </p>
                </div>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginModal;
