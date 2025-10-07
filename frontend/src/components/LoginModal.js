// src/components/LoginModal.js
import React, { useState } from "react";
import { useAuth } from "../contexts/AuthContext";

const LoginModal = ({ show, onHide, onLoginSuccess }) => {
  const [isLogin, setIsLogin] = useState(true);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [name, setName] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const { login, register } = useAuth();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      let result;
      if (isLogin) {
        result = await login(email, password);
      } else {
        result = await register(email, password, name);
      }

      if (result.success) {
        onLoginSuccess && onLoginSuccess();
        onHide();
        // Formu temizle
        setEmail("");
        setPassword("");
        setName("");
      } else {
        setError(result.error);
      }
    } catch (error) {
      setError("Bir hata oluştu. Lütfen tekrar deneyin.");
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
      style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
      onClick={onHide}
    >
      <div
        className="modal-dialog modal-dialog-centered"
        onClick={handleModalClick}
      >
        <div className="modal-content border-0 shadow-lg">
          <div
            className="modal-header bg-gradient border-0"
            style={{
              background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
            }}
          >
            <h5 className="modal-title text-white fw-bold">
              {isLogin ? "Giriş Yap" : "Hesap Oluştur"}
            </h5>
            <button
              type="button"
              className="btn-close btn-close-white"
              onClick={onHide}
            ></button>
          </div>

          <div className="modal-body p-4">
            {/* Demo Bilgilendirme */}
            <div className="alert alert-info d-flex align-items-center mb-3">
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
              {!isLogin && (
                <div className="mb-3">
                  <label htmlFor="name" className="form-label">
                    <i className="fas fa-user me-2"></i>Ad Soyad
                  </label>
                  <input
                    type="text"
                    className="form-control form-control-lg"
                    id="name"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    required={!isLogin}
                    placeholder="Adınızı ve soyadınızı girin"
                  />
                </div>
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
                />
              </div>

              <button
                type="submit"
                className="btn btn-lg w-100 text-white fw-bold mb-3"
                style={{
                  background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                  border: "none",
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
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginModal;
