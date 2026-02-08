import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";

const initialLogin = { email: "", password: "" };
const initialRegister = {
  firstName: "",
  lastName: "",
  email: "",
  phoneNumber: "",
  password: "",
  confirmPassword: "",
};

const AccountPage = () => {
  const navigate = useNavigate();
  const { user, login, register, logout } = useAuth();

  const [mode, setMode] = useState("login");
  const [loginForm, setLoginForm] = useState(initialLogin);
  const [registerForm, setRegisterForm] = useState(initialRegister);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  useEffect(() => {
    if (user) {
      setMessage(`Hoş geldin ${user.firstName || user.name || ""}`);
    }
  }, [user]);

  const handleLoginSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setMessage("");
    setLoading(true);

    try {
      const result = await login(loginForm.email, loginForm.password);
      if (result?.success) {
        navigate("/profile");
      } else {
        setError(result?.error || "Giriş yapılamadı");
      }
    } catch (err) {
      setError(err.message || "Giriş sırasında hata oluştu");
    } finally {
      setLoading(false);
    }
  };

  const handleRegisterSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setMessage("");
    setLoading(true);

    try {
      const result = await register(
        registerForm.email,
        registerForm.password,
        registerForm.firstName,
        registerForm.lastName,
        registerForm.phoneNumber,
        registerForm.confirmPassword,
      );

      if (result?.success) {
        setMessage("Kayıt başarılı. Profil sayfanıza yönlendiriliyorsunuz.");
        navigate("/profile");
      } else {
        setError(result?.error || "Kayıt yapılamadı");
      }
    } catch (err) {
      setError(err.message || "Kayıt sırasında hata oluştu");
    } finally {
      setLoading(false);
    }
  };

  if (user) {
    return (
      <div className="container py-4 py-md-5" style={{ maxWidth: "640px" }}>
        <div className="card shadow-sm border-0" style={{ borderRadius: "16px" }}>
          <div className="card-body p-4">
            <h3 className="fw-bold mb-2">Hesabım</h3>
            <p className="text-muted mb-4">
              Giriş yaptınız. Profil bilgilerinizi güncellemek veya şifrenizi değiştirmek için profil sayfasına geçin.
            </p>

            <div className="d-flex flex-wrap gap-2">
              <button className="btn btn-warning" onClick={() => navigate("/profile")}>
                Profile Git
              </button>
              <button
                className="btn btn-outline-danger"
                onClick={async () => {
                  await logout();
                  setMessage("Çıkış yapıldı");
                }}
              >
                Çıkış Yap
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="container py-4 py-md-5" style={{ maxWidth: "640px" }}>
      <div className="card shadow-sm border-0" style={{ borderRadius: "16px" }}>
        <div className="card-body p-4">
          <div className="d-flex gap-2 mb-4">
            <button
              type="button"
              className={`btn ${mode === "login" ? "btn-warning" : "btn-outline-warning"}`}
              onClick={() => setMode("login")}
            >
              Giriş Yap
            </button>
            <button
              type="button"
              className={`btn ${mode === "register" ? "btn-warning" : "btn-outline-warning"}`}
              onClick={() => setMode("register")}
            >
              Kayıt Ol
            </button>
          </div>

          {error && <div className="alert alert-danger py-2">{error}</div>}
          {message && <div className="alert alert-success py-2">{message}</div>}

          {mode === "login" ? (
            <form onSubmit={handleLoginSubmit} className="d-grid gap-3">
              <div>
                <label className="form-label fw-semibold">E-posta</label>
                <input
                  type="email"
                  className="form-control"
                  value={loginForm.email}
                  onChange={(e) =>
                    setLoginForm((prev) => ({ ...prev, email: e.target.value }))
                  }
                  required
                />
              </div>
              <div>
                <label className="form-label fw-semibold">Şifre</label>
                <input
                  type="password"
                  className="form-control"
                  value={loginForm.password}
                  onChange={(e) =>
                    setLoginForm((prev) => ({ ...prev, password: e.target.value }))
                  }
                  required
                />
              </div>
              <button type="submit" className="btn btn-warning" disabled={loading}>
                {loading ? "Giriş yapılıyor..." : "Giriş Yap"}
              </button>
            </form>
          ) : (
            <form onSubmit={handleRegisterSubmit} className="d-grid gap-3">
              <div className="row g-3">
                <div className="col-12 col-md-6">
                  <label className="form-label fw-semibold">Ad</label>
                  <input
                    type="text"
                    className="form-control"
                    value={registerForm.firstName}
                    onChange={(e) =>
                      setRegisterForm((prev) => ({ ...prev, firstName: e.target.value }))
                    }
                    required
                  />
                </div>
                <div className="col-12 col-md-6">
                  <label className="form-label fw-semibold">Soyad</label>
                  <input
                    type="text"
                    className="form-control"
                    value={registerForm.lastName}
                    onChange={(e) =>
                      setRegisterForm((prev) => ({ ...prev, lastName: e.target.value }))
                    }
                    required
                  />
                </div>
              </div>

              <div>
                <label className="form-label fw-semibold">E-posta</label>
                <input
                  type="email"
                  className="form-control"
                  value={registerForm.email}
                  onChange={(e) =>
                    setRegisterForm((prev) => ({ ...prev, email: e.target.value }))
                  }
                  required
                />
              </div>

              <div>
                <label className="form-label fw-semibold">Telefon</label>
                <input
                  type="tel"
                  className="form-control"
                  value={registerForm.phoneNumber}
                  onChange={(e) =>
                    setRegisterForm((prev) => ({ ...prev, phoneNumber: e.target.value }))
                  }
                  placeholder="05xx xxx xx xx"
                />
              </div>

              <div className="row g-3">
                <div className="col-12 col-md-6">
                  <label className="form-label fw-semibold">Şifre</label>
                  <input
                    type="password"
                    className="form-control"
                    value={registerForm.password}
                    onChange={(e) =>
                      setRegisterForm((prev) => ({ ...prev, password: e.target.value }))
                    }
                    required
                  />
                </div>
                <div className="col-12 col-md-6">
                  <label className="form-label fw-semibold">Şifre Tekrar</label>
                  <input
                    type="password"
                    className="form-control"
                    value={registerForm.confirmPassword}
                    onChange={(e) =>
                      setRegisterForm((prev) => ({ ...prev, confirmPassword: e.target.value }))
                    }
                    required
                  />
                </div>
              </div>

              <button type="submit" className="btn btn-warning" disabled={loading}>
                {loading ? "Kayıt oluşturuluyor..." : "Kayıt Ol"}
              </button>
            </form>
          )}
        </div>
      </div>
    </div>
  );
};

export default AccountPage;
