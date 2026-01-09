import React, { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";

export default function ResetPassword() {
  const location = useLocation();
  const navigate = useNavigate();
  const params = new URLSearchParams(location.search);

  const initialEmail = params.get("email") || "";
  const initialToken = params.get("token") || "";

  const [email] = useState(initialEmail);
  const [token] = useState(initialToken);
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [loading, setLoading] = useState(false);

  const hasValidParams = Boolean(email && token);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setSuccess("");

    if (!hasValidParams) {
      setError("Geçersiz veya eksik şifre sıfırlama bağlantısı.");
      return;
    }

    if (!password || password.length < 6) {
      setError("Şifre en az 6 karakter olmalıdır.");
      return;
    }

    if (password !== confirmPassword) {
      setError("Şifreler eşleşmiyor.");
      return;
    }

    setLoading(true);
    try {
      const response = await fetch(
        `${
          process.env.REACT_APP_API_URL || "http://localhost:5153"
        }/auth/reset-password`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            email,
            token,
            newPassword: password,
            confirmPassword,
          }),
        }
      );

      if (response.ok) {
        setSuccess(
          "Şifreniz başarıyla güncellendi. Artık giriş yapabilirsiniz."
        );
        setPassword("");
        setConfirmPassword("");
      } else {
        let message =
          "Şifre sıfırlama işlemi başarısız oldu. Lütfen bağlantınızı kontrol edin.";
        try {
          const data = await response.json();
          if (data?.Message || data?.message) {
            message = data.Message || data.message;
          }
        } catch {
          // ignore parse errors
        }
        setError(message);
      }
    } catch (err) {
      setError("Bir hata oluştu. Lütfen tekrar deneyin.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      style={{
        minHeight: "100vh",
        background:
          "linear-gradient(135deg, #fff3e0 0%, #ffe0b2 50%, #ffcc80 100%)",
        paddingTop: "2rem",
        paddingBottom: "2rem",
      }}
    >
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-md-6 col-lg-5">
            <div
              className="card shadow-lg border-0"
              style={{ borderRadius: "20px" }}
            >
              <div
                className="card-header text-white text-center border-0"
                style={{
                  background:
                    "linear-gradient(45deg, #ff6f00, #ff8f00, #ffa000)",
                  borderTopLeftRadius: "20px",
                  borderTopRightRadius: "20px",
                  padding: "1.5rem",
                }}
              >
                <h4 className="mb-0 fw-bold">Şifre Sıfırla</h4>
              </div>
              <div className="card-body p-4">
                {!hasValidParams && (
                  <div className="alert alert-danger">
                    Geçersiz veya eksik şifre sıfırlama bağlantısı.
                  </div>
                )}

                {error && (
                  <div className="alert alert-danger">
                    <i className="fas fa-exclamation-triangle me-2"></i>
                    {error}
                  </div>
                )}

                {success && (
                  <div className="alert alert-success">
                    <i className="fas fa-check-circle me-2"></i>
                    {success}
                  </div>
                )}

                <form onSubmit={handleSubmit}>
                  <div className="mb-3">
                    <label className="form-label">E-posta</label>
                    <input
                      type="email"
                      className="form-control"
                      value={email}
                      disabled
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label">Yeni Şifre</label>
                    <input
                      type="password"
                      className="form-control"
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      minLength={6}
                      required
                      disabled={!hasValidParams || loading}
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label">Yeni Şifre (Tekrar)</label>
                    <input
                      type="password"
                      className="form-control"
                      value={confirmPassword}
                      onChange={(e) => setConfirmPassword(e.target.value)}
                      minLength={6}
                      required
                      disabled={!hasValidParams || loading}
                    />
                  </div>

                  <button
                    type="submit"
                    className="btn btn-primary w-100"
                    style={{
                      background:
                        "linear-gradient(135deg, #ff6b35, #ff8c00)",
                      border: "none",
                    }}
                    disabled={!hasValidParams || loading}
                  >
                    {loading ? "Gönderiliyor..." : "Şifreyi Güncelle"}
                  </button>
                </form>

                <button
                  type="button"
                  className="btn btn-link mt-3 p-0"
                  onClick={() => navigate("/")}
                  style={{ color: "#ff6b35", textDecoration: "none" }}
                >
                  ← Ana sayfaya dön
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

